using Avalonia;
using CarinaStudio.AppSuite.Native;
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
			var screen = window.Screens.ScreenFromWindow(window.PlatformImpl.AsNonNull()) ?? window.Screens.Primary;
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
	}
}
