using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CarinaStudio.AppSuite.Native
{
    internal static class Win32
    {
        public enum GWL : int
        {
            EXSTYLE = -20,
            HINSTANCE = -6,
            HWNDPARENT = -8,
            ID = -12,
            STYLE = -16,
            USERDATA = -21,
            WNDPROC = -4,
        }
        
        
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }


        [Flags]
        public enum WS : int
        {
            CAPTION	= 0x00C00000,
            MAXIMIZEBOX = 0x00010000,
            MINIMIZEBOX	= 0x00020000,
            SYSMENU	= 0x00080000,
        }


        [DllImport("Kernel32")]
        public static extern uint GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, uint nSize);


        [DllImport("User32")]
        public static extern nint GetWindowLong(IntPtr hWnd, GWL nIndex);


        [DllImport("User32")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);


        [DllImport("User32")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);


        [DllImport("User32")]
        public static extern nint SetWindowLong(IntPtr hWnd, GWL nIndex, nint dwNewLong);
    }
}
