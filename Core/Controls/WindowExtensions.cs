using System;
using System.Runtime.InteropServices;

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
		/// <param name="window"><see cref="Window"/>.</param>
		public static void ActivateAndBringToFront(this Avalonia.Controls.Window window)
		{
			window.VerifyAccess();
			window.Activate();
			if (Platform.IsWindows)
				SetForegroundWindow(window.PlatformImpl.Handle.Handle);
		}


		// Bring window to foreground.
		[DllImport("User32")]
		static extern bool SetForegroundWindow(IntPtr hWnd);
	}
}
