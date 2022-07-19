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
using Avalonia.Styling;
using Avalonia.VisualTree;
using CarinaStudio.AppSuite.Animation;
using CarinaStudio.AppSuite.Product;
using CarinaStudio.AutoUpdate;
using CarinaStudio.AutoUpdate.Resolvers;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Net;
using CarinaStudio.Threading;
using CarinaStudio.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NLog;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        const int MinSplashWindowDuration = 1800;
        const int SplashWindowShowingDuration = 1000;
        const int SplashWindowLoadingThemeDuration = 400;


        // Static fields.
        static readonly SettingKey<string> AgreedPrivacyPolicyVersionKey = new SettingKey<string>("AgreedPrivacyPolicyVersion", "");
        static readonly SettingKey<string> AgreedUserAgreementVersionKey = new SettingKey<string>("AgreedUserAgreementVersion", "");
        static readonly string AppDirectoryPath = Global.Run(() =>
        {
            var mainModule = Process.GetCurrentProcess().MainModule;
            if (mainModule != null && Path.GetFileNameWithoutExtension(mainModule.FileName) != "dotnet")
                return Path.GetDirectoryName(mainModule.FileName) ?? "";
            var codeBase = System.Reflection.Assembly.GetEntryAssembly()?.GetName()?.CodeBase;
            if (codeBase != null && codeBase.StartsWith("file://") && codeBase.Length > 7)
            {
                if (Platform.IsWindows)
                    return Path.GetDirectoryName(codeBase.Substring(8).Replace('/', '\\')) ?? Environment.CurrentDirectory;
                return Path.GetDirectoryName(codeBase.Substring(7)) ?? Environment.CurrentDirectory;
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
        static readonly SettingKey<bool> IsAcceptNonStableApplicationUpdateInitKey = new SettingKey<bool>("IsAcceptNonStableApplicationUpdateInitialized", false);
        static readonly SettingKey<int> LogOutputTargetPortKey = new SettingKey<int>("LogOutputTargetPort");
        static readonly SettingKey<byte[]> MainWindowViewModelStatesKey = new SettingKey<byte[]>("MainWindowViewModelStates", new byte[0]);
        static readonly Regex X11MonitorLineRegex = new Regex("^[\\s]*[\\d]+[\\s]*\\:[\\s]*\\+\\*(?<Name>[^\\s]+)");


        // Fields.
        Avalonia.Controls.ResourceDictionary? accentColorResources;
        readonly LinkedList<MainWindowHolder> activeMainWindowList = new LinkedList<MainWindowHolder>();
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
        readonly Styles extraStyles = new Styles();
        long frameworkInitializedTime;
        HardwareInfo? hardwareInfo;
        bool isRestartAsAdminRequested;
        bool isRestartingMainWindowsRequested;
        bool isRestartRequested;
        bool isShutdownStarted;
        int logOutputTargetPort;
        readonly Dictionary<Window, MainWindowHolder> mainWindowHolders = new Dictionary<Window, MainWindowHolder>();
        readonly ObservableList<Window> mainWindows = new ObservableList<Window>();
        readonly CancellationTokenSource multiInstancesServerCancellationTokenSource = new CancellationTokenSource();
        NamedPipeServerStream? multiInstancesServerStream;
        string multiInstancesServerStreamName = "";
        readonly List<MainWindowHolder> pendingMainWindowHolders = new List<MainWindowHolder>();
        PersistentStateImpl? persistentState;
        readonly string persistentStateFilePath;
        long prepareStartingTime;
        ProcessInfo? processInfo;
        IDisposable? processInfoHfUpdateToken;
        IProductManager? productManager;
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
                    this.Logger.LogError($"***** Unhandled application exception ***** {exceptionObj}");
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
            this.MainWindows = this.mainWindows.AsReadOnly();

            // setup default culture
            CultureInfo.CurrentCulture = this.cultureInfo;
            CultureInfo.CurrentUICulture = this.cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = this.cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = this.cultureInfo;
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
                    }
                    if (Platform.IsLinux)
                        it.With(new X11PlatformOptions());
                    if (setupAction != null)
                        setupAction(it);
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
                return themeMode != this.stylesThemeMode;
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
        /// Check application update information asynchronously.
        /// </summary>
        /// <returns>Task to wait for checking.</returns>
        public async Task<ApplicationUpdateInfo?> CheckUpdateInfoAsync()
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
            this.Logger.LogDebug($"New application version found: {packageVersion}");
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
                    throw new ArgumentException();
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
        Style DefineBrushTransitionsStyle(Func<Selector?, Selector> selector, TimeSpan duration)
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
        public void EnsureClosingToolTipIfWindowIsInactive(Avalonia.Controls.Control control)
        {
            if (!Platform.IsMacOS || control is Avalonia.Controls.Button)
                return;
            new Controls.MacOSToolTipHelper(control);
        }


        /// <inheritdoc/>
        public virtual IEnumerable<ExternalDependency> ExternalDependencies { get; } = new ExternalDependency[0];


        /// <inheritdoc/>
        public abstract int ExternalDependenciesVersion { get; }


        /// <summary>
        /// Get fall-back theme mode if <see cref="IsSystemThemeModeSupported"/> is false.
        /// </summary>
        public virtual ThemeMode FallbackThemeMode { get; } = Platform.IsMacOS ? ThemeMode.Light : ThemeMode.Dark;


        // Transform RGB color values.
        static Color GammaTransform(Color color, double gamma)
        {
            double r = (color.R / 255.0);
            double g = (color.G / 255.0);
            double b = (color.B / 255.0);
            return Color.FromArgb(color.A, (byte)(Math.Pow(r, gamma) * 255 + 0.5), (byte)(Math.Pow(g, gamma) * 255 + 0.5), (byte)(Math.Pow(b, gamma) * 255 + 0.5));
        }


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


        /// <summary>
        /// Get options to launch application which is converted by arguments passed to application.
        /// </summary>
        public IDictionary<string, object> LaunchOptions { get; private set; } = new Dictionary<string, object>().AsReadOnly();


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
                    Message = this.GetString("MainWindow.ConfirmLayoutingLotsOfMainWindows"),
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
                    this.Logger.LogError(ex, $"Failed to load configuration from '{this.configurationFilePath}'");
                }
            }
            else
            {
                this.configuration = new CarinaStudio.Configuration.MemorySettings();
                this.configuration.SettingChanged += this.OnConfigurationChanged;
            }
        }


        /// <inheritdoc/>
        public virtual event EventHandler<IAppSuiteApplication, CultureInfo>? LoadingStrings;


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
                this.Logger.LogError(ex, $"Failed to load persistent state from '{this.persistentStateFilePath}'");
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
                this.Logger.LogTrace($"[Performance] Took {time} ms to load persistent state");
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
                this.Logger.LogError(ex, $"Failed to load settings from '{this.settingsFilePath}'");
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
                this.Logger.LogTrace($"[Performance] Took {time} ms to load persistent state");
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
                this.Logger.LogWarning($"Unable to load string resource from {uri}");
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
                _ = this.CheckUpdateInfoAsync();
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
            this.Logger.LogTrace($"[Performance] Took {this.frameworkInitializedTime - this.creationTime} ms to initialize Avalonia framework");
            
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

            // create hardware and process information
            this.hardwareInfo = new HardwareInfo(this);
            this.processInfo = new ProcessInfo(this);

            // attach to lifetime
            if (desktopLifetime != null)
            {
                desktopLifetime.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
                desktopLifetime.ShutdownRequested += (_, e) =>
                {
                    if (!this.isShutdownStarted)
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
                this.Logger.LogTrace($"[Performance] Took {this.prepareStartingTime - this.frameworkInitializedTime} ms to perform actions before starting");

                // load configuration
                var time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;
                await this.LoadConfigurationAsync();
                if (time > 0)
                    this.Logger.LogTrace($"[Performance] Took {this.stopWatch.ElapsedMilliseconds - time} ms to load configuration");

                // prepare
                await this.OnPrepareStartingAsync();

                // restore main windows
                if (this.IsRestoringMainWindowsRequested)
                    this.OnRestoreMainWindows();
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
        /// <returns><see cref="IStyle"/>.</returns>
        protected virtual IStyle? OnLoadTheme(ThemeMode themeMode) => null;


        // Called when IsActive of main window changed.
        void OnMainWindowActivationChanged(Window mainWindow, bool isActive)
        {
            if (isActive)
            {
                if (Platform.IsMacOS)
                {
                    this.UpdateCultureInfo(true);
                    this.UpdateSystemThemeMode(true);
                }
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
            }
            else if (mainWindowHolder.ActiveListNode.List != null)
                this.activeMainWindowList.Remove(mainWindowHolder.ActiveListNode);
            this.mainWindows.Remove(mainWindow);
            mainWindow.Closed -= this.OnMainWindowClosed;

            this.Logger.LogDebug($"Main window closed, {this.mainWindows.Count} remains");

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
                                if (!this.ShowMainWindow(pendingMainWindowHolder.ViewModel, pendingMainWindowHolder.WindowCreatedAction))
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
                        if (this.ShowMainWindow(mainWindowHolder.ViewModel, mainWindowHolder.WindowCreatedAction))
                            return;
                        this.Logger.LogError("Unable to restart single main window");
                    }
                }
                else
                    this.Logger.LogError("Unable to restart main window when shutting down");
            }

            // dispose view model
            await this.OnDisposeMainWindowViewModelAsync(mainWindowHolder.ViewModel);

            // shut down
            this.mainWindowHolders.Remove(mainWindow);
            if (this.mainWindowHolders.IsEmpty())
                this.Shutdown();
        }


        /// <summary>
        /// Called to perform asynchronous operations after closing main window.
        /// </summary>
        /// <param name="mainWindow">Closed main window.</param>
        /// <param name="viewModel">View-model of main window.</param>
        /// <returns>Task of performing operations.</returns>
        protected virtual async Task OnMainWindowClosedAsync(Window mainWindow, ViewModel viewModel)
        {
            // save settings
            await this.SaveSettingsAsync();
        }


        /// <summary>
        /// Called when new application instance has been launched and be redirected to current application instance.
        /// </summary>
        /// <remarks>The method will be call ONLY when <see cref="IsMultipleProcessesSupported"/> is False.</remarks>
        /// <param name="launchOptions">Options to launch new instance.</param>
        protected virtual void OnNewInstanceLaunched(IDictionary<string, object> launchOptions)
        { }


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
                this.Logger.LogWarning($"Dispose {this.pendingMainWindowHolders.Count} pending view-model of main windows before shutting down");
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
        protected virtual Controls.SplashWindowParams OnPrepareSplashWindow() => new Controls.SplashWindowParams()
        {
            IconUri = AvaloniaLocator.Current.GetService<IAssetLoader>().Let(loader =>
            {
                if (loader != null)
                {
                    var uri = new Uri($"avares://{this.Assembly.GetName().Name}/{this.Name}.ico");
                    if (loader.Exists(uri))
                        return uri;
                    uri = new Uri($"avares://{this.Assembly.GetName().Name}/AppIcon.ico");
                    if (loader.Exists(uri))
                        return uri;
                }
                throw new NotImplementedException("Cannot get default icon.");
            }),
        };


        /// <summary>
        /// Called to prepare application after Avalonia framework initialized.
        /// </summary>
        /// <returns>Task of preparation.</returns>
        protected virtual async Task OnPrepareStartingAsync()
        {
            // load persistent state and settings
            await this.LoadPersistentStateAsync();
            await this.LoadSettingsAsync();
            this.Settings.SettingChanged += this.OnSettingChanged;

            // start log output to localhost
            this.logOutputTargetPort = this.PersistentState.GetValueOrDefault(LogOutputTargetPortKey);
            if (this.logOutputTargetPort == 0)
                this.logOutputTargetPort = this.DefaultLogOutputTargetPort;
            this.UpdateLogOutputToLocalhost();

            // start checking update
            this.PackageManifestUri?.Let(it =>
            {
                this.checkUpdateInfoAction = new ScheduledAction(() =>
                {
                    _ = this.CheckUpdateInfoAsync();
                });
                this.checkUpdateInfoAction?.Schedule();
            });

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

            // create base theme
            var time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;
            this.baseTheme = new Avalonia.Themes.Fluent.FluentTheme(new Uri("avares://Avalonia.Themes.Fluent/"));
            this.Styles.Add(this.baseTheme);
            if (time > 0)
                this.Logger.LogTrace($"[Performance] Took {this.stopWatch.ElapsedMilliseconds - time} ms to create base theme");
            
            // setup effective theme mode
            this.SelectCurrentThemeMode().Let(themeMode =>
            {
                if (this.EffectiveThemeMode != themeMode)
                {
                    this.EffectiveThemeMode = themeMode;
                    this.OnPropertyChanged(nameof(EffectiveThemeMode));
                }
            });

            // show splash window
            if (this.IsSplashWindowNeeded)
            {
                time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;
                var splashWindowParams = this.OnPrepareSplashWindow();
                if (time > 0)
                {
                    var currentTime = this.stopWatch.ElapsedMilliseconds;
                    this.Logger.LogTrace($"[Performance] Took {currentTime - time} ms to prepare parameters of splash window");
                    time = currentTime;
                }
                this.splashWindow = new Controls.SplashWindowImpl()
                {
                    IconUri = splashWindowParams.IconUri,
                };
                if (time > 0)
                {
                    var currentTime = this.stopWatch.ElapsedMilliseconds;
                    this.Logger.LogTrace($"[Performance] Took {currentTime - time} ms to create splash window");
                    time = currentTime;
                }
                this.splashWindow.Show();
                this.splashWindowShownTime = this.stopWatch.ElapsedMilliseconds;
                await Task.Delay(SplashWindowShowingDuration);
            }

            // load built-in resources
            this.UpdateSplashWindowMessage(this.GetStringNonNull("AppSuiteApplication.LoadingTheme", ""));
            await Task.Delay(SplashWindowLoadingThemeDuration);
            this.Resources.MergedDictionaries.Add(new ResourceInclude()
            {
                Source = new Uri("avares://CarinaStudio.AppSuite.Core/Resources/Icons.axaml")
            });

            // setup styles
            this.UpdateStyles();

            // attach to system event
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
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
            }

            // initialize network manager
            await Net.NetworkManager.InitializeAsync(this);

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
                        this.productManager = (IProductManager)pmType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static)!.GetGetMethod()!.Invoke(null, new object?[0])!;
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

            // check for external dependencies
            foreach (var externalDependency in this.ExternalDependencies)
                await externalDependency.WaitForCheckingAvailability();
        }


        /// <summary>
        /// Called to restore main windows when starting application.
        /// </summary>
        protected virtual void OnRestoreMainWindows()
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
                return;
            }

            // restore
            this.Logger.LogWarning("Restore main windows");
            using (jsonDocument)
            {
                if (jsonDocument.RootElement.ValueKind != JsonValueKind.Array)
                    return;
                foreach (var stateElement in jsonDocument.RootElement.EnumerateArray())
                    this.ShowMainWindow(this.OnCreateMainWindowViewModel(stateElement), null);
            }
        }


        /// <summary>
        /// Called to check whether application needs to enter debug mode or not.
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnSelectEnteringDebugMode()
        {
            if (this.LaunchOptions.TryGetValue(DebugModeRequestedKey, out var value) && value is bool boolValue)
                return boolValue;
            return false;
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
                _ = this.CheckUpdateInfoAsync();
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


        // Called when Windows UI color changed.
        void OnWindowsUIColorValueChanged(object? sender, object result)
        {
            this.SynchronizationContext.Post(() =>
            {
                this.UpdateSystemThemeMode(true);
            });
        }


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
            return launchOptions.AsReadOnly();
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


        /// <summary>
        /// Request restarting given main window.
        /// </summary>
        /// <param name="mainWindow">Main window to restart.</param>
        /// <returns>True if restarting has been accepted.</returns>
        public bool RestartMainWindow(Window mainWindow)
        {
            // check state
            this.VerifyAccess();
            if (this.IsShutdownStarted)
            {
                this.Logger.LogWarning("Cannot restart main window when shutting down");
                return false;
            }
            if (!this.mainWindowHolders.TryGetValue(mainWindow, out var mainWindowHolder))
            {
                this.Logger.LogError("Unknown main window to restart");
                return false;
            }
            if (mainWindowHolder.IsRestartingRequested)
                return true;

            // restart
            this.Logger.LogWarning("Request restarting main window");
            mainWindowHolder.IsRestartingRequested = true;
            this.SynchronizationContext.Post(() =>
            {
                if (!mainWindow.IsClosed)
                    mainWindow.Close();
            });
            return true;
        }


        /// <summary>
        /// Request restarting all main windows.
        /// </summary>
        /// <returns>True if restarting has been accepted.</returns>
        public bool RestartMainWindows()
        {
            // check state
            this.VerifyAccess();
            if (this.IsShutdownStarted)
            {
                this.Logger.LogWarning("Cannot restart main windows when shutting down");
                return false;
            }
            if (this.mainWindowHolders.IsEmpty())
            {
                this.Logger.LogWarning("No main window to restart");
                return false;
            }
            if (this.isRestartingMainWindowsRequested)
                return true;

            // restart
            this.Logger.LogWarning($"Request restarting all {this.mainWindowHolders.Count} main window(s)");
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
            return true;
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
                this.Logger.LogError(ex, $"Failed to save configuration to '{this.configurationFilePath}'");
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
                this.Logger.LogError(ex, $"Failed to save persistent state to '{this.persistentStateFilePath}'");
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
                this.Logger.LogError(ex, $"Failed to save settings to '{this.settingsFilePath}'");
            }
        }


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


        /// <inheritdoc/>
        public bool ShowMainWindow(Action<Window>? windowCreatedAction = null) => this.ShowMainWindow(null, windowCreatedAction);


        // Create and show main window.
        bool ShowMainWindow(ViewModel? viewModel, Action<Window>? windowCreatedAction)
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

            // update styles and culture
            if (mainWindowCount == 0)
                this.UpdateStyles();

            // create view-model
            var time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;
            if (viewModel == null)
            {
                viewModel = this.OnCreateMainWindowViewModel(null);
                if (time > 0)
                    this.Logger.LogTrace($"[Performance] Took {this.stopWatch.ElapsedMilliseconds - time} ms to create view-model of main window");
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
                this.Logger.LogTrace($"[Performance] Took {this.stopWatch.ElapsedMilliseconds - time} ms to create main window");

            // attach to main window
            var mainWindowHolder = new MainWindowHolder(viewModel, mainWindow, windowCreatedAction);
            this.mainWindowHolders[mainWindow] = mainWindowHolder;
            this.mainWindows.Add(mainWindow);
            mainWindow.Closed += this.OnMainWindowClosed;
            mainWindow.GetObservable(Window.IsActiveProperty).Subscribe(new Observer<bool>(value =>
            {
                this.OnMainWindowActivationChanged(mainWindow, value);
            }));

            this.Logger.LogDebug($"Show main window, {this.mainWindows.Count} created");

            // show main window
            this.ShowMainWindow(mainWindowHolder);
            return true;
        }


        // Show given main window.
        async void ShowMainWindow(MainWindowHolder mainWindowHolder)
        {
            if (mainWindowHolder.Window == null)
            {
                this.Logger.LogError("No main window instance created to show");
                return;
            }
            if (this.splashWindow != null)
            {
                var duration = (this.stopWatch.ElapsedMilliseconds - this.splashWindowShownTime);
                this.Logger.LogTrace($"[Performance] Took {duration} ms between showing splash window and main window");
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
        public async void Shutdown()
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

            // close all main windows
            if (this.mainWindows.IsNotEmpty())
            {
                if (isFirstCall)
                {
                    this.Logger.LogWarning($"Close {this.mainWindows.Count} main window(s) to shut down");
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
        }


        // Update culture info according to settings.
        void UpdateCultureInfo(bool updateStringResources)
        {
            // get culture info
            var cultureInfo = this.Settings.GetValueOrDefault(SettingKeys.Culture).ToCultureInfo(true);
            cultureInfo.ClearCachedData();
            if (object.Equals(cultureInfo, this.cultureInfo))
                return;

            this.Logger.LogDebug($"Change culture info to {cultureInfo.Name}");

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
            this.Logger.LogWarning($"Set log output target to tcp://127.0.0.1:{port}");

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
                this.Logger.LogTrace($"[Performance] Took {time} ms to update log output to localhost");
            }
        }


        /// <summary>
        /// Update message shown on splash window.
        /// </summary>
        /// <param name="message">Message to show.</param>
        protected void UpdateSplashWindowMessage(string message)
        {
            this.VerifyAccess();
            this.splashWindow?.Let(it => it.Message = message);
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
                                this.Logger.LogWarning($"No built-in string resource for {this.cultureInfo.Name} (Linux)");
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
                                this.Logger.LogWarning($"No built-in string resource for {this.cultureInfo.Name} (Linux)");
                        }
                    }
                    else
                        this.Logger.LogWarning($"No built-in string resource for {this.cultureInfo.Name}");

                    // load custom resource
                    var resource = (Avalonia.Controls.IResourceProvider?)null;
                    try
                    {
                        resource = this.OnLoadStringResource(this.cultureInfo);
                    }
                    catch
                    {
                        this.Logger.LogWarning($"No string resource for {this.cultureInfo.Name}");
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
                this.Logger.LogTrace($"[Performance] Took {time} ms to update string resources");
            }
        }


        // Update styles.
        void UpdateStyles()
        {
            // get theme mode
            var themeMode = this.SelectCurrentThemeMode();

            // update styles
            var time = this.IsDebugMode ? this.stopWatch.ElapsedMilliseconds : 0L;
            if (this.styles == null || this.stylesThemeMode != themeMode)
            {
                // setup base theme
                if (this.baseTheme == null)
                    this.baseTheme = (Avalonia.Themes.Fluent.FluentTheme)this.Styles[0];
                this.baseTheme.Mode = themeMode switch
                {
                    ThemeMode.Light => Avalonia.Themes.Fluent.FluentThemeMode.Light,
                    _ => Avalonia.Themes.Fluent.FluentThemeMode.Dark,
                };
                if (time > 0)
                {
                    var currentTime = this.stopWatch.ElapsedMilliseconds;
                    this.Logger.LogTrace($"[Performance] Took {currentTime - time} ms to setup base theme");
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
                    Source = new Uri($"avares://CarinaStudio.AppSuite.Core/Themes/{themeMode}.axaml"),
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
                    this.Logger.LogTrace($"[Performance] Took {currentTime - subTime} ms to load default theme");
                    subTime = currentTime;
                }
                this.styles = this.OnLoadTheme(themeMode)?.Let(it =>
                {
                    var styles = new Styles();
                    styles.Add(this.styles);
                    styles.Add(it);
                    if (subTime > 0)
                    {
                        var currentTime = this.stopWatch.ElapsedMilliseconds;
                        this.Logger.LogTrace($"[Performance] Took {currentTime - subTime} ms to load theme");
                        subTime = currentTime;
                    }
                    return (IStyle)styles;
                }) ?? this.styles;

                // apply styles
                this.Styles.Add(this.styles);
                this.stylesThemeMode = themeMode;
                if (subTime > 0)
                {
                    var currentTime = this.stopWatch.ElapsedMilliseconds;
                    this.Logger.LogTrace($"[Performance] Took {currentTime - subTime} ms to apply theme");
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
                var sysAccentColorDark1 = GammaTransform(accentColor, 2.8);
                var sysAccentColorLight1 = GammaTransform(accentColor, 0.682);
                this.accentColorResources["SystemAccentColor"] = accentColor;
                this.accentColorResources["SystemAccentColorDark1"] = sysAccentColorDark1;
                this.accentColorResources["SystemAccentColorDark2"] = GammaTransform(accentColor, 4.56);
                this.accentColorResources["SystemAccentColorDark3"] = GammaTransform(accentColor, 5.365);
                this.accentColorResources["SystemAccentColorLight1"] = sysAccentColorLight1;
                this.accentColorResources["SystemAccentColorLight2"] = GammaTransform(accentColor, 0.431);
                this.accentColorResources["SystemAccentColorLight3"] = GammaTransform(accentColor, 0.006);
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
                this.Logger.LogTrace($"[Performance] Took {time} ms to update styles");
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
                                    this.Logger.LogWarning($"Unknown system theme mode on macOS: {interfaceStyle}");
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

            this.Logger.LogDebug($"System theme mode changed to {themeMode}");

            // update state
            this.systemThemeMode = themeMode;
            if (checkRestartingMainWindows)
                this.CheckRestartingMainWindowsNeeded();
            
            // check performance
            if (time > 0)
            {
                time = this.stopWatch.ElapsedMilliseconds - time;
                this.Logger.LogTrace($"[Performance] Took {time} ms to update system theme mode");
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
        /// Get assembly of Windows SDK.
        /// </summary>
        protected virtual Assembly? WindowsSdkAssembly { get; }


        // Interface implementations.
        string IAppSuiteApplication.Name { get => this.Name ?? ""; }
    }
}
