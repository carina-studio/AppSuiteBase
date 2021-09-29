using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using CarinaStudio.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NLog;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
#if WINDOWS10_0_17763_0_OR_GREATER
using Windows.UI.ViewManagement;
#endif

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Base implementation of <see cref="IAppSuiteApplication{TApp}"/>.
    /// </summary>
    public abstract class AppSuiteApplication<TApp> : Application, IAppSuiteApplication<TApp> where TApp : class, IAppSuiteApplication<TApp>
    {
        // Holder of main window.
        class MainWindowHolder
        {
            // Fields.
            public readonly object? CreationParam;
            public bool IsRestartingRequested;
            public readonly ViewModel<TApp> ViewModel;
            public readonly Window<TApp> Window;

            // Constructor.
            public MainWindowHolder(object? param, ViewModel<TApp> viewModel, Window<TApp> window)
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
            readonly AppSuiteApplication<TApp> app;

            // Constructor.
            public PersistentStateImpl(AppSuiteApplication<TApp> app) : base(JsonSettingsSerializer.Default)
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
            readonly AppSuiteApplication<TApp> app;

            // Constructor.
            public SettingsImpl(AppSuiteApplication<TApp> app) : base(JsonSettingsSerializer.Default)
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


        // Fields.
        ResourceDictionary? accentColorResources;
        CultureInfo cultureInfo = CultureInfo.GetCultureInfo("en-US");
        bool isShutdownStarted;
        readonly Dictionary<Window<TApp>, MainWindowHolder> mainWindowHolders = new Dictionary<Window<TApp>, MainWindowHolder>();
        readonly ObservableList<Window<TApp>> mainWindows = new ObservableList<Window<TApp>>();
        PersistentStateImpl? persistentState;
        readonly string persistentStateFilePath;
        SettingsImpl? settings;
        readonly string settingsFilePath;
        ResourceDictionary? stringResource;
        CultureInfo? stringResourceCulture;
        IStyle? styles;
        ThemeMode stylesThemeMode = ThemeMode.System;
        ThemeMode systemThemeMode = ThemeMode.Dark;
#if WINDOWS10_0_17763_0_OR_GREATER
        readonly UISettings uiSettings = new UISettings();
#endif


        /// <summary>
        /// Initialize new <see cref="AppSuiteApplication{TApp}"/> instance.
        /// </summary>
        protected AppSuiteApplication()
        {
            // create logger
            this.LoggerFactory = new LoggerFactory(new ILoggerProvider[] { this.OnCreateLoggerProvider() });
            this.Logger = this.LoggerFactory.CreateLogger(this.GetType().Name);

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
                var themeMode = this.Settings.GetValueOrDefault(this.ThemeModeSettingKey).Let(it =>
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
        /// Get key of application culture setting.
        /// </summary>
        public virtual SettingKey<ApplicationCulture> CultureSettingKey { get; } = new SettingKey<ApplicationCulture>("Culture", ApplicationCulture.System);


        /// <summary>
        /// Get current culture info of application.
        /// </summary>
        public override CultureInfo CultureInfo { get => cultureInfo; }


        /// <summary>
        /// Get <see cref="AppSuiteApplication{TApp}"/> instance for current process.
        /// </summary>
        public static new AppSuiteApplication<TApp> Current { get => (AppSuiteApplication<TApp>)Application.Current; }


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
        /// Check whether application is running in debug mode or not.
        /// </summary>
        public bool IsDebugMode { get; private set; }


        /// <summary>
        /// Check whether restarting all main windows is needed or not.
        /// </summary>
        public bool IsRestartingMainWindowsNeeded { get; private set; }


        /// <summary>
        /// Check whether application is shutting down or not.
        /// </summary>
        public override bool IsShutdownStarted { get => isShutdownStarted; }


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
        public IList<Window<TApp>> MainWindows { get; }


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
        protected abstract Window<TApp> OnCreateMainWindow(object? param);


        /// <summary>
        /// Called to create view-model of main window.
        /// </summary>
        /// <param name="param">Parameter which is same as passing to <see cref="OnCreateMainWindow(object?)"/>.</param>
        /// <returns>View-model.</returns>
        protected abstract ViewModel<TApp> OnCreateMainWindowViewModel(object? param);


        /// <summary>
        /// Called when Avalonia framework initialized.
        /// </summary>
        public override void OnFrameworkInitializationCompleted()
        {
            // call base
            base.OnFrameworkInitializationCompleted();

            // parse arguments
            var desktopLifetime = (this.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);
            if (desktopLifetime != null)
                this.LaunchOptions = this.ParseArguments(desktopLifetime.Args);

            // enter debug mode
            if (this.OnSelectEnteringDebugMode())
            {
                this.Logger.LogWarning("Enter debug mode");
                this.IsDebugMode = true;
            }

            // attach to lifetime
            if (desktopLifetime != null)
            {
                desktopLifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
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

            // prepare
            _ = this.OnPrepareStartingAsync();
        }


        /// <summary>
        /// Called to load default string resource.
        /// </summary>
        /// <returns>Default string resource.</returns>
        protected virtual IResourceProvider? OnLoadDefaultStringResource() => null;


        /// <summary>
        /// Called to load string resource for given culture.
        /// </summary>
        /// <param name="cultureInfo">Culture info.</param>
        /// <returns>String resource.</returns>
        protected virtual IResourceProvider? OnLoadStringResource(CultureInfo cultureInfo) => null;


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
            if (sender is not Window<TApp> mainWindow)
                return;
            if (!this.mainWindowHolders.TryGetValue(mainWindow, out var mainWindowHolder))
                return;
            this.mainWindowHolders.Remove(mainWindow);
            if (!this.mainWindows.Remove(mainWindow))
                return;
            mainWindow.Closed -= this.OnMainWindowClosed;

            this.Logger.LogDebug($"Main window closed, {this.mainWindows.Count} remains");

            // perform operations
            await this.OnMainWindowClosedAsync(mainWindow);

            // restart main window
            if (mainWindowHolder.IsRestartingRequested)
            {
                this.Logger.LogWarning("Restart main window requested");
                if (this.ShowMainWindow(mainWindowHolder.CreationParam, mainWindowHolder.ViewModel))
                    return;
                this.Logger.LogError("Unable to restart main window");
            }

            // dispose view model
            await mainWindowHolder.ViewModel.WaitForNecessaryTasksAsync();
            mainWindowHolder.ViewModel.Dispose();

            // shut down
            if (this.mainWindows.IsEmpty())
                this.Shutdown();
        }


        /// <summary>
        /// Called to perform asynchronous operations after closing main window.
        /// </summary>
        /// <param name="mainWindow">Closed main window.</param>
        /// <returns>Task of performing operations.</returns>
        protected virtual async Task OnMainWindowClosedAsync(Window mainWindow)
        {
            // save persistent state and settings
            await this.SavePersistentStateAsync();
            await this.SaveSettingsAsync();
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
        protected virtual Task OnPrepareShuttingDownAsync()
        {
            // detach from system event
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SystemEvents.UserPreferenceChanged -= this.OnWindowsUserPreferenceChanged;
#if WINDOWS10_0_17763_0_OR_GREATER
                this.uiSettings.ColorValuesChanged -= this.OnWindowsUIColorValueChanged;
#endif
            }

            // complete
            return Task.CompletedTask;
        }


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
            if (e.Key == this.CultureSettingKey)
                this.UpdateCultureInfo(true);
            else if (e.Key == this.ThemeModeSettingKey)
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
        /// Request restarting given main window.
        /// </summary>
        /// <param name="mainWindow">Main window to restart.</param>
        /// <returns>True if restarting has been accepted.</returns>
        public bool RestartMainWindow(Window<TApp> mainWindow)
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

            // restart
            this.Logger.LogWarning($"Request restarting all {this.mainWindowHolders.Count} main window(s)");
            foreach (var mainWindowHolder in this.mainWindowHolders.Values)
                mainWindowHolder.IsRestartingRequested = true;
            this.SynchronizationContext.Post(() =>
            {
                foreach (var mainWindow in this.mainWindowHolders.Keys)
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


        /// <summary>
        /// Get application settings.
        /// </summary>
        public override ISettings Settings { get => this.settings ?? throw new InvalidOperationException("Application is not initialized yet."); }


        /// <summary>
        /// Get version of <see cref="Settings"/>.
        /// </summary>
        protected virtual int SettingsVersion { get => 1; }


        /// <summary>
        /// Create and show main window.
        /// </summary>
        /// <param name="param">Parameter to create main window.</param>
        /// <returns>True if main window created and shown successfully.</returns>
        public bool ShowMainWindow(object? param = null) => this.ShowMainWindow(param, null);


        // Create and show main window.
        bool ShowMainWindow(object? param, ViewModel<TApp>? viewModel)
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
                return false;
            }

            // update styles
            if (mainWindowCount == 0)
                this.UpdateStyles();

            // create view-model
            if (viewModel == null)
                viewModel = this.OnCreateMainWindowViewModel(param);

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
        void ShowMainWindow(MainWindowHolder mainWindowHolder)
        {
            this.SynchronizationContext.Post(() =>
            {
                mainWindowHolder.Window.DataContext = mainWindowHolder.ViewModel;
                mainWindowHolder.Window.Show();
            });
        }


        /// <summary>
        /// Close all main windows and shut down application.
        /// </summary>
        public async void Shutdown()
        {
            // check state
            this.VerifyAccess();
            if (this.isShutdownStarted)
                return;

            // update state
            this.isShutdownStarted = true;
            this.OnPropertyChanged(nameof(IsShutdownStarted));

            // close all main windows
            if (this.mainWindows.IsNotEmpty())
            {
                this.Logger.LogWarning($"Close {this.mainWindows.Count} main window(s) to shut down");
                foreach (var mainWindow in this.mainWindows)
                    mainWindow.Close();
                return;
            }

            // prepare
            this.Logger.LogWarning("Prepare shutting down");
            await this.OnPrepareShuttingDownAsync();

            // shut down
            this.Logger.LogWarning("Shut down");
            (this.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        }


        /// <summary>
        /// Get key of theme mode setting.
        /// </summary>
        public virtual SettingKey<ThemeMode> ThemeModeSettingKey { get; } = new SettingKey<ThemeMode>("ThemeMode", ThemeMode.System);


        // Update culture info according to settings.
        void UpdateCultureInfo(bool updateStringResources)
        {
            // get culture info
            var cultureInfo = this.Settings.GetValueOrDefault(this.CultureSettingKey).ToCultureInfo();
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
                    var resource = (IResourceProvider?)null;
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
                        this.stringResource = new ResourceDictionary();
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
            var themeMode = this.Settings.GetValueOrDefault(this.ThemeModeSettingKey).Let(it =>
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
                var builtInStyles = new StyleInclude(new Uri("avares://CarinaStudio.AppSuite.Core/")).Also(it =>
                {
                    it.Source = new Uri($"avares://CarinaStudio.AppSuite.Core/Themes/{themeMode}.axaml");
                });
                this.styles = this.OnLoadTheme(themeMode)?.Let(it =>
                {
                    var styles = new Styles();
                    styles.Add(builtInStyles);
                    styles.Add(it);
                    return (IStyle)styles;
                }) ?? builtInStyles;

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
                    this.accentColorResources = new ResourceDictionary();
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
    }
}
