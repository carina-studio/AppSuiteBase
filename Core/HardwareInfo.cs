using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Hardware information.
/// </summary>
public class HardwareInfo : INotifyPropertyChanged
{
    // Native symbols.
    [DllImport("Kernel32")]
    static extern bool GetPhysicallyInstalledSystemMemory(out ulong TotalMemoryInKilobytes);
    
    
    // Static fields.
    static ILogger? StaticLogger;


    // Fields.
    readonly IAppSuiteApplication app;
    bool hasDedicatedGraphicsCard;
    Task<bool>? initCheckGraphicsCardTask;
    Task<long?>? initCheckPhysicalMemoryTask;
    long? totalPhysicalMemory;


    // Constructor.
    internal HardwareInfo(IAppSuiteApplication app)
    {
        // setup fields
        this.app = app;

        // start checking graphics card
        this.initCheckGraphicsCardTask = CheckGraphicsCardAsync();
        this.initCheckGraphicsCardTask.GetAwaiter().OnCompleted(() =>
        {
            this.hasDedicatedGraphicsCard = this.initCheckGraphicsCardTask.Result;
            this.initCheckGraphicsCardTask = null;
        });

        // start checking physical memory
        this.initCheckPhysicalMemoryTask = CheckPhysicalMemoryAsync();
        this.initCheckPhysicalMemoryTask.GetAwaiter().OnCompleted(() =>
        {
            this.totalPhysicalMemory = this.initCheckPhysicalMemoryTask.Result;
            this.initCheckPhysicalMemoryTask = null;
        });

        // start monitoring hardware change
        if (Platform.IsWindows)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
#pragma warning disable CA1416
                var graphicsCardWatcher = new ManagementEventWatcher("SELECT * FROM Win32_VideoController");
                graphicsCardWatcher.EventArrived += async (_, _) =>
                {
                    var hasGraphicsCard = await CheckGraphicsCardAsync();
                    this.app.SynchronizationContext.Post(() =>
                    {
                        if (this.hasDedicatedGraphicsCard != hasGraphicsCard)
                        {
                            this.Logger.LogTrace("Dedicated graphics card: {hasDedicatedGraphicsCard}", hasGraphicsCard);
                            this.hasDedicatedGraphicsCard = hasGraphicsCard;
                            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasDedicatedGraphicsCard)));
                        }
                    });
                };
#pragma warning restore CA1416
            }, null);
        }
    }
    
    
    // Check graphics card.
    static Task<bool> CheckGraphicsCardAsync()
    {
        if (Platform.IsNotWindows)
            return Task.FromResult(false);
        return Task.Run(() =>
        {
            try
            {
#pragma warning disable CA1416
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                foreach (var obj in searcher.Get())
                {
                    var hasDedicatedGraphicsCard = obj["Description"].ToString()?.Let(it =>
                    {
                        it = it.ToLower();
                        return it.StartsWith("nvidia") || it.Contains(" wddm ");
                    }) ?? false;
                    if (hasDedicatedGraphicsCard)
                        return true;
                }
#pragma warning restore CA1416
            }
            catch (Exception ex)
            {
                StaticLogger ??= AppSuiteApplication.CurrentOrNull?.LoggerFactory.CreateLogger(nameof(HardwareInfo));
                StaticLogger?.LogError(ex, "Failed to check graphics card");
            }
            return false;
        });
    }


    // Check physical memory.
    static Task<long?> CheckPhysicalMemoryAsync()
    {
        StaticLogger ??= AppSuiteApplication.CurrentOrNull?.LoggerFactory.CreateLogger(nameof(HardwareInfo));
        if (Platform.IsWindows)
        {
            try
            {
                if (GetPhysicallyInstalledSystemMemory(out var totalMemoryKB))
                    return Task.FromResult<long?>((long)totalMemoryKB << 10);
                StaticLogger?.LogError("Unable to get total physical memory on Windows");
            }
            catch (Exception ex)
            {
                StaticLogger?.LogError(ex, "Unable to get total physical memory on Windows");
            }
            return Task.FromResult<long?>(null);
        }
        if (Platform.IsLinux)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var reader = new StreamReader("/proc/meminfo", Encoding.UTF8);
                    var physicalMemorySize = default(long?);
                    var regex = new Regex("^[\\s]*MemTotal\\:[\\s]*(?<Size>[\\d]+)[\\s]*(?<Unit>[\\w]+)", RegexOptions.IgnoreCase);
                    var line = reader.ReadLine();
                    while (line is not null)
                    {
                        var match = regex.Match(line);
                        if (match.Success && long.TryParse(match.Groups["Size"].Value, out var size))
                        {
                            physicalMemorySize = match.Groups["Unit"].Value.ToLower() switch
                            {
                                "kb" => size << 10,
                                _ => null,
                            };
                            break;
                        }
                        line = reader.ReadLine();
                    }
                    if (!physicalMemorySize.HasValue)
                        StaticLogger?.LogWarning("Unable to get total physical memory on Linux");
                    return physicalMemorySize;
                }
                catch (Exception ex)
                {
                    StaticLogger?.LogError(ex, "Unable to get total physical memory on Linux");
                    return null;
                }
            });
        }
        if (Platform.IsMacOS)
        {
            return Task.Run(() =>
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
                    if (process is not null)
                    {
                        using var reader = process.StandardOutput;
                        var physicalMemorySize = default(long?);
                        var regex = new Regex("^[\\s]*hw\\.memsize[\\s]*:[\\s]*(?<Size>[\\d]+)", RegexOptions.IgnoreCase);
                        var line = reader.ReadLine();
                        while (line is not null)
                        {
                            var match = regex.Match(line);
                            if (match.Success && long.TryParse(match.Groups["Size"].Value, out var size))
                            {
                                physicalMemorySize = size;
                                break;
                            }
                            line = reader.ReadLine();
                        }
                        if (!physicalMemorySize.HasValue)
                            StaticLogger?.LogWarning("Unable to get total physical memory on macOS");
                        return physicalMemorySize;
                    }
                    StaticLogger?.LogWarning("Unable to start process to get total physical memory on macOS");
                }
                catch (Exception ex)
                {
                    StaticLogger?.LogError(ex, "Unable to get total physical memory on macOS");
                }
                return null;
            });
        }
        return Task.FromResult<long?>(null);
    }


    /// <summary>
    /// Check whether at least one dedicated graphics has been attached to device or not.
    /// </summary>
    public bool? HasDedicatedGraphicsCard
    {
        get
        {
            this.initCheckGraphicsCardTask?.Wait();
            return this.hasDedicatedGraphicsCard;
        }
    }
    
    
    // Logger
    ILogger Logger => StaticLogger ??= this.app.LoggerFactory.CreateLogger(nameof(HardwareInfo));


    /// <summary>
    /// Raised when property changed.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;


    /// <summary>
    /// Get size of total physical memory on device in bytes.
    /// </summary>
    public long? TotalPhysicalMemory
    {
        get
        {
            this.initCheckPhysicalMemoryTask?.Wait();
            return this.totalPhysicalMemory;
        }
    }
}
