﻿using Avalonia;
using Avalonia.Controls;

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
            if (Platform.IsWindows || Platform.IsMacOS)
                return new Thickness(0, 1, 0, 0);
            return new Thickness();
        });


        /// <summary>
        /// Get padding of window content when <see cref="Avalonia.Controls.Window.WindowState"/> is <see cref="WindowState.Maximized"/> or <see cref="WindowState.FullScreen"/>.
        /// </summary>
        public static Thickness ContentPaddingInMaximized { get; } = Global.Run(() =>
        {
            if (Platform.IsMacOS)
                return new Thickness(0, 1, 0, 0);
            if (Platform.IsNotWindows)
                return new Thickness();
            if (Platform.IsWindows10OrAbove)
                return new Thickness(6); // Windows 10
            if (Platform.IsWindows8OrAbove)
                return new Thickness(7); // Windows 8
            return new Thickness();
        });


        /// <summary>
        /// Check whether extending client area is supported on current platform or not.
        /// </summary>
        public static bool IsExtendedClientAreaSupported { get; } = Global.Run(() =>
        {
            if (Platform.IsWindows)
                return Platform.IsWindows8OrAbove;
            if (Platform.IsMacOS)
                return true;
            return false;
        });


        /// <summary>
        /// Check whether <see cref="SystemChromePlacement"/> is <see cref="PlacementMode.Left"/> or not.
        /// </summary>
        public static bool IsSystemChromePlacedAtLeft => SystemChromePlacement == PlacementMode.Left;


        /// <summary>
        /// Check whether <see cref="SystemChromePlacement"/> is <see cref="PlacementMode.Right"/> or not.
        /// </summary>
        public static bool IsSystemChromePlacedAtRight => SystemChromePlacement == PlacementMode.Right;


        /// <summary>
        /// Check whether system chrome is visible when <see cref="Avalonia.Controls.Window.WindowState"/> is <see cref="WindowState.FullScreen"/> or not.
        /// </summary>
        public static bool IsSystemChromeVisibleInFullScreen => false;


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
                return 135; // Windows 8+, managed chrome
            if (Platform.IsMacOS)
                return 80;
            return 0;
        });
    }
}
