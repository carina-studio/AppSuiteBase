using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Threading;
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
    readonly Dictionary<TopLevel, nint> baseWndProcPointers = new();
    Win32.ITaskbarList3? windowsTaskbarList;
    // ReSharper disable once CollectionNeverQueried.Local
    readonly Dictionary<TopLevel, Win32.WNDPROC> wndProcStubDelegates = new();


    // Apply current theme mode on given window.
    void ApplyThemeModeOnWindows(Window window)
    {
        if (!Platform.IsWindows11OrAbove)
            return;
        if (window.IsExtendedIntoWindowDecorations || window.SystemDecorations == SystemDecorations.None)
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
    
    
    // Attach window procedure to given TopLevel.
    void AttachWndProc(TopLevel topLevel)
    {
        // get handle
        var hWnd = topLevel.TryGetPlatformHandle()?.Handle ?? default;
        if (hWnd == default)
        {
            this.Logger.LogError("No handle for TopLevel {id:x8} to attach WndProc", topLevel.GetHashCode());
            return;
        }

        // get current window procedure
        var baseWndProc = Win32.GetWindowLongPtr(hWnd, Win32.GWL.WNDPROC);
        if (baseWndProc == default)
        {
            this.Logger.LogError("Base WndProc not found for TopLevel {id:x8} to attach WndProc", topLevel.GetHashCode());
            return;
        }
        this.baseWndProcPointers.Add(topLevel, baseWndProc);

        // prepare stub to attach window procedure
        IntPtr WndProcStub(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                return Win32.CallWindowProc(baseWndProc, hWnd, Msg, wParam, lParam);
            }
            catch (Exception ex)
            {
                this.Logger.LogWarning(ex, "Unhandled exception occurred in application lifetime caught by WndProc of TopLevel {id:x8}", topLevel.GetHashCode());
                LogToConsole($"Unhandled exception occurred in application lifetime caught by WndProc of TopLevel {topLevel.GetHashCode():x8}: {ex.GetType().Name}, {ex.Message}");
                LogToConsole(ex.StackTrace);
                if (!this.HandleExceptionOccurredInApplicationLifetime(ex))
                    throw;
                this.Logger.LogWarning("Exception was handled");
                return Win32.DefWindowProc(hWnd, Msg, wParam, lParam);
            }
        }
        var wndProcStub = new Win32.WNDPROC(WndProcStub);
        this.wndProcStubDelegates.Add(topLevel, wndProcStub); // keep delegate from GC

        // attach window procedure
        this.Logger.LogTrace("Attach WndProc to TopLevel {id:x8}", topLevel.GetHashCode());
        Win32.SetWindowLongPtr(hWnd, Win32.GWL.WNDPROC, wndProcStub);
        topLevel.Closed += this.OnTopLevelClosedToDetachWndProc;
    }
    
    
    // Detach window procedure from given TopLevel.
    void DetachWndProc(TopLevel topLevel)
    {
        // get handle
        var hWnd = topLevel.TryGetPlatformHandle()?.Handle ?? default;
        
        // get base window procedure
        if (!this.baseWndProcPointers.Remove(topLevel, out var baseWndProc))
        {
            this.Logger.LogError("Base WndProc not found for TopLevel {id:x8} to detach WndProc", topLevel.GetHashCode());
            return;
        }
        
        // detach window procedure
        this.Logger.LogTrace("Detach WndProc from TopLevel {id:x8}", topLevel.GetHashCode());
        if (hWnd != default)
            Win32.SetWindowLongPtr(hWnd, Win32.GWL.WNDPROC, baseWndProc);
        Dispatcher.UIThread.Post(() => this.wndProcStubDelegates.Remove(topLevel), DispatcherPriority.Background);
        topLevel.Closed -= this.OnTopLevelClosedToDetachWndProc;
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


    // Called after closing TopLevel which is needed to be detached from WndProc.
    void OnTopLevelClosedToDetachWndProc(object? sender, EventArgs e)
    {
        if (Platform.IsWindows && sender is TopLevel topLevel)
            this.DetachWndProc(topLevel);
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
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Win32.ITaskbarList3))]
    [MemberNotNullWhen(true, nameof(windowsTaskbarList))]
    bool SetupWindowsTaskbarList()
    {
        if (this.windowsTaskbarList is not null)
            return true;
        Win32.CoInitialize();
#pragma warning disable IL2050
        var result = Win32.CoCreateInstance(in Win32.CLSID_TaskBarList, null, Win32.CLSCTX.INPROC_SERVER, in Win32.IID_TaskBarList3, out var obj);
#pragma warning restore IL2050
        if (obj is null)
        {
            this.Logger.LogError("Unable to create ITaskBarList3 object, result: {result}", result);
            return false;
        }
        this.windowsTaskbarList = obj as Win32.ITaskbarList3;
        if (this.windowsTaskbarList is null)
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