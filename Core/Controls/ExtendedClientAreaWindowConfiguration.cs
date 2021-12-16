using Avalonia;
using Avalonia.Controls;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Configuration of <see cref="Window"/> with <see cref="Avalonia.Controls.Window.ExtendClientAreaToDecorationsHint"/> set to true.
    /// </summary>
    public static class ExtendedClientAreaWindowConfiguration
    {
        /// <summary>
        /// Get padding of window content when <see cref="Avalonia.Controls.Window.WindowState"/> is <see cref="WindowState.Normal"/>.
        /// </summary>
        public static Thickness ContentPadding { get; } = Global.Run(() =>
        {
            return new Thickness(0, 6, 0, 0); // Windows 10
        });


        /// <summary>
        /// Get padding of window content when <see cref="Avalonia.Controls.Window.WindowState"/> is <see cref="WindowState.Maximized"/> or <see cref="WindowState.FullScreen"/>.
        /// </summary>
        public static Thickness ContentPaddingInMaximized { get; } = Global.Run(() =>
        {
            if (!Platform.IsWindows)
                return new Thickness();
            var version = Environment.OSVersion.Version;
            if (version.Major >= 10)
                return new Thickness(6); // Windows 10
            if (version.Major >= 6 && version.Minor >= 3)
                return new Thickness(7); // Windows 8.1
            return new Thickness(0);
        });


        /// <summary>
        /// Check whether extending client area is supported on current platform or not.
        /// </summary>
        public static bool IsExtendedClientAreaSupported { get; } = Global.Run(() =>
        {
            if (Platform.IsWindows)
            {
                var version = Environment.OSVersion.Version;
                return version.Major > 6 || (version.Major == 6 && version.Minor >= 3 /* Windows 8.1 */);
            }
            if (Platform.IsMacOS)
                return true;
            return false;
        });


        /// <summary>
        /// Check whether <see cref="SystemChromePlacement"/> is <see cref="PlacementMode.Left"/> or not.
        /// </summary>
        public static bool IsSystemChromePlacedAtLeft { get => SystemChromePlacement == PlacementMode.Left; }


        /// <summary>
        /// Check whether <see cref="SystemChromePlacement"/> is <see cref="PlacementMode.Right"/> or not.
        /// </summary>
        public static bool IsSystemChromePlacedAtRight { get => SystemChromePlacement == PlacementMode.Right; }


        /// <summary>
        /// Check whether system chrome is visible when <see cref="Avalonia.Controls.Window.WindowState"/> is <see cref="WindowState.FullScreen"/> or not.
        /// </summary>
        public static bool IsSystemChromeVisibleInFullScreen { get; } = false;


        /// <summary>
        /// Get placement of system chrome.
        /// </summary>
        public static PlacementMode SystemChromePlacement { get; } = Global.Run(() =>
        {
            if (Platform.IsMacOS)
                return PlacementMode.Left;
            return PlacementMode.Right;
        });


        /// <summary>
        /// Get width of system chrome.
        /// </summary>
        public static double SystemChromeWidth { get; } = Global.Run(() =>
        {
            if (Platform.IsWindows)
                return 140; // Windows 8.1+
            if (Platform.IsMacOS)
                return 70;
            return 0;
        });
    }
}
