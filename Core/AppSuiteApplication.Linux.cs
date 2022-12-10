using CarinaStudio.Collections;
using Microsoft.Extensions.Logging;
using System.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite;

partial class AppSuiteApplication
{
    // Info of X11 monitor.
    record class X11MonitorInfo(string Name, int PixelWidth, int PixelHeight);


    // Static fields.
    static Regex? X11MonitorLineRegex;


    // Apply given screen scale factor for Linux.
    static void ApplyScreenScaleFactorOnLinux()
    {
        if (CustomScreenScaleFactorFilePath != null)
        {
            CachedCustomScreenScaleFactor = 1;
            try
            {
                if (File.Exists(CustomScreenScaleFactorFilePath) 
                    && CarinaStudio.IO.File.TryOpenRead(CustomScreenScaleFactorFilePath, 5000, out var stream)
                    && stream != null)
                {
                    using (stream)
                    {
                        using var reader = new StreamReader(stream, Encoding.UTF8);
                        var line = reader.ReadLine();
                        if (line != null && double.TryParse(line, out CachedCustomScreenScaleFactor))
                            CachedCustomScreenScaleFactor = Math.Max(1, CachedCustomScreenScaleFactor);
                    }
                }
            }
            catch
            { }
            if (!double.IsFinite(CachedCustomScreenScaleFactor))
                CachedCustomScreenScaleFactor = 1;
            ApplyScreenScaleFactorOnLinux(CachedCustomScreenScaleFactor);
        }
    }
    static void ApplyScreenScaleFactorOnLinux(double factor)
    {
        // check state
        if (!double.IsFinite(factor) || factor < 1)
            return;
        if (Math.Abs(factor - 1) < 0.01)
            return;
        
        // get all screens
        var monitors = GetX11Monitors();
        if (monitors.IsEmpty())
            return;
        
        // set environment variable
        var valueBuilder = new StringBuilder();
        foreach (var monitor in monitors)
        {
            if (valueBuilder.Length > 0)
                valueBuilder.Append(';');
            valueBuilder.Append(monitor.Name);
            valueBuilder.Append('=');
            valueBuilder.AppendFormat("{0:F1}", factor);
        }
        Environment.SetEnvironmentVariable("AVALONIA_SCREEN_SCALE_FACTORS", valueBuilder.ToString());
    }


    // List all X11 monitors.
    static IList<X11MonitorInfo> GetX11Monitors()
    {
        var monitors = new List<X11MonitorInfo>();
        try
        {
            X11MonitorLineRegex ??= new(@"^[\s]*[\d]+[\s]*:[\s]*\+\*(?<Name>[\S]+)\s+(?<PixelWidth>\d+)/\d+x(?<PixelHeight>\d+)");
            using var process = Process.Start(new ProcessStartInfo()
            {
                Arguments = "--listactivemonitors",
                CreateNoWindow = true,
                FileName = "xrandr",
                RedirectStandardOutput = true,
                UseShellExecute = false,
            });
            if (process == null)
                return Array.Empty<X11MonitorInfo>();
            using var reader = process.StandardOutput;
            var line = reader.ReadLine();
            while (line != null)
            {
                var match = X11MonitorLineRegex!.Match(line);
                if (match.Success
                    && int.TryParse(match.Groups["PixelWidth"].Value, out var pixelWidth)
                    && int.TryParse(match.Groups["PixelHeight"].Value, out var pixelHeight))
                {
                    monitors.Add(new(match.Groups["Name"].Value, pixelWidth, pixelHeight));
                }
                line = reader.ReadLine();
            }
        }
        catch
        { }
        return monitors;
    }


    // Save custom screen scale factor.
    async Task SaveCustomScreenScaleFactorOnLinuxAsync()
    {
        if (double.IsFinite(CachedCustomScreenScaleFactor))
        {
            if (CustomScreenScaleFactorFilePath == null)
                this.Logger.LogError("Unknown path to save custom screen scale factor");
            else if (Math.Abs(CachedCustomScreenScaleFactor - 1) <= 0.1)
            {
                this.Logger.LogWarning("Reset custom screen scale factor");
                await Task.Run(() =>
                {
                    Global.RunWithoutError(() => System.IO.File.Delete(CustomScreenScaleFactorFilePath));
                });
            }
            else
            {
                this.Logger.LogWarning("Save custom screen scale factor");
                await Task.Run(() =>
                {
                    if (CarinaStudio.IO.File.TryOpenWrite(CustomScreenScaleFactorFilePath, 5000, out var stream) && stream != null)
                    {
                        try
                        {
                            using (stream)
                            {
                                using var writer = new StreamWriter(stream, Encoding.UTF8);
                                writer.Write(string.Format("{0:F2}", Math.Max(1, CachedCustomScreenScaleFactor)));
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogError(ex, "Failed to save custom screen scale factor");
                        }
                    }
                    else
                        this.Logger.LogError("Unable to open file to save custom screen scale factor");
                });
            }
        }
    }
}