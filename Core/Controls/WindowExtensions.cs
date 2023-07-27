using Avalonia;
using CarinaStudio.AppSuite.Native;
using CarinaStudio.MacOS.AppKit;
using CarinaStudio.MacOS.ObjectiveC;
using System;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// Extensions for <see cref="Avalonia.Controls.Window"/>.
	/// </summary>
	public static class WindowExtensions
	{
		/// <summary>
		/// Activate window and bring window to foreground.
		/// </summary>
		/// <param name="window"><see cref="Avalonia.Controls.Window"/>.</param>
		public static void ActivateAndBringToFront(this Avalonia.Controls.Window window)
		{
			window.VerifyAccess();
			window.Activate();
			if (Platform.IsWindows)
				Win32.SetForegroundWindow((window.TryGetPlatformHandle()?.Handle).GetValueOrDefault());
		}


		/// <summary>
		/// Get size of system decorations.
		/// </summary>
		/// <param name="window"><see cref="Avalonia.Controls.Window"/>.</param>
		/// <returns>Size of system decorations.</returns>
		public static Thickness GetSystemDecorationSizes(this Avalonia.Controls.Window window)
        {
			var screen = window.Screens.ScreenFromWindow(window) ?? window.Screens.Primary;
			if (screen == null)
				return default;
			var scaling = screen.Scaling;
			if (Platform.IsLinux)
				return new Thickness(0, 75 / scaling, 0, 0); // Ubuntu
			if (Platform.IsWindows)
            {
	            if (Win32.GetWindowRect((window.TryGetPlatformHandle()?.Handle).GetValueOrDefault(), out var rect))
	            {
					var clientSize = window.ClientSize;
					var windowWidth = (rect.right - rect.left) / scaling;
					var windowHeight = (rect.bottom - rect.top) / scaling;
					var borderWidth = Math.Max(0, (windowWidth - clientSize.Width) / 2);
					return new Thickness(borderWidth, Math.Max(0, windowHeight - clientSize.Height - borderWidth), borderWidth, borderWidth);
				}
            }
			return default;
		}


		/// <summary>
		/// Hide all buttons on caption (title bar) of window.
		/// </summary>
		/// <param name="window">Window.</param>
		public static void HideCaptionButtons(this Avalonia.Controls.Window window)
		{
			var handle = (window.TryGetPlatformHandle()?.Handle).GetValueOrDefault();
			if (handle == default)
				return;
			if (Platform.IsWindows)
			{
				var style = (Win32.WS)Win32.GetWindowLong(handle, Win32.GWL.STYLE);
				if (style == default)
					return;
				Win32.SetWindowLong(handle, Win32.GWL.STYLE, (nint)(style & ~Win32.WS.SYSMENU));
			}
			else if (Platform.IsMacOS)
			{
				NSObject.FromHandle<NSWindow>(handle)?.Use(nsWindow =>
				{
					nsWindow.StandardWindowButton(NSWindow.ButtonType.CloseButton)?.Use(it => it.IsHidden = true);
					nsWindow.StandardWindowButton(NSWindow.ButtonType.MiniaturizeButton)?.Use(it => it.IsHidden = true);
					nsWindow.StandardWindowButton(NSWindow.ButtonType.ZoomButton)?.Use(it => it.IsHidden = true);
				});
			}
		}
	}
}
