using CarinaStudio.MacOS;
using CarinaStudio.MacOS.CoreGraphics;
using System;
using System.Runtime.InteropServices;

namespace CarinaStudio.AppSuite.Native;

static unsafe class MacOS
{
    public static readonly delegate*<uint, IntPtr> CGDisplayCopyDisplayMode;
    public static readonly delegate*<IntPtr, uint> CGDisplayModeGetPixelWidth;
    public static readonly delegate*<IntPtr, void> CGDisplayModeRelease;
    public static readonly delegate*<CGRect, uint, uint*, uint*, int> CGGetDisplaysWithRect;
    public static readonly delegate*<uint> CGMainDisplayID;
    
    
    // Static constructor.
    static MacOS()
    {
        if (Platform.IsNotMacOS)
            return;
        var libHandle = NativeLibraryHandles.CoreGraphics;
        CGDisplayCopyDisplayMode = (delegate*<uint, IntPtr>)NativeLibrary.GetExport(libHandle, nameof(CGDisplayCopyDisplayMode));
        CGDisplayModeGetPixelWidth = (delegate*<IntPtr, uint>)NativeLibrary.GetExport(libHandle, nameof(CGDisplayModeGetPixelWidth));
        CGDisplayModeRelease = (delegate*<IntPtr, void>)NativeLibrary.GetExport(libHandle, nameof(CGDisplayModeRelease));
        CGGetDisplaysWithRect = (delegate*<CGRect, uint, uint*, uint*, int>)NativeLibrary.GetExport(libHandle, nameof(CGGetDisplaysWithRect));
        CGMainDisplayID = (delegate*<uint>)NativeLibrary.GetExport(libHandle, nameof(CGMainDisplayID));
    }
}