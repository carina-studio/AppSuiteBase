using CarinaStudio.Collections;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Net;

/// <summary>
/// Component to provide network related information.
/// </summary>
public class NetworkManager : BaseApplicationObject<IAppSuiteApplication>, INotifyPropertyChanged
{
    // Constants.
    const int NetworkAddressesUpdateDelay = 300;
    const int NetworkConnectionCheckingInterval = 10 * 60 * 1000;


    // Static fields.
    static NetworkManager? DefaultInstance;
    static readonly string[] PingTargetAddresses = new []{
        "208.67.222.222", // OpenDNS
        "208.67.220.220", // OpenDNS
        "1.1.1.1", // Cloudflare
        "1.0.0.1", // Cloudflare
        "8.8.8.8", // Google DNS
        "8.8.4.4", // Google DNS
    };


    // Fields.
    readonly ScheduledAction checkNetworkConnectionAction;
    bool isWirelessInterfaceUp;
    readonly ILogger logger;
    readonly ObservableList<IPAddress> ipAddresses = new();
    readonly ScheduledAction updateNetworkAddressesAction;


    // Constructor.
    NetworkManager(IAppSuiteApplication app) : base(app)
    { 
        // create logger
        this.logger = app.LoggerFactory.CreateLogger(nameof(NetworkManager));

        // monitor network change
        NetworkChange.NetworkAddressChanged += this.OnNetworkAddressChanged;
        NetworkChange.NetworkAvailabilityChanged += this.OnNetworkAvailabilityChanged;

        // setup scheduled actions
        this.checkNetworkConnectionAction = new(this.CheckNetworkConnection);
        this.updateNetworkAddressesAction = new(this.UpdateNetworkAddresses);

        // check network state
        try
        {
            this.UpdateNetworkAddresses();
            if (NetworkInterface.GetIsNetworkAvailable())
                this.checkNetworkConnectionAction.Execute();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error occurred while checking network state");
        }

        // setup properties
        this.IPAddresses = this.ipAddresses.AsReadOnly();
    }


    // Check whether network connection is available or not.
    async void CheckNetworkConnection()
    {
        // ping
        var isConnected = false;
        using var ping = new Ping();
        var pingData = new byte[4];
        foreach (var server in PingTargetAddresses)
        {
            try
            {
                await Task.Run(() =>
                {
                    if (ping.Send(server, 5000, pingData)?.Status != IPStatus.Success)
                        throw new Exception();
                });
                this.logger.LogTrace($"Network connection checked by '{server}'");
                isConnected = true;
                break;
            }
            catch
            {
                this.logger.LogWarning($"Failed to ping '{server}'");
            }
        }

        // update state
        if (this.IsNetworkConnected != isConnected)
        {
            if (!isConnected)
                this.logger.LogWarning("Network connection is down");
            this.IsNetworkConnected = isConnected;
            this.PropertyChanged?.Invoke(this, new(nameof(IsNetworkConnected)));
        }
        if (isConnected && this.isWirelessInterfaceUp)
        {
            if (!this.IsWirelessNetworkConnected)
            {
                this.IsWirelessNetworkConnected = true;
                this.PropertyChanged?.Invoke(this, new(nameof(IsWirelessNetworkConnected)));
            }
        }
        else if (this.IsWirelessNetworkConnected)
        {
            this.IsWirelessNetworkConnected = false;
            this.PropertyChanged?.Invoke(this, new(nameof(IsWirelessNetworkConnected)));
        }

        // schedule next checking
        this.checkNetworkConnectionAction.Schedule(NetworkConnectionCheckingInterval);
    }


    /// <summary>
    /// Get default instance.
    /// </summary>
    public static NetworkManager Default { get => DefaultInstance ?? throw new InvalidOperationException("Component is not initialized yet."); }


    /// <summary>
    /// Initialize asynchronously.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <returns>Task of initialization.</returns>
    public static Task InitializeAsync(IAppSuiteApplication app)
    {
        // check state
        app.VerifyAccess();
        if (DefaultInstance != null)
            throw new InvalidOperationException();
        
        // create instance
        DefaultInstance = new(app);
        return Task.CompletedTask;
    }


    /// <summary>
    /// Get all available IPv4 and IPv6 addresses of current device.
    /// </summary>
    public IList<IPAddress> IPAddresses { get; }


    /// <summary>
    /// Check whether network connection is available or not.
    /// </summary>
    public bool IsNetworkConnected { get; private set; }


    /// <summary>
    /// Check whether wireless network is connected and being used or not.
    /// </summary>
    public bool IsWirelessNetworkConnected { get; private set; }


    // Called when network address changed.
    void OnNetworkAddressChanged(object? sender, EventArgs e) =>
        this.updateNetworkAddressesAction.Schedule(NetworkAddressesUpdateDelay);


    // Called when network availability changed.
    void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        if (e.IsAvailable)
            this.checkNetworkConnectionAction.Schedule();
        else
            this.checkNetworkConnectionAction.Cancel();
    }


    /// <summary>
    /// Get primary physical address of network adapter on current device.
    /// </summary>
    public PhysicalAddress? PrimaryPhysicalAddress { get; private set; }


    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;


    // Update network addresses.
    void UpdateNetworkAddresses()
    {
        try
        {
            // get addresses
            var addresses = new HashSet<IPAddress>();
            var primaryPhysicalAddress = (PhysicalAddress?)null;
            var primaryInterfaceType = NetworkInterfaceType.Ethernet;
            var isWirelessInterfaceUp = false;
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                        isWirelessInterfaceUp = true;
                    foreach (var addressInfo in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        var address = addressInfo.Address;
                        if (address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            if (!address.Equals(IPAddress.Loopback) && !address.Equals(IPAddress.IPv6Loopback))
                                addresses.Add(address);
                        }
                    }
                }
                if (primaryPhysicalAddress == null)
                {
                    primaryInterfaceType = networkInterface.NetworkInterfaceType;
                    primaryPhysicalAddress = networkInterface.GetPhysicalAddress();
                }
                else
                {
                    switch (networkInterface.NetworkInterfaceType)
                    {
                        case NetworkInterfaceType.Ethernet:
                            if (primaryInterfaceType != NetworkInterfaceType.Ethernet)
                            {
                                primaryInterfaceType = NetworkInterfaceType.Ethernet;
                                primaryPhysicalAddress = networkInterface.GetPhysicalAddress();
                            }
                            break;
                        case NetworkInterfaceType.Wireless80211:
                            if (primaryInterfaceType != NetworkInterfaceType.Ethernet
                                && primaryInterfaceType != NetworkInterfaceType.Wireless80211)
                            {
                                primaryInterfaceType = NetworkInterfaceType.Wireless80211;
                                primaryPhysicalAddress = networkInterface.GetPhysicalAddress();
                            }
                            break;
                    }
                }
            }

            // report physical address
            if (this.PrimaryPhysicalAddress?.Equals(primaryPhysicalAddress) != true)
            {
                this.PrimaryPhysicalAddress = primaryPhysicalAddress;
                this.PropertyChanged?.Invoke(this, new(nameof(PrimaryPhysicalAddress)));
            }

            // report IP addresses
            this.ipAddresses.RemoveAll(it => !addresses.Contains(it));
            foreach (var address in addresses)
            {
                if (!this.ipAddresses.Contains(address))
                    this.ipAddresses.Add(address);
            }

            // check network connection
            this.isWirelessInterfaceUp = isWirelessInterfaceUp;
            if (NetworkInterface.GetIsNetworkAvailable())
                this.checkNetworkConnectionAction.Reschedule();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error occurred while updating IP addresses");
        }
    }
}