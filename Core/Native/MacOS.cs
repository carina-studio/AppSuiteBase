using CarinaStudio.MacOS.CoreGraphics;
using System;
using System.Runtime.InteropServices;

namespace CarinaStudio.AppSuite.Native;

static unsafe class MacOS
{
    [DllImport(CarinaStudio.MacOS.NativeLibraryNames.CoreGraphics)]
    public static extern IntPtr CGDisplayCopyDisplayMode(uint display);
    
    [DllImport(CarinaStudio.MacOS.NativeLibraryNames.CoreGraphics)]
    public static extern uint CGDisplayModeGetPixelWidth(IntPtr mode);

    [DllImport(CarinaStudio.MacOS.NativeLibraryNames.CoreGraphics)]
    public static extern void CGDisplayModeRelease(IntPtr mode);

    [DllImport(CarinaStudio.MacOS.NativeLibraryNames.CoreGraphics)]
    public static extern int CGGetDisplaysWithRect(CGRect rect, uint maxDisplays, uint* displays, uint* matchingDisplayCount);

    [DllImport(CarinaStudio.MacOS.NativeLibraryNames.CoreGraphics)]
    public static extern uint CGMainDisplayID();
}