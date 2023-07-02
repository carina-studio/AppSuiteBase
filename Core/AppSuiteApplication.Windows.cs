using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace CarinaStudio.AppSuite;

unsafe partial class AppSuiteApplication
{
    // Native COM interfaces.
    [ComImport]
    [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ITaskbarList3
    {
        // ITaskbarList
        void HrInit();
        void AddTab(IntPtr hwnd);
        void DeleteTab(IntPtr hwnd);
        void ActivateTab(IntPtr hwnd);
        void SetActiveAlt(IntPtr hwnd);

        // ITaskbarList2
        void MarkFullscreenWindow(IntPtr hwnd, bool fFullscreen);

        // ITaskbarList3
        void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
        void SetProgressState(IntPtr hwnd, uint tbpFlags);
        void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
        void UnregisterTab(IntPtr hwndTab);
        void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
        void SetTabActive(IntPtr hwndTab, IntPtr hwndMDI);
        void ThumbBarAddButtons(IntPtr hwnd, uint cButtons, IntPtr pButton);
        void ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, IntPtr pButton);
        void ThumbBarSetImageList(IntPtr hwnd, IntPtr himl);
        void SetOverlayIcon(IntPtr hwnd, IntPtr hIcon, string? pszDescription);
        void SetThumbnailTooltip(IntPtr hwnd, string? pszTip);
        void SetThumbnailClip(IntPtr hwnd, void* prcClip);
    }


    // Constants.
    const uint CLSCTX_INPROC_SERVER = 0x1;
    static readonly Guid CLSID_TaskBarList = new("56fdf344-fd6d-11d0-958a-006097c9a090");
    const uint DWMWA_CAPTION_COLOR = 35;
    const uint DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    const uint DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    const int DWMWCP_ROUND = 2;
    static readonly Guid IID_TaskBarList3 = new("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf");
    const uint TBPF_ERROR = 0x4;
    const uint TBPF_INDETERMINATE = 0x1;
    const uint TBPF_NOPROGRESS = 0x0;
    const uint TBPF_NORMAL = 0x2;
    const uint TBPF_PAUSED = 0x8;


    // Native symbols.
    [DllImport("Ole32", SetLastError = true)]
    static extern int CoCreateInstance(in Guid rclsid, [MarshalAs(UnmanagedType.IUnknown)] object? pUnkOuter, uint dwClsContext, in Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object? ppv);
    [DllImport("Ole32", SetLastError = true)]
    static extern int CoInitialize(IntPtr pvReserved = default);
    [DllImport("Dwmapi", SetLastError = true)]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, uint dwAttribute, void* pvAttribute, uint cbAttribute);


    // Fields.
    ITaskbarList3? windowsTaskbarList;


    // Apply current theme mode on given window.
    void ApplyThemeModeOnWindows(Avalonia.Controls.Window window)
    {
        if (!Platform.IsWindows11OrAbove)
            return;
        if (window.IsExtendedIntoWindowDecorations || window.SystemDecorations == Avalonia.Controls.SystemDecorations.None)
            return;
        var hwnd = (window.TryGetPlatformHandle()?.Handle).GetValueOrDefault();
        if (hwnd != default)
        {
            // setup window corner
            var cornerPreference = DWMWCP_ROUND;
            var result = DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, &cornerPreference, sizeof(int));
            if (result != default)
            {
                this.Logger.LogWarning("Failed to set corner preference of window '{title}', result: {result}", window.Title, result);
                Marshal.SetLastSystemError(0);
            }
            

            // enable/disable dark mode
            var darkMode = this.EffectiveThemeMode == ThemeMode.Dark;
            result = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, &darkMode, sizeof(int) /* size of BOOL is same as DWORD */);
            if (result != default)
            {
                this.Logger.LogWarning("Failed to set dark mode of window '{title}', result: {result}", window.Title, result);
                Marshal.SetLastSystemError(0);
            }
            
            // setup title bar color
            var titleBarColor = this.FindResourceOrDefault<Color>("Color/Window.TitleBar");
            var win32Color = (titleBarColor.B << 16) | (titleBarColor.G << 8) | titleBarColor.R;
            result = DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, &win32Color, sizeof(int));
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
    
    
    // Setup AppBuilder for Windows.
    static void SetupWindowsAppBuilder(AppBuilder builder, UnicodeRange cjkUnicodeRanges)
    {
        builder.ConfigureFonts(fontManager =>
        {
            fontManager.AddFontCollection(new EmbeddedFontCollection(
                new Uri("fonts:Inter", UriKind.Absolute),
                new Uri($"avares://{Assembly.GetExecutingAssembly().GetName().Name}/Fonts", UriKind.Absolute)));
        });
        builder.With(new FontManagerOptions
        {
            // ReSharper disable StringLiteralTypo
            FontFallbacks = new FontFallback[]
            {
                new()
                {
                    FontFamily = new("Microsoft JhengHei UI"),
                    UnicodeRange = cjkUnicodeRanges,
                },
                new()
                {
                    FontFamily = new("Microsoft YaHei UI"),
                    UnicodeRange = cjkUnicodeRanges,
                },
                new()
                {
                    FontFamily = new("PMingLiU"),
                    UnicodeRange = cjkUnicodeRanges,
                },
                new()
                {
                    FontFamily = new("MingLiU"),
                    UnicodeRange = cjkUnicodeRanges,
                }
            },
            // ReSharper restore StringLiteralTypo
        });
    }


    // Setup related objects for taskbar.
    [MemberNotNullWhen(true, nameof(windowsTaskbarList))]
    bool SetupWindowsTaskbarList()
    {
        if (this.windowsTaskbarList != null)
            return true;
        CoInitialize();
        var result = CoCreateInstance(in CLSID_TaskBarList, null, CLSCTX_INPROC_SERVER, in IID_TaskBarList3, out var obj);
        if (obj == null)
        {
            this.Logger.LogError("Unable to create ITaskBarList3 object, result: {result}", result);
            return false;
        }
        this.windowsTaskbarList = obj as ITaskbarList3;
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
        var hwnd = (window.TryGetPlatformHandle()?.Handle).GetValueOrDefault();
        if (hwnd == default)
            return;
        this.windowsTaskbarList.SetProgressValue(hwnd, (ulong)(window.TaskbarIconProgress * 1000 + 0.5), 1000UL);
    }


    // Update progress state of task bar item.
    internal void UpdateWindowsTaskBarProgressState(Controls.Window window)
    {
        if (!this.SetupWindowsTaskbarList())
            return;
        var hwnd = (window.TryGetPlatformHandle()?.Handle).GetValueOrDefault();
        if (hwnd == default)
            return;
        this.windowsTaskbarList.SetProgressState(hwnd, window.TaskbarIconProgressState switch
        {
            Controls.TaskbarIconProgressState.Error => TBPF_ERROR,
            Controls.TaskbarIconProgressState.Indeterminate => TBPF_INDETERMINATE,
            Controls.TaskbarIconProgressState.Normal => TBPF_NORMAL,
            Controls.TaskbarIconProgressState.Paused => TBPF_PAUSED,
            _ => TBPF_NOPROGRESS,
        });
    }
}