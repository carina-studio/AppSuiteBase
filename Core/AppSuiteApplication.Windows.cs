using CarinaStudio.Controls;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace CarinaStudio.AppSuite;

unsafe partial class AppSuiteApplication
{
    // Constants.
    const uint DWMWA_CAPTION_COLOR = 35;
    const uint DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    const uint DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    const int DWMWCP_ROUND = 2;


    // Native symbols.
    [DllImport("Dwmapi", SetLastError = true)]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, uint dwAttribute, void* pvAttribute, uint cbAttribute);


    // Apply current theme mode on given window.
    void ApplyThemeModeOnWindows(Avalonia.Controls.Window window)
    {
        if (!Platform.IsWindows11OrAbove)
            return;
        if (window.IsExtendedIntoWindowDecorations || window.SystemDecorations == Avalonia.Controls.SystemDecorations.None)
            return;
        var hwnd = (window.PlatformImpl?.Handle?.Handle).GetValueOrDefault();
        if (hwnd != default)
        {
            // setup window corner
            var cornerPreference = DWMWCP_ROUND;
            var result = DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, &cornerPreference, sizeof(int));
            if (result != default)
            {
                this.Logger.LogWarning("Failed to set corner preference of window, result: {result}", result);
                Marshal.SetLastSystemError(0);
            }
            

            // enable/disable dark mode
            var darkMode = this.EffectiveThemeMode == ThemeMode.Dark;
            result = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, &darkMode, sizeof(int) /* size of BOOL is same as DWORD */);
            if (result != default)
            {
                this.Logger.LogWarning("Failed to set dark mode of window, result: {result}", result);
                Marshal.SetLastSystemError(0);
            }
            
            // setup title bar color
            var titleBarColor = this.FindResourceOrDefault<Avalonia.Media.Color>("Color/Window.TitleBar");
            var win32Color = (titleBarColor.B << 16) | (titleBarColor.G << 8) | titleBarColor.R;
            result = DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, &win32Color, sizeof(int));
            if (result != default)
            {
                this.Logger.LogWarning("Failed to set caption color of window, result: {result}", result);
                Marshal.SetLastSystemError(0);
            }
        }
    }


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