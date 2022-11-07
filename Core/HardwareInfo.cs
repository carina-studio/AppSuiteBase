using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Hardware information.
    /// </summary>
    public class HardwareInfo : INotifyPropertyChanged
    {
        // Fields.
        readonly IAppSuiteApplication app;
        readonly ScheduledAction checkGraphicsCardAction;
        readonly ManagementEventWatcher? graphicsCardWatcher;
        readonly ILogger logger;
        readonly SingleThreadSynchronizationContext hwCheckingSyncContext = new("Hardware info checker");


        // Constructor.
        internal HardwareInfo(IAppSuiteApplication app)
        {
            // setup fields
            this.app = app;
            this.checkGraphicsCardAction = new ScheduledAction(this.hwCheckingSyncContext, this.CheckGraphicsCard);
            this.logger = app.LoggerFactory.CreateLogger(nameof(HardwareInfo));

            // start checking graphics card
            this.CheckGraphicsCard();

            // get physical memory
            this.CheckPhysicalMemory();

            // start monitoring hardware change
            if (Platform.IsWindows)
            {
#pragma warning disable CA1416
                this.graphicsCardWatcher = new ManagementEventWatcher("SELECT * FROM Win32_VideoController");
                this.graphicsCardWatcher.EventArrived += (_, e) => this.checkGraphicsCardAction.Schedule();
#pragma warning restore CA1416
            }
        }


        // Check graphics card.
        void CheckGraphicsCard()
        {
            var hasDedicatedGraphicsCard = (bool?)null;
            if (Platform.IsWindows)
            {
#pragma warning disable CA1416
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                    foreach (var obj in searcher.Get())
                    {
                        obj["Description"]?.ToString()?.Let(it =>
                        {
                            it = it.ToLower();
                            if (it.StartsWith("nvidia") || it.Contains(" wddm "))
                            {
                                hasDedicatedGraphicsCard = true;
                            }
                        });
                        if (hasDedicatedGraphicsCard == true)
                            break;
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to check graphics card");
                }
#pragma warning restore CA1416
            }
            this.app.SynchronizationContext.Post(() =>
            {
                if (this.HasDedicatedGraphicsCard != hasDedicatedGraphicsCard)
                {
                    this.logger.LogTrace("Dedicated graphics card: {hasDedicatedGraphicsCard}", hasDedicatedGraphicsCard);
                    this.HasDedicatedGraphicsCard = hasDedicatedGraphicsCard;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasDedicatedGraphicsCard)));
                }
            });
        }


        // Check physical memory.
        void CheckPhysicalMemory()
        {
            var physicalMemorySize = (long?)null;
            if (Platform.IsWindows)
            {
#pragma warning disable CA1416
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
                    var size = 0L;
                    foreach (var obj in searcher.Get())
                    {
                        obj["Capacity"]?.ToString()?.Let(it =>
                        {
                            if (long.TryParse(it, out var partialSize) && partialSize > 0)
                                size += partialSize;
                        });
                    }
                    if (size > 0)
                        physicalMemorySize = size;
                    else
                        this.logger.LogWarning("Unable to get total physical memory on Windows");
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Unable to get total physical memory on Windows");
                }
#pragma warning restore CA1416
            }
            else if (Platform.IsLinux)
            {
                try
                {
                    using var reader = new StreamReader("/proc/meminfo", Encoding.UTF8);
                    var regex = new Regex("^[\\s]*MemTotal\\:[\\s]*(?<Size>[\\d]+)[\\s]*(?<Unit>[\\w]+)", RegexOptions.IgnoreCase);
                    var line = reader.ReadLine();
                    while (line != null)
                    {
                        var match = regex.Match(line);
                        if (match.Success && long.TryParse(match.Groups["Size"].Value, out var size))
                        {
                            physicalMemorySize = match.Groups["Unit"].Value.ToLower() switch
                            {
                                "kb" => size << 10,
                                _ => (long?)null,
                            };
                            break;
                        }
                    }
                    if (physicalMemorySize == null)
                        this.logger.LogWarning("Unable to get total physical memory on Linux");
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Unable to get total physical memory on Linux");
                }
            }
            else if (Platform.IsMacOS)
            {
                try
                {
                    using var process = Process.Start(new ProcessStartInfo()
                    {
                        Arguments = "hw.memsize",
                        CreateNoWindow = true,
                        FileName = "sysctl",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                    });
                    if (process != null)
                    {
                        using var reader = process.StandardOutput;
                        var regex = new Regex("^[\\s]*hw\\.memsize[\\s]*:[\\s]*(?<Size>[\\d]+)", RegexOptions.IgnoreCase);
                        var line = reader.ReadLine();
                        while (line != null)
                        {
                            var match = regex.Match(line);
                            if (match.Success && long.TryParse(match.Groups["Size"].Value, out var size))
                            {
                                physicalMemorySize = size;
                                break;
                            }
                        }
                        if (physicalMemorySize == null)
                            this.logger.LogWarning("Unable to get total physical memory on macOS");
                    }
                    else
                        this.logger.LogWarning("Unable to start process to get total physical memory on macOS");
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Unable to get total physical memory on macOS");
                }
            }
            if (this.TotalPhysicalMemory != physicalMemorySize)
            {
                this.TotalPhysicalMemory = physicalMemorySize;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPhysicalMemory)));
            }
        }


        /// <summary>
        /// Check whether at least one dedicated graphics has been attached to device or not.
        /// </summary>
        public bool? HasDedicatedGraphicsCard { get; private set; }


        /// <summary>
        /// Raised when property changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;


        /// <summary>
        /// Get size of total physical memory on device in bytes.
        /// </summary>
        public long? TotalPhysicalMemory{ get; private set; }
    }
}
