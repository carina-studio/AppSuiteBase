using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using CarinaStudio.Collections;
using CarinaStudio.Controls;
using CarinaStudio.MacOS.AppKit;
using CarinaStudio.MacOS.CoreGraphics;
using CarinaStudio.MacOS.ObjectiveC;
using ObjCSelector = CarinaStudio.MacOS.ObjectiveC.Selector;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CarinaStudio.AppSuite;

partial class AppSuiteApplication
{
    // Application call-back for macOS.
    class AppSuiteAppDelegate : NSObject
    {
        // Static fields.
        static readonly Class? AppSuiteAppDelegateClass;

        // Fields.
        readonly AppSuiteApplication app;
        readonly NSObject? baseAppDelegate;

        // Static initializer.
        static AppSuiteAppDelegate()
        {
            if (Platform.IsNotMacOS)
                return;
            AppSuiteAppDelegateClass = Class.DefineClass(nameof(AppSuiteAppDelegate), cls =>
            {
                cls.DefineMethod<IntPtr, IntPtr>("application:openFiles:", (self, cmd, app, fileName) =>
                {
                    Current.macOSAppDelegate?.SendMessageToBaseAppDelegate(cmd, app, fileName);
                });
                cls.DefineMethod<IntPtr, IntPtr>("application:openURLs:", (self, cmd, app, urls) =>
                {
                    Current.macOSAppDelegate?.SendMessageToBaseAppDelegate(cmd, app, urls);
                });
                cls.DefineMethod<IntPtr>("applicationDidFinishLaunching:", (self, cmd, notification) =>
                {
                    Current.macOSAppDelegate?.SendMessageToBaseAppDelegate(cmd, notification);
                });
                cls.DefineMethod<IntPtr, NSApplication.TerminateReply>("applicationShouldTerminate:", (self, cmd, app) =>
                {
                    return Current.macOSAppDelegate.Let(it =>
                    {
                        if (it == null)
                            return NSApplication.TerminateReply.TerminateNow;
                        it.SendMessageToBaseAppDelegateWithResult(cmd, NSApplication.TerminateReply.TerminateNow, app);
                        if (it.app.shutdownSource == ShutdownSource.None)
                            it.app.shutdownSource = ShutdownSource.System;
                        if (!it.app.isShutdownStarted)
                        {
                            it.app.Logger.LogWarning("Shutting down has been requested by system");
                            it.app.Shutdown();
                        }
                        if (it.app.shutdownSource == ShutdownSource.Application)
                            return NSApplication.TerminateReply.TerminateNow;
                        return NSApplication.TerminateReply.TerminateLater;
                    });
                });
                cls.DefineMethod<IntPtr, bool, bool>("applicationShouldHandleReopen:hasVisibleWindows:", (self, cmd, app, flag) =>
                {
                    var asApp = AppSuiteApplication.Current;
                    asApp.macOSAppDelegate?.SendMessageToBaseAppDelegate(cmd, app, flag);
                    if (asApp.IsBackgroundMode)
                        asApp.OnTryExitingBackgroundMode();
                    return true;
                });
                cls.DefineMethod<IntPtr>("applicationWillFinishLaunching:", (self, cmd, notification) =>
                {
                    Current.macOSAppDelegate?.SendMessageToBaseAppDelegate(cmd, notification);
                });
            });
        }

        // Constructor.
        public AppSuiteAppDelegate(AppSuiteApplication app, NSObject? baseAppDelegate) : base(Initialize(AppSuiteAppDelegateClass!.Allocate()), true)
        { 
            this.app = app;
            this.baseAppDelegate = baseAppDelegate;
        }

        // Send message to base delegate.
        void SendMessageToBaseAppDelegate(ObjCSelector cmd, params object?[] args)
        {
            if (this.baseAppDelegate?.Class.HasMethod(cmd) == true)
                this.baseAppDelegate.SendMessage(cmd, args);
        }
        T SendMessageToBaseAppDelegateWithResult<T>(ObjCSelector cmd, T defaultResult, params object?[] args)
        {
            if (this.baseAppDelegate?.Class.HasMethod(cmd) == true)
            {
                try
                {
                    return this.baseAppDelegate.SendMessage<T>(cmd, args);
                }
                catch (Exception ex)
                {
                    AppSuiteApplication.Current.Logger.LogError(ex, "Error occurred while calling base delegate by '{cmdName}'", cmd.Name);
                }
            }
            return defaultResult;
        }
    }


    // Fields.
    AppSuiteAppDelegate? macOSAppDelegate;
    NSDockTile? macOSAppDockTile;
    SKBitmap? macOSAppDockTileOverlayBitmap;
    byte[]? macOSAppDockTileOverlayBitmapBuffer;
    GCHandle macOSAppDockTileOverlayBitmapBufferHandle;
    CGDataProvider? macOSAppDockTileOverlayBitmapBufferProvider;
    CGImage? macOSAppDockTileOverlayCGImage;
    NSImageView? macOSAppDockTileOverlayImageView;
    NSImage? macOSAppDockTileOverlayNSImage;
    ScheduledAction? updateMacOSAppDockTileProgressAction;


    // Apply theme mode on current application
    void ApplyThemeModeOnMacOS()
    {
        NSApplication.Current!.Appearance = this.EffectiveThemeMode switch
        {
            ThemeMode.Dark => NSAppearance.DarkAqua,
            _ => NSAppearance.Aqua,
        };
    }


    // Define extra styles by code for macOS.
    static void DefineExtraStylesForMacOS()
    {
        var clickHandler = new EventHandler<RoutedEventArgs>((sender, _) =>
            Avalonia.Controls.ToolTip.SetIsOpen((Avalonia.Controls.Control)sender.AsNonNull(), false));
        var templateAppliedHandler = new EventHandler<RoutedEventArgs>((sender, _) =>
        {
            if (sender is Avalonia.Controls.Control control)
            {
                control.GetObservable(Avalonia.Controls.ToolTip.IsOpenProperty).Subscribe(isOpen =>
                {
                    if (isOpen && control.FindAncestorOfType<Avalonia.Controls.Window>()?.IsActive == false)
                        Avalonia.Controls.ToolTip.SetIsOpen(control, false);
                });
            }
        });
        Avalonia.Controls.Button.ClickEvent.AddClassHandler(typeof(Avalonia.Controls.Button), clickHandler);
        Avalonia.Controls.Button.TemplateAppliedEvent.AddClassHandler(typeof(Avalonia.Controls.Button), templateAppliedHandler);
    }


    /// <summary>
    /// [Workaround] Ensure that tooltip of given control will be closed if its window is inactive.
    /// </summary>
    /// <param name="control">Control.</param>
    /// <remark>The method is designed for macOS.</remark>
#pragma warning disable CA1822
    public void EnsureClosingToolTipIfWindowIsInactive(Avalonia.Controls.Control control)
    {
        if (!Platform.IsMacOS || control is Avalonia.Controls.Button)
            return;
#pragma warning disable CA1806
        // ReSharper disable once ObjectCreationAsStatement
        new Controls.MacOSToolTipHelper(control);
#pragma warning restore CA1806
    }
#pragma warning restore CA1822


    // Get system theme mode on macOS.
    ThemeMode GetMacOSThemeMode()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo()
            {
                Arguments = "read -g AppleInterfaceStyle",
                CreateNoWindow = true,
                FileName = "defaults",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            });
            if (process != null)
            {
                var interfaceStyle = process.StandardOutput.ReadLine();
                return interfaceStyle == null
                    ? ThemeMode.Light
                    : interfaceStyle switch
                    {
                        "Dark" => ThemeMode.Dark,
                        _ => Global.Run(() =>
                        {
                            this.Logger.LogWarning("Unknown system theme mode on macOS: {interfaceStyle}", interfaceStyle);
                            return this.FallbackThemeMode;
                        }),
                    };
            }
            else
            {
                this.Logger.LogError("Unable to start 'defaults' to check system theme mode on macOS");
                return this.FallbackThemeMode;
            }
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Unable to check system theme mode on macOS");
            return this.FallbackThemeMode;
        }
    }


    // Called when IsActive of main window changed on macOS.
    void OnMainWindowActivationChangedOnMacOS()
    {
        this.UpdateCultureInfo(true);
        this.UpdateSystemThemeMode(true);
        this.updateMacOSAppDockTileProgressAction?.Schedule();
    }


    // Setup NSApplication on macOS.
    void SetupMacOSApp()
    {
        NSApplication.Current?.Let(app =>
        {
            this.macOSAppDelegate = new(this, app.Delegate);
            app.Delegate = this.macOSAppDelegate;
        });
        this.updateMacOSAppDockTileProgressAction = new(this.UpdateMacOSAppDockTileProgress);
    }


    // Setup AppBuilder for macOS.
    static void SetupMacOSAppBuilder(AppBuilder builder)
    {
        builder.With(new MacOSPlatformOptions()
        {
            DisableDefaultApplicationMenuItems = true,
        });

        /* [Workaround]
         * Reduce UI frame rate to lower the CPU usage
         * Please refer to https://github.com/AvaloniaUI/Avalonia/issues/4500
         */
        var initWindowingSubSystem = builder.WindowingSubsystemInitializer;
        builder.UseWindowingSubsystem(() =>
        {
            initWindowingSubSystem?.Invoke();
            AvaloniaLocator.CurrentMutable.Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(30));
        });
    }


    // Perform necessary setup for dock tile on macOS.
    void SetupMacOSAppDockTile()
    {
        // check state
        if (Platform.IsNotMacOS || this.macOSAppDockTile != null)
            return;
        
        // get application
        var app = NSApplication.Shared;

        // create NSView for dock tile
        var dockTileSize = default(Size);
        this.macOSAppDockTile = app.DockTile.Also(dockTile =>
        {
            // prepare icon
            var iconImage = app.ApplicationIconImage;
            if (Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName) == "dotnet")
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CarinaStudio.AppSuite.Resources.AppIcon_macOS_256.png");
                if (stream != null)
                    iconImage = NSImage.FromStream(stream);
            }

            // setup dock tile
            dockTileSize = dockTile.Size.Let(it => new Size(it.Width, it.Height));
            dockTile.ContentView = new NSImageView(new(0, 0, dockTileSize.Width, dockTileSize.Height)).Also(imageView =>
            {
                imageView.Image = iconImage;
                imageView.ImageAlignment = NSImageAlignment.Bottom;
                imageView.ImageScaling = NSImageScaling.ProportionallyUpOrDown;
                this.macOSAppDockTileOverlayImageView = new(new(0, 0, dockTileSize.Width, dockTileSize.Height));
                imageView.AddSubView(this.macOSAppDockTileOverlayImageView);
            });
            dockTile.Display();
        });

        // create overlay bitmap
        this.macOSAppDockTileOverlayBitmap = new(
            (int)dockTileSize.Width, 
            (int)dockTileSize.Height
        );
        this.macOSAppDockTileOverlayBitmapBuffer = new byte[this.macOSAppDockTileOverlayBitmap.ByteCount];
        this.macOSAppDockTileOverlayBitmapBufferHandle = GCHandle.Alloc(this.macOSAppDockTileOverlayBitmapBuffer, GCHandleType.Pinned);
        this.macOSAppDockTileOverlayBitmap.InstallPixels(new(
            this.macOSAppDockTileOverlayBitmap.Width, 
            this.macOSAppDockTileOverlayBitmap.Height, 
            SKColorType.Rgba8888, 
            SKAlphaType.Unpremul, 
            SKColorSpace.CreateSrgb()
        ), this.macOSAppDockTileOverlayBitmapBufferHandle.AddrOfPinnedObject());
        this.macOSAppDockTileOverlayBitmapBufferProvider = new(this.macOSAppDockTileOverlayBitmapBuffer);
    }


    // Perform necessary actions to shutdown application on macOS.
    void ShutdownMacOSApp()
    {
        this.Logger.LogWarning("Shut down");
        switch (this.shutdownSource)
        {
            case ShutdownSource.Application:
            {
                var selector = ObjCSelector.FromName("terminate:");
                NSApplication.Current?.SendMessage(selector, IntPtr.Zero);
                break;
            }
            case ShutdownSource.System:
            {
                var selector = ObjCSelector.FromName("replyToApplicationShouldTerminate:");
                NSApplication.Current?.SendMessage(selector, true);
                break;
            }
            default:
                throw new NotSupportedException($"Unknown source of shutting down: {this.shutdownSource}.");
        }
    }


    // Update dock tile on macOS.
    void UpdateMacOSAppDockTileProgress()
    {
        this.SetupMacOSAppDockTile();
        var window = this.mainWindows.IsNotEmpty() ? this.LatestActiveMainWindow as Controls.Window : null;
        var state = window?.TaskbarIconProgressState ?? Controls.TaskbarIconProgressState.None;
        switch (state)
        {
            case Controls.TaskbarIconProgressState.Indeterminate:
                // Unsupported
                goto default;
            case Controls.TaskbarIconProgressState.Normal:
            case Controls.TaskbarIconProgressState.Error:
            case Controls.TaskbarIconProgressState.Paused:
                // update overlay bitmap
                new SKCanvas(this.macOSAppDockTileOverlayBitmap).Use(canvas =>
                {
                    // get info of dock tile
                    var dockTileWidth = this.macOSAppDockTileOverlayBitmap!.Width;
                    var dockTileHeight = this.macOSAppDockTileOverlayBitmap.Height;
                    var progressBackgroundColor = this.FindResourceOrDefault("Color/AppSuiteApplication.MacOSDockTile.Progress.Background", Colors.Black);
                    var progressForegroundColor = state switch
                    {
                        Controls.TaskbarIconProgressState.Error => this.FindResourceOrDefault("Color/AppSuiteApplication.MacOSDockTile.Progress.Foreground.Error", Colors.Red),
                        Controls.TaskbarIconProgressState.Paused => this.FindResourceOrDefault("Color/AppSuiteApplication.MacOSDockTile.Progress.Foreground.Paused", Colors.Yellow),
                        _ => this.FindResourceOrDefault("Color/AppSuiteApplication.MacOSDockTile.Progress.Foreground", Colors.LightGray),
                    };

                    // prepare progress background
                    using var progressBackgroundPaint = new SKPaint()
                    {
                        Color = new(progressBackgroundColor.R, progressBackgroundColor.G, progressBackgroundColor.B, progressBackgroundColor.A),
                        IsAntialias = true,
                        Style = SKPaintStyle.Fill,
                    };
                    var progressBackgroundWidth = (int)(dockTileWidth * 0.65 + 0.5);
                    var progressBackgroundHeight = (int)(dockTileHeight * 0.1 + 0.5);
                    var progressBackgroundLeft = (dockTileWidth - progressBackgroundWidth) >> 1;
                    var progressBackgroundTop = (int)(dockTileHeight * 0.7 + 0.5);
                    var progressBackgroundRect = new SKRect(progressBackgroundLeft, progressBackgroundTop, progressBackgroundLeft + progressBackgroundWidth, progressBackgroundTop + progressBackgroundHeight);

                    // prepare progress foreground
                    var progress = window?.TaskbarIconProgress ?? 0;
                    using var progressForegroundPaint = new SKPaint()
                    {
                        Color = new(progressForegroundColor.R, progressForegroundColor.G, progressForegroundColor.B, progressForegroundColor.A),
                        IsAntialias = true,
                        Style = SKPaintStyle.Fill,
                    };
                    var progressBorderWidth = (int)(progressBackgroundHeight * 0.15 + 0.5);
                    var progressForegroundWidth = (int)((progressBackgroundWidth - progressBorderWidth - progressBorderWidth) * progress + 0.5);
                    var progressForegroundHeight = progressBackgroundHeight - progressBorderWidth - progressBorderWidth;
                    var progressForegroundLeft = progressBackgroundLeft + progressBorderWidth;
                    var progressForegroundTop = progressBackgroundTop + progressBorderWidth;
                    var progressForegroundRect = new SKRect(progressForegroundLeft, progressForegroundTop, progressForegroundLeft + progressForegroundWidth, progressForegroundTop + progressForegroundHeight);

                    // clear buffer
                    canvas.Clear(new(0, 0, 0, 0));

                    // draw progress
                    if (progress >= 0.001)
                    {
                        canvas.DrawRoundRect(new(progressBackgroundRect, progressBackgroundHeight / 2f), progressBackgroundPaint);
                        canvas.DrawRoundRect(new(progressForegroundRect, progressForegroundHeight / 2f), progressForegroundPaint);
                    }

                    // draw dot on top-right
                    if (state != Controls.TaskbarIconProgressState.Normal)
                    {
                        var centerX = (int)(dockTileWidth * 0.87 + 0.5);
                        var centerY = (int)(dockTileHeight * 0.13 + 0.5);
                        var radius = (int)(dockTileWidth * 0.1 + 0.5);
                        var borderWidth = (int)(dockTileWidth * 0.015 + 0.5);
                        canvas.DrawCircle(centerX, centerY, radius + borderWidth, progressBackgroundPaint);
                        canvas.DrawCircle(centerX, centerY, radius, progressForegroundPaint);
                    }
                });

                // create new image for overlay
                this.macOSAppDockTileOverlayImageView!.Image = null;
                this.macOSAppDockTileOverlayNSImage?.Release();
                this.macOSAppDockTileOverlayCGImage?.Release();
                this.macOSAppDockTileOverlayCGImage = new CGImage(
                    this.macOSAppDockTileOverlayBitmap!.Width, 
                    this.macOSAppDockTileOverlayBitmap.Height, 
                    CGImagePixelFormatInfo.Packed, 
                    8, 
                    CGImageByteOrderInfo.ByteOrderDefault, 
                    this.macOSAppDockTileOverlayBitmap.RowBytes, 
                    CGImageAlphaInfo.AlphaLast, 
                    this.macOSAppDockTileOverlayBitmapBufferProvider!,
                    CGColorSpace.SRGB
                );
                
                // show overlay image
                this.macOSAppDockTileOverlayNSImage = NSImage.FromCGImage(this.macOSAppDockTileOverlayCGImage);
                this.macOSAppDockTileOverlayImageView!.Image = this.macOSAppDockTileOverlayNSImage;
                break;
            default:
                if (this.macOSAppDockTileOverlayNSImage != null)
                {
                    this.macOSAppDockTileOverlayImageView!.Image = null;
                    this.macOSAppDockTileOverlayNSImage.Release();
                    this.macOSAppDockTileOverlayNSImage = null;
                }
                if (this.macOSAppDockTileOverlayCGImage != null)
                {
                    this.macOSAppDockTileOverlayCGImage.Release();
                    this.macOSAppDockTileOverlayCGImage = null;
                }
                this.macOSAppDockTile?.Let(it =>
                    it.BadgeLabel = null);
                break;
        }
        this.macOSAppDockTile?.Display();
        this.SynchronizationContext.PostDelayed(() => // [Workaround] Make sure that dock tile redraws as expected
            this.macOSAppDockTile?.Display(), 100);
    }


    // Update progress state of dock tile on macOS.
    internal void UpdateMacOSDockTileProgressState() =>
        this.updateMacOSAppDockTileProgressAction?.Schedule();
}