using CarinaStudio.MacOS;
using CarinaStudio.MacOS.CoreGraphics;
using System;
using System.Runtime.InteropServices;

namespace CarinaStudio.AppSuite.Native;

static class MacOS
{
    // Static fields.
    static unsafe readonly delegate*<uint, IntPtr> CGDisplayCopyDisplayMode;
    static unsafe readonly delegate*<IntPtr, uint> CGDisplayModeGetPixelWidth;
    static unsafe readonly delegate*<IntPtr, void> CGDisplayModeRelease;
    static unsafe readonly delegate*<CGRect, uint, uint*, uint*, int> CGGetDisplaysWithRect;
    static unsafe readonly delegate*<uint> CGMainDisplayID;
    
    
    // Static constructor.
    static MacOS()
    {
        // check platform
        if (Platform.IsNotMacOS)
            return;
        
        // find functions
        unsafe
        {
            var libHandle = NativeLibraryHandles.CoreGraphics;
            CGDisplayCopyDisplayMode = (delegate*<uint, IntPtr>)NativeLibrary.GetExport(libHandle, nameof(CGDisplayCopyDisplayMode));
            CGDisplayModeGetPixelWidth = (delegate*<IntPtr, uint>)NativeLibrary.GetExport(libHandle, nameof(CGDisplayModeGetPixelWidth));
            CGDisplayModeRelease = (delegate*<IntPtr, void>)NativeLibrary.GetExport(libHandle, nameof(CGDisplayModeRelease));
            CGGetDisplaysWithRect = (delegate*<CGRect, uint, uint*, uint*, int>)NativeLibrary.GetExport(libHandle, nameof(CGGetDisplaysWithRect));
            CGMainDisplayID = (delegate*<uint>)NativeLibrary.GetExport(libHandle, nameof(CGMainDisplayID));
        }
    }


    // Get width of the display which contains the given rectangle, in pixels. Returns 0 if the width is unavailable.
    public static uint GetDisplayPixelWidth(CGRect rect)
    {
        // check platform
        if (Platform.IsNotMacOS)
            return 0;
        
        // get width of the display
        unsafe
        {
            // find the display which contains the rectangle
            var displayId = 0u;
            var displayCount = 0u;
            CGGetDisplaysWithRect(rect, 1, &displayId, &displayCount);
            if (displayCount == 0)
                displayId = CGMainDisplayID();
            
            // get width from the mode of the display
            var displayModeRef = CGDisplayCopyDisplayMode(displayId);
            if (displayModeRef == IntPtr.Zero)
                return 0;
            try
            {
                return CGDisplayModeGetPixelWidth(displayModeRef);
            }
            finally
            {
                CGDisplayModeRelease(displayModeRef);
            }
        }
    }
}