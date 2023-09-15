using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CarinaStudio.AppSuite.Native;

static unsafe class Win32
{
    public static readonly Guid CLSID_TaskBarList = new("56fdf344-fd6d-11d0-958a-006097c9a090");
    public static readonly Guid IID_TaskBarList3 = new("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf");


    public enum CLSCTX : uint
    {
        INPROC_SERVER = 0x1,
    }
    
    
    public enum DWMWA : uint
    {
        CAPTION_COLOR = 35,
        WINDOW_CORNER_PREFERENCE = 33,
        USE_IMMERSIVE_DARK_MODE = 20,
    }

    public enum DWMWCP
    {
        ROUND = 2,
    }

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
    
    
    [ComImport]
    [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ITaskbarList3
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
        void SetProgressState(IntPtr hwnd, Win32.TBPF tbpFlags);
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
    
    
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }


    [Flags]
    public enum TBPF : uint
    {
        ERROR = 0x4,
        INDETERMINATE = 0x1,
        NOPROGRESS = 0x0,
        NORMAL = 0x2,
        PAUSED = 0x8,
    }


    [Flags]
    public enum WS : int
    {
        CAPTION	= 0x00C00000,
        MAXIMIZEBOX = 0x00010000,
        MINIMIZEBOX	= 0x00020000,
        SYSMENU	= 0x00080000,
    }
    
    
    [DllImport("Ole32", SetLastError = true)]
    public static extern int CoCreateInstance(in Guid rclsid, [MarshalAs(UnmanagedType.IUnknown)] object? pUnkOuter, CLSCTX dwClsContext, in Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object? ppv);
    
    
    [DllImport("Ole32", SetLastError = true)]
    public static extern int CoInitialize(IntPtr pvReserved = default);
    
    
    [DllImport("Dwmapi", SetLastError = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWA dwAttribute, void* pvAttribute, uint cbAttribute);


    [DllImport("Kernel32", SetLastError = true)]
    public static extern uint GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, uint nSize);


    [DllImport("User32", SetLastError = true)]
    public static extern nint GetWindowLong(IntPtr hWnd, GWL nIndex);


    [DllImport("User32", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);


    [DllImport("User32", SetLastError = true)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);


    [DllImport("User32", SetLastError = true)]
    public static extern nint SetWindowLong(IntPtr hWnd, GWL nIndex, nint dwNewLong);
}

