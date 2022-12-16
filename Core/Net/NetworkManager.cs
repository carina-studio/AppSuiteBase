using System.IO;
using CarinaStudio.Collections;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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
    static readonly Regex IPv4Regex = new("(?<Address>\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})");
    static readonly string[] PingTargetAddresses = new []{
        "208.67.222.222", // OpenDNS
        "208.67.220.220", // OpenDNS
        "1.1.1.1", // Cloudflare
        "1.0.0.1", // Cloudflare
        "8.8.8.8", // Google DNS
        "8.8.4.4", // Google DNS
    };
    static readonly string[] PublicIPCheckingServers = new []{
        "https://ipv4.icanhazip.com/",
        "http://checkip.dyndns.org/",
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
        Task.Run(() =>
        {
            NetworkChange.NetworkAddressChanged += this.OnNetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += this.OnNetworkAvailabilityChanged;
        });

        // setup scheduled actions
        this.checkNetworkConnectionAction = new(this.CheckNetworkConnection);
        this.updateNetworkAddressesAction = new(this.UpdateNetworkAddresses);

        // check network state
        this.UpdateNetworkAddresses();

        // setup properties
        this.IPAddresses = ListExtensions.AsReadOnly(this.ipAddresses);
    }


    // Check whether network connection is available or not.
    async void CheckNetworkConnection()
    {
        // ping
        var isConnected = false;
        using var ping = new Ping();
        var pingData = Array.Empty<byte>();
        foreach (var server in PingTargetAddresses)
        {
            try
            {
                var success = await Task.Run(() =>
                    ping.Send(server, 5000, pingData)?.Status == IPStatus.Success);
                if (success)
                {
                    this.logger.LogTrace("Network connection checked by '{server}'", server);
                    isConnected = true;
                    break;
                }
            }
            catch
            { }
            this.logger.LogWarning("Failed to ping '{server}'", server);
        }

        // get IP addresses
        var ipAddress = (IPAddress?)null;
        var publicIPAddress = (IPAddress?)null;
        if (isConnected)
        {
            ipAddress = await Task.Run(() =>
            {
                try
                {
                    using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.Connect(new IPAddress(new byte[]{ 8, 8, 8, 8}), 65530);
                    return (socket.LocalEndPoint as IPEndPoint)?.Address;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error occurred while getting active IPv4 address");
                    return null;
                }
            });
            if (ipAddress == null)
            {
                this.logger.LogError("Unable to get active IPv4 address");
                ipAddress = this.IPAddress;
            }
            using var httpClient = new HttpClient();
            foreach (var server in PublicIPCheckingServers)
            {
                try
                {
                    using var stream = await httpClient.GetStreamAsync(server);
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    var match = IPv4Regex.Match(await reader.ReadToEndAsync());
                    if (match.Success)
                    {
                        publicIPAddress = IPAddress.Parse(match.Groups["Address"].Value);
                        break;
                    }
                }
                catch
                { }
            }
            if (publicIPAddress == null)
            {
                this.logger.LogError("Unable to get active IPv4 address for connection to internet");
                publicIPAddress = this.PublicIPAddress;
            }
        }
        else
        {
            ipAddress = this.IPAddress;
            publicIPAddress = this.PublicIPAddress;
        }

        // update state
        if (ipAddress != this.IPAddress)
        {
            this.logger.LogTrace("IPv4 address: {ipAddress}", ipAddress);
            this.IPAddress = ipAddress;
            this.PropertyChanged?.Invoke(this, new(nameof(IPAddress)));
        }
        if (publicIPAddress != this.PublicIPAddress)
        {
            this.logger.LogTrace("Public IPv4 address: {publicIPAddress}", publicIPAddress);
            this.PublicIPAddress = publicIPAddress;
            this.PropertyChanged?.Invoke(this, new(nameof(PublicIPAddress)));
        }
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
    internal static Task InitializeAsync(IAppSuiteApplication app)
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
    /// Get active IPv4 address for outgoing connection.
    /// </summary>
    public IPAddress? IPAddress { get; private set; }


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
    /// Get name of primary network interface.
    /// </summary>
    public string? PrimaryInterfaceName { get; private set; }


    /// <summary>
    /// Get type of primary network interface.
    /// </summary>
    public NetworkInterfaceType PrimaryInterfaceType { get; private set; } = NetworkInterfaceType.Unknown;


    /// <summary>
    /// Get physical address of primary network interface on current device.
    /// </summary>
    public PhysicalAddress? PrimaryPhysicalAddress { get; private set; }


    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;


    /// <summary>
    /// Get active IPv4 address for outgoing connection to internet.
    /// </summary>
    public IPAddress? PublicIPAddress { get; private set; }


    // Update network addresses.
    async void UpdateNetworkAddresses()
    {
        try
        {
            // get addresses
            var addresses = new HashSet<IPAddress>();
            var primaryPhysicalAddress = (PhysicalAddress?)null;
            var primaryInterfaceName = default(string);
            var primaryInterfaceType = NetworkInterfaceType.Ethernet;
            var isWirelessInterfaceUp = false;
            var isNetworkAvailable = false;
            await Task.Run(() =>
            {
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
                        primaryInterfaceName = networkInterface.Name;
                        primaryInterfaceType = networkInterface.NetworkInterfaceType;
                        primaryPhysicalAddress = networkInterface.GetPhysicalAddress();
                    }
                    else
                    {
                        switch (networkInterface.NetworkInterfaceType)
                        {
                            case NetworkInterfaceType.Ethernet:
                                if (primaryInterfaceType == NetworkInterfaceType.Ethernet
                                    && string.CompareOrdinal(primaryInterfaceName, networkInterface.Name) < 0)
                                {
                                    continue;
                                }
                                break;
                            case NetworkInterfaceType.Wireless80211:
                                if (primaryInterfaceType == NetworkInterfaceType.Ethernet)
                                    continue;
                                if (primaryInterfaceType == NetworkInterfaceType.Wireless80211
                                    && string.CompareOrdinal(primaryInterfaceName, networkInterface.Name) < 0)
                                {
                                    continue;
                                }
                                break;
                            default:
                                continue;
                        }
                        primaryInterfaceName = networkInterface.Name;
                        primaryInterfaceType = networkInterface.NetworkInterfaceType;
                        primaryPhysicalAddress = networkInterface.GetPhysicalAddress();
                    }
                }
                isNetworkAvailable = NetworkInterface.GetIsNetworkAvailable();
            });

            // report primary interface
            if (this.PrimaryInterfaceName != primaryInterfaceName)
            {
                this.PrimaryInterfaceName = primaryInterfaceName;
                this.PropertyChanged?.Invoke(this, new(nameof(PrimaryInterfaceName)));
            }
            if (this.PrimaryInterfaceType != primaryInterfaceType)
            {
                this.PrimaryInterfaceType = primaryInterfaceType;
                this.PropertyChanged?.Invoke(this, new(nameof(PrimaryInterfaceType)));
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
            if (isNetworkAvailable)
                this.checkNetworkConnectionAction.Reschedule();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error occurred while updating IP addresses");
        }
    }
}