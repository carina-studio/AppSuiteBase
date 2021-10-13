using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if WINDOWS10_0_17763_0_OR_GREATER
using Windows.UI.ViewManagement;
#endif

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Base implementation of <see cref="IAppSuiteApplication"/>.
    /// </summary>
    public abstract class AppSuiteApplication : Application, IAppSuiteApplication
    {
        // Holder of main window.
        class MainWindowHolder
        {
            // Fields.
            public readonly object? CreationParam;
            public bool IsRestartingRequested;
            public readonly ViewModel ViewModel;
            public readonly Window? Window;

            // Constructor.
            public MainWindowHolder(object? param, ViewModel viewModel, Window? window)
            {
                this.CreationParam = param;
                this.ViewModel = viewModel;
                this.Window = window;
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


        // Constants.
        const string DebugModeRequestedKey = "IsDebugModeRequested";
        const string RestoreStateRequestedKey = "IsRestoringStateRequested";
        const int MinSplashWindowDuration = 2000;
        const int SplashWindowShowingDuration = 1000;
        const int UpdateCheckingInterval = 3600000; // 1 hr


        // Fields.
        Avalonia.Controls.ResourceDictionary? accentColorResources;
        ScheduledAction? checkUpdateInfoAction;
        CultureInfo cultureInfo = CultureInfo.GetCultureInfo("en-US");
        HardwareInfo? hardwareInfo;
        bool isRestartingMainWindowsRequested;
        bool isShutdownStarted;
        readonly Dictionary<Window, MainWindowHolder> mainWindowHolders = new Dictionary<Window, MainWindowHolder>();
        readonly ObservableList<Window> mainWindows = new ObservableList<Window>();
        readonly CancellationTokenSource multiInstancesServerCancellationTokenSource = new CancellationTokenSource();
        NamedPipeServerStream? multiInstancesServerStream;
        string multiInstancesServerStreamName = "";
        readonly List<MainWindowHolder> pendingMainWindowHolders = new List<MainWindowHolder>();
        PersistentStateImpl? persistentState;
        readonly string persistentStateFilePath;
        ProcessInfo? processInfo;
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
#if WINDOWS10_0_17763_0_OR_GREATER
        readonly UISettings uiSettings = new UISettings();
#endif


        /// <summary>
        /// Initialize new <see cref="AppSuiteApplication"/> instance.
        /// </summary>
        protected AppSuiteApplication()
        {
            // create logger
            LogManager.Configuration = new NLog.Config.LoggingConfiguration().Also(it =>
            {
                var fileTarget = new NLog.Targets.FileTarget("file")
                {
                    FileName = Path.Combine(this.RootPrivateDirectoryPath, "Log", "log.txt"),
                    Layout = "${longdate} ${pad:padding=-5:inner=${processid}} ${pad:padding=-4:inner=${threadid}} ${pad:padding=-5:inner=${level:uppercase=true}} ${logger:shortName=true}: ${message} ${all-event-properties} ${exception:format=tostring}",
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
            this.persistentStateFilePath = Path.Combine(this.RootPrivateDirectoryPath, "PersistentState.json");
            this.settingsFilePath = Path.Combine(this.RootPrivateDirectoryPath, "Settings.json");

            // setup properties
            this.MainWindows = this.mainWindows.AsReadOnly();
        }


        /// <summary>
        /// Check whether multiple main windows are allowed or not.
        /// </summary>
        protected virtual bool AllowMultipleMainWindows { get => false; }


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
            this.checkUpdateInfoAction?.Reschedule(UpdateCheckingInterval);

            // check update by package manifest
            var stopWatch = new Stopwatch().Also(it => it.Start());
            var packageResolver = new JsonPackageResolver() { Source = new WebRequestStreamProvider(packageManifestUri) };
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
            if (!this.ForceAcceptingUpdateInfo && packageVersion <= this.Assembly.GetName().Version)
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


        // Create server stream for multi-instances.
        bool CreateMultiInstancesServerStream(bool printErrorLog = true)
        {
            if (this.multiInstancesServerStream != null)
                return true;
            if (this.IsShutdownStarted)
            {
                this.Logger.LogWarning("No need to create multi-instances server stream when shutting down");
                return false;
            }
            try
            {
                this.multiInstancesServerStream = new NamedPipeServerStream(this.multiInstancesServerStreamName, PipeDirection.In, 1);
                this.Logger.LogWarning("Multi-instances server stream created");
                return true;
            }
            catch (Exception ex)
            {
                if (printErrorLog)
                    this.Logger.LogError(ex, "Unable to create multi-instances server stream");
                return false;
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
        /// Get theme mode which is currently applied to application.
        /// </summary>
        public ThemeMode EffectiveThemeMode { get; private set; } = ThemeMode.Dark;


        /// <summary>
        /// Get fall-back theme mode if <see cref="IsSystemThemeModeSupported"/> is false.
        /// </summary>
        public virtual ThemeMode FallbackThemeMode { get; } = Platform.IsMacOS ? ThemeMode.Light : ThemeMode.Dark;


        /// <summary>
        /// Check whether retrieved application update info should be always accepted or not. This is designed for debugging purpose.
        /// </summary>
        protected virtual bool ForceAcceptingUpdateInfo { get; } = false;


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
            return null;
        }


        /// <summary>
        /// Get information of hardware.
        /// </summary>
        public HardwareInfo HardwareInfo { get => this.hardwareInfo ?? throw new InvalidOperationException("Application is not initialized yet."); }



        /// <summary>
        /// Check whether application is running in debug mode or not.
        /// </summary>
        public bool IsDebugMode { get; private set; }


        /// <summary>
        /// Check whether multiple application processes is supported or not.
        /// </summary>
        protected virtual bool IsMultipleProcessesSupported { get; } = false;


        /// <summary>
        /// Check whether restarting all main windows is needed or not.
        /// </summary>
        public bool IsRestartingMainWindowsNeeded { get; private set; }


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
#if WINDOWS10_0_17763_0_OR_GREATER
                return true;
#else
                return false;
#endif
            }
        }


        /// <summary>
        /// Get options to launch application which is converted by arguments passed to application.
        /// </summary>
        public IDictionary<string, object> LaunchOptions { get; private set; } = new Dictionary<string, object>().AsReadOnly();


        /// <summary>
        /// Load <see cref="PersistentState"/> from file.
        /// </summary>
        /// <returns>Task of loading.</returns>
        public async Task LoadPersistentStateAsync()
        {
            // check state
            this.VerifyAccess();

            // create persistent state
            if (this.persistentState == null)
                this.persistentState = new PersistentStateImpl(this);

            // load from file
            this.Logger.LogDebug("Start loading persistent state");
            try
            {
                await this.persistentState.LoadAsync(this.persistentStateFilePath);
                this.Logger.LogDebug("Complete loading persistent state");
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, $"Failed to load persistent state from '{this.persistentStateFilePath}'");
            }
        }


        /// <summary>
        /// Load <see cref="Settings"/> from file.
        /// </summary>
        /// <returns>Task of loading.</returns>
        public async Task LoadSettingsAsync()
        {
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

            // disable accepting non-stable update for stable build
            if (this.settings.GetRawValue(SettingKeys.AcceptNonStableApplicationUpdate) == null && this.ReleasingType == ApplicationReleasingType.Stable)
                this.settings.SetValue<bool>(SettingKeys.AcceptNonStableApplicationUpdate, false);

            // Fall-back to default theme mode if 'System' is unsuported
            if (this.settings.GetValueOrDefault(SettingKeys.ThemeMode) == ThemeMode.System
                && !this.IsSystemThemeModeSupported)
            {
                this.settings.SetValue<ThemeMode>(SettingKeys.ThemeMode, this.FallbackThemeMode);
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
        /// Get list of main windows.
        /// </summary>
        public IList<Window> MainWindows { get; }


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
        /// <param name="param">Parameter to create main window.</param>
        /// <returns>Main window.</returns>
        protected abstract Window OnCreateMainWindow(object? param);


        /// <summary>
        /// Called to create view-model of main window.
        /// </summary>
        /// <param name="param">Parameter which is same as passing to <see cref="OnCreateMainWindow(object?)"/>.</param>
        /// <returns>View-model.</returns>
        protected abstract ViewModel OnCreateMainWindowViewModel(object? param);


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
                    return nameBuilder.ToString();
                });
                if (Platform.IsLinux)
                {
                    // [workaround] treat process as client first becase limitation of max server instance seems not working on Linux
                    if (this.SendArgumentsToMultiInstancesServer(desktopLifetime.Args))
                    {
                        this.SynchronizationContext.Post(() => desktopLifetime.Shutdown());
                        return;
                    }
                }
                if (this.CreateMultiInstancesServerStream(false))
                    this.WaitForMultiInstancesClient();
                else
                {
                    this.SendArgumentsToMultiInstancesServer(desktopLifetime.Args);
                    this.SynchronizationContext.Post(() => desktopLifetime.Shutdown());
                    return;
                }
            }

            // create hardware and process information
            this.hardwareInfo = new HardwareInfo(this);
            this.processInfo = new ProcessInfo(this);

            // parse arguments
            if (desktopLifetime != null)
                this.LaunchOptions = this.ParseArguments(desktopLifetime.Args);

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
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "Failed to setup default NLog rule");
                }
            }

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

            // start checking update
            this.PackageManifestUri?.Let(it =>
            {
                this.checkUpdateInfoAction = new ScheduledAction(() =>
                {
                    _ = this.CheckUpdateInfoAsync();
                });
                this.checkUpdateInfoAction?.Schedule();
            });

            // prepare
            this.SynchronizationContext.Post(() =>
            {
                if (!this.IsShutdownStarted)
                    _ = this.OnPrepareStartingAsync();
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


        // Called when main window closed.
        async void OnMainWindowClosed(object? sender, EventArgs e)
        {
            // detach from main window
            if (sender is not Window mainWindow)
                return;
            if (!this.mainWindowHolders.TryGetValue(mainWindow, out var mainWindowHolder))
                return;
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
                        this.pendingMainWindowHolders.Add(new MainWindowHolder(mainWindowHolder.CreationParam, mainWindowHolder.ViewModel, null));
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
                                if (!this.ShowMainWindow(pendingMainWindowHolder.CreationParam, pendingMainWindowHolder.ViewModel))
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
                        if (this.ShowMainWindow(mainWindowHolder.CreationParam, mainWindowHolder.ViewModel))
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
            // save persistent state and settings
            await this.SavePersistentStateAsync();
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
                case "-debug":
                    launchOptions[DebugModeRequestedKey] = true;
                    break;
                case "-restore":
                    launchOptions[RestoreStateRequestedKey] = true;
                    break;
                default:
                    this.Logger.LogWarning($"Unknown argument: {arg}");
                    break;
            }
            return (index + 1);
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
#if WINDOWS10_0_17763_0_OR_GREATER
                this.uiSettings.ColorValuesChanged -= this.OnWindowsUIColorValueChanged;
#endif
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
        }


        /// <summary>
        /// Called to prepare showing splash window when launching application.
        /// </summary>
        /// <returns>Parameters of splash window.</returns>
        protected virtual Controls.SplashWindowParams OnPrepareSplashWindow() => new Controls.SplashWindowParams()
        {
            IconUri = new Uri($"avares://{this.Assembly.GetName().Name}/AppIcon.ico"),
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

            // setup culture info
            this.UpdateCultureInfo(false);

            // load strings
            this.Resources.MergedDictionaries.Add(new ResourceInclude()
            {
                Source = new Uri("avares://CarinaStudio.AppSuite.Core/Strings/Default.axaml")
            });
            this.OnLoadDefaultStringResource()?.Let(it => this.Resources.MergedDictionaries.Add(it));
            this.UpdateStringResources();

            // load built-in resources
            this.Resources.MergedDictionaries.Add(new ResourceInclude()
            {
                Source = new Uri("avares://CarinaStudio.AppSuite.Core/Resources/Icons.axaml")
            });

            // setup styles
            this.UpdateSystemThemeMode(false);
            this.UpdateStyles();

            // show splash window
            if (this.IsSplashWindowNeeded)
            {
                var splashWindowParams = this.OnPrepareSplashWindow();
                this.splashWindow = new Controls.SplashWindowImpl()
                {
                    IconUri = splashWindowParams.IconUri,
                };
                this.splashWindow.Show();
                this.splashWindowShownTime = this.stopWatch.ElapsedMilliseconds;
                await Task.Delay(SplashWindowShowingDuration);
            }

            // attach to system event
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SystemEvents.UserPreferenceChanged += this.OnWindowsUserPreferenceChanged;
#if WINDOWS10_0_17763_0_OR_GREATER
                this.uiSettings.ColorValuesChanged += this.OnWindowsUIColorValueChanged;
#endif
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
            else if (e.Key == SettingKeys.ThemeMode)
                this.CheckRestartingMainWindowsNeeded();
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


#if WINDOWS10_0_17763_0_OR_GREATER
        // Called when Windows UI color changed.
        void OnWindowsUIColorValueChanged(UISettings sender, object result)
        {
            this.SynchronizationContext.Post(() =>
            {
                this.UpdateSystemThemeMode(true);
            });
        }
#endif


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


        /// <summary>
        /// Get information of current process.
        /// </summary>
        public ProcessInfo ProcessInfo { get => this.processInfo ?? throw new InvalidOperationException("Application is not initialized yet."); }


        /// <summary>
        /// Get type of application releasing.
        /// </summary>
        public virtual ApplicationReleasingType ReleasingType { get; } = ApplicationReleasingType.Development;


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


        /// <summary>
        /// Create and show main window.
        /// </summary>
        /// <param name="param">Parameter to create main window.</param>
        /// <returns>True if main window created and shown successfully.</returns>
        public bool ShowMainWindow(object? param = null) => this.ShowMainWindow(param, null);


        // Create and show main window.
        bool ShowMainWindow(object? param, ViewModel? viewModel)
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

            // update styles
            if (mainWindowCount == 0)
                this.UpdateStyles();

            // create view-model
            if (viewModel == null)
                viewModel = this.OnCreateMainWindowViewModel(param);

            // creat and show window later if restarting main windows
            if (this.isRestartingMainWindowsRequested)
            {
                this.Logger.LogWarning("Show main window later after closing all main windows");
                this.pendingMainWindowHolders.Add(new MainWindowHolder(param, viewModel, null));
                return true;
            }

            // create main window
            var mainWindow = this.OnCreateMainWindow(param);
            if (mainWindowCount != this.mainWindows.Count)
            {
                viewModel.Dispose();
                throw new InternalStateCorruptedException("Nested main window showing found.");
            }

            // attach to main window
            var mainWindowHolder = new MainWindowHolder(param, viewModel, mainWindow);
            this.mainWindowHolders[mainWindow] = mainWindowHolder;
            this.mainWindows.Add(mainWindow);
            mainWindow.Closed += this.OnMainWindowClosed;

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
                var delay = MinSplashWindowDuration - (this.stopWatch.ElapsedMilliseconds - this.splashWindowShownTime);
                if (delay > 0)
                {
                    this.Logger.LogDebug("Delay for showing splash window");
                    await Task.Delay((int)delay);
                }
            }
            this.SynchronizationContext.Post(() =>
            {
                mainWindowHolder.Window.DataContext = mainWindowHolder.ViewModel;
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
                    foreach (var mainWindow in this.mainWindows.ToArray())
                        mainWindow.Close();
                }
                return;
            }

            // prepare
            this.Logger.LogWarning("Prepare shutting down");
            await this.OnPrepareShuttingDownAsync();

            // shut down
            this.Logger.LogWarning("Shut down");
            (this.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        }


        // Update culture info according to settings.
        void UpdateCultureInfo(bool updateStringResources)
        {
            // get culture info
            var cultureInfo = this.Settings.GetValueOrDefault(SettingKeys.Culture).ToCultureInfo();
            cultureInfo.ClearCachedData();
            if (object.Equals(cultureInfo, this.cultureInfo))
                return;

            this.Logger.LogDebug($"Change culture info to {cultureInfo.Name}");

            // change culture info
            this.cultureInfo = cultureInfo;
            this.OnPropertyChanged(nameof(CultureInfo));

            // update string
            if (updateStringResources)
                this.UpdateStringResources();
        }


        /// <summary>
        /// Get latest checked application update information.
        /// </summary>
        public ApplicationUpdateInfo? UpdateInfo { get; private set; }


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
                    var builtInResource = new ResourceInclude().Let(it =>
                    {
                        it.Source = new Uri($"avares://CarinaStudio.AppSuite.Core/Strings/{this.cultureInfo.Name}.axaml");
                        try
                        {
                            _ = it.Loaded;  // trigger error if resource not found
                            return it;
                        }
                        catch
                        {
                            this.Logger.LogWarning($"No built-in string resource for {this.cultureInfo.Name}");
                            return null;
                        }
                    });

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
                if (this.Resources.TryGetResource("String/TextBox.FallbackFontFamilies", out var res) && res is string fontFamilies)
                    this.Resources["FontFamily/TextBox.FallbackFontFamilies"] = new FontFamily(fontFamilies);
                else
                    this.Resources.Remove("FontFamily/TextBox.FallbackFontFamilies");
            }

            // raise event
            if (resourceUpdated)
                this.OnStringUpdated(EventArgs.Empty);
        }


        // Update styles.
        void UpdateStyles()
        {
            // get theme mode
            var themeMode = this.Settings.GetValueOrDefault(SettingKeys.ThemeMode).Let(it =>
            {
                if (it == ThemeMode.System)
                    return this.systemThemeMode;
                return it;
            });

            // update styles
            if (this.styles == null || this.stylesThemeMode != themeMode)
            {
                // remove current styles
                if (this.styles != null)
                {
                    this.Styles.Remove(this.styles);
                    this.styles = null;
                }

                // load styles
                this.styles = new StyleInclude(new Uri("avares://CarinaStudio.AppSuite.Core/"))
                {
                    Source = new Uri($"avares://CarinaStudio.AppSuite.Core/Themes/{themeMode}.axaml"),
                };
                if (Platform.IsWindows)
                {
                    var osVersion = Environment.OSVersion.Version;
                    if (osVersion.Major < 10 || (osVersion.Major == 10 && osVersion.Build < 22000))
                    {
                        this.styles = new Styles().Also(it =>
                        {
                            it.Add(this.styles);
                            it.Add(new StyleInclude(new Uri("avares://CarinaStudio.AppSuite.Core/"))
                            {
                                Source = new Uri($"avares://CarinaStudio.AppSuite.Core/Themes/{themeMode}-Windows10.axaml"),
                            });
                        });
                    }
                }
                this.styles = this.OnLoadTheme(themeMode)?.Let(it =>
                {
                    var styles = new Styles();
                    styles.Add(this.styles);
                    styles.Add(it);
                    return (IStyle)styles;
                }) ?? this.styles;

                // apply styles
                this.Styles.Add(this.styles);
                this.stylesThemeMode = themeMode;
            }
            else if (!this.Styles.Contains(this.styles))
                this.Styles.Add(this.styles);

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

                // icon colors
                this.accentColorResources["Brush.Icon.Active"] = new SolidColorBrush(sysAccentColorLight1);
                this.accentColorResources["Brush.Icon.LogProfile"] = new SolidColorBrush(sysAccentColorLight1);

                // [Workaround] Brushes of ToggleButton
                this.accentColorResources["ToggleButtonBackgroundCheckedPointerOver"] = new SolidColorBrush(sysAccentColorDark1);

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
        }


        // Update system theme mode.
        void UpdateSystemThemeMode(bool checkRestartingMainWindows)
        {
            // get current theme
#if WINDOWS10_0_17763_0_OR_GREATER
            var backgroundColor = this.uiSettings.GetColorValue(UIColorType.Background);
            var themeMode = (backgroundColor.R + backgroundColor.G +backgroundColor.B) / 3 < 128
                ? ThemeMode.Dark
                : ThemeMode.Light;
#else
            var themeMode = ThemeMode.Dark;
#endif
            if (this.systemThemeMode == themeMode)
                return;

            this.Logger.LogDebug($"System theme mode changed to {themeMode}");

            // update state
            this.systemThemeMode = themeMode;
            if (checkRestartingMainWindows)
                this.CheckRestartingMainWindowsNeeded();
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
                Global.RunWithoutError(() => this.multiInstancesServerStream.Close());
                this.multiInstancesServerStream = null;

                // handle next connection
                if (!this.multiInstancesServerCancellationTokenSource.IsCancellationRequested)
                {
                    this.SynchronizationContext.Post(() =>
                    {
                        if (this.CreateMultiInstancesServerStream())
                            this.WaitForMultiInstancesClient();
                    });
                }
            }
        }


        // Interface implementations.
        string IAppSuiteApplication.Name { get => this.Name ?? ""; }
    }
}
