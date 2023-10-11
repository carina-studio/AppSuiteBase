using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using CarinaStudio.AppSuite.Native;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace CarinaStudio.AppSuite;

unsafe partial class AppSuiteApplication
{
    // Fields.
    Win32.ITaskbarList3? windowsTaskbarList;


    // Apply current theme mode on given window.
    void ApplyThemeModeOnWindows(Avalonia.Controls.Window window)
    {
        if (!Platform.IsWindows11OrAbove)
            return;
        if (window.IsExtendedIntoWindowDecorations || window.SystemDecorations == Avalonia.Controls.SystemDecorations.None)
            return;
        var hWnd = (window.TryGetPlatformHandle()?.Handle).GetValueOrDefault();
        if (hWnd != default)
        {
            // setup window corner
            var cornerPreference = Win32.DWMWCP.ROUND;
            var result = Win32.DwmSetWindowAttribute(hWnd, Win32.DWMWA.WINDOW_CORNER_PREFERENCE, &cornerPreference, sizeof(int));
            if (result != default)
            {
                this.Logger.LogWarning("Failed to set corner preference of window '{title}', result: {result}", window.Title, result);
                Marshal.SetLastSystemError(0);
            }
            

            // enable/disable dark mode
            var darkMode = this.EffectiveThemeMode == ThemeMode.Dark;
            result = Win32.DwmSetWindowAttribute(hWnd, Win32.DWMWA.USE_IMMERSIVE_DARK_MODE, &darkMode, sizeof(int) /* size of BOOL is same as DWORD */);
            if (result != default)
            {
                this.Logger.LogWarning("Failed to set dark mode of window '{title}', result: {result}", window.Title, result);
                Marshal.SetLastSystemError(0);
            }
            
            // setup title bar color
            var titleBarColor = this.FindResourceOrDefault<Color>("Color/Window.TitleBar");
            var win32Color = (titleBarColor.B << 16) | (titleBarColor.G << 8) | titleBarColor.R;
            result = Win32.DwmSetWindowAttribute(hWnd, Win32.DWMWA.CAPTION_COLOR, &win32Color, sizeof(int));
            if (result != default)
            {
                this.Logger.LogWarning("Failed to set caption color of window '{title}', result: {result}", window.Title, result);
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
        _ = this.UpdateSystemThemeModeAsync(true); // in case of system event was not received
    }


#pragma warning disable CA1416
    // Called when user preference changed on Windows
    void OnWindowsUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        switch (e.Category)
        {
            case UserPreferenceCategory.General:
                this.SynchronizationContext.Post(() => _ = this.UpdateSystemThemeModeAsync(true));
                break;
            case UserPreferenceCategory.Locale:
                this.SynchronizationContext.Post(() => _ = this.UpdateCultureInfoAsync(true));
                break;
        }
    }
#pragma warning restore CA1416
    
    
    // Setup AppBuilder for Windows.
    static void SetupWindowsAppBuilder(AppBuilder builder, ISettings initSettings, UnicodeRange cjkUnicodeRanges, IList<FontFamily> embeddedChineseFonts)
    {
        builder.ConfigureFonts(fontManager =>
        {
            fontManager.AddFontCollection(new EmbeddedFontCollection(
                new Uri("fonts:Inter", UriKind.Absolute),
                new Uri($"avares://{Assembly.GetExecutingAssembly().GetName().Name}/Fonts", UriKind.Absolute)));
        });
        builder.With(new FontManagerOptions
        {
            FontFallbacks = new List<FontFallback>(8).Also(it =>
            {
                foreach (var fontFamily in embeddedChineseFonts)
                {
                    it.Add(new()
                    {
                        FontFamily = fontFamily,
                        UnicodeRange = cjkUnicodeRanges,
                    });
                }
                // ReSharper disable StringLiteralTypo
                it.Add(new()
                {
                    FontFamily = new("Microsoft JhengHei UI"),
                    UnicodeRange = cjkUnicodeRanges,
                });
                it.Add(new()
                {
                    FontFamily = new("Microsoft YaHei UI"),
                    UnicodeRange = cjkUnicodeRanges,
                });
                it.Add(new()
                {
                    FontFamily = new("PMingLiU"),
                    UnicodeRange = cjkUnicodeRanges,
                });
                it.Add(new()
                {
                    FontFamily = new("MingLiU"),
                    UnicodeRange = cjkUnicodeRanges,
                });
                // ReSharper restore StringLiteralTypo
            }).ToArray(),
        });
        if (initSettings.GetValueOrDefault(InitSettingKeys.DisableAngle))
        {
            builder.With(new Win32PlatformOptions
            {
                RenderingMode = new[] { Win32RenderingMode.Wgl, Win32RenderingMode.Software }
            });
        }
    }


    // Setup related objects for taskbar.
    [MemberNotNullWhen(true, nameof(windowsTaskbarList))]
    bool SetupWindowsTaskbarList()
    {
        if (this.windowsTaskbarList != null)
            return true;
        Win32.CoInitialize();
        var result = Win32.CoCreateInstance(in Win32.CLSID_TaskBarList, null, Win32.CLSCTX.INPROC_SERVER, in Win32.IID_TaskBarList3, out var obj);
        if (obj == null)
        {
            this.Logger.LogError("Unable to create ITaskBarList3 object, result: {result}", result);
            return false;
        }
        this.windowsTaskbarList = obj as Win32.ITaskbarList3;
        if (this.windowsTaskbarList == null)
        {
            this.Logger.LogError("Unable to get implementation of ITaskBarList3");
            return false;
        }
        this.windowsTaskbarList.HrInit();
        return true;
    }


    // Update progress of task bar item.
    internal void UpdateWindowsTaskBarProgress(Controls.Window window)
    {
        if (!this.SetupWindowsTaskbarList())
            return;
        var hWnd = (window.TryGetPlatformHandle()?.Handle).GetValueOrDefault();
        if (hWnd == default)
            return;
        this.windowsTaskbarList.SetProgressValue(hWnd, (ulong)(window.TaskbarIconProgress * 1000 + 0.5), 1000UL);
    }


    // Update progress state of task bar item.
    internal void UpdateWindowsTaskBarProgressState(Controls.Window window)
    {
        if (!this.SetupWindowsTaskbarList())
            return;
        var hWnd = (window.TryGetPlatformHandle()?.Handle).GetValueOrDefault();
        if (hWnd == default)
            return;
        this.windowsTaskbarList.SetProgressState(hWnd, window.TaskbarIconProgressState switch
        {
            Controls.TaskbarIconProgressState.Error => Win32.TBPF.ERROR,
            Controls.TaskbarIconProgressState.Indeterminate => Win32.TBPF.INDETERMINATE,
            Controls.TaskbarIconProgressState.Normal => Win32.TBPF.NORMAL,
            Controls.TaskbarIconProgressState.Paused => Win32.TBPF.PAUSED,
            _ => Win32.TBPF.NOPROGRESS,
        });
    }
}