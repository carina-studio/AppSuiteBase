using CarinaStudio.MacOS;
using CarinaStudio.MacOS.AppKit;
using CarinaStudio.MacOS.CoreGraphics;
using CarinaStudio.MacOS.ObjectiveC;
using System;
using System.Runtime.InteropServices;

namespace CarinaStudio.AppSuite.Native;

static class MacOS
{
    // Constants.
    const uint CFStringEncodingUTF8 = 0x08000100; // kCFStringEncodingUTF8
    const string IOKitLibraryPath = "/System/Library/Frameworks/IOKit.framework/IOKit";


    // Static fields.
    static bool? CachedIsBuiltInDisplayPartiallyOccluded;
    static unsafe readonly delegate*<IntPtr, byte*> CFDataGetBytePtr;
    static unsafe readonly delegate*<IntPtr, nint> CFDataGetLength;
    static unsafe readonly delegate*<nuint> CFDataGetTypeID;
    static unsafe readonly delegate*<IntPtr, nuint> CFGetTypeID;
    static unsafe readonly delegate*<IntPtr, void> CFRelease;
    static unsafe readonly delegate*<IntPtr, byte*, uint, IntPtr> CFStringCreateWithCString;
    static unsafe readonly delegate*<uint, IntPtr> CGDisplayCopyDisplayMode;
    static unsafe readonly delegate*<IntPtr, uint> CGDisplayModeGetPixelWidth;
    static unsafe readonly delegate*<IntPtr, void> CGDisplayModeRelease;
    static unsafe readonly delegate*<CGRect, uint, uint*, uint*, int> CGGetDisplaysWithRect;
    static unsafe readonly delegate*<uint> CGMainDisplayID;
    static Selector? CountSelector;
    static unsafe readonly delegate*<uint, int> IOObjectRelease;
    static unsafe readonly delegate*<uint, IntPtr, IntPtr, uint, IntPtr> IORegistryEntryCreateCFProperty;
    static unsafe readonly delegate*<uint, byte*, uint> IORegistryEntryFromPath;
    static bool IsBuiltInDisplayOcclusionChecked;
    static Class? NSScreenClass;
    static Selector? ObjectAtIndexSelector;
    static Selector? SafeAreaInsetsSelector;
    static Selector? ScreensSelector;
    
    
    // Static constructor.
    static MacOS()
    {
        // check platform
        if (Platform.IsNotMacOS)
            return;
        
        // find functions
        unsafe
        {
            // functions of Core Foundation
            var libHandle = NativeLibraryHandles.CoreFoundation;
            CFDataGetBytePtr = (delegate*<IntPtr, byte*>)NativeLibrary.GetExport(libHandle, nameof(CFDataGetBytePtr));
            CFDataGetLength = (delegate*<IntPtr, nint>)NativeLibrary.GetExport(libHandle, nameof(CFDataGetLength));
            CFDataGetTypeID = (delegate*<nuint>)NativeLibrary.GetExport(libHandle, nameof(CFDataGetTypeID));
            CFGetTypeID = (delegate*<IntPtr, nuint>)NativeLibrary.GetExport(libHandle, nameof(CFGetTypeID));
            CFRelease = (delegate*<IntPtr, void>)NativeLibrary.GetExport(libHandle, nameof(CFRelease));
            CFStringCreateWithCString = (delegate*<IntPtr, byte*, uint, IntPtr>)NativeLibrary.GetExport(libHandle, nameof(CFStringCreateWithCString));

            // functions of Core Graphics
            libHandle = NativeLibraryHandles.CoreGraphics;
            CGDisplayCopyDisplayMode = (delegate*<uint, IntPtr>)NativeLibrary.GetExport(libHandle, nameof(CGDisplayCopyDisplayMode));
            CGDisplayModeGetPixelWidth = (delegate*<IntPtr, uint>)NativeLibrary.GetExport(libHandle, nameof(CGDisplayModeGetPixelWidth));
            CGDisplayModeRelease = (delegate*<IntPtr, void>)NativeLibrary.GetExport(libHandle, nameof(CGDisplayModeRelease));
            CGGetDisplaysWithRect = (delegate*<CGRect, uint, uint*, uint*, int>)NativeLibrary.GetExport(libHandle, nameof(CGGetDisplaysWithRect));
            CGMainDisplayID = (delegate*<uint>)NativeLibrary.GetExport(libHandle, nameof(CGMainDisplayID));

            // functions of IOKit
            libHandle = NativeLibrary.Load(IOKitLibraryPath);
            IOObjectRelease = (delegate*<uint, int>)NativeLibrary.GetExport(libHandle, nameof(IOObjectRelease));
            IORegistryEntryCreateCFProperty = (delegate*<uint, IntPtr, IntPtr, uint, IntPtr>)NativeLibrary.GetExport(libHandle, nameof(IORegistryEntryCreateCFProperty));
            IORegistryEntryFromPath = (delegate*<uint, byte*, uint>)NativeLibrary.GetExport(libHandle, nameof(IORegistryEntryFromPath));
        }
    }


    // Check whether the built-in display is partially occluded by camera housing (notch) according to product information in device tree.
    // Returns Null if the product information is unavailable, e.g. on Macs with Intel processors.
    static bool? CheckBuiltInDisplayOcclusion()
    {
        unsafe
        {
            // find product information in device tree
            var entry = 0u;
            fixed (byte* path = "IODeviceTree:/product\0"u8)
                entry = IORegistryEntryFromPath(0, path); // kIOMainPortDefault
            if (entry == 0)
                return null;

            // check flag of occlusion, the flag is absent on Macs without built-in display or without camera housing
            var key = IntPtr.Zero;
            var value = IntPtr.Zero;
            try
            {
                fixed (byte* keyName = "partially-occluded-display\0"u8)
                    key = CFStringCreateWithCString(IntPtr.Zero, keyName, CFStringEncodingUTF8);
                if (key == IntPtr.Zero)
                    return null;
                value = IORegistryEntryCreateCFProperty(entry, key, IntPtr.Zero, 0);
                if (value == IntPtr.Zero)
                    return false;
                if (CFGetTypeID(value) != CFDataGetTypeID())
                    return null;
                var bytes = CFDataGetBytePtr(value);
                var length = CFDataGetLength(value);
                for (var i = 0; i < length; ++i)
                {
                    if (bytes[i] != 0)
                        return true;
                }
                return false;
            }
            finally
            {
                if (value != IntPtr.Zero)
                    CFRelease(value);
                if (key != IntPtr.Zero)
                    CFRelease(key);
                IOObjectRelease(entry);
            }
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


    // Check whether the built-in display of the Mac is partially occluded by camera housing (notch) or not, even if the display is inactive (e.g. lid of laptop is closed).
    // Returns Null if it cannot be determined.
    public static bool? IsBuiltInDisplayPartiallyOccluded()
    {
        // check platform
        if (Platform.IsNotMacOS)
            return false;

        // use cached result, it won't be changed because it depends on hardware
        if (IsBuiltInDisplayOcclusionChecked)
            return CachedIsBuiltInDisplayPartiallyOccluded;

        // check occlusion
        CachedIsBuiltInDisplayPartiallyOccluded = CheckBuiltInDisplayOcclusion();
        IsBuiltInDisplayOcclusionChecked = true;
        return CachedIsBuiltInDisplayPartiallyOccluded;
    }


    // Check whether at least one of active screens has camera housing (notch) at top or not.
    public static bool IsCameraHousingScreenPresent()
    {
        // check platform
        if (Platform.IsNotMacOS)
            return false;

        // get active screens
        NSScreenClass ??= Class.GetClass("NSScreen").AsNonNull();
        ScreensSelector ??= Selector.FromName("screens");
        var screensHandle = NSObject.SendMessage<IntPtr>(NSScreenClass.Handle, ScreensSelector);
        if (screensHandle == IntPtr.Zero)
            return false;

        // check unsafe area at top of each screen
        CountSelector ??= Selector.FromName("count");
        ObjectAtIndexSelector ??= Selector.FromName("objectAtIndex:");
        SafeAreaInsetsSelector ??= Selector.FromName("safeAreaInsets");
        var screenCount = NSObject.SendMessage<int>(screensHandle, CountSelector);
        for (var i = 0; i < screenCount; ++i)
        {
            var screenHandle = NSObject.SendMessage<IntPtr>(screensHandle, ObjectAtIndexSelector, (nuint)i);
            if (screenHandle != IntPtr.Zero && NSObject.SendMessage<NSEdgeInsets>(screenHandle, SafeAreaInsetsSelector).Top > 0)
                return true;
        }
        return false;
    }
}