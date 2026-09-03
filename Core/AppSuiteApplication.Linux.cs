using Avalonia;
using CarinaStudio.Logging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite;

partial class AppSuiteApplication
{
    // Fields.
    bool? isGSettingsAvailable;


    // Apply given screen scale factor for Linux.
    static void ApplyScreenScaleFactorOnLinux(double scaleFactor)
    {
        // [Workaround] Ignore unsupported distributions
        if (Platform.IsNotLinux)
            return;
        
        // check parameter
        if (!double.IsFinite(scaleFactor) || scaleFactor <= 0.0)
        {
            LogToConsole($"Invalid screen scale factor: {scaleFactor}");
            return;
        }
        
        // setup GDK
        if (!Native.Gdk.Initialize())
            return;

        // get scale factor of screens
        //var valueBuilder = new StringBuilder();
        if (!Native.Gdk.TryGetMinMonitorScaleFactor(out var minScaleFactor))
            LogToConsole("Default display not found.");

        // set environment variable
        if (minScaleFactor < int.MaxValue)
            LogToConsole($"Apply screen scale factor {scaleFactor}, detected screen scale factor: {minScaleFactor}");
        else
            LogToConsole($"Apply screen scale factor {scaleFactor}");
        Environment.SetEnvironmentVariable("AVALONIA_GLOBAL_SCALE_FACTOR", scaleFactor.ToString(CultureInfo.InvariantCulture));
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
    void OnMainWindowActivationChangedOnLinux(bool isActive)
    {
        if (this.IsSystemThemeModeSupportedOnLinux && isActive)
            _ = this.UpdateSystemThemeModeAsync(true);
    }
    
    
    // Setup AppBuilder for Linux.
    static void SetupLinuxAppBuilder(AppBuilder builder)
    {
        builder.With(new X11PlatformOptions());
    }
}