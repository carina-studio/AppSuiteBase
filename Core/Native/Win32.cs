using System;
using System.Runtime.InteropServices;

namespace CarinaStudio.AppSuite.Native
{
    internal static class Win32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }


        [DllImport("User32")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);


        [DllImport("User32")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
