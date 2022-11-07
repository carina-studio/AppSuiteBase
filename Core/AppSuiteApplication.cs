//#define APPLY_CONTROL_BRUSH_ANIMATIONS
//#define APPLY_ITEM_BRUSH_ANIMATIONS

using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.VisualTree;
#if APPLY_CONTROL_BRUSH_ANIMATIONS || APPLY_ITEM_BRUSH_ANIMATIONS
using CarinaStudio.AppSuite.Animation;
#endif
using CarinaStudio.AppSuite.Net;
using CarinaStudio.AppSuite.Product;
using CarinaStudio.AppSuite.Scripting;
using CarinaStudio.AutoUpdate;
using CarinaStudio.AutoUpdate.Resolvers;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.MacOS.AppKit;
using CarinaStudio.MacOS.ObjectiveC;
using ObjCSelector = CarinaStudio.MacOS.ObjectiveC.Selector;
using CarinaStudio.Net;
using CarinaStudio.Threading;
using CarinaStudio.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NLog;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Base implementation of <see cref="IAppSuiteApplication"/>.
    /// </summary>
    public abstract class AppSuiteApplication : Application, IAppSuiteApplication
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
                            if (!it.app.isShutdownStarted)
                            {
                                it.app.Logger.LogWarning("Shutting down has been requested by system");
                                it.app.Shutdown();
                            }
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


        // Implementation of Configuration.
        class ConfigurationImpl : PersistentSettings
        {
            // Constructor.
            public ConfigurationImpl() : base(JsonSettingsSerializer.Default)
            { }

            // Implementations.
            protected override void OnUpgrade(int oldVersion)
            { }
            public override int Version { get; } = 1;
        }


        // Token of custom resources.
        class CustomResourceToken : IDisposable
        {
            // Fields.
            readonly AppSuiteApplication app;
            readonly Avalonia.Controls.IResourceProvider resource;

            // Constructor.
            public CustomResourceToken(AppSuiteApplication app, Avalonia.Controls.IResourceProvider resources)
            {
                this.app = app;
                this.resource = resources;
            }

            // Dispose.
            public void Dispose() =>
                this.app.RemoveCustomResource(this.resource);
        }


        // Token of custom style.
        class CustomStyleToken : IDisposable
        {
            // Fields.
            readonly AppSuiteApplication app;
            readonly IStyle style;

            // Constructor.
            public CustomStyleToken(AppSuiteApplication app, IStyle style)
            {
                this.app = app;
                this.style = style;
            }

            // Dispose.
            public void Dispose() =>
                this.app.RemoveCustomStyle(this.style);
        }


        // Holder of main window.
        class MainWindowHolder
        {
            // Fields.
            public readonly LinkedListNode<MainWindowHolder> ActiveListNode;
            public bool IsRestartingRequested;
            public readonly ViewModel ViewModel;
            public readonly Window? Window;
            public Action<Window>? WindowCreatedAction;

            // Constructor.
            public MainWindowHolder(ViewModel viewModel, Window? window, Action<Window>? windowCreatedAction)
            {
                this.ActiveListNode = new LinkedListNode<MainWindowHolder>(this);
                this.ViewModel = viewModel;
                this.Window = window;
                this.WindowCreatedAction = windowCreatedAction;
            }
        }


        // Implementation of PersistentState.
        class PersistentStateImpl : PersistentSettings
        {
            // Fields.
            readonly AppSuiteApplication app;

            // Constructor.
            public PersistentStateImpl(AppSuiteApplication app) : base(JsonSettingsSerializer.Default)
            {
                this.app = app;
            }

            // Implementations.
            protected override void OnUpgrade(int oldVersion) => this.app.OnUpgradePersistentState(this, oldVersion, this.Version);
            public override int Version { get => this.app.PersistentStateVersion; }
        }


        // Implementation of Settings.
        class SettingsImpl : PersistentSettings
        {
            // Fields.
            readonly AppSuiteApplication app;

            // Constructor.
            public SettingsImpl(AppSuiteApplication app) : base(JsonSettingsSerializer.Default)
            {
                this.app = app;
            }

            // Implementations.
            protected override void OnUpgrade(int oldVersion) => this.app.OnUpgradeSettings(this, oldVersion, this.Version);
            public override int Version { get => this.app.SettingsVersion; }
        }


        /// <summary>
        /// Argument indicates to enable debug mode.
        /// </summary>
        public const string DebugArgument = "-debug";
        /// <summary>
        /// Argument indicates to restore main windows.
        /// </summary>
        public const string RestoreMainWindowsArgument = "-restore-main-windows";


        // Constants.
        const string DebugModeRequestedKey = "IsDebugModeRequested";
        const string RestoreMainWindowsRequestedKey = "IsRestoringMainWindowsRequested";
        const int MinSplashWindowDuration = 2000;
        const int SplashWindowShowingDuration = 1500;
        const int SplashWindowLoadingThemeDuration = 400;


        // Static fields.
        static readonly SettingKey<string> AgreedPrivacyPolicyVersionKey = new("AgreedPrivacyPolicyVersion", "");
        static readonly SettingKey<string> AgreedUserAgreementVersionKey = new("AgreedUserAgreementVersion", "");
        static readonly string AppDirectoryPath = Global.Run(() =>
        {
            var mainModule = Process.GetCurrentProcess().MainModule;
            if (mainModule != null && Path.GetFileNameWithoutExtension(mainModule.FileName) != "dotnet")
                return Path.GetDirectoryName(mainModule.FileName) ?? "";
#pragma warning disable SYSLIB0044
            var codeBase = System.Reflection.Assembly.GetEntryAssembly()?.GetName()?.CodeBase;
#pragma warning restore SYSLIB0044
            if (codeBase != null && codeBase.StartsWith("file://") && codeBase.Length > 7)
            {
                if (Platform.IsWindows)
                    return Path.GetDirectoryName(codeBase[8..^0].Replace('/', '\\')) ?? Environment.CurrentDirectory;
                return Path.GetDirectoryName(codeBase[7..^0]) ?? Environment.CurrentDirectory;
            }
            return Environment.CurrentDirectory;
        });
        static double CachedCustomScreenScaleFactor = double.NaN;
        static readonly string? CustomScreenScaleFactorFilePath = Global.Run(() =>
        {
            if (!Platform.IsLinux)
                return null;
            return Path.Combine(AppDirectoryPath, "ScreenScaleFactor");
        });
        static readonly SettingKey<bool> IsAcceptNonStableApplicationUpdateInitKey = new("IsAcceptNonStableApplicationUpdateInitialized", false);
        static readonly SettingKey<int> LogOutputTargetPortKey = new("LogOutputTargetPort");
        static readonly SettingKey<byte[]> MainWindowViewModelStatesKey = new("MainWindowViewModelStates", Array.Empty<byte>());
        static readonly Regex X11MonitorLineRegex = new("^[\\s]*[\\d]+[\\s]*\\:[\\s]*\\+\\*(?<Name>[^\\s]+)");


        // Fields.
        Avalonia.Controls.ResourceDictionary? accentColorResources;
        readonly LinkedList<MainWindowHolder> activeMainWindowList = new();
        Avalonia.Themes.Fluent.FluentTheme? baseTheme;
        readonly bool canUseWindows10Features = Environment.OSVersion.Version.Let(version =>
        {
            if (!Platform.IsWindows)
                return false;
            return version.Major > 10 || (version.Major == 10 && version.Build >= 17763);
        });
        ScheduledAction? checkUpdateInfoAction;
        ISettings? configuration;
        readonly string configurationFilePath;
        readonly long creationTime;
        CultureInfo cultureInfo = CultureInfo.GetCultureInfo("en-US");
        readonly Styles extraStyles = new();
        long frameworkInitializedTime;
        HardwareInfo? hardwareInfo;
        bool isActivatingProVersion;
        bool isCompactStyles;
        bool isNetworkConnForProductActivationNotified;
        bool isReActivatingProVersion;
		bool isReActivatingProVersionNeeded;
        bool isRestartAsAdminRequested;
        bool isRestartingMainWindowsRequested;
        bool isRestartRequested;
        bool isShutdownStarted;
        Task? loadingInitPersistentStateTask;
        Task? loadingInitSettingsTask;
        int logOutputTargetPort;
        AppSuiteAppDelegate? macOSAppDelegate;
        NSDockTile? macOSAppDockTile;
        NSProgressIndicator? macOSAppDockTileProgressBar;
        readonly Dictionary<Window, MainWindowHolder> mainWindowHolders = new();
        readonly ObservableList<Window> mainWindows = new();
        readonly CancellationTokenSource multiInstancesServerCancellationTokenSource = new();
        NamedPipeServerStream? multiInstancesServerStream;
        string multiInstancesServerStreamName = "";
        ScheduledAction? notifyNetworkConnForProductActivationAction;
        readonly List<MainWindowHolder> pendingMainWindowHolders = new();
        PersistentStateImpl? persistentState;
        readonly string persistentStateFilePath;
        long prepareStartingTime;
        ProcessInfo? processInfo;
        IDisposable? processInfoHfUpdateToken;
        IProductManager? productManager;
        ScheduledAction? reActivateProVersionAction;
        string? restartArgs;
        SettingsImpl? settings;
        readonly string settingsFilePath;
        Controls.SplashWindowImpl? splashWindow;
        long splashWindowShownTime;
        readonly Stopwatch stopWatch = new Stopwatch().Also(it => it.Start());
        Avalonia.Controls.ResourceDictionary? stringResource;
        CultureInfo? stringResourceCulture;
        IStyle? styles;
        ThemeMode stylesThemeMode = ThemeMode.System;
        ThemeMode systemThemeMode = ThemeMode.Dark;
        readonly object? uiColorTypeBackground;
        readonly object? uiSettings;
        Delegate? uiSettingsColorValueChangedHandler;
        readonly MethodInfo? uiSettingsGetColorValueMethod;
        readonly Type? uiSettingsType;
        ScheduledAction? updateMacOSAppDockTileProgressAction;
        readonly Dictionary<Avalonia.Controls.Window, List<IDisposable>> windowObserverTokens = new();
        readonly ObservableList<Avalonia.Controls.Window> windows = new();
        readonly PropertyInfo? windowsColorBProperty;
        readonly PropertyInfo? windowsColorGProperty;
        readonly PropertyInfo? windowsColorRProperty;
        readonly Type? windowsColorType;


        /// <summary>
        /// Initialize new <see cref="AppSuiteApplication"/> instance.
        /// </summary>
        protected AppSuiteApplication()
        {
            // get time for performance check
            this.creationTime = this.stopWatch.ElapsedMilliseconds;

            /* 
             * Prevent using Avalonia with version >= 0.11.0 because some control styles are not compatible.
             * Need to update control styles if upgrading to Avalonia with version >= 0.11.0:
             * - Use Foreground property for ContentPresenter instead of TextBlock.Foreground.
             */
            typeof(AvaloniaObject).Assembly.GetName().Version?.Let(version =>
            {
                if (version.Major > 0 || version.Minor > 10)
                    throw new NotSupportedException($"Incompatible Avalonia version: {version}");
            });

            // create logger
            LogManager.Configuration = new NLog.Config.LoggingConfiguration().Also(it =>
            {
                var fileTarget = new NLog.Targets.FileTarget("file")
                {
                    ArchiveAboveSize = 10L << 20, // 10 MB per log file
                    ArchiveFileKind = NLog.Targets.FilePathKind.Absolute,
                    ArchiveFileName = Path.Combine(this.RootPrivateDirectoryPath, "Log", "log.txt"),
                    ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Sequence,
                    FileName = Path.Combine(this.RootPrivateDirectoryPath, "Log", "log.txt"),
                    Layout = "${longdate} ${pad:padding=-5:inner=${processid}} ${pad:padding=-4:inner=${threadid}} ${pad:padding=-5:inner=${level:uppercase=true}} ${logger:shortName=true}: ${message} ${all-event-properties} ${exception:format=tostring}",
                    MaxArchiveFiles = 10,
                };
                var rule = new NLog.Config.LoggingRule("logToFile").Also(rule =>
                {
                    rule.LoggerNamePattern = "*";
                    rule.SetLoggingLevels(NLog.LogLevel.Trace, NLog.LogLevel.Error);
                    rule.Targets.Add(fileTarget);
                });
                it.AddTarget(fileTarget);
                it.LoggingRules.Add(rule);
            });
            this.LoggerFactory = new LoggerFactory(new ILoggerProvider[] { this.OnCreateLoggerProvider() });
            this.Logger = this.LoggerFactory.CreateLogger(this.GetType().Name);
            this.Logger.LogDebug("Created");

            // setup global exception handler
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                var exceptionObj = e.ExceptionObject;
                if (exceptionObj is Exception exception)
                    this.Logger.LogError(exception, "***** Unhandled application exception *****");
                else
                    this.Logger.LogError("***** Unhandled application exception ***** {exceptionObj}", exceptionObj);
            };

            // get file paths
            this.configurationFilePath = Path.Combine(this.RootPrivateDirectoryPath, "ConfigOverride.json");
            this.persistentStateFilePath = Path.Combine(this.RootPrivateDirectoryPath, "PersistentState.json");
            this.settingsFilePath = Path.Combine(this.RootPrivateDirectoryPath, "Settings.json");

            // create UISettings to monitor system UI change
            if (this.canUseWindows10Features)
            {
                var assembly = this.WindowsSdkAssembly;
                if (assembly != null)
                {
                    this.uiSettingsType = assembly.GetType("Windows.UI.ViewManagement.UISettings");
                    this.windowsColorType = assembly.GetType("Windows.UI.Color");
                }
                else
                    this.Logger.LogWarning("Cannot find Windows SDK assembly");
                var uiColorType = assembly?.GetType("Windows.UI.ViewManagement.UIColorType");
                if (this.uiSettingsType != null 
                    && this.windowsColorType != null 
                    && uiColorType != null)
                {
                    this.uiSettingsGetColorValueMethod = this.uiSettingsType.GetMethod("GetColorValue", new Type[] { uiColorType });
                    this.windowsColorRProperty = this.windowsColorType.GetProperty("R");
                    this.windowsColorGProperty = this.windowsColorType.GetProperty("G");
                    this.windowsColorBProperty = this.windowsColorType.GetProperty("B");
                    if (this.uiSettingsGetColorValueMethod != null 
                        && this.windowsColorRProperty != null
                        && this.windowsColorGProperty != null
                        && this.windowsColorBProperty != null)
                    {
                        try
                        {
                            this.uiSettings = Activator.CreateInstance(this.uiSettingsType);
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogError(ex, "Failed to create UISettings for Windows 10");
                        }
                    }
                    else
                    {
                        if (this.uiSettingsGetColorValueMethod == null)
                            this.Logger.LogError("Cannot find UISettings.GetColorValue() for Windows 10 to monitor theme change");
                        if (this.windowsColorRProperty == null)
                            this.Logger.LogError("Cannot find Color.R for Windows 10 to monitor theme change");
                        if (this.windowsColorGProperty == null)
                            this.Logger.LogError("Cannot find Color.G for Windows 10 to monitor theme change");
                        if (this.windowsColorBProperty == null)
                            this.Logger.LogError("Cannot find Color.B for Windows 10 to monitor theme change");
                    }
                }
                else
                {
                    if (this.uiSettingsType == null)
                        this.Logger.LogWarning("Cannot find UISettings for Windows 10 to monitor theme change");
                    if (this.windowsColorType == null)
                        this.Logger.LogWarning("Cannot find Color for Windows 10 to monitor theme change");
                    if (uiColorType == null)
                        this.Logger.LogWarning("Cannot find UIColorType for Windows 10 to monitor theme change");
                }
                if (uiColorType != null && !Enum.TryParse(uiColorType, "Background", false, out this.uiColorTypeBackground))
                    this.Logger.LogError("Unable to get UIColorType.Background for Windows 10 to monitor theme change");
            }

            // check whether process is running as admin or not
            if (Platform.IsWindows)
            {
#pragma warning disable CA1416
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                this.IsRunningAsAdministrator = principal.IsInRole(WindowsBuiltInRole.Administrator);
#pragma warning restore CA1416
            }
            if (this.IsRunningAsAdministrator)
                this.Logger.LogWarning("Application is running as administrator/superuser");

            // setup properties
            this.MainWindows = ListExtensions.AsReadOnly(this.mainWindows);
            this.Windows = ListExtensions.AsReadOnly(this.windows);

            // setup default culture
            CultureInfo.CurrentCulture = this.cultureInfo;
            CultureInfo.CurrentUICulture = this.cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = this.cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = this.cultureInfo;
        }


        /// <summary>
		/// Activate Pro version.
		/// </summary>
		public void ActivateProVersion() =>
			_ = this.ActivateProVersionAsync(this.LatestActiveMainWindow);


        /// <inheritdoc/>
        public async Task ActivateProVersionAsync(Avalonia.Controls.Window? window)
		{
			this.VerifyAccess();
			if (this.isActivatingProVersion)
				return;
            var productId = this.ProVersionProductId;
            if (productId == null)
                return;
			this.isActivatingProVersion = true;
			await this.ProductManager.ActivateProductAsync(productId, window);
			this.isActivatingProVersion = false;
		}


        /// <inheritdoc/>
        public IDisposable AddCustomResource(Avalonia.Controls.IResourceProvider resource)
        {
            this.VerifyAccess();
            this.Resources.MergedDictionaries.Add(resource);
            return new CustomResourceToken(this, resource);
        }


        /// <inheritdoc/>
        public IDisposable AddCustomStyle(IStyle style) 
        {
            this.VerifyAccess();
            this.Styles.Add(style);
            return new CustomStyleToken(this, style);
        }


        /// <inheritdoc/>
        public void AgreePrivacyPolicy()
        {
            // check state
            this.VerifyAccess();
            if (this.IsPrivacyPolicyAgreed)
                return;
            if (this.IsShutdownStarted)
            {
                this.Logger.LogWarning("Cannot change Privacy Policy state when shutting down");
                return;
            }

            this.Logger.LogWarning("User agreed the Privacy Policy");

            // update state and save
            this.PrivacyPolicyVersion?.Let(version =>
            {
                this.PersistentState.SetValue<string>(AgreedPrivacyPolicyVersionKey, version.ToString());
                _ = this.SavePersistentStateAsync();
            });
            this.IsPrivacyPolicyAgreed = true;
            this.OnPropertyChanged(nameof(IsPrivacyPolicyAgreed));
            if (!this.IsPrivacyPolicyAgreedBefore)
            {
                this.IsPrivacyPolicyAgreedBefore = true;
                this.OnPropertyChanged(nameof(IsPrivacyPolicyAgreedBefore));
            }
        }


        /// <inheritdoc/>
        public void AgreeUserAgreement()
        {
            // check state
            this.VerifyAccess();
            if (this.IsUserAgreementAgreed)
                return;
            if (this.IsShutdownStarted)
            {
                this.Logger.LogWarning("Cannot change User Agreement state when shutting down");
                return;
            }

            this.Logger.LogWarning("User agreed the User Agreement");

            // update state and save
            this.UserAgreementVersion?.Let(version =>
            {
                this.PersistentState.SetValue<string>(AgreedUserAgreementVersionKey, version.ToString());
                _ = this.SavePersistentStateAsync();
            });
            this.IsUserAgreementAgreed = true;
            this.OnPropertyChanged(nameof(IsUserAgreementAgreed));
            if (!this.IsUserAgreementAgreedBefore)
            {
                this.IsUserAgreementAgreedBefore = true;
                this.OnPropertyChanged(nameof(IsUserAgreementAgreedBefore));
            }
        }


        /// <summary>
        /// Check whether multiple main windows are allowed or not.
        /// </summary>
        protected virtual bool AllowMultipleMainWindows { get => false; }


        // Apply given screen scale factor for Linux.
        static void ApplyScreenScaleFactor(double factor)
        {
            // check state
            if (!Platform.IsLinux || !double.IsFinite(factor) || factor < 1)
                return;
            if (Math.Abs(factor - 1) < 0.01)
                return;
            
            // get all screens
            var screenNames = new List<string>();
            try
            {
                using var process = Process.Start(new ProcessStartInfo()
                {
                    Arguments = "--listactivemonitors",
                    CreateNoWindow = true,
                    FileName = "xrandr",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                });
                if (process == null)
                    return;
                using var reader = process.StandardOutput;
                var line = reader.ReadLine();
                while (line != null)
                {
                    var match = X11MonitorLineRegex.Match(line);
                    if (match.Success)
                        screenNames.Add(match.Groups["Name"].Value);
                    line = reader.ReadLine();
                }
            }
            catch
            { }
            if (screenNames.IsEmpty())
                return;
            
            // set environment variable
            var valueBuilder = new StringBuilder();
            foreach (var screenName in screenNames)
            {
                if (valueBuilder.Length > 0)
                    valueBuilder.Append(';');
                valueBuilder.Append(screenName);
                valueBuilder.Append('=');
                valueBuilder.AppendFormat("{0:F1}", factor);
            }
            Environment.SetEnvironmentVariable("AVALONIA_SCREEN_SCALE_FACTORS", valueBuilder.ToString());
        }


        /// <summary>
        /// Build application.
        /// </summary>
        /// <param name="setupAction">Action to do further setup.</param>
        /// <typeparam name="TApp">Type of application.</typeparam>
        /// <returns></returns>
        protected static AppBuilder BuildApplication<TApp>(Action<AppBuilder>? setupAction = null) where TApp: AppSuiteApplication, new()
        {
            // apply screen scale factor
            if (Platform.IsLinux)
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
                    ApplyScreenScaleFactor(CachedCustomScreenScaleFactor);
                }
            }

            CultureInfo cultureInfo = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            // build application
            return AppBuilder.Configure<TApp>()
                .UsePlatformDetect()
                .LogToTrace().Also(it =>
                {
                    if (Platform.IsWindows11OrAbove)
                    {
                        // enable Mica effect
                        it.With(new Win32PlatformOptions()
                        {
                            UseWindowsUIComposition = true,
                        });
                    }
                    if (Platform.IsMacOS)
                    {
                        it.With(new MacOSPlatformOptions()
                        {
                            DisableDefaultApplicationMenuItems = true,
                        });

                        /* [Workaround]
                         * Reduce UI frame rate to lower the CPU usage
                         * Please refer to https://github.com/AvaloniaUI/Avalonia/issues/4500
                         */
                        var initWindowingSubSystem = it.WindowingSubsystemInitializer;
                        it.UseWindowingSubsystem(() =>
                        {
                            initWindowingSubSystem();
                            AvaloniaLocator.CurrentMutable.Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(30));
                        });
                    }
                    if (Platform.IsLinux)
                        it.With(new X11PlatformOptions());
                    setupAction?.Invoke(it);
                });
        }


        // Check whether restarting all main windows is needed or not.
        void CheckRestartingMainWindowsNeeded()
        {
            if (this.IsShutdownStarted)
                return;
            var isRestartingNeeded = Global.Run(() =>
            {
                if (this.mainWindowHolders.IsEmpty())
                    return false;
                var themeMode = this.Settings.GetValueOrDefault(SettingKeys.ThemeMode).Let(it =>
                {
                    if (it == ThemeMode.System)
                        return this.systemThemeMode;
                    return it;
                });
                var useCompactUI = this.Settings.GetValueOrDefault(SettingKeys.UseCompactUserInterface);
                return themeMode != this.stylesThemeMode
                    || useCompactUI != this.isCompactStyles;
            });
            if (this.IsRestartingMainWindowsNeeded != isRestartingNeeded)
            {
                if (isRestartingNeeded)
                    this.Logger.LogWarning("Need to restart main windows");
                else
                    this.Logger.LogWarning("No need to restart main windows");
                this.IsRestartingMainWindowsNeeded = isRestartingNeeded;
                this.OnPropertyChanged(nameof(IsRestartingMainWindowsNeeded));
            }
        }


        /// <summary>
        /// Check for application update.
        /// </summary>
        public void CheckForApplicationUpdate() =>
			_ = this.CheckForApplicationUpdateAsync(this.LatestActiveMainWindow, true);


        /// <inheritdoc/>
		public Task<bool> CheckForApplicationUpdateAsync(Avalonia.Controls.Window? owner, bool forceShowingDialog)
        {
            this.VerifyAccess();
            if (!forceShowingDialog)
            {
                var updateInfo = this.UpdateInfo;
                if (updateInfo == null || updateInfo.Version <= Controls.ApplicationUpdateDialog.LatestShownVersion)
                    return Task.FromResult(false);
            }
            this.Logger.LogDebug("Show application update dialog");
            return this.ShowAppUpdateDialog(owner, forceShowingDialog);
        }


        /// <summary>
        /// Check application update information asynchronously.
        /// </summary>
        /// <returns>Task to wait for checking.</returns>
        public async Task<ApplicationUpdateInfo?> CheckForApplicationUpdateAsync()
        {
            // check state
            this.VerifyAccess();
            if (this.IsShutdownStarted)
                return null;

            // check package manifest URI
            var packageManifestUri = this.PackageManifestUri;
            if (packageManifestUri == null)
            {
                this.Logger.LogWarning("No package manifest URI specified to check update");
                return null;
            }

            // schedule next checking
            this.checkUpdateInfoAction?.Reschedule(Math.Max(1000, this.Configuration.GetValueOrDefault(ConfigurationKeys.AppUpdateInfoCheckingInterval)));

            // check update by package manifest
            var stopWatch = new Stopwatch().Also(it => it.Start());
            var packageResolver = new JsonPackageResolver(this, null) { Source = new WebRequestStreamProvider(packageManifestUri) };
            this.Logger.LogInformation("Start checking update");
            try
            {
                await packageResolver.StartAndWaitAsync();
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to check update");
                return null;
            }

            // delay to make UX better
            var delay = (1000 - stopWatch.ElapsedMilliseconds);
            if (delay > 0)
                await Task.Delay((int)delay);

            // check version
            var packageVersion = packageResolver.PackageVersion;
            if (packageVersion == null)
            {
                this.Logger.LogError("No application version gotten from package manifest");
                return null;
            }
            if (!this.Configuration.GetValueOrDefault(ConfigurationKeys.ForceAcceptingAppUpdateInfo) && packageVersion <= this.Assembly.GetName().Version)
            {
                this.Logger.LogInformation("This is the latest application");
                if (this.UpdateInfo != null)
                {
                    this.UpdateInfo = null;
                    this.OnPropertyChanged(nameof(UpdateInfo));
                }
                return null;
            }

            // create update info
            this.Logger.LogDebug("New application version found: {packageVersion}", packageVersion);
            var updateInfo = new ApplicationUpdateInfo(packageVersion, packageResolver.PageUri, packageResolver.PackageUri);
            if (updateInfo != this.UpdateInfo)
            {
                this.UpdateInfo = updateInfo;
                this.OnPropertyChanged(nameof(UpdateInfo));
            }
            return this.UpdateInfo;
        }


        /// <inheritdoc/>
        public ISettings Configuration { get => this.configuration ?? throw new InvalidOperationException("Application is not initialized yet."); }


        /// <inheritdoc/>
        public virtual ViewModels.ApplicationInfo CreateApplicationInfoViewModel() => new();


        /// <inheritdoc/>
        public abstract ViewModels.ApplicationOptions CreateApplicationOptionsViewModel();


        // Create server stream for multi-instances.
        bool CreateMultiInstancesServerStream(bool canRetry, bool printErrorStackTrace)
        {
            if (this.multiInstancesServerStream != null)
                return true;
            if (this.IsShutdownStarted)
            {
                this.Logger.LogWarning("No need to create multi-instances server stream when shutting down");
                return false;
            }
            var retryCount = 20;
            while (true)
            {
                try
                {
                    this.multiInstancesServerStream = new NamedPipeServerStream(this.multiInstancesServerStreamName, PipeDirection.In, 1);
                    this.Logger.LogWarning("Multi-instances server stream created");
                    return true;
                }
                catch (Exception ex)
                {
                    --retryCount;
                    if (retryCount > 0 && canRetry)
                    {
                        if (printErrorStackTrace)
                            this.Logger.LogError(ex, "Unable to create multi-instances server stream, retry later");
                        else
                            this.Logger.LogWarning("Unable to create multi-instances server stream, retry later");
                        Thread.Sleep(500);
                    }
                    else
                    {
                        if (printErrorStackTrace)
                            this.Logger.LogError(ex, "Unable to create multi-instances server stream");
                        else
                            this.Logger.LogWarning("Unable to create multi-instances server stream");
                        return false;
                    }
                }
            }
        }


        /// <summary>
        /// Get current culture info of application.
        /// </summary>
        public override CultureInfo CultureInfo { get => cultureInfo; }


        /// <summary>
        /// Get <see cref="AppSuiteApplication"/> instance for current process.
        /// </summary>
        public static new AppSuiteApplication Current { get => (AppSuiteApplication)Application.Current; }


        /// <summary>
        /// Get <see cref="AppSuiteApplication"/> instance for current process or null if <see cref="AppSuiteApplication"/> is not ready yet.
        /// </summary>
        public static new AppSuiteApplication? CurrentOrNull { get => Application.CurrentOrNull as AppSuiteApplication; }


        /// <summary>
        /// Get or set custom screen scale factor for Linux.
        /// </summary>
        public double CustomScreenScaleFactor 
        {
            get => CachedCustomScreenScaleFactor;
            set
            {
                if (!Platform.IsLinux)
                    return;
                if (!double.IsFinite(value))
                    throw new ArgumentException("Scale should be finite number.");
                CachedCustomScreenScaleFactor = Math.Max(1, value);
                this.OnPropertyChanged(nameof(CustomScreenScaleFactor));
            }
        }


        /// <summary>
        /// Default port at localhost to receive log output.
        /// </summary>
        public virtual int DefaultLogOutputTargetPort { get; } = 0;


        // Define extra styles by code.
        void DefineExtraStyles()
        {
            // check state
            if (this.extraStyles.IsNotEmpty())
                return;

            // get resources
            var duration = this.TryFindResource<TimeSpan>("TimeSpan/Animation", out var timeSpanRes) ? timeSpanRes.GetValueOrDefault() : new TimeSpan();
            var durationFast = this.TryFindResource("TimeSpan/Animation.Fast", out timeSpanRes) ? timeSpanRes.GetValueOrDefault() : new TimeSpan();
            var easing = this.TryFindResource<Easing>("Easing/Animation", out var easingRes) ? easingRes : null;

            // define styles
#if APPLY_CONTROL_BRUSH_ANIMATIONS
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.Button))
                .Template().Name("PART_ContentPresenter"), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.ComboBox))
                .Template().Name("Background"), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.DatePicker))
                .Template().Name("FlyoutButton"), durationFast));
            this.extraStyles.Add(new Style(s => s.OfType(typeof(Controls.LinkTextBlock))).Also(style =>
            {
                style.Setters.Add(new Setter(Animatable.TransitionsProperty, new Transitions().Also(transitions =>
                {
                    transitions.Add(new Animation.BrushTransition()
                    {
                        Duration = durationFast,
                        Easing = easing,
                        Property = Avalonia.Controls.TextBlock.ForegroundProperty,
                    });
                })));
            }));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.ListBox)), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.RepeatButton))
                .Template().Name("PART_ContentPresenter"), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.Primitives.ScrollBar))
                .Template().OfType(typeof(Avalonia.Controls.RepeatButton)).Class("line").Template().Name("Root"), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.Primitives.ScrollBar))
                .Template().OfType(typeof(Avalonia.Controls.Primitives.Thumb)).Class("thumb"), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.Slider))
                .Template().Name("SliderContainer"), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.Slider))
                .Template().Name("PART_IncreaseButton"), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.Slider))
                .Template().Name("PART_DecreaseButton"), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.TextBox))
                .Template().Name("PART_BorderElement"), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.Primitives.Thumb)), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.TimePicker))
                .Template().Name("FlyoutButton"), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.Primitives.ToggleButton))
                .Template().Name("PART_ContentPresenter"), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.ToggleSwitch))
                .Template().Name("OuterBorder"), durationFast));
#endif
#if APPLY_ITEM_BRUSH_ANIMATIONS
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.ComboBoxItem))
               .Template().Name("PART_ContentPresenter"), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.ListBoxItem))
                .Template().Name("PART_ContentPresenter"), durationFast));
            this.extraStyles.Add(this.DefineBrushTransitionsStyle(s => s.OfType(typeof(Avalonia.Controls.MenuItem))
                .Template().Name("PART_LayoutRoot"), durationFast));
#endif
            this.extraStyles.Add(new Style(s => s.OfType(typeof(Avalonia.Controls.Window))
                .Template().Name("PART_Background")).Also(style =>
            {
                style.Setters.Add(new Setter(Animatable.TransitionsProperty, new Transitions().Also(transitions =>
                {
                    transitions.Add(new Animation.BrushTransition()
                    {
                        Duration = duration,
                        Easing = easing,
                        Property = Avalonia.Controls.Primitives.TemplatedControl.BackgroundProperty,
                    });
                })));
            }));

            // disable pointer wheel on ComboBox/NumericUpDown
            Avalonia.Controls.TextBox.PointerWheelChangedEvent.AddClassHandler(typeof(Avalonia.Controls.ComboBox), (s, e) =>
            {
                var comboBox = (Avalonia.Controls.ComboBox)s.AsNonNull();
                if (!comboBox.IsDropDownOpen)
                {
                    comboBox.Parent?.Let(parent =>
                        parent.RaiseEvent(e));
                    e.Handled = true;
                }
            }, RoutingStrategies.Tunnel);
            Avalonia.Controls.TextBox.PointerWheelChangedEvent.AddClassHandler(typeof(Avalonia.Controls.NumericUpDown), (s, e) =>
            {
                ((Avalonia.Controls.NumericUpDown)s.AsNonNull()).Parent?.Let(parent =>
                    parent.RaiseEvent(e));
                e.Handled = true;
            }, RoutingStrategies.Tunnel);

            // [Workaround] Prevent menu flicker in Avalonia 0.10.11
            var pointerReleasedHandler = new EventHandler<RoutedEventArgs>((sender, e) =>
            {
                var pointerEventArgs = (Avalonia.Input.PointerReleasedEventArgs)e;
                if (pointerEventArgs.InitialPressMouseButton != Avalonia.Input.MouseButton.Right)
                    return;
                var textBox = (Avalonia.Controls.TextBox)sender.AsNonNull();
                var contextMenu = textBox.ContextMenu;
                if (textBox.ContextFlyout == null && contextMenu != null && contextMenu.PlacementMode == Avalonia.Controls.PlacementMode.AnchorAndGravity)
                {
                    var position = pointerEventArgs.GetPosition(textBox);
                    contextMenu.HorizontalOffset = position.X;
                    contextMenu.VerticalOffset = position.Y;
                    contextMenu.Open(textBox);
                }
            });
            Avalonia.Controls.TextBox.PointerReleasedEvent.AddClassHandler(typeof(Avalonia.Controls.TextBox), pointerReleasedHandler, RoutingStrategies.Tunnel);

            // [Workaround] Prevent tooltip stays open after changing focus to another window
            if (Platform.IsMacOS)
            {
                var clickHandler = new EventHandler<RoutedEventArgs>((sender, e) =>
                    Avalonia.Controls.ToolTip.SetIsOpen((Avalonia.Controls.Control)sender.AsNonNull(), false));
                var templateAppliedHandler = new EventHandler<RoutedEventArgs>((sender, e) =>
                {
                    if (sender is Avalonia.Controls.Control control)
                    {
                        control.GetObservable(Avalonia.Controls.ToolTip.IsOpenProperty).Subscribe(isOpen =>
                        {
                            if (isOpen && control.FindAncestorOfType<Window>()?.IsActive == false)
                                Avalonia.Controls.ToolTip.SetIsOpen(control, false);
                        });
                    }
                });
                Avalonia.Controls.Button.ClickEvent.AddClassHandler(typeof(Avalonia.Controls.Button), clickHandler);
                Avalonia.Controls.Button.TemplateAppliedEvent.AddClassHandler(typeof(Avalonia.Controls.Button), templateAppliedHandler);
            }

            // add to top styles
            this.Styles.Add(this.extraStyles);
        }


        // Define style for brush transitions of control.
#if APPLY_CONTROL_BRUSH_ANIMATIONS
        Style DefineBrushTransitionsStyle(Func<Avalonia.Styling.Selector?, Avalonia.Styling.Selector> selector, TimeSpan duration)
        {
            var easing = this.TryFindResource<Easing>("Easing/Animation", out var easingRes) ? easingRes : null;
            return new Style(selector).Also(style =>
            {
                style.Setters.Add(new Setter(Animatable.TransitionsProperty, new Transitions().Also(transitions =>
                {
                    transitions.Add(new Animation.BrushTransition()
                    {
                        Duration = duration,
                        Easing = easing,
                        Property = Avalonia.Controls.Primitives.TemplatedControl.BackgroundProperty,
                    });
                    transitions.Add(new Animation.BrushTransition()
                    {
                        Duration = duration,
                        Easing = easing,
                        Property = Avalonia.Controls.Primitives.TemplatedControl.BorderBrushProperty,
                    });
                })));
            });
        }
#endif


        /// <inheritdoc/>
        public double EffectiveCustomScreenScaleFactor { get; } = CachedCustomScreenScaleFactor;


        /// <summary>
        /// Get theme mode which is currently applied to application.
        /// </summary>
        public ThemeMode EffectiveThemeMode { get; private set; } = ThemeMode.Dark;


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


        // Enter background mode.
        bool EnterBackgroundMode()
        {
            // check state
            if (!this.IsBackgroundModeSupported
                || this.isShutdownStarted 
                || this.isRestartRequested 
                || this.windows.IsNotEmpty())
            {
                return false;
            }
            if (this.IsBackgroundMode)
                return true;
            
            this.Logger.LogDebug("Enter background mode");
            
            // enter background mode
            this.IsBackgroundMode = true;
            this.OnPropertyChanged(nameof(IsBackgroundMode));
            if (this.IsBackgroundMode)
                this.OnBackgroundModeEntered();
            return this.IsBackgroundMode;
        }


        // Exit background mode.
        void ExitBackgroundMode()
        {
            // check state
            if (!this.IsBackgroundModeSupported
                || !this.IsBackgroundMode)
            {
                return;
            }

            this.Logger.LogDebug("Exit background mode");

            // exit background mode
            this.IsBackgroundMode = false;
            this.OnPropertyChanged(nameof(IsBackgroundMode));
            if (!this.IsBackgroundMode)
                this.OnBackgroundModeExited();
        }


        /// <inheritdoc/>
        public virtual IEnumerable<ExternalDependency> ExternalDependencies { get; } = Array.Empty<ExternalDependency>();


        /// <inheritdoc/>
        public abstract int ExternalDependenciesVersion { get; }


        /// <summary>
        /// Get fall-back theme mode if <see cref="IsSystemThemeModeSupported"/> is false.
        /// </summary>
        public virtual ThemeMode FallbackThemeMode { get; } = Platform.IsMacOS ? ThemeMode.Light : ThemeMode.Dark;


        // Transform RGB color values.
        static Color GammaTransform(Color color, double gamma)
        {
            var r = (color.R / 255.0);
            var g = (color.G / 255.0);
            var b = (color.B / 255.0);
            var l = (r + g + b) / 3;
            var scale = Math.Pow(l, gamma) / l;
            return Color.FromArgb(color.A, 
                (byte)(Math.Min(255, r * scale * 255) + 0.5), 
                (byte)(Math.Min(255, g * scale * 255) + 0.5), 
                (byte)(Math.Min(255, b * scale * 255) + 0.5)
            );
        }


        /// <inheritdoc/>
        public override IObservable<string?> GetObservableString(string key) =>
            Avalonia.Controls.ResourceNodeExtensions.GetResourceObservable(this, $"String/{key}").Cast<object?, string?>();


        /// <summary>
        /// Get string from resources.
        /// </summary>
        /// <param name="key">Key of string.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>String from resources.</returns>
        public override string? GetString(string key, string? defaultValue = null)
        {
            if (this.TryFindResource<string>($"String/{key}", out var str))
                return str;
            return defaultValue;
        }


        /// <summary>
        /// Get information of hardware.
        /// </summary>
        public HardwareInfo HardwareInfo { get => this.hardwareInfo ?? throw new InvalidOperationException("Application is not initialized yet."); }


        /// <inheritdoc/>
        public bool IsBackgroundMode { get; private set; }


        /// <summary>
        /// Check whether application can can running in background mode or not.
        /// </summary>
#pragma warning disable CA1822
        protected bool IsBackgroundModeSupported { get => Platform.IsMacOS; }
#pragma warning restore CA1822


        /// <summary>
        /// Check whether application is running in debug mode or not.
        /// </summary>
        public bool IsDebugMode { get; private set; }


        /// <inheritdoc/>
        public bool IsFirstLaunch { get; private set; }


        /// <summary>
        /// Check whether multiple application processes is supported or not.
        /// </summary>
        protected virtual bool IsMultipleProcessesSupported { get; } = false;


        /// <inheritdoc/>
        public bool IsPrivacyPolicyAgreed { get; private set; }


        /// <inheritdoc/>
        public bool IsPrivacyPolicyAgreedBefore { get; private set; }


        /// <summary>
        /// Check whether restarting all main windows is needed or not.
        /// </summary>
        public bool IsRestartingMainWindowsNeeded { get; private set; }


        /// <summary>
        /// Check whether restoring main windows when launching is requested or not.
        /// </summary>
        protected bool IsRestoringMainWindowsRequested { get; private set; }


        /// <inheritdoc/>
        public bool IsRunningAsAdministrator { get; private set; }


        /// <summary>
        /// Check whether application is shutting down or not.
        /// </summary>
        public override bool IsShutdownStarted { get => isShutdownStarted; }


        /// <summary>
        /// Check whether splash window is needed when launching application or not.
        /// </summary>
        protected virtual bool IsSplashWindowNeeded { get; } = true;


        /// <summary>
        /// Check whether <see cref="ThemeMode.System"/> is supported or not.
        /// </summary>
        public bool IsSystemThemeModeSupported
        {
            get
            {
                if (Platform.IsMacOS)
                    return true;
                return (this.uiSettings != null);
            }
        }


        /// <inheritdoc/>
        public bool IsUserAgreementAgreed { get; private set; }


        /// <inheritdoc/>
        public bool IsUserAgreementAgreedBefore { get; private set; }


        /// <inheritdoc/>
        public Window? LatestActiveMainWindow { get => this.activeMainWindowList.IsNotEmpty() ? this.activeMainWindowList.First?.Value?.Window : null; }


        /// <inheritdoc/>
        public Avalonia.Controls.Window? LatestActiveWindow { get; private set; }


        /// <summary>
        /// Get options to launch application which is converted by arguments passed to application.
        /// </summary>
        public IDictionary<string, object> LaunchOptions { get; private set; } = DictionaryExtensions.AsReadOnly(new Dictionary<string, object>());


        /// <inheritdoc/>
        public async void LayoutMainWindows(Avalonia.Platform.Screen screen, Controls.MultiWindowLayout layout, Window? activeMainWindow)
        {
            // check state
            this.VerifyAccess();
            if (this.isShutdownStarted)
            {
                this.Logger.LogError("Cannot layout main windows when shutting down");
                return;
            }
            if (screen == null)
            {
                this.Logger.LogError("No screen to layout main windows");
                return;
            }
            var mainWindowCount = this.mainWindows.Count;
            if (mainWindowCount <= 0)
            {
                this.Logger.LogWarning("No main window to layout");
                return;
            }

            // layout single main window
            if (mainWindowCount == 1)
            {
                this.mainWindows.First().Let(it =>
                {
                    switch (it.WindowState)
                    {
                        case Avalonia.Controls.WindowState.FullScreen:
                        case Avalonia.Controls.WindowState.Maximized:
                            break;
                        default:
                            it.WindowState = Avalonia.Controls.WindowState.Maximized;
                            break;
                    }
                    Controls.WindowExtensions.ActivateAndBringToFront(it);
                });
                return;
            }

            // confirm layouting lots of main windows
            if (activeMainWindow == null)
                activeMainWindow = this.LatestActiveMainWindow ?? this.mainWindows[0];
            if (mainWindowCount > 4)
            {
                Controls.WindowExtensions.ActivateAndBringToFront(activeMainWindow);
                var result = await new Controls.MessageDialog()
                {
                    Buttons = Controls.MessageDialogButtons.YesNo,
                    Icon = Controls.MessageDialogIcon.Question,
                    Message = Avalonia.Controls.ResourceNodeExtensions.GetResourceObservable(this, "String/MainWindow.ConfirmLayoutingLotsOfMainWindows"),
                }.ShowDialog(activeMainWindow);
                if (result != Controls.MessageDialogResult.Yes)
                    return;
            }

            // layout main windows
            var workingArea = screen.WorkingArea;
            var pixelDensity = screen.PixelDensity;
            var windowBounds = new PixelRect[mainWindowCount];
            switch (layout)
            {
                case Controls.MultiWindowLayout.Horizontal:
                    {
                        var width = (workingArea.Width / mainWindowCount);
                        var height = workingArea.Height;
                        var left = workingArea.Right - width;
                        var top = workingArea.Y;
                        for (var i = mainWindowCount - 1; i >= 0; --i, left -= width)
                        {
                            if (i > 0)
                                windowBounds[i] = new PixelRect(left, top, width, height);
                            else
                                windowBounds[i] = new PixelRect(workingArea.X, top, windowBounds[1].X - workingArea.X, height);
                        }
                    }
                    break;
                case Controls.MultiWindowLayout.Tile:
                    {
                        var columnCount = (int)(Math.Ceiling(Math.Sqrt(mainWindowCount)) + 0.1);
                        var rowCount = (mainWindowCount / columnCount);
                        if (mainWindowCount > (columnCount * rowCount))
                            ++rowCount;
                        var width = workingArea.Width / columnCount;
                        var height = workingArea.Height / rowCount;
                        var left = workingArea.X;
                        var top = workingArea.Y;
                        var column = 0;
                        for (var i = 0; i < mainWindowCount; ++i)
                        {
                            windowBounds[i] = new PixelRect(left, top, width, height);
                            ++column;
                            if (column < columnCount)
                                left += width;
                            else
                            {
                                column = 0;
                                left = workingArea.X;
                                top += height;
                            }
                        }
                    }
                    break;
                case Controls.MultiWindowLayout.Vertical:
                    {
                        var width = workingArea.Width;
                        var height = (workingArea.Height / mainWindowCount);
                        var left = workingArea.X;
                        var top = workingArea.Bottom - height;
                        for (var i = mainWindowCount - 1; i >= 0; --i, top -= height)
                        {
                            if (i > 0)
                                windowBounds[i] = new PixelRect(left, top, width, height);
                            else
                                windowBounds[i] = new PixelRect(left, workingArea.Y, width, windowBounds[1].Y - workingArea.Y);
                        }
                    }
                    break;
                default:
                    return;
            }
            for (var i = mainWindowCount - 1; i >= 0; --i)
            {
                this.mainWindows[i].Let(it =>
                {
                    var bounds = windowBounds[i];
                    var sysDecorSizes = it.IsExtendedIntoWindowDecorations
                        ? new Thickness()
                        : Controls.WindowExtensions.GetSystemDecorationSizes(it);
                    it.WindowState = Avalonia.Controls.WindowState.Normal;
                    it.Position = new PixelPoint(bounds.X, bounds.Y);
                    if (Platform.IsLinux)
                    {
                        // [Workaround] Sometimes the first position setting won't be applied
                        this.SynchronizationContext.PostDelayed(() =>
                            it.Position = new PixelPoint(bounds.X, bounds.Y), 100);
                    }
                    if (!it.IsExtendedIntoWindowDecorations)
                    {
                        // [Workaround] Height of window may be changed automatically later after setting size of window
                        this.SynchronizationContext.PostDelayed(() =>
                            (it as Controls.IMainWindow)?.CancelSavingSize(), 300);
                    }
                    if (Platform.IsMacOS)
                    {
                        it.Width = bounds.Width - sysDecorSizes.Left - sysDecorSizes.Right;
                        it.Height = bounds.Height - sysDecorSizes.Top - sysDecorSizes.Bottom;
                    }
                    else
                    {
                        it.Width = (bounds.Width / pixelDensity) - sysDecorSizes.Left - sysDecorSizes.Right;
                        it.Height = (bounds.Height / pixelDensity) - sysDecorSizes.Top - sysDecorSizes.Bottom;
                    }
                    (it as Controls.IMainWindow)?.CancelSavingSize();
                    Controls.WindowExtensions.ActivateAndBringToFront(it);
                });
            }
            Controls.WindowExtensions.ActivateAndBringToFront(activeMainWindow);
        }


        // Load configuration.
        async Task LoadConfigurationAsync()
        {
            if (this.IsDebugMode)
            {
                // create instance
                var config = new ConfigurationImpl();
                this.configuration = config;
                this.configuration.SettingChanged += this.OnConfigurationChanged;

                // load from file
                this.Logger.LogDebug("Start loading configuration");
                try
                {
                    await config.LoadAsync(this.configurationFilePath);
                    this.Logger.LogDebug("Complete loading configuration");
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "Failed to load configuration from '{configurationFilePath}'", this.configurationFilePath);
                }
            }
            else
            {
                this.configuration = new CarinaStudio.Configuration.MemorySettings();
                this.configuration.SettingChanged += this.OnConfigurationChanged;
            }
        }


        /// <inheritdoc/>
        public event EventHandler<IAppSuiteApplication, CultureInfo>? LoadingStrings;


        /// <summary>
        /// Load <see cref="PersistentState"/> from file.
        /// </summary>
        /// <returns>Task of loading.</returns>
        public async Task LoadPersistentStateAsync()
        {
            // check performance
            var time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;

            // check state
            this.VerifyAccess();

            // create persistent state
            if (this.persistentState == null)
                this.persistentState = new PersistentStateImpl(this);

            // load from file
            this.Logger.LogDebug("Start loading persistent state");
            try
            {
                // load from file
                await this.persistentState.LoadAsync(this.persistentStateFilePath);
                this.Logger.LogDebug("Complete loading persistent state");

                // save immediately for first launch
                if (this.IsFirstLaunch && this.IsMultipleProcessesSupported)
                {
                    this.Logger.LogDebug("Save persistent state for first launch");
                    await this.persistentState.SaveAsync(this.persistentStateFilePath);
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to load persistent state from '{persistentStateFilePath}'", this.persistentStateFilePath);
            }

            // check privacy policy state
            this.PrivacyPolicyVersion?.Let(version =>
            {
                var agreedVersion = this.persistentState.GetValueOrDefault(AgreedPrivacyPolicyVersionKey).Let(it =>
                {
                    if (Version.TryParse(it, out var v))
                        return v;
                    return null;
                });
                bool isAgreed = (agreedVersion != null && agreedVersion >= version);
                if (agreedVersion != null)
                {
                    if (!this.IsPrivacyPolicyAgreedBefore)
                    {
                        this.IsPrivacyPolicyAgreedBefore = true;
                        this.OnPropertyChanged(nameof(IsPrivacyPolicyAgreedBefore));
                    }
                }
                else
                {
                    if (this.IsPrivacyPolicyAgreedBefore)
                    {
                        this.IsPrivacyPolicyAgreedBefore = false;
                        this.OnPropertyChanged(nameof(IsPrivacyPolicyAgreedBefore));
                    }
                }
                if (isAgreed)
                    this.Logger.LogDebug("Current Privacy Policy has been agreed");
                else if (this.IsPrivacyPolicyAgreedBefore)
                    this.Logger.LogWarning("Privacy Policy has been updated and is not agreed yet");
                else
                    this.Logger.LogWarning("Privacy Policy is not agreed yet");
                if (isAgreed != this.IsPrivacyPolicyAgreed)
                {
                    this.IsPrivacyPolicyAgreed = isAgreed;
                    this.OnPropertyChanged(nameof(IsPrivacyPolicyAgreed));
                }
            });

            // check user agreement state
            this.UserAgreementVersion?.Let(version =>
            {
                var agreedVersion = this.persistentState.GetValueOrDefault(AgreedUserAgreementVersionKey).Let(it =>
                {
                    if (Version.TryParse(it, out var v))
                        return v;
                    return null;
                });
                bool isAgreed = (agreedVersion != null && agreedVersion >= version);
                if (agreedVersion != null)
                {
                    if (!this.IsUserAgreementAgreedBefore)
                    {
                        this.IsUserAgreementAgreedBefore = true;
                        this.OnPropertyChanged(nameof(IsUserAgreementAgreedBefore));
                    }
                }
                else
                {
                    if (this.IsUserAgreementAgreedBefore)
                    {
                        this.IsUserAgreementAgreedBefore = false;
                        this.OnPropertyChanged(nameof(IsUserAgreementAgreedBefore));
                    }
                }
                if (isAgreed)
                    this.Logger.LogDebug("Current User Agreement has been agreed");
                else if (this.IsUserAgreementAgreedBefore)
                    this.Logger.LogWarning("User Agreement has been updated and is not agreed yet");
                else
                    this.Logger.LogWarning("User Agreement is not agreed yet");
                if (isAgreed != this.IsUserAgreementAgreed)
                {
                    this.IsUserAgreementAgreed = isAgreed;
                    this.OnPropertyChanged(nameof(IsUserAgreementAgreed));
                }
            });

            // check performance
            if (time > 0)
            {
                time = this.stopWatch.ElapsedMilliseconds - time;
                this.Logger.LogTrace("[Performance] Took {time} ms to load persistent state", time);
            }
        }


        /// <summary>
        /// Load <see cref="Settings"/> from file.
        /// </summary>
        /// <returns>Task of loading.</returns>
        public async Task LoadSettingsAsync()
        {
            // check performance
            var time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;

            // check state
            this.VerifyAccess();

            // create settings
            if (this.settings == null)
                this.settings = new SettingsImpl(this);

            // load from file
            this.Logger.LogDebug("Start loading settings");
            try
            {
                await this.settings.LoadAsync(this.settingsFilePath);
                this.Logger.LogDebug("Complete loading settings");
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to load settings from '{settingsFilePath}'", this.settingsFilePath);
            }

            // setup accepting non-stable update
            if (!this.PersistentState.GetValueOrDefault(IsAcceptNonStableApplicationUpdateInitKey))
            {
                this.settings.SetValue<bool>(SettingKeys.AcceptNonStableApplicationUpdate, this.ReleasingType != ApplicationReleasingType.Stable);
                this.PersistentState.SetValue<bool>(IsAcceptNonStableApplicationUpdateInitKey, true);
            }

            // Fall-back to default theme mode if 'System' is unsupported
            if (this.settings.GetValueOrDefault(SettingKeys.ThemeMode) == ThemeMode.System
                && !this.IsSystemThemeModeSupported)
            {
                this.settings.SetValue<ThemeMode>(SettingKeys.ThemeMode, this.FallbackThemeMode);
            }

            // check settings
            if (!this.settings.GetValueOrDefault(SettingKeys.ShowProcessInfo))
                this.processInfoHfUpdateToken = this.processInfoHfUpdateToken.DisposeAndReturnNull();
            else if (this.processInfoHfUpdateToken == null)
                this.processInfoHfUpdateToken = this.processInfo?.RequestHighFrequencyUpdate();
            
            // check performance
            if (time > 0)
            {
                time = this.stopWatch.ElapsedMilliseconds - time;
                this.Logger.LogTrace("[Performance] Took {time} ms to load settings", time);
            }
        }


        /// <summary>
        /// Load string resource in XAML format.
        /// </summary>
        /// <param name="uri">URI of string resource.</param>
        /// <returns>Loaded string resource, or Null if failed to load.</returns>
        public Avalonia.Controls.IResourceProvider? LoadStringResource(Uri uri)
        {
            try
            {
                return new ResourceInclude().Also(it =>
                {
                    it.Source = uri;
                    var resDictionary = it.Loaded;  // trigger error if resource not found
                    foreach (var pair in resDictionary.ToArray())
                    {
                        if (pair.Value is not string str)
                            continue;
                        if (str.EndsWith(':'))
                            resDictionary[pair.Key] = str + " ";
                    }
                });
            }
            catch
            {
                this.Logger.LogWarning("Unable to load string resource from {uri}", uri);
                return null;
            }
        }


        /// <summary>
        /// Get logger.
        /// </summary>
        protected Microsoft.Extensions.Logging.ILogger Logger { get; }


        /// <summary>
        /// Get logger factory.
        /// </summary>
        public override ILoggerFactory LoggerFactory { get; }


        /// <summary>
        /// Get or set port at localhost to receive log output.
        /// </summary>
        public int LogOutputTargetPort
        {
            get => this.logOutputTargetPort;
            set
            {
                this.VerifyAccess();
                if (this.logOutputTargetPort == value)
                    return;
                this.logOutputTargetPort = value;
                this.PersistentState.SetValue<int>(LogOutputTargetPortKey, value);
                this.UpdateLogOutputToLocalhost();
                this.OnPropertyChanged(nameof(LogOutputTargetPort));
            }
        }


        /// <summary>
        /// Get list of main windows.
        /// </summary>
        public IList<Window> MainWindows { get; }


        /// <summary>
        /// Called after entering background mode.
        /// </summary>
        protected virtual void OnBackgroundModeEntered()
        { }


        /// <summary>
        /// Called after exiting background mode.
        /// </summary>
        protected virtual void OnBackgroundModeExited()
        { }


        // Called when one of configuration has been changed.
        void OnConfigurationChanged(object? sender, SettingChangedEventArgs e) =>
            this.OnConfigurationChanged(e);


        /// <summary>
        /// Called when one of configuration has been changed.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected virtual void OnConfigurationChanged(SettingChangedEventArgs e)
        {
            if (e.Key == ConfigurationKeys.AppUpdateInfoCheckingInterval 
                || e.Key == ConfigurationKeys.ForceAcceptingAppUpdateInfo)
            {
                _ = this.CheckForApplicationUpdateAsync();
            }
        }


        /// <summary>
        /// Called to create <see cref="ILoggerProvider"/> for <see cref="LogFactory"/>.
        /// </summary>
        /// <returns><see cref="ILoggerProvider"/>.</returns>
        /// <remarks>The method will be called DIRECTLY in constructor.</remarks>
        protected virtual ILoggerProvider OnCreateLoggerProvider()
        {
            return new NLogLoggerProvider();
        }


        /// <summary>
        /// Called to create main window.
        /// </summary>
        /// <returns>Main window.</returns>
        protected abstract Window OnCreateMainWindow();


        /// <summary>
        /// Called to create view-model of main window.
        /// </summary>
        /// <param name="savedState">Saved state in JSON format generated by <see cref="ViewModels.MainWindowViewModel.SaveState(Utf8JsonWriter)"/>.</param>
        /// <returns>View-model.</returns>
        protected abstract ViewModel OnCreateMainWindowViewModel(JsonElement? savedState);


        /// <summary>
        /// Called to dispose view-model of main window.
        /// </summary>
        /// <param name="viewModel">View-model to dispose.</param>
        /// <returns>Task of disposing view-model.</returns>
        protected virtual async Task OnDisposeMainWindowViewModelAsync(ViewModel viewModel)
        {
            await viewModel.WaitForNecessaryTasksAsync();
            viewModel.Dispose();
        }


        /// <summary>
        /// Called when Avalonia framework initialized.
        /// </summary>
        public override void OnFrameworkInitializationCompleted()
        {
            // check performance
            this.frameworkInitializedTime = this.stopWatch.ElapsedMilliseconds;
            this.Logger.LogTrace("[Performance] Took {duration} ms to initialize Avalonia framework", this.frameworkInitializedTime - this.creationTime);
            
            // call base
            base.OnFrameworkInitializationCompleted();

            // start multi-instances server or send arguments to server
            var desktopLifetime = (this.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);
            if (!this.IsMultipleProcessesSupported && desktopLifetime != null)
            {
                this.multiInstancesServerStreamName = this.Name.Let(it =>
                {
                    var nameBuilder = new StringBuilder(it);
                    for (var i = nameBuilder.Length - 1; i >= 0; --i)
                    {
                        if (!char.IsLetterOrDigit(nameBuilder[i]))
                            nameBuilder[i] = '_';
                    }
                    nameBuilder.Append('-');
                    nameBuilder.Append(Math.Abs(this.RootPrivateDirectoryPath.GetHashCode()));
                    return nameBuilder.ToString();
                });
                if (Platform.IsNotWindows)
                {
                    // [workaround] treat process as client first becase limitation of max server instance seems not working on Linux
                    if (this.SendArgumentsToMultiInstancesServer(desktopLifetime.Args))
                    {
                        this.SynchronizationContext.Post(() => desktopLifetime.Shutdown());
                        return;
                    }
                }
                if (this.CreateMultiInstancesServerStream(true, false))
                    this.WaitForMultiInstancesClient();
                else
                {
                    this.SendArgumentsToMultiInstancesServer(desktopLifetime.Args);
                    this.SynchronizationContext.Post(() => desktopLifetime.Shutdown());
                    return;
                }
            }

            // parse arguments
            if (desktopLifetime != null)
            {
                this.LaunchOptions = this.ParseArguments(desktopLifetime.Args);
                if (this.LaunchOptions.TryGetValue(RestoreMainWindowsRequestedKey, out var value)
                    && value is bool boolValue
                    && boolValue)
                {
                    this.Logger.LogWarning("Restoring main windows is requested");
                    this.IsRestoringMainWindowsRequested = true;
                }
            }

            // enter debug mode
            if (this.OnSelectEnteringDebugMode())
            {
                this.Logger.LogWarning("Enter debug mode");
                this.IsDebugMode = true;
            }
            else
            {
                try
                {
                    LogManager.Configuration.FindRuleByName("logToFile").SetLoggingLevels(NLog.LogLevel.Debug, NLog.LogLevel.Error);
                    LogManager.ReconfigExistingLoggers();
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "Failed to setup default NLog rule");
                }
            }

            // setup NSApplication and dock tile on macOS
            if (Platform.IsMacOS)
            {
                NSApplication.Current?.Let(app =>
                {
                    this.macOSAppDelegate = new(this, app.Delegate);
                    app.Delegate = this.macOSAppDelegate;
                });
                this.updateMacOSAppDockTileProgressAction = new(() =>
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
                });
            }

            // start monitoring windows.
            Avalonia.Controls.Window.WindowClosedEvent.AddClassHandler(typeof(Avalonia.Controls.Window), (sender, _) =>
            {
                if (sender is Avalonia.Controls.Window window)
                {
                    this.windows.Remove(window);
                    this.OnWindowClosed(window);
                }
            }, RoutingStrategies.Direct);
            Avalonia.Controls.Window.WindowOpenedEvent.AddClassHandler(typeof(Avalonia.Controls.Window), (sender, _) =>
            {
                if (sender is Avalonia.Controls.Window window)
                {
                    this.windows.Add(window);
                    this.OnWindowOpened(window);
                }
            }, RoutingStrategies.Direct);

            // start loading persistent state and settings
            this.loadingInitPersistentStateTask = this.LoadPersistentStateAsync();
            this.loadingInitSettingsTask = this.LoadSettingsAsync();

            // create hardware and process information
            this.hardwareInfo = new HardwareInfo(this);
            this.processInfo = new ProcessInfo(this);

            // attach to lifetime
            if (desktopLifetime != null)
            {
                desktopLifetime.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
                desktopLifetime.ShutdownRequested += (_, e) =>
                {
                    if (!this.isShutdownStarted && Platform.IsNotMacOS)
                    {
                        this.Logger.LogWarning("Application has been shut down unexpectedly");
                        this.isShutdownStarted = true;
                        this.OnPropertyChanged(nameof(IsShutdownStarted));
                    }
                };
            }

            // check first launch
            try
            {
                var isFirstLaunch = false;
                var syncLock = new object();
                lock (syncLock)
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        isFirstLaunch = !File.Exists(this.persistentStateFilePath);
                        lock (syncLock)
                            Monitor.Pulse(syncLock);
                    });
                    if (!Monitor.Wait(syncLock, 5000))
                        throw new TimeoutException("Timeout waiting for checking first launch");
                }
                this.IsFirstLaunch = isFirstLaunch;
                if (isFirstLaunch)
                    this.Logger.LogWarning("This is the first launch");
                else
                    this.Logger.LogTrace("This is not the first launch");
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Error occurred while checking first launch");
            }

            // check privacy policy version
            if (this.PrivacyPolicyVersion == null)
            {
                this.Logger.LogWarning("No Privacy Policy");
                this.IsPrivacyPolicyAgreed = true;
                this.OnPropertyChanged(nameof(IsPrivacyPolicyAgreed));
            }

            // check user agreement version
            if (this.UserAgreementVersion == null)
            {
                this.Logger.LogWarning("No User Agreement");
                this.IsUserAgreementAgreed = true;
                this.OnPropertyChanged(nameof(IsUserAgreementAgreed));
            }

            // prepare
            this.SynchronizationContext.Post(async () =>
            {
                // check state
                if (this.IsShutdownStarted)
                    return;
                
                // check performance
                this.prepareStartingTime = this.stopWatch.ElapsedMilliseconds;
                this.Logger.LogTrace("[Performance] Took {duration} ms to perform actions before starting", this.prepareStartingTime - this.frameworkInitializedTime);

                // load configuration
                var time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;
                await this.LoadConfigurationAsync();
                if (time > 0)
                    this.Logger.LogTrace("[Performance] Took {duration} ms to load configuration", this.stopWatch.ElapsedMilliseconds - time);

                // prepare
                await this.OnPrepareStartingAsync();

                // restore main windows
                if (this.IsRestoringMainWindowsRequested)
                    _ = this.OnRestoreMainWindowsAsync();
            });
        }


        /// <summary>
        /// Called to load default string resource.
        /// </summary>
        /// <returns>Default string resource.</returns>
        protected virtual Avalonia.Controls.IResourceProvider? OnLoadDefaultStringResource() => null;


        /// <summary>
        /// Called to load string resource for given culture.
        /// </summary>
        /// <param name="cultureInfo">Culture info.</param>
        /// <returns>String resource.</returns>
        protected virtual Avalonia.Controls.IResourceProvider? OnLoadStringResource(CultureInfo cultureInfo) => null;


        /// <summary>
        /// Called to load <see cref="IStyle"/> for given theme mode.
        /// </summary>
        /// <param name="themeMode">Theme mode.</param>
        /// <param name="useCompactUI">True to use compact user interface.</param>
        /// <returns><see cref="IStyle"/>.</returns>
        protected virtual IStyle? OnLoadTheme(ThemeMode themeMode, bool useCompactUI) => null;


        // Called when IsActive of main window changed.
        void OnMainWindowActivationChanged(Window mainWindow, bool isActive)
        {
            if (isActive)
            {
                if (Platform.IsMacOS)
                {
                    this.UpdateCultureInfo(true);
                    this.UpdateSystemThemeMode(true);
                    this.updateMacOSAppDockTileProgressAction?.Schedule();
                }
                this.ProVersionProductId?.Let(productId =>
                {
                    if (this.notifyNetworkConnForProductActivationAction?.IsScheduled == false
                        && !this.ProductManager.IsProductActivated(productId, true)
                        && !NetworkManager.Default.IsNetworkConnected
                        && !this.isNetworkConnForProductActivationNotified)
                    {
                        this.notifyNetworkConnForProductActivationAction?.Schedule(this.Configuration.GetValueOrDefault(ConfigurationKeys.TimeoutToNotifyNetworkConnectionForProductActivation));
                    }
                    if (this.isReActivatingProVersionNeeded)
                        this.reActivateProVersionAction?.Schedule();
                });
                if (this.activeMainWindowList.IsNotEmpty() && this.activeMainWindowList.First?.Value?.Window == mainWindow)
                    return;
                if (this.mainWindowHolders.TryGetValue(mainWindow, out var mainWindowHolder))
                {
                    if (mainWindowHolder.ActiveListNode.List != null)
                        this.activeMainWindowList.Remove(mainWindowHolder.ActiveListNode);
                    this.activeMainWindowList.AddFirst(mainWindowHolder.ActiveListNode);
                    this.OnPropertyChanged(nameof(LatestActiveMainWindow));
                }
            }
        }


        // Called when main window closed.
        async void OnMainWindowClosed(object? sender, EventArgs e)
        {
            // detach from main window
            if (sender is not Window mainWindow)
                return;
            if (!this.mainWindowHolders.TryGetValue(mainWindow, out var mainWindowHolder))
                return;
            if (this.activeMainWindowList.IsNotEmpty() && this.activeMainWindowList.First?.Value?.Window == mainWindow)
            {
                this.activeMainWindowList.RemoveFirst();
                this.OnPropertyChanged(nameof(LatestActiveMainWindow));
                this.updateMacOSAppDockTileProgressAction?.Schedule();
            }
            else if (mainWindowHolder.ActiveListNode.List != null)
                this.activeMainWindowList.Remove(mainWindowHolder.ActiveListNode);
            this.mainWindows.Remove(mainWindow);
            mainWindow.Closed -= this.OnMainWindowClosed;

            this.Logger.LogDebug("Main window closed, {count} remains", this.mainWindows.Count);

            // perform operations
            await this.OnMainWindowClosedAsync(mainWindow, mainWindowHolder.ViewModel);

            // restart main window
            if (mainWindowHolder.IsRestartingRequested)
            {
                if (!this.IsShutdownStarted)
                {
                    if (this.isRestartingMainWindowsRequested)
                    {
                        this.mainWindowHolders.Remove(mainWindow);
                        this.pendingMainWindowHolders.Add(new MainWindowHolder(mainWindowHolder.ViewModel, null, mainWindowHolder.WindowCreatedAction));
                        if (this.mainWindowHolders.IsEmpty())
                        {
                            this.Logger.LogWarning("Restart all main windows");
                            this.isRestartingMainWindowsRequested = false;
                            var pendingMainWindowHolders = this.pendingMainWindowHolders.ToArray().Also(_ =>
                            {
                                this.pendingMainWindowHolders.Clear();
                            });
                            foreach (var pendingMainWindowHolder in pendingMainWindowHolders)
                            {
                                if (!await this.ShowMainWindowAsync(pendingMainWindowHolder.ViewModel, pendingMainWindowHolder.WindowCreatedAction))
                                {
                                    this.Logger.LogError("Unable to restart main window");
                                    await this.OnDisposeMainWindowViewModelAsync(pendingMainWindowHolder.ViewModel);
                                }
                            }
                        }
                        else
                            this.Logger.LogWarning("Restart main window later after closing all main windows");
                        return;
                    }
                    else
                    {
                        this.Logger.LogWarning("Restart single main window requested");
                        this.mainWindowHolders.Remove(mainWindow);
                        if (await this.ShowMainWindowAsync(mainWindowHolder.ViewModel, mainWindowHolder.WindowCreatedAction))
                            return;
                        this.Logger.LogError("Unable to restart single main window");
                    }
                }
                else
                    this.Logger.LogError("Unable to restart main window when shutting down");
            }

            // dispose view model
            await this.OnDisposeMainWindowViewModelAsync(mainWindowHolder.ViewModel);

            // remove from window list
            this.mainWindowHolders.Remove(mainWindow);

            // enter background mode or shut down
            if (!this.EnterBackgroundMode() && this.mainWindowHolders.IsEmpty() && this.windows.IsEmpty())
            {
                this.Logger.LogWarning("All main windows were closed, start shutting down");
                this.Shutdown();
            }
        }


        /// <summary>
        /// Called to perform asynchronous operations after closing main window.
        /// </summary>
        /// <param name="mainWindow">Closed main window.</param>
        /// <param name="viewModel">View-model of main window.</param>
        /// <returns>Task of performing operations.</returns>
        protected virtual async Task OnMainWindowClosedAsync(Window mainWindow, ViewModel viewModel)
        {
            // cancel notification for network connection
			if (this.MainWindows.IsEmpty())
				this.notifyNetworkConnForProductActivationAction?.Cancel();
            
            // save settings
            await this.SaveSettingsAsync();
        }


        // Called when HasDialogs of main window changed.
        void OnMainWindowDialogsChanged(bool hasDialogs)
        {
            if (!hasDialogs && this.ProVersionProductId != null)
            {
                if (!this.isNetworkConnForProductActivationNotified)
                    this.notifyNetworkConnForProductActivationAction?.Schedule();
                if (this.isReActivatingProVersionNeeded)
                    this.reActivateProVersionAction?.Schedule();
            }
        }


        /// <summary>
        /// Called when new application instance has been launched and be redirected to current application instance.
        /// </summary>
        /// <remarks>The method will be call ONLY when <see cref="IsMultipleProcessesSupported"/> is False.</remarks>
        /// <param name="launchOptions">Options to launch new instance.</param>
        protected virtual void OnNewInstanceLaunched(IDictionary<string, object> launchOptions)
        { }


        // Called when property of network manager changed.
		void OnNetworkManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
            var productId = this.ProVersionProductId;
			if (productId != null 
                && sender is NetworkManager networkManager 
				&& e.PropertyName == nameof(NetworkManager.IsNetworkConnected))
			{
				if (networkManager.IsNetworkConnected)
					this.notifyNetworkConnForProductActivationAction?.Cancel();
				else if (!this.ProductManager.IsProductActivated(productId, true)
					&& !this.isNetworkConnForProductActivationNotified)
				{
					this.notifyNetworkConnForProductActivationAction?.Reschedule(this.Configuration.GetValueOrDefault(ConfigurationKeys.TimeoutToNotifyNetworkConnectionForProductActivation));
				}
			}
		}


        /// <summary>
        /// Called to parse single argument in argument list passed to application.
        /// </summary>
        /// <param name="args">Argument list.</param>
        /// <param name="index">Index of argument to parse.</param>
        /// <param name="launchOptions">Dictionary to hold parsed arguments.</param>
        /// <returns>Index of next argument to parse.</returns>
        protected virtual int OnParseArguments(string[] args, int index, IDictionary<string, object> launchOptions)
        {
            var arg = args[index];
            switch (arg)
            {
                case DebugArgument:
                    launchOptions[DebugModeRequestedKey] = true;
                    break;
                case RestoreMainWindowsArgument:
                    launchOptions[RestoreMainWindowsRequestedKey] = true;
                    break;
                default:
                    return index;
            }
            return index + 1;
        }


        /// <summary>
        /// Called to perform asynchronous operations before shutting down.
        /// </summary>
        /// <returns>Task of performing operations.</returns>
        protected virtual async Task OnPrepareShuttingDownAsync()
        {
            // dispose pending view-model of main windows
            if (this.pendingMainWindowHolders.IsNotEmpty())
            {
                this.Logger.LogWarning("Dispose {count} pending view-model of main windows before shutting down", this.pendingMainWindowHolders.Count);
                foreach (var mainWindowHolder in this.pendingMainWindowHolders)
                    await this.OnDisposeMainWindowViewModelAsync(mainWindowHolder.ViewModel);
                this.pendingMainWindowHolders.Clear();
            }

            // detach from system event
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SystemEvents.UserPreferenceChanged -= this.OnWindowsUserPreferenceChanged;
                if (this.uiSettings != null)
                {
                    var colorValuesChangedEvent = this.uiSettings.GetType().GetEvent("ColorValuesChanged");
                    if (colorValuesChangedEvent != null && this.uiSettingsColorValueChangedHandler != null)
                        Global.RunWithoutError(() => colorValuesChangedEvent.RemoveEventHandler(this.uiSettings, this.uiSettingsColorValueChangedHandler));
                }
            }

            // cancel checking update
            this.checkUpdateInfoAction?.Cancel();

            // close server stream for multi-instances
            this.multiInstancesServerCancellationTokenSource.Cancel();
            if (this.multiInstancesServerStream != null)
            {
                this.Logger.LogWarning("Close multi-instances server stream");
                Global.RunWithoutError(() => this.multiInstancesServerStream.Close());
                this.multiInstancesServerStream = null;
            }

            // save configuration
            await this.SaveConfigurationAsync();

            // save persistent state
            await this.SavePersistentStateAsync();

            // save custom screen scale factor
            if (Platform.IsLinux && double.IsFinite(CachedCustomScreenScaleFactor))
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


        /// <summary>
        /// Called to prepare showing splash window when launching application.
        /// </summary>
        /// <returns>Parameters of splash window.</returns>
        protected virtual Controls.SplashWindowParams OnPrepareSplashWindow() => new Controls.SplashWindowParams().Also((ref Controls.SplashWindowParams it) =>
        {
            var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>().AsNonNull();
            it.BackgroundImageOpacity = 0.2;
            it.BackgroundImageUri = Global.Run(() =>
            {
                var uri = new Uri($"avares://{this.Assembly.GetName().Name}/SplashWindowBackground.jpg");
                if (assetLoader.Exists(uri))
                    return uri;
                uri = new Uri($"avares://{this.Assembly.GetName().Name}/SplashWindowBackground.png");
                if (assetLoader.Exists(uri))
                    return uri;
                return null;
            });
            it.IconUri = Global.Run(() =>
            {
                var uri = new Uri($"avares://{this.Assembly.GetName().Name}/{this.Name}.ico");
                if (assetLoader.Exists(uri))
                    return uri;
                uri = new Uri($"avares://{this.Assembly.GetName().Name}/AppIcon.ico");
                if (assetLoader.Exists(uri))
                    return uri;
                throw new NotImplementedException("Cannot get default icon.");
            });
        });


        /// <summary>
        /// Called to prepare application after Avalonia framework initialized.
        /// </summary>
        /// <returns>Task of preparation.</returns>
        protected virtual async Task OnPrepareStartingAsync()
        {
            // start log output to localhost
            this.logOutputTargetPort = this.PersistentState.GetValueOrDefault(LogOutputTargetPortKey);
            if (this.logOutputTargetPort == 0)
                this.logOutputTargetPort = this.DefaultLogOutputTargetPort;
            this.UpdateLogOutputToLocalhost();

            // setup scheduled actions
			this.notifyNetworkConnForProductActivationAction = new(() =>
			{
                var productId = this.ProVersionProductId;
				var window = this.LatestActiveMainWindow;
				if (productId == null 
                    || window == null 
					|| !window.IsActive
					|| window.HasDialogs
					|| this.isNetworkConnForProductActivationNotified
					|| NetworkManager.Default.IsNetworkConnected
					|| this.ProductManager.IsProductActivated(productId, true))
				{
					return;
				}
				this.isNetworkConnForProductActivationNotified = true;
				_ = new Controls.MessageDialog()
				{
					Icon = Controls.MessageDialogIcon.Information,
					Message = new FormattedString().Also(it =>
					{
						it.Bind(FormattedString.Arg1Property, Avalonia.Controls.ResourceNodeExtensions.GetResourceObservable(this, $"String/Product.{productId}"));
						it.Bind(FormattedString.FormatProperty, Avalonia.Controls.ResourceNodeExtensions.GetResourceObservable(this, "String/AppSuiteApplication.NetworkConnectionNeededForProductActivation"));
					}),
				}.ShowDialog(window);
			});
			this.reActivateProVersionAction = new(async () =>
			{
                var productId = this.ProVersionProductId;
				var window = this.LatestActiveMainWindow;
				if (productId == null 
                    || !this.isReActivatingProVersionNeeded 
					|| window == null 
					|| window.HasDialogs 
					|| !window.IsActive 
					|| this.isActivatingProVersion
					|| this.isReActivatingProVersion)
				{
					return;
				}
				this.isReActivatingProVersionNeeded = false;
				if (this.ProductManager.TryGetProductState(productId, out var state)
					&& state == ProductState.Deactivated)
				{
					await new Controls.MessageDialog()
					{
						Icon = Controls.MessageDialogIcon.Warning,
						Message = new FormattedString().Also(it =>
						{
							it.Bind(FormattedString.Arg1Property, Avalonia.Controls.ResourceNodeExtensions.GetResourceObservable(this, $"String/Product.{productId}"));
							it.Bind(FormattedString.FormatProperty, Avalonia.Controls.ResourceNodeExtensions.GetResourceObservable(this, "String/AppSuiteApplication.ReActivateProductNeeded"));
						}),
					}.ShowDialog(this.LatestActiveMainWindow);
					this.isReActivatingProVersion = true;
					await this.ActivateProVersionAsync(this.LatestActiveMainWindow);
					this.isReActivatingProVersion = false;
				}
			});

            // start checking update
            this.PackageManifestUri?.Let(it =>
            {
                this.checkUpdateInfoAction = new ScheduledAction(() =>
                {
                    _ = this.CheckForApplicationUpdateAsync();
                });
                this.checkUpdateInfoAction?.Schedule();
            });

            // complete loading persistent state and settings
            if (this.loadingInitPersistentStateTask != null)
            {
                await this.loadingInitPersistentStateTask;
                this.loadingInitPersistentStateTask = null;
            }
            if (this.loadingInitSettingsTask != null)
            {
                await this.loadingInitSettingsTask!;
                this.loadingInitSettingsTask = null;
                this.Settings.SettingChanged += this.OnSettingChanged;
            }

            // setup culture info
            this.UpdateCultureInfo(false);

            // load strings
            this.Resources.MergedDictionaries.Add(this.LoadStringResource(new Uri("avares://CarinaStudio.AppSuite.Core/Strings/Default.axaml")).AsNonNull());
            if (Platform.IsLinux)
                this.Resources.MergedDictionaries.Add(this.LoadStringResource(new Uri("avares://CarinaStudio.AppSuite.Core/Strings/Default-Linux.axaml")).AsNonNull());
            else if (Platform.IsMacOS)
                this.Resources.MergedDictionaries.Add(this.LoadStringResource(new Uri("avares://CarinaStudio.AppSuite.Core/Strings/Default-OSX.axaml")).AsNonNull());
            this.OnLoadDefaultStringResource()?.Let(it => this.Resources.MergedDictionaries.Add(it));
            this.UpdateStringResources();

            // get current system theme mode
            this.UpdateSystemThemeMode(false);
            
            // setup effective theme mode
            this.SelectCurrentThemeMode().Let(themeMode =>
            {
                if (this.EffectiveThemeMode != themeMode)
                {
                    this.EffectiveThemeMode = themeMode;
                    this.OnPropertyChanged(nameof(EffectiveThemeMode));
                }
            });

            // create base theme
            var time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;
            this.baseTheme = new Avalonia.Themes.Fluent.FluentTheme(new Uri("avares://Avalonia.Themes.Fluent/"))
            {
                Mode = this.EffectiveThemeMode switch
                {
                    ThemeMode.Light => Avalonia.Themes.Fluent.FluentThemeMode.Light,
                    _ => Avalonia.Themes.Fluent.FluentThemeMode.Dark,
                }
            };
            this.Styles.Add(this.baseTheme);
            if (time > 0)
                this.Logger.LogTrace("[Performance] Took {duration} ms to create base theme", this.stopWatch.ElapsedMilliseconds - time);

            // show splash window
            var showSplashWindow = this.IsSplashWindowNeeded && this.Settings.GetValueOrDefault(SettingKeys.LaunchWithSplashWindow);
            if (showSplashWindow)
            {
                time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;
                var splashWindowParams = this.OnPrepareSplashWindow();
                if (time > 0)
                {
                    var currentTime = this.stopWatch.ElapsedMilliseconds;
                    this.Logger.LogTrace("[Performance] Took {duration} ms to prepare parameters of splash window", currentTime - time);
                    time = currentTime;
                }
                this.splashWindow = new Controls.SplashWindowImpl()
                {
                    AccentColor = splashWindowParams.AccentColor,
                    BackgroundImageOpacity = splashWindowParams.BackgroundImageOpacity,
                    BackgroundImageUri = splashWindowParams.BackgroundImageUri,
                    IconUri = splashWindowParams.IconUri,
                };
                if (time > 0)
                {
                    var currentTime = this.stopWatch.ElapsedMilliseconds;
                    this.Logger.LogTrace("[Performance] Took {duration} ms to create splash window", currentTime - time);
                    time = currentTime;
                }
                this.splashWindow.Show();
                this.splashWindowShownTime = this.stopWatch.ElapsedMilliseconds;
                await Task.Delay(SplashWindowShowingDuration);
            }

            // load built-in resources
            if (showSplashWindow)
            {
                this.UpdateSplashWindowMessage(this.GetStringNonNull("AppSuiteApplication.LoadingTheme", ""));
                await Task.Delay(SplashWindowLoadingThemeDuration);
            }
            this.Resources.MergedDictionaries.Add(new ResourceInclude()
            {
                Source = new Uri("avares://CarinaStudio.AppSuite.Core/Resources/Icons.axaml")
            });

            // start initializing network manager
            var initNetworkManagerTask = Net.NetworkManager.InitializeAsync(this);

            // setup styles
            this.UpdateStyles();

            // attach to system event
            if (Platform.IsWindows)
            {
#pragma warning disable CA1416
                SystemEvents.UserPreferenceChanged += this.OnWindowsUserPreferenceChanged;
                if (this.uiSettings != null)
                {
                    var colorValuesChangedEvent = this.uiSettings.GetType().GetEvent("ColorValuesChanged");
                    if (colorValuesChangedEvent != null)
                    {
                        try
                        {
                            this.uiSettingsColorValueChangedHandler = Delegate.CreateDelegate(colorValuesChangedEvent.EventHandlerType!, this, typeof(AppSuiteApplication).GetMethod("OnWindowsUIColorValueChanged", BindingFlags.Instance | BindingFlags.NonPublic).AsNonNull());
                            colorValuesChangedEvent.AddEventHandler(this.uiSettings, this.uiSettingsColorValueChangedHandler);
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogError(ex, "Failed to attach to UISettings.ColorValuesChanged event");
                        }
                    }
                    else
                        this.Logger.LogError("Cannot find UISettings.ColorValuesChanged event to attach");
                }
#pragma warning restore CA1416
            }

            // start checking external dependencies
            var checkExtDepTasks = new List<Task>();
            foreach (var externalDependency in this.ExternalDependencies)
                checkExtDepTasks.Add(externalDependency.WaitForCheckingAvailability());

            // complete initializing network manager
            await initNetworkManagerTask;
            NetworkManager.Default.PropertyChanged += this.OnNetworkManagerPropertyChanged;

            // initialize product manager
            try
            {
                var pmType = this.ProductManagerImplType;
                if (pmType != null)
                {
                    if (pmType.Assembly.GetName().FullName.StartsWith("CarinaStudio.AppSuite.Product,"))
                    {
                        // initialize
                        await (Task)pmType.GetMethod("InitializeAsync", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(IAppSuiteApplication) })!.Invoke(null, new object?[] { this })!;

                        // get instance
                        this.productManager = (IProductManager)pmType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static)!.GetGetMethod()!.Invoke(null, Array.Empty<object?>())!;
                    }
                    else
                        this.Logger.LogError("Unexpected type of implementation of product manager");
                }
                else
                    this.Logger.LogWarning("No implementation of product manager");
            }
            catch (Exception ex)
            { 
                this.Logger.LogError(ex, "Failed to create implementation of product manager");
            }
            if (this.productManager == null)
            {
                this.Logger.LogDebug("Use mock product manager");
                this.productManager = new MockProductManager(this);
            }
            else
            {
                this.productManager.ProductStateChanged += this.OnProductStateChanged;
                this.ProVersionProductId?.Let(it =>
                    this.OnProductStateChanged(this.productManager, it));
            }

            // complete checking external dependencies
            await Task.WhenAll(checkExtDepTasks);

            // initialize script manager
            await ScriptManager.InitializeAsync(this, this.ScriptManagerImplType);
        }


        // Called when product state changed.
		void OnProductStateChanged(IProductManager productManager, string productId)
		{
			if (productId != this.ProVersionProductId
				|| !productManager.TryGetProductState(productId, out var state))
			{
				return;
			}
			switch (state)
			{
				case ProductState.Activated:
					this.notifyNetworkConnForProductActivationAction?.Cancel();
					goto default;
				case ProductState.Deactivated:
					if (productManager.TryGetProductActivationFailure(productId, out var failure)
						&& failure != ProductActivationFailure.NoNetworkConnection
						&& !this.isReActivatingProVersion)
					{
						this.Logger.LogWarning("Need to reactivate Pro-version because of {failure}", failure);
						this.isReActivatingProVersionNeeded = true;
						this.reActivateProVersionAction?.Schedule();
					}
					break;
				default:
					this.isReActivatingProVersionNeeded = false;
					this.reActivateProVersionAction?.Cancel();
					break;
			}
		}


        /// <summary>
        /// Called to restore main windows asynchronously when starting application.
        /// </summary>
        /// <returns>Task of restoring main windows. The result will be True if main windows have been restored successfully.</returns>
        protected virtual async Task<bool> OnRestoreMainWindowsAsync()
        {
            // load saved states
            using var stateStream = new MemoryStream(this.PersistentState.GetValueOrDefault(MainWindowViewModelStatesKey));
            var jsonDocument = (JsonDocument?)null;
            try
            {
                jsonDocument = JsonDocument.Parse(stateStream);
            }
            catch
            { }
            if (jsonDocument == null)
            {
                this.Logger.LogWarning("No main windows to restore");
                return false;
            }

            // restore
            using (jsonDocument)
            {
                if (jsonDocument.RootElement.ValueKind != JsonValueKind.Array)
                {
                    this.Logger.LogWarning("Invalid root JSON element to restore main windows");
                    return false;
                }
                if (jsonDocument.RootElement.GetArrayLength() <= 0)
                {
                    this.Logger.LogWarning("Empty root JSON element to restore main windows");
                    return false;
                }
                this.Logger.LogWarning("Restore main windows");
                foreach (var stateElement in jsonDocument.RootElement.EnumerateArray())
                    await this.ShowMainWindowAsync(this.OnCreateMainWindowViewModel(stateElement), null);
            }
            if (this.mainWindows.IsNotEmpty())
                return true;
            this.Logger.LogWarning("No main windows restored");
            return false;
        }


        /// <summary>
        /// Called to check whether application needs to enter debug mode or not.
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnSelectEnteringDebugMode()
        {
            if (this.LaunchOptions.TryGetValue(DebugModeRequestedKey, out var value) && value is bool boolValue)
                return boolValue;
            return this.ReleasingType == ApplicationReleasingType.Development;
        }


        // Called when application setting changed.
        void OnSettingChanged(object? sender, SettingChangedEventArgs e) => this.OnSettingChanged(e);


        /// <summary>
        /// Called when application setting changed.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected virtual void OnSettingChanged(SettingChangedEventArgs e)
        {
            if (e.Key == SettingKeys.AcceptNonStableApplicationUpdate)
                _ = this.CheckForApplicationUpdateAsync();
            else if (e.Key == SettingKeys.Culture)
                this.UpdateCultureInfo(true);
            else if (e.Key == SettingKeys.ShowProcessInfo)
            {
                if (!(bool)e.Value)
                    this.processInfoHfUpdateToken = this.processInfoHfUpdateToken.DisposeAndReturnNull();
                else if (this.processInfoHfUpdateToken == null)
                    this.processInfoHfUpdateToken = this.processInfo?.RequestHighFrequencyUpdate();
            }
            else if (e.Key == SettingKeys.ThemeMode)
            {
                if (Platform.IsMacOS && (ThemeMode)e.Value == ThemeMode.System)
                    this.UpdateSystemThemeMode(false);
                this.CheckRestartingMainWindowsNeeded();
            }
            else if (e.Key == SettingKeys.UseCompactUserInterface)
                this.CheckRestartingMainWindowsNeeded();
        }


        /// <summary>
        /// Called when user trying to exit background mode.
        /// </summary>
        /// <returns>True if background mode has been exited successfully.</returns>
        protected virtual bool OnTryExitingBackgroundMode()
        { 
            if (this.LatestActiveWindow != null)
            {
                Controls.WindowExtensions.ActivateAndBringToFront(this.LatestActiveWindow);
                return true;
            }
            return false;
        }


        /// <summary>
        /// Called to upgrade data in <see cref="PersistentState"/>.
        /// </summary>
        /// <param name="persistentState">Persistent state to upgrade.</param>
        /// <param name="oldVersion">Old version.</param>
        /// <param name="newVersion">New version.</param>
        protected virtual void OnUpgradePersistentState(ISettings persistentState, int oldVersion, int newVersion)
        { }


        /// <summary>
        /// Called to upgrade data in <see cref="Settings"/>.
        /// </summary>
        /// <param name="settings">Persistent state to upgrade.</param>
        /// <param name="oldVersion">Old version.</param>
        /// <param name="newVersion">New version.</param>
        protected virtual void OnUpgradeSettings(ISettings settings, int oldVersion, int newVersion)
        { }


        /// <summary>
        /// Called when window closed.
        /// </summary>
        /// <param name="window">Closed window.</param>
        protected virtual void OnWindowClosed(Avalonia.Controls.Window window)
        {
            // detach from window
            if (this.windowObserverTokens.Remove(window, out var tokens))
            {
                foreach (var token in tokens)
                    token.Dispose();
            }

            // update property
            if (this.LatestActiveWindow == window)
            {
                this.LatestActiveWindow = null;
                this.OnPropertyChanged(nameof(LatestActiveWindow));
            }

            // enter background mode or shut down
            if (!this.EnterBackgroundMode() && this.mainWindowHolders.IsEmpty() && this.windows.IsEmpty())
            {
                this.Logger.LogWarning("All windows were closed, start shutting down");
                this.Shutdown();
            }
        }


        /// <summary>
        /// Called when window opened.
        /// </summary>
        /// <param name="window">Opened window.</param>
        protected virtual void OnWindowOpened(Avalonia.Controls.Window window)
        {
            // attach to window
            var tokens = new List<IDisposable>() {
                window.GetObservable(Avalonia.Controls.Window.IsActiveProperty).Subscribe(isActive =>
                {
                    if (isActive && this.LatestActiveWindow != window)
                    {
                        this.LatestActiveWindow = window;
                        this.OnPropertyChanged(nameof(LatestActiveWindow));
                    }
                }),
            };
            this.windowObserverTokens.Add(window, tokens);

            // exit background mode
            this.ExitBackgroundMode();
        }


        // Called when Windows UI color changed.
#pragma warning disable IDE0051
#pragma warning disable IDE0060
        void OnWindowsUIColorValueChanged(object? sender, object result)
        {
            this.SynchronizationContext.Post(() =>
            {
                this.UpdateSystemThemeMode(true);
            });
        }
#pragma warning restore IDE0051
#pragma warning restore IDE0060


#pragma warning disable CA1416
        // Called when user preference changed on Windows
        void OnWindowsUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.Locale)
                this.SynchronizationContext.Post(() => this.UpdateCultureInfo(true));
        }
#pragma warning restore CA1416


        /// <summary>
        /// Get URI of application package manifest.
        /// </summary>
        public virtual Uri? PackageManifestUri { get; }


        // Parse arguments to launch options.
        IDictionary<string, object> ParseArguments(string[] args)
        {
            var launchOptions = new Dictionary<string, object>();
            var argCount = args.Length;
            for (var index = 0; index < argCount;)
            {
                var nextIndex = this.OnParseArguments(args, index, launchOptions);
                if (nextIndex > index)
                    index = nextIndex;
                else
                    ++index;
            }
            return DictionaryExtensions.AsReadOnly(launchOptions);
        }


        /// <summary>
        /// Get persistent state of application.
        /// </summary>
        public override ISettings PersistentState { get => this.persistentState ?? throw new InvalidOperationException("Application is not initialized yet."); }


        /// <summary>
        /// Get version of <see cref="PersistentState"/>.
        /// </summary>
        protected virtual int PersistentStateVersion { get => 1; }


        /// <inheritdoc/>
        public abstract Version? PrivacyPolicyVersion { get; }


        /// <summary>
        /// Product ID of Pro-version.
        /// </summary>
        protected virtual string? ProVersionProductId { get => null; }


        /// <summary>
        /// Get information of current process.
        /// </summary>
        public ProcessInfo ProcessInfo { get => this.processInfo ?? throw new InvalidOperationException("Application is not initialized yet."); }


        /// <inheritdoc/>
        public IProductManager ProductManager { get => this.productManager ?? throw new InvalidOperationException("Application is not initialized yet."); }


        /// <summary>
        /// Get type of implementation of <see cref="IProductManager"/>.
        /// </summary>
        protected virtual Type? ProductManagerImplType { get; }


        /// <summary>
        /// Start purchasing Pro version.
        /// </summary>
        public void PurchaseProVersion() =>
            this.PurchaseProVersionAsync(this.LatestActiveMainWindow);


        /// <inheritdoc/>
        public void PurchaseProVersionAsync(Avalonia.Controls.Window? window)
        {
            this.VerifyAccess();
            var productId = this.ProVersionProductId;
            if (productId != null)
                this.ProductManager.PurchaseProduct(productId, window);
        }


        /// <summary>
        /// Get type of application releasing.
        /// </summary>
        public virtual ApplicationReleasingType ReleasingType { get; } = ApplicationReleasingType.Development;


        // Remove custom resource.
        void RemoveCustomResource(Avalonia.Controls.IResourceProvider resource)
        {
            this.VerifyAccess();
            this.Resources.MergedDictionaries.Remove(resource);
        }


        // Remove custom style.
        void RemoveCustomStyle(IStyle style)
        {
            this.VerifyAccess();
            this.Styles.Remove(style);
        }


        /// <inheritdoc/>
        public bool Restart(string? args = null, bool asAdministrator = false)
        {
            // check state
            this.VerifyAccess();
            if (this.isRestartRequested)
            {
                if (this.restartArgs == args)
                {
                    this.isRestartAsAdminRequested |= asAdministrator;
                    return true;
                }
                this.Logger.LogError("Cannot restart with different arguments");
                return false;
            }

            // update state
            this.Logger.LogWarning("Request restarting");
            this.isRestartRequested = true;
            this.isRestartAsAdminRequested = asAdministrator;
            this.restartArgs = args;

            // shutdown to restart
            this.Shutdown();
            return true;
        }


        /// <inheritdoc/>
        public Task<bool> RestartMainWindowAsync(Window mainWindow)
        {
            // check state
            this.VerifyAccess();
            if (this.IsShutdownStarted)
            {
                this.Logger.LogWarning("Cannot restart main window when shutting down");
                return Task.FromResult(false);
            }
            if (!this.mainWindowHolders.TryGetValue(mainWindow, out var mainWindowHolder))
            {
                this.Logger.LogError("Unknown main window to restart");
                return Task.FromResult(false);
            }
            if (mainWindowHolder.IsRestartingRequested)
                return Task.FromResult(true);

            // restart
            this.Logger.LogWarning("Request restarting main window");
            mainWindowHolder.IsRestartingRequested = true;
            this.SynchronizationContext.Post(() =>
            {
                if (!mainWindow.IsClosed)
                    mainWindow.Close();
            });
            return Task.FromResult(true);
        }


        /// <inheritdoc/>
        public Task<bool> RestartMainWindowsAsync()
        {
            // check state
            this.VerifyAccess();
            if (this.IsShutdownStarted)
            {
                this.Logger.LogWarning("Cannot restart main windows when shutting down");
                return Task.FromResult(false);
            }
            if (this.mainWindowHolders.IsEmpty())
            {
                this.Logger.LogWarning("No main window to restart");
                return Task.FromResult(false);
            }
            if (this.isRestartingMainWindowsRequested)
                return Task.FromResult(true);

            // restart
            this.Logger.LogWarning("Request restarting all {count} main window(s)", this.mainWindowHolders.Count);
            this.isRestartingMainWindowsRequested = true;
            foreach (var mainWindowHolder in this.mainWindowHolders.Values)
                mainWindowHolder.IsRestartingRequested = true;
            this.SynchronizationContext.Post(() =>
            {
                foreach (var mainWindow in this.mainWindowHolders.Keys.ToArray())
                {
                    if (!mainWindow.IsClosed)
                        mainWindow.Close();
                }
            });
            return Task.FromResult(true);
        }


        /// <inheritdoc/>
        public override string RootPrivateDirectoryPath { get => AppDirectoryPath; }


        // Save configuration.
        async Task SaveConfigurationAsync()
        {
            // check state
            if (this.configuration is not ConfigurationImpl config)
                return;

            // save
            this.Logger.LogDebug("Start saving configuration");
            try
            {
                await config.SaveAsync(this.configurationFilePath);
                this.Logger.LogDebug("Complete saving configuration");
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to save configuration to '{configurationFilePath}'", this.configurationFilePath);
            }
        }


        /// <summary>
        /// Save <see cref="PersistentState"/> to file.
        /// </summary>
        /// <returns>Task of saving.</returns>
        public async Task SavePersistentStateAsync()
        {
            // check state
            this.VerifyAccess();
            if (this.persistentState == null)
                return;

            // save
            this.Logger.LogDebug("Start saving persistent state");
            try
            {
                await this.persistentState.SaveAsync(this.persistentStateFilePath);
                this.Logger.LogDebug("Complete saving persistent state");
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to save persistent state to '{persistentStateFilePath}'", this.persistentStateFilePath);
            }
        }


        /// <summary>
        /// Save <see cref="Settings"/> to file.
        /// </summary>
        /// <returns>Task of saving.</returns>
        public async Task SaveSettingsAsync()
        {
            // check state
            this.VerifyAccess();
            if (this.settings == null)
                return;

            // save
            this.Logger.LogDebug("Start saving settings");
            try
            {
                await this.settings.SaveAsync(this.settingsFilePath);
                this.Logger.LogDebug("Complete saving settings");
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to save settings to '{settingsFilePath}'", this.settingsFilePath);
            }
        }


        /// <summary>
        /// Get type of implementation of <see cref="IScriptManager"/>.
        /// </summary>
        protected virtual Type? ScriptManagerImplType { get; }


        // Select current theme mode.
        ThemeMode SelectCurrentThemeMode() => this.Settings.GetValueOrDefault(SettingKeys.ThemeMode).Let(it =>
        {
            if (it == ThemeMode.System)
                return this.systemThemeMode;
            return it;
        });


        // Try sending arguments to multi-instances server.
        bool SendArgumentsToMultiInstancesServer(string[] args)
        {
            try
            {
                // connect
                this.Logger.LogWarning("Try connect to multi-instances server");
                using var clientStream = new NamedPipeClientStream(".", this.multiInstancesServerStreamName, PipeDirection.Out);
                clientStream.Connect(500);

                // send arguments
                this.Logger.LogWarning("Send application arguments to multi-instances server");
                using var writer = new BinaryWriter(clientStream);
                writer.Write(args.Length);
                foreach (var arg in args)
                    writer.Write(arg);
                return true;
            }
            catch (TimeoutException)
            {
                this.Logger.LogWarning("Unable to connect to multi-instances server");
                return false;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Unable to send application arguments to multi-instances server");
                return false;
            }
        }


        /// <summary>
        /// Get application settings.
        /// </summary>
        public override ISettings Settings { get => this.settings ?? throw new InvalidOperationException("Application is not initialized yet."); }


        /// <summary>
        /// Get version of <see cref="Settings"/>.
        /// </summary>
        protected virtual int SettingsVersion { get; } = 2;


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


        /// <summary>
		/// Show application info dialog.
		/// </summary>
		public void ShowApplicationInfoDialog() =>
			_ = this.ShowApplicationInfoDialogAsync(this.LatestActiveMainWindow);


        /// <inheritdoc/>
        public async Task ShowApplicationInfoDialogAsync(Avalonia.Controls.Window? owner)
        {
            this.VerifyAccess();
            using var appInfo = this.CreateApplicationInfoViewModel();
            await new Controls.ApplicationInfoDialog(appInfo).ShowDialog(owner);
        }


        /// <summary>
		/// Show application options dialog.
		/// </summary>
		public void ShowApplicationOptionsDialog() =>
			_ = this.ShowApplicationOptionsDialogAsync(this.LatestActiveMainWindow);


        /// <inheritdoc/>
        public abstract Task ShowApplicationOptionsDialogAsync(Avalonia.Controls.Window? owner, string? section = null);


        // Show application update dialog.
        async Task<bool> ShowAppUpdateDialog(Avalonia.Controls.Window? owner, bool checkAppUpdateWhenOpening)
        {
            // check for update
			using var appUpdater = new AppSuite.ViewModels.ApplicationUpdater();
			var result = await new AppSuite.Controls.ApplicationUpdateDialog(appUpdater)
			{
				CheckForUpdateWhenShowing = checkAppUpdateWhenOpening
			}.ShowDialog(owner);

            // save settings
            _ = this.SaveSettingsAsync();

			// shutdown to update
			if (result == AppSuite.Controls.ApplicationUpdateDialogResult.ShutdownNeeded)
			{
                this.Logger.LogWarning("Shut down to update application");
                this.Shutdown(300); // [Workaround] Prevent crashing on macOS if shutting down immediately after closing dialog.
                return true;
			}
            return false;
        }


        /// <inheritdoc/>
        public Task<bool> ShowMainWindowAsync(Action<Window>? windowCreatedAction = null) => 
            this.ShowMainWindowAsync(null, windowCreatedAction);


        // Create and show main window.
        async Task<bool> ShowMainWindowAsync(ViewModel? viewModel, Action<Window>? windowCreatedAction)
        {
            // check state
            this.VerifyAccess();
            if (this.isShutdownStarted)
            {
                this.Logger.LogError("Cannot show main window when shutting down");
                return false;
            }
            var mainWindowCount = this.mainWindows.Count;
            if (mainWindowCount > 0 && !this.AllowMultipleMainWindows)
            {
                this.Logger.LogError("Multiple main windows are not allowed");
                if (viewModel != null)
                    _ = this.OnDisposeMainWindowViewModelAsync(viewModel);
                return false;
            }

            // update message on splash window
            this.UpdateSplashWindowMessage(this.GetStringNonNull("SplashWindow.ShowingMainWindow"));
            if (this.splashWindow != null)
            {
                if (!double.IsNaN(this.splashWindow.Progress) && this.splashWindow.Progress < 0.99)
                    this.splashWindow.Progress = 1.0;
                await this.splashWindow.WaitForAnimationAsync();
            }

            // update styles and culture
            if (mainWindowCount == 0)
                this.UpdateStyles();

            // create view-model
            var time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;
            if (viewModel == null)
            {
                viewModel = this.OnCreateMainWindowViewModel(null);
                if (time > 0)
                    this.Logger.LogTrace("[Performance] Took {time} ms to create view-model of main window", this.stopWatch.ElapsedMilliseconds - time);
            }

            // creat and show window later if restarting main windows
            if (this.isRestartingMainWindowsRequested)
            {
                this.Logger.LogWarning("Show main window later after closing all main windows");
                this.pendingMainWindowHolders.Add(new MainWindowHolder(viewModel, null, windowCreatedAction));
                return true;
            }

            // create main window
            time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;
            var mainWindow = this.OnCreateMainWindow();
            if (mainWindowCount != this.mainWindows.Count)
            {
                viewModel.Dispose();
                throw new InternalStateCorruptedException("Nested main window showing found.");
            }
            if (time > 0)
                this.Logger.LogTrace("[Performance] Took {time} ms to create main window", this.stopWatch.ElapsedMilliseconds - time);

            // attach to main window
            var mainWindowHolder = new MainWindowHolder(viewModel, mainWindow, windowCreatedAction);
            this.mainWindowHolders[mainWindow] = mainWindowHolder;
            this.mainWindows.Add(mainWindow);
            mainWindow.Closed += this.OnMainWindowClosed;
            mainWindow.GetObservable(Window.HasDialogsProperty).Subscribe(new Observer<bool>(value =>
            {
                this.OnMainWindowDialogsChanged(value);
            }));
            mainWindow.GetObservable(Window.IsActiveProperty).Subscribe(new Observer<bool>(value =>
            {
                this.OnMainWindowActivationChanged(mainWindow, value);
            }));

            this.Logger.LogDebug("Show main window, {count} created", this.mainWindows.Count);

            // show main window
            await this.ShowMainWindowAsync(mainWindowHolder);
            return true;
        }


        // Show given main window.
        async Task ShowMainWindowAsync(MainWindowHolder mainWindowHolder)
        {
            if (mainWindowHolder.Window == null)
            {
                this.Logger.LogError("No main window instance created to show");
                return;
            }
            if (this.splashWindow != null)
            {
                var duration = (this.stopWatch.ElapsedMilliseconds - this.splashWindowShownTime);
                this.Logger.LogTrace("[Performance] Took {duration} ms between showing splash window and main window", duration);
                var delay = MinSplashWindowDuration - duration;
                if (delay > 0)
                {
                    this.Logger.LogDebug("Delay for showing splash window");
                    await Task.Delay((int)delay);
                }
            }
            this.SynchronizationContext.Post(() =>
            {
                // [Workaround] sync culture back to system because it may be resetted
                CultureInfo.CurrentCulture = this.cultureInfo;
                CultureInfo.CurrentUICulture = this.cultureInfo;
                CultureInfo.DefaultThreadCurrentCulture = this.cultureInfo;
                CultureInfo.DefaultThreadCurrentUICulture = this.cultureInfo;

                // setup data context
                mainWindowHolder.Window.DataContext = mainWindowHolder.ViewModel;

                // notify window created
                if (mainWindowHolder.WindowCreatedAction != null)
                {
                    mainWindowHolder.WindowCreatedAction(mainWindowHolder.Window);
                    mainWindowHolder.WindowCreatedAction = null;
                }

                // show window
                mainWindowHolder.Window.Show();
                this.SynchronizationContext.Post(() =>
                {
                    this.splashWindow = this.splashWindow?.Let(it =>
                    {
                        it.Close();
                        return (Controls.SplashWindowImpl?)null;
                    });
                });
            });
        }


        /// <summary>
        /// Close all main windows and shut down application.
        /// </summary>
        /// <param name="delay">Delay to start shutting down process in milliseconds.</param>
        public async void Shutdown(int delay = 0)
        {
            // check state
            this.VerifyAccess();

            // update state
            bool isFirstCall = !this.isShutdownStarted;
            if (isFirstCall)
            {
                this.isShutdownStarted = true;
                this.OnPropertyChanged(nameof(IsShutdownStarted));
            }

            // delay
            if (isFirstCall && delay > 0)
            {
                this.Logger.LogWarning("Delay {delay} ms before starting shutting down process", delay);
                await Task.Delay(delay);
            }

            // close all main windows
            if (this.mainWindowHolders.IsNotEmpty()) // check 'mainWindowHolders' becuse it will be updated after all tasks of closing main window are completed
            {
                if (isFirstCall && this.mainWindows.IsNotEmpty())
                {
                    this.Logger.LogWarning("Close {count} main window(s) to shut down", this.mainWindows.Count);
                    using var stateStream = new MemoryStream();
                    using (var stateWriter = new Utf8JsonWriter(stateStream))
                    {
                        stateWriter.WriteStartArray();
                        foreach (var mainWindow in this.mainWindows.ToArray())
                        {
                            if (mainWindow.DataContext is ViewModels.MainWindowViewModel viewModel)
                                viewModel.SaveState(stateWriter);
                            mainWindow.Close();
                        }
                        stateWriter.WriteEndArray();
                    }
                    this.Logger.LogWarning($"Save main window view-model states");
                    this.PersistentState.SetValue<byte[]>(MainWindowViewModelStatesKey, stateStream.ToArray());
                }
                return;
            }
            else if (isFirstCall)
            {
                this.Logger.LogWarning($"Clear main window view-model states because of shutting down without main windows");
                this.PersistentState.ResetValue(MainWindowViewModelStatesKey);
            }

            // prepare
            this.Logger.LogWarning("Prepare shutting down");
            await this.OnPrepareShuttingDownAsync();

            // shut down
            this.Logger.LogWarning("Shut down");
            (this.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();

            // restart application
            if (isRestartRequested)
            {
                try
                {
                    if (this.isRestartAsAdminRequested)
                        this.Logger.LogWarning("Restart as administrator/superuser");
                    else
                        this.Logger.LogWarning("Restart");
                    var process = new Process().Also(process =>
                    {
                        process.StartInfo.Let(it =>
                        {
                            var exeName = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                            if (exeName.EndsWith("/dotnet") || exeName.EndsWith("\\dotnet.exe", true, null))
                                it.Arguments = $"{Environment.CommandLine} {this.restartArgs}";
                            else
                                it.Arguments = this.restartArgs ?? "";
                            it.FileName = exeName;
                            if (this.isRestartAsAdminRequested && Platform.IsWindows)
                            {
                                it.UseShellExecute = true;
                                it.Verb = "runas";
                            }
                        });
                    });
                    process.Start();
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "Unable to restart");
                }
            }

            // reply to system that application can be shutted down now
            if (Platform.IsMacOS)
            {
                var selector = ObjCSelector.FromName("replyToApplicationShouldTerminate:");
                NSApplication.Current?.SendMessage(selector, true);
            }
        }


        // Update culture info according to settings.
        void UpdateCultureInfo(bool updateStringResources)
        {
            // get culture info
            var cultureInfo = this.Settings.GetValueOrDefault(SettingKeys.Culture).ToCultureInfo(true);
            cultureInfo.ClearCachedData();
            if (object.Equals(cultureInfo, this.cultureInfo))
                return;

            this.Logger.LogDebug("Change culture info to {cultureInfoName}", cultureInfo.Name);

            // change culture info
            this.cultureInfo = cultureInfo;
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            this.OnPropertyChanged(nameof(CultureInfo));

            // update string
            if (updateStringResources)
                this.UpdateStringResources();
        }


        /// <summary>
        /// Get latest checked application update information.
        /// </summary>
        public ApplicationUpdateInfo? UpdateInfo { get; private set; }


        /// <inheritdoc/>
        public abstract Version? UserAgreementVersion { get; }


        // Update log output.
        void UpdateLogOutputToLocalhost()
        {
            // check performance
            var time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;

            // get port
            var port = this.PersistentState.GetValueOrDefault(LogOutputTargetPortKey);
            var config = LogManager.Configuration;
            if (port == 0 || !this.IsDebugMode)
            {
                port = this.IsDebugMode ? this.DefaultLogOutputTargetPort : 0;
                if (port <= 0 || port > ushort.MaxValue)
                {
                    this.Logger.LogDebug("No need to output log to localhost");
                    config.RemoveRuleByName("outputToLocalhost");
                    return;
                }
            }

            // setup target
            var target = config.AllTargets.FirstOrDefault(it => it.Name == "outputToLocalhost") as NLog.Targets.NLogViewerTarget;
            target = new NLog.Targets.NLogViewerTarget("outputToLocalhost")
            {
                Address = new NLog.Layouts.SimpleLayout($"tcp://127.0.0.1:{port}"),
                NewLine = true,
            };
            config.RemoveTarget("outputToLocalhost");
            config.AddTarget(target);
            this.Logger.LogWarning("Set log output target to tcp://127.0.0.1:{port}", port);

            // setup rule
            config.RemoveRuleByName("outputToLocalhost");
            config.LoggingRules.Add(new NLog.Config.LoggingRule().Also(it =>
            {
                it.EnableLoggingForLevels(NLog.LogLevel.Trace, NLog.LogLevel.Fatal);
                it.LoggerNamePattern = "*";
                it.RuleName = "outputToLocalhost";
                it.Targets.Add(target);
            }));

            // update loggers
            LogManager.ReconfigExistingLoggers();

            // check performance
            if (time > 0)
            {
                time = this.stopWatch.ElapsedMilliseconds - time;
                this.Logger.LogTrace("[Performance] Took {time} ms to update log output to localhost", time);
            }
        }


        // Update progress state of dock tile on macOS.
        internal void UpdateMacOSDockTileProgressState() =>
            this.updateMacOSAppDockTileProgressAction?.Schedule();


        /// <summary>
        /// Update message shown on splash window.
        /// </summary>
        /// <param name="message">Message to show.</param>
        protected void UpdateSplashWindowMessage(string message)
        {
            this.VerifyAccess();
            this.splashWindow?.Let(it => it.Message = message);
        }


        /// <summary>
        /// Update progress shown on splash window.
        /// </summary>
        /// <param name="progress">Progress, the range is [0.0, 1.0].</param>
        protected void UpdateSplashWindowProgress(double progress)
        {
            this.VerifyAccess();
            this.splashWindow?.Let(it => it.Progress = progress);
        }


        // Update string resource according to current culture.
        void UpdateStringResources()
        {
            // check performance
            var time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;

            // update string resources
            var resourceUpdated = false;
            if (this.cultureInfo.Name != "en-US")
            {
                if (this.stringResource == null || !object.Equals(this.stringResourceCulture, this.cultureInfo))
                {
                    // remove previous resource
                    if (this.stringResource != null)
                    {
                        this.Resources.MergedDictionaries.Remove(this.stringResource);
                        this.stringResource = null;
                        resourceUpdated = true;
                    }

                    // load built-in resource
                    var builtInResource = this.LoadStringResource(new Uri($"avares://CarinaStudio.AppSuite.Core/Strings/{this.cultureInfo.Name}.axaml"));
                    if (builtInResource != null)
                    {
                        if (Platform.IsLinux)
                        {
                            var builtInResourcesForOS = this.LoadStringResource(new Uri($"avares://CarinaStudio.AppSuite.Core/Strings/{this.cultureInfo.Name}-Linux.axaml"));
                            if (builtInResourcesForOS != null)
                            {
                                builtInResource = new Avalonia.Controls.ResourceDictionary().Also(it =>
                                {
                                    it.MergedDictionaries.Add(builtInResource);
                                    it.MergedDictionaries.Add(builtInResourcesForOS);
                                });
                            }
                            else
                                this.Logger.LogWarning("No built-in string resource for {cultureInfoName} (Linux)", this.cultureInfo.Name);
                        }
                        else if (Platform.IsMacOS)
                        {
                            var builtInResourcesForOS = this.LoadStringResource(new Uri($"avares://CarinaStudio.AppSuite.Core/Strings/{this.cultureInfo.Name}-OSX.axaml")).AsNonNull();
                            if (builtInResourcesForOS != null)
                            {
                                builtInResource = new Avalonia.Controls.ResourceDictionary().Also(it =>
                                {
                                    it.MergedDictionaries.Add(builtInResource);
                                    it.MergedDictionaries.Add(builtInResourcesForOS);
                                });
                            }
                            else
                                this.Logger.LogWarning("No built-in string resource for {cultureInfoName} (Linux)", this.cultureInfo.Name);
                        }
                    }
                    else
                        this.Logger.LogWarning("No built-in string resource for {cultureInfoName}", this.cultureInfo.Name);

                    // load custom resource
                    var resource = (Avalonia.Controls.IResourceProvider?)null;
                    try
                    {
                        resource = this.OnLoadStringResource(this.cultureInfo);
                    }
                    catch
                    {
                        this.Logger.LogWarning("No string resource for {cultureInfoName}", this.cultureInfo.Name);
                    }

                    // merge resources
                    if (builtInResource != null || resource != null)
                    {
                        this.stringResource = new Avalonia.Controls.ResourceDictionary();
                        builtInResource?.Let(it => this.stringResource.MergedDictionaries.Add(it));
                        resource?.Let(it => this.stringResource.MergedDictionaries.Add(it));
                        this.stringResourceCulture = this.cultureInfo;
                        this.Resources.MergedDictionaries.Add(this.stringResource);
                        resourceUpdated = true;
                    }
                }
                else if (!this.Resources.MergedDictionaries.Contains(this.stringResource))
                {
                    this.Resources.MergedDictionaries.Add(this.stringResource);
                    resourceUpdated = true;
                }
            }
            else if (this.stringResource != null)
            {
                this.Resources.MergedDictionaries.Remove(this.stringResource);
                resourceUpdated = true;
            }

            // update fall-back font families
            if (resourceUpdated)
            {
                this.LoadingStrings?.Invoke(this, cultureInfo);
                if (this.Resources.TryGetResource("String/TextBox.FallbackFontFamilies", out var res) && res is string fontFamilies)
                    this.Resources["FontFamily/TextBox.FallbackFontFamilies"] = new FontFamily(fontFamilies);
                else
                    this.Resources.Remove("FontFamily/TextBox.FallbackFontFamilies");
            }

            // raise event
            if (resourceUpdated)
                this.OnStringUpdated(EventArgs.Empty);
            
            // check performance
            if (time > 0)
            {
                time = this.stopWatch.ElapsedMilliseconds - time;
                this.Logger.LogTrace("[Performance] Took {time} ms to update string resources", time);
            }
        }


        // Update styles.
        void UpdateStyles()
        {
            // get theme mode
            var themeMode = this.SelectCurrentThemeMode();
            var useCompactUI = this.Settings.GetValueOrDefault(SettingKeys.UseCompactUserInterface);

            // update styles
            var time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;
            if (this.styles == null 
                || this.stylesThemeMode != themeMode
                || this.isCompactStyles != useCompactUI)
            {
                // setup base theme
                (themeMode switch
                {
                    ThemeMode.Light => Avalonia.Themes.Fluent.FluentThemeMode.Light,
                    _ => Avalonia.Themes.Fluent.FluentThemeMode.Dark,
                }).Let(it =>
                {
                    if (this.baseTheme != null && this.baseTheme.Mode != it)
                        this.baseTheme.Mode = it;
                });
                if (time > 0)
                {
                    var currentTime = this.stopWatch.ElapsedMilliseconds;
                    this.Logger.LogTrace("[Performance] Took {time} ms to setup base theme", currentTime - time);
                    time = currentTime;
                }
                
                // remove current styles
                if (this.styles != null)
                {
                    this.Styles.Remove(this.styles);
                    this.styles = null;
                }

                // load styles
                var subTime = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;
                this.styles = new StyleInclude(new Uri("avares://CarinaStudio.AppSuite.Core/"))
                {
                    Source = useCompactUI
                        ? new Uri($"avares://CarinaStudio.AppSuite.Core/Themes/{themeMode}-Compact.axaml")
                        : new Uri($"avares://CarinaStudio.AppSuite.Core/Themes/{themeMode}.axaml"),
                };
                if (Platform.WindowsVersion == WindowsVersion.Windows7) // Windows 7 specific styles
                {
                    this.styles = new Styles().Also(styles =>
                    {
                        styles.Add(this.styles);
                        styles.Add(new StyleInclude(new Uri("avares://CarinaStudio.AppSuite.Core/"))
                        {
                            Source = new Uri($"avares://CarinaStudio.AppSuite.Core/Themes/{themeMode}-Windows7.axaml"),
                        });
                    });
                }
                else if (Platform.IsMacOS)
                {
                    this.styles = new Styles().Also(styles =>
                    {
                        styles.Add(this.styles);
                        styles.Add(new StyleInclude(new Uri("avares://CarinaStudio.AppSuite.Core/"))
                        {
                            Source = new Uri($"avares://CarinaStudio.AppSuite.Core/Themes/{themeMode}-OSX.axaml"),
                        });
                    });
                }
                if (subTime > 0)
                {
                    var currentTime = this.stopWatch.ElapsedMilliseconds;
                    this.Logger.LogTrace("[Performance] Took {time} ms to load default theme", currentTime - subTime);
                    subTime = currentTime;
                }
                this.styles = this.OnLoadTheme(themeMode, useCompactUI)?.Let(it =>
                {
                    var styles = new Styles()
                    {
                        this.styles,
                        it,
                    };
                    if (subTime > 0)
                    {
                        var currentTime = this.stopWatch.ElapsedMilliseconds;
                        this.Logger.LogTrace("[Performance] Took {time} ms to load theme", currentTime - subTime);
                        subTime = currentTime;
                    }
                    return (IStyle)styles;
                }) ?? this.styles;

                // apply styles
                this.Styles.Add(this.styles);
                this.isCompactStyles = useCompactUI;
                this.stylesThemeMode = themeMode;
                if (subTime > 0)
                {
                    var currentTime = this.stopWatch.ElapsedMilliseconds;
                    this.Logger.LogTrace("[Performance] Took {time} ms to apply theme", currentTime - subTime);
                    subTime = currentTime;
                }
            }
            else if (!this.Styles.Contains(this.styles))
                this.Styles.Add(this.styles);

            // define extra styles
            this.DefineExtraStyles();

            // update accent color
            if (this.Styles.TryGetResource("Color/Accent", out var res) && res is Color accentColor)
            {
                // create resources
                if (this.accentColorResources == null)
                {
                    this.accentColorResources = new Avalonia.Controls.ResourceDictionary();
                    this.Resources.MergedDictionaries.Add(this.accentColorResources);
                }

                // accent colors
                var gammaLight1 = 0.8;
                var gammaLight2 = 0.65;
                var gammaLight3 = 0.5;
                var sysAccentColorDark1 = GammaTransform(accentColor, 1 / gammaLight1);
                var sysAccentColorLight1 = GammaTransform(accentColor, gammaLight1);
                this.accentColorResources["SystemAccentColor"] = accentColor;
                this.accentColorResources["SystemAccentColorDark1"] = sysAccentColorDark1;
                this.accentColorResources["SystemAccentColorDark2"] = GammaTransform(accentColor, 1 / gammaLight2);
                this.accentColorResources["SystemAccentColorDark3"] = GammaTransform(accentColor, 1 / gammaLight3);
                this.accentColorResources["SystemAccentColorLight1"] = sysAccentColorLight1;
                this.accentColorResources["SystemAccentColorLight2"] = GammaTransform(accentColor, gammaLight2);
                this.accentColorResources["SystemAccentColorLight3"] = GammaTransform(accentColor, gammaLight3);
                this.accentColorResources["Color/Accent.WithOpacity.0.75"] = Color.FromArgb((byte)(accentColor.A * 0.75 + 0.5), accentColor.R, accentColor.G, accentColor.B);
                this.accentColorResources["Color/Accent.WithOpacity.0.5"] = Color.FromArgb((byte)(accentColor.A * 0.5 + 0.5), accentColor.R, accentColor.G, accentColor.B);
                this.accentColorResources["Color/Accent.WithOpacity.0.25"] = Color.FromArgb((byte)(accentColor.A * 0.25 + 0.5), accentColor.R, accentColor.G, accentColor.B);
                this.accentColorResources["Color/Accent.Transparent"] = Color.FromArgb(0, accentColor.R, accentColor.G, accentColor.B);

                // icon colors
                this.accentColorResources["Brush.Icon.Active"] = new SolidColorBrush(sysAccentColorLight1);

                // [Workaround] Brushes of Slider
                this.accentColorResources["SliderThumbBackgroundPointerOver"] = new SolidColorBrush(sysAccentColorLight1);
                this.accentColorResources["SliderThumbBackgroundPressed"] = new SolidColorBrush(sysAccentColorDark1);

                // [Workaround] Brushes of ToggleSwitch
                this.accentColorResources["ToggleSwitchFillOnPointerOver"] = new SolidColorBrush(sysAccentColorLight1);
                this.accentColorResources["ToggleSwitchFillOnPressed"] = new SolidColorBrush(sysAccentColorDark1);
                this.accentColorResources["ToggleSwitchStrokeOnPointerOver"] = new SolidColorBrush(sysAccentColorLight1);
                this.accentColorResources["ToggleSwitchStrokeOnPressed"] = new SolidColorBrush(sysAccentColorDark1);
            }

            // update property
            if (this.EffectiveThemeMode != themeMode)
            {
                this.EffectiveThemeMode = themeMode;
                this.OnPropertyChanged(nameof(EffectiveThemeMode));
            }

            // check state
            this.CheckRestartingMainWindowsNeeded();

            // check performance
            if (time > 0)
            {
                time = this.stopWatch.ElapsedMilliseconds - time;
                this.Logger.LogTrace("[Performance] Took {time} ms to update styles", time);
            }
        }


        // Update system theme mode.
        void UpdateSystemThemeMode(bool checkRestartingMainWindows)
        {
            // check performance
            var time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;

            // get current theme
            var themeMode = ThemeMode.Dark;
            if (this.uiSettings != null 
                && this.windowsColorType != null 
                && this.uiSettingsGetColorValueMethod != null
                && this.windowsColorRProperty != null
                && this.windowsColorGProperty != null
                && this.windowsColorBProperty != null)
            {
                var backgroundColor = this.uiSettingsGetColorValueMethod.Invoke(this.uiSettings, new object?[] { this.uiColorTypeBackground }).AsNonNull();
                var r = (byte)this.windowsColorRProperty.GetValue(backgroundColor).AsNonNull();
                var g = (byte)this.windowsColorGProperty.GetValue(backgroundColor).AsNonNull();
                var b = (byte)this.windowsColorBProperty.GetValue(backgroundColor).AsNonNull();
                themeMode = (r + g + b) / 3 < 128
                    ? ThemeMode.Dark
                    : ThemeMode.Light;
            }
            else if (Platform.IsMacOS)
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
                        themeMode = interfaceStyle == null
                            ? ThemeMode.Light
                            : interfaceStyle switch
                            {
                                "Dark" => ThemeMode.Dark,
                                _ => Global.Run(() =>
                                {
                                    this.Logger.LogWarning("Unknown system theme mode on macOS: {interfaceStyle}", interfaceStyle);
                                    return themeMode;
                                }),
                            };
                    }
                    else
                        this.Logger.LogError("Unable to start 'defaults' to check system theme mode on macOS");
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "Unable to check system theme mode on macOS");
                }
            }
            if (this.systemThemeMode == themeMode)
                return;

            this.Logger.LogDebug("System theme mode changed to {themeMode}", themeMode);

            // update state
            this.systemThemeMode = themeMode;
            if (checkRestartingMainWindows)
                this.CheckRestartingMainWindowsNeeded();
            
            // check performance
            if (time > 0)
            {
                time = this.stopWatch.ElapsedMilliseconds - time;
                this.Logger.LogTrace("[Performance] Took {time} ms to update system theme mode", time);
            }
        }


        // Wait for client of multi-instances and handle incoming messages.
        async void WaitForMultiInstancesClient()
        {
            if (this.multiInstancesServerStream == null)
            {
                this.Logger.LogError("No multi-instances server stream");
                return;
            }
            try
            {
                // wait for connection
                this.Logger.LogWarning("Start waiting for multi-instances client");
                await this.multiInstancesServerStream.WaitForConnectionAsync(this.multiInstancesServerCancellationTokenSource.Token);

                // read arguments and parse
                this.Logger.LogWarning("Start reading arguments from multi-instances client");
                var launchOptions = await Task.Run(() =>
                {
                    // read arguments
                    using var reader = new BinaryReader(this.multiInstancesServerStream, Encoding.UTF8);
                    var argCount = Math.Max(0, reader.ReadInt32());
                    var argList = new List<string>(argCount);
                    for (var i = argCount; i > 0; --i)
                        argList.Add(reader.ReadString());

                    // parse arguments
                    return this.ParseArguments(argList.ToArray());
                });

                // handle new instance
                this.OnNewInstanceLaunched(launchOptions);
            }
            catch (Exception ex)
            {
                if (!this.multiInstancesServerCancellationTokenSource.IsCancellationRequested)
                    this.Logger.LogError(ex, "Error occurred while waiting for or handling multi-instances client");
            }
            finally
            {
                // close server stream
                Global.RunWithoutError(() => this.multiInstancesServerStream?.Close());
                this.multiInstancesServerStream = null;

                // handle next connection
                if (!this.multiInstancesServerCancellationTokenSource.IsCancellationRequested)
                {
                    this.SynchronizationContext.Post(() =>
                    {
                        if (this.CreateMultiInstancesServerStream(false, true))
                            this.WaitForMultiInstancesClient();
                    });
                }
            }
        }


        /// <summary>
        /// Wait for completion of animation of splash window.
        /// </summary>
        /// <returns>Task of waiting for completion.</returns>
        protected Task WaitForSplashWindowAnimationAsync() =>
            this.splashWindow?.WaitForAnimationAsync() ?? Task.CompletedTask;


        /// <inheritdoc/>
        public IList<Avalonia.Controls.Window> Windows { get; }


        /// <summary>
        /// Get assembly of Windows SDK.
        /// </summary>
        protected virtual Assembly? WindowsSdkAssembly { get; }


        // Interface implementations.
        string IAppSuiteApplication.Name { get => this.Name ?? ""; }
    }
}
