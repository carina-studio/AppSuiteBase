using Avalonia;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite;

partial class AppSuiteApplication
{
    // Static fields.
    static IntPtr gdkLibHandle;
    static unsafe delegate*unmanaged[Cdecl]<void*> getDefaultGdkDisplay;
    static unsafe delegate*unmanaged[Cdecl]<void*, int, void*> getGdkMonitor;
    static unsafe delegate*unmanaged[Cdecl]<void*, int> getGdkMonitorCount;
    static unsafe delegate*unmanaged[Cdecl]<void*, int> getGdkMonitorScaleFactor;


    // Fields.
    bool? isGSettingsAvailable;


    // Apply given screen scale factor for Linux.
    static unsafe void ApplyScreenScaleFactorOnLinux()
    {
        // [Workaround] Ignore unsupported distributions
        if (Platform.LinuxDistribution == LinuxDistribution.Ubuntu)
            return;
        
        // setup GDK
        if (!InitializeGdk())
            return;
        
        // get all monitors
        var display = getDefaultGdkDisplay();
        if (display is null)
            return;
        
        // set environment variable
        //var valueBuilder = new StringBuilder();
        var minScaleFactor = int.MaxValue;
        for (var i = getGdkMonitorCount(display) - 1; i >= 0; --i)
        {
            var monitor = getGdkMonitor(display, i);
            /*
            var monitorModelPtr = getGdkMonitorModel(monitor);
            if (monitorModelPtr is null && i > 0)
                continue;
            */
            var scaleFactor = Math.Max(1, getGdkMonitorScaleFactor(monitor));
            /*
            if (valueBuilder.Length > 0)
                valueBuilder.Append(';');
            valueBuilder.Append(monitorModelPtr is not null ? new string(monitorModelPtr) : "default");
            valueBuilder.Append('=');
            valueBuilder.Append(scaleFactor);
            */
            if (scaleFactor < minScaleFactor)
                minScaleFactor = scaleFactor;
        }
        if (minScaleFactor < int.MaxValue)
            Environment.SetEnvironmentVariable("AVALONIA_GLOBAL_SCALE_FACTOR", minScaleFactor.ToString());
        //Environment.SetEnvironmentVariable("AVALONIA_SCREEN_SCALE_FACTORS", valueBuilder.ToString());
    }


    // Get system theme mode on Linux.
    async Task<ThemeMode> GetLinuxThemeModeAsync()
    {
        if (!this.IsSystemThemeModeSupportedOnLinux)
            return this.FallbackThemeMode;
        try
        {
            return await Task.Run(() =>
            {
                using var process = Process.Start(new ProcessStartInfo()
                {
                    Arguments = "get org.gnome.desktop.interface color-scheme",
                    CreateNoWindow = true,
                    FileName = "gsettings",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                });
                if (process is not null)
                {
                    var colorScheme = process.StandardOutput.ReadLine();
                    if (string.IsNullOrWhiteSpace(colorScheme))
                        return this.FallbackThemeMode;
                    return colorScheme.ToLower().Contains("dark")
                        ? ThemeMode.Dark
                        : ThemeMode.Light;
                }
                this.Logger.LogError("Unable to start 'gsettings' to check system theme mode on Linux");
                return this.FallbackThemeMode;
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Unable to check system theme mode on Linux");
            return this.FallbackThemeMode;
        }
    }


    // Initialize GDK.
    static unsafe bool InitializeGdk()
    {
        // check state
        if (gdkLibHandle != IntPtr.Zero)
            return true;
        
        // load library
        if (!NativeLibrary.TryLoad("libgdk-3.so.0", out var libHandle))
        {
            Console.Error.WriteLine("Unable to load GDK.");
            return false;
        }
        
        // find functions
        if (!NativeLibrary.TryGetExport(libHandle, "gdk_init", out var funcPtr))
        {
            Console.Error.WriteLine("Unable to find gdk_init().");
            return false;
        }
        var initGdk = (delegate*unmanaged[Cdecl]<int, void*, void>)funcPtr;
        if (!NativeLibrary.TryGetExport(libHandle, "gdk_display_get_default", out funcPtr))
        {
            Console.Error.WriteLine("Unable to find gdk_display_get_default().");
            return false;
        }
        getDefaultGdkDisplay = (delegate*unmanaged[Cdecl]<void*>)funcPtr;
        if (!NativeLibrary.TryGetExport(libHandle, "gdk_display_get_n_monitors", out funcPtr))
        {
            Console.Error.WriteLine("Unable to find gdk_display_get_n_monitors().");
            return false;
        }
        getGdkMonitorCount= (delegate*unmanaged[Cdecl]<void*, int>)funcPtr;
        if (!NativeLibrary.TryGetExport(libHandle, "gdk_display_get_monitor", out funcPtr))
        {
            Console.Error.WriteLine("Unable to find gdk_display_get_monitor().");
            return false;
        }
        getGdkMonitor = (delegate*unmanaged[Cdecl]<void*, int, void*>)funcPtr;
        if (!NativeLibrary.TryGetExport(libHandle, "gdk_monitor_get_scale_factor", out funcPtr))
        {
            Console.Error.WriteLine("Unable to find gdk_monitor_get_scale_factor().");
            return false;
        }
        getGdkMonitorScaleFactor = (delegate*unmanaged[Cdecl]<void*, int>)funcPtr;

        // initialize GDK
        initGdk(0, null);

        // complete
        gdkLibHandle = libHandle;
        return true;
    }


    // Check whether 'gsettings' tool is available on device or not.
    bool IsGSettingsAvailable
    {
        get
        {
            if (this.isGSettingsAvailable.HasValue)
                return this.isGSettingsAvailable.Value;
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    FileName = "gsettings",
                    UseShellExecute = false,
                });
                process?.Kill();
                this.isGSettingsAvailable = process is not null;
            }
            catch
            {
                this.isGSettingsAvailable = false;
            }
            if (this.isGSettingsAvailable.Value)
            {
                this.Logger.LogInformation("gsettings found on device");
                return true;
            }
            this.Logger.LogInformation("gsettings is unavailable on device");
            return false;
        }
    }


    /// <summary>
    /// Check whether system theme mode is supported on Linux or not.
    /// </summary>
    internal bool IsSystemThemeModeSupportedOnLinux => this.IsGSettingsAvailable;


    // Called when IsActive of main window changed on Linux.
    void OnMainWindowActivationChangedOnLinux()
    {
        if (this.IsSystemThemeModeSupportedOnLinux)
            _ = this.UpdateSystemThemeModeAsync(true);
    }
    
    
    // Setup AppBuilder for Linux.
    static void SetupLinuxAppBuilder(AppBuilder builder)
    {
        builder.With(new X11PlatformOptions());
    }
}