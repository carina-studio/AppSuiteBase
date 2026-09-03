using System;
using System.Runtime.InteropServices;

namespace CarinaStudio.AppSuite.Native;

static class Gdk
{
    // Static fields.
    static unsafe delegate*unmanaged[Cdecl]<void*> getDefaultDisplay;
    static unsafe delegate*unmanaged[Cdecl]<void*, int, void*> getMonitor;
    static unsafe delegate*unmanaged[Cdecl]<void*, int> getMonitorCount;
    static unsafe delegate*unmanaged[Cdecl]<void*, int> getMonitorScaleFactor;
    static IntPtr libHandle;


    // Initialize GDK.
    public static bool Initialize()
    {
        // check state
        if (libHandle != IntPtr.Zero)
            return true;
        
        // load library
        if (!NativeLibrary.TryLoad("libgdk-3.so.0", out var handle))
        {
            Console.Error.WriteLine("Unable to load GDK.");
            return false;
        }
        
        // find functions and initialize GDK
        unsafe
        {
            if (!NativeLibrary.TryGetExport(handle, "gdk_init", out var funcPtr))
            {
                Console.Error.WriteLine("Unable to find gdk_init().");
                return false;
            }
            var initGdk = (delegate*unmanaged[Cdecl]<int, void*, void>)funcPtr;
            if (!NativeLibrary.TryGetExport(handle, "gdk_display_get_default", out funcPtr))
            {
                Console.Error.WriteLine("Unable to find gdk_display_get_default().");
                return false;
            }
            getDefaultDisplay = (delegate*unmanaged[Cdecl]<void*>)funcPtr;
            if (!NativeLibrary.TryGetExport(handle, "gdk_display_get_n_monitors", out funcPtr))
            {
                Console.Error.WriteLine("Unable to find gdk_display_get_n_monitors().");
                return false;
            }
            getMonitorCount = (delegate*unmanaged[Cdecl]<void*, int>)funcPtr;
            if (!NativeLibrary.TryGetExport(handle, "gdk_display_get_monitor", out funcPtr))
            {
                Console.Error.WriteLine("Unable to find gdk_display_get_monitor().");
                return false;
            }
            getMonitor = (delegate*unmanaged[Cdecl]<void*, int, void*>)funcPtr;
            if (!NativeLibrary.TryGetExport(handle, "gdk_monitor_get_scale_factor", out funcPtr))
            {
                Console.Error.WriteLine("Unable to find gdk_monitor_get_scale_factor().");
                return false;
            }
            getMonitorScaleFactor = (delegate*unmanaged[Cdecl]<void*, int>)funcPtr;
            initGdk(0, null);
        }

        // complete
        libHandle = handle;
        return true;
    }


    // Try getting the minimum scale factor of all monitors of the default display. Returns False if the default display cannot be found.
    public static bool TryGetMinMonitorScaleFactor(out int scaleFactor)
    {
        unsafe
        {
            // get the default display
            var display = getDefaultDisplay();
            if (display is null)
            {
                scaleFactor = int.MaxValue;
                return false;
            }
            
            // find the minimum scale factor of all monitors
            var minScaleFactor = int.MaxValue;
            for (var i = getMonitorCount(display) - 1; i >= 0; --i)
            {
                var monitor = getMonitor(display, i);
                /*
                var monitorModelPtr = getGdkMonitorModel(monitor);
                if (monitorModelPtr is null && i > 0)
                    continue;
                */
                var monitorScaleFactor = Math.Max(1, getMonitorScaleFactor(monitor));
                /*
                if (valueBuilder.Length > 0)
                    valueBuilder.Append(';');
                valueBuilder.Append(monitorModelPtr is not null ? new string(monitorModelPtr) : "default");
                valueBuilder.Append('=');
                valueBuilder.Append(scaleFactor);
                */
                if (monitorScaleFactor < minScaleFactor)
                    minScaleFactor = monitorScaleFactor;
            }
            scaleFactor = minScaleFactor;
            return true;
        }
    }
}