using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Security.Principal;

namespace CarinaStudio.AppSuite;

unsafe partial class AppSuiteApplication
{
#pragma warning disable CA1416
    // Get current system theme mode on Windows.
    ThemeMode GetWindowsThemeMode()
    {
        if (!Platform.IsWindows10OrAbove)
            return this.FallbackThemeMode;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var themeValue = key?.GetValue("AppsUseLightTheme");
            if (themeValue is IConvertible convertible)
                return convertible.ToInt32(null) == 0 ? ThemeMode.Dark : ThemeMode.Light;
            this.Logger.LogError("Failed to get system theme mode on Windows");
            return this.FallbackThemeMode;
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to get system theme mode on Windows");
            return this.FallbackThemeMode;
        }
    }
#pragma warning restore CA1416


#pragma warning disable CA1416
    // Check whether the process is running as Administrator or not.
    static bool IsRunningAsAdministratorOnWindows()
    {
        if (Platform.IsNotWindows)
            return false;
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
#pragma warning restore CA1416


    // Called when activation state of main window changed.
    void OnMainWindowActivationChangedOnWindows()
    {
        this.UpdateSystemThemeMode(true); // in case of system event was not received
    }


#pragma warning disable CA1416
    // Called when user preference changed on Windows
    void OnWindowsUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        switch (e.Category)
        {
            case UserPreferenceCategory.General:
                this.SynchronizationContext.Post(() => this.UpdateSystemThemeMode(true));
                break;
            case UserPreferenceCategory.Locale:
                this.SynchronizationContext.Post(() => this.UpdateCultureInfo(true));
                break;
        }
    }
#pragma warning restore CA1416
}