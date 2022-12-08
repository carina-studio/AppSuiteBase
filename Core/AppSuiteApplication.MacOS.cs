using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using CarinaStudio.Collections;
using CarinaStudio.MacOS.AppKit;
using CarinaStudio.MacOS.ObjectiveC;
using ObjCSelector = CarinaStudio.MacOS.ObjectiveC.Selector;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

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
                    AppSuiteApplication.Current.macOSAppDelegate?.SendMessageToBaseAppDelegate(cmd, app, fileName);
                });
                cls.DefineMethod<IntPtr, IntPtr>("application:openURLs:", (self, cmd, app, urls) =>
                {
                    AppSuiteApplication.Current.macOSAppDelegate?.SendMessageToBaseAppDelegate(cmd, app, urls);
                });
                cls.DefineMethod<IntPtr>("applicationDidFinishLaunching:", (self, cmd, notification) =>
                {
                    AppSuiteApplication.Current.macOSAppDelegate?.SendMessageToBaseAppDelegate(cmd, notification);
                });
                cls.DefineMethod<IntPtr, NSApplication.TerminateReply>("applicationShouldTerminate:", (self, cmd, app) =>
                {
                    return AppSuiteApplication.Current.macOSAppDelegate.Let(it =>
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
                    AppSuiteApplication.Current.macOSAppDelegate?.SendMessageToBaseAppDelegate(cmd, notification);
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
            if (this.baseAppDelegate?.Class?.HasMethod(cmd) == true)
                this.baseAppDelegate.SendMessage(cmd, args);
        }
        T SendMessageToBaseAppDelegateWithResult<T>(ObjCSelector cmd, T defaultResult, params object?[] args)
        {
            if (this.baseAppDelegate?.Class?.HasMethod(cmd) == true)
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
    NSProgressIndicator? macOSAppDockTileProgressBar;
    ScheduledAction? updateMacOSAppDockTileProgressAction;


    // Define extra styles by code for macOS.
    void DefineExtraStylesForMacOS()
    {
        var clickHandler = new EventHandler<RoutedEventArgs>((sender, e) =>
            Avalonia.Controls.ToolTip.SetIsOpen((Avalonia.Controls.Control)sender.AsNonNull(), false));
        var templateAppliedHandler = new EventHandler<RoutedEventArgs>((sender, e) =>
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
        if (Platform.IsNotMacOS || this.macOSAppDockTile != null)
            return;
        var app = NSApplication.Shared;
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
            var dockTileSize = dockTile.Size;
            dockTile.ContentView = new NSImageView(new(default, dockTileSize)).Also(imageView =>
            {
                imageView.Image = iconImage;
                imageView.ImageAlignment = NSImageAlignment.Bottom;
                imageView.ImageScaling = NSImageScaling.ProportionallyUpOrDown;
                var progressBarWidth = (dockTileSize.Width * 0.58);
                var progressBarBottom = dockTileSize.Height * 0.11;
                this.macOSAppDockTileProgressBar = new NSProgressIndicator(new((dockTileSize.Width - progressBarWidth) / 2, progressBarBottom, progressBarWidth, 20)).Also(it =>
                {
                    it.IsHidden = true;
                    it.IsIndeterminate = false;
                    imageView.AddSubView(it);
                });
            });
            dockTile.Display();
        });
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
                // Usupported
                goto default;
            case Controls.TaskbarIconProgressState.Normal:
            case Controls.TaskbarIconProgressState.Error:
            case Controls.TaskbarIconProgressState.Paused:
                this.macOSAppDockTileProgressBar?.Let(it =>
                {
                    var value = it.MaxValue * (window?.TaskbarIconProgress ?? 0);
                    it.IsHidden = state != Controls.TaskbarIconProgressState.Normal && value < 0.1;
                    it.DoubleValue = value;
                    this.macOSAppDockTile?.Let(it =>
                    {
                        it.BadgeLabel = state switch
                        {
                            Controls.TaskbarIconProgressState.Error => "✖",
                            //Controls.TaskbarIconProgressState.Paused => "‖",
                            _ => null,
                        };
                    });
                });
                break;
            default:
                this.macOSAppDockTileProgressBar?.Let(it =>
                {
                    it.IsHidden = true;
                    it.DoubleValue = 0;
                });
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