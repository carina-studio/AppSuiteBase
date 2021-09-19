using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuiteBase
{
    /// <summary>
    /// Base implementation of <see cref="IApplication"/>.
    /// </summary>
    public abstract class Application : CarinaStudio.Application
    {
        // Implementation of PersistentState.
        class PersistentStateImpl : PersistentSettings
        {
            // Fields.
            readonly Application app;

            // Constructor.
            public PersistentStateImpl(Application app) : base(JsonSettingsSerializer.Default)
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
            readonly Application app;

            // Constructor.
            public SettingsImpl(Application app) : base(JsonSettingsSerializer.Default)
            {
                this.app = app;
            }

            // Implementations.
            protected override void OnUpgrade(int oldVersion) => this.app.OnUpgradeSettings(this, oldVersion, this.Version);
            public override int Version { get => this.app.SettingsVersion; }
        }


        // Fields.
        CultureInfo cultureInfo = CultureInfo.CurrentCulture;
        bool isShutdownStarted;
        readonly ObservableList<Window> mainWindows = new ObservableList<Window>();
        PersistentStateImpl? persistentState;
        readonly string persistentStateFilePath;
        SettingsImpl? settings;
        readonly string settingsFilePath;


        /// <summary>
        /// Initialize new <see cref="Application"/> instance.
        /// </summary>
        protected Application()
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


        /// <summary>
        /// Get current culture info of application.
        /// </summary>
        public override CultureInfo CultureInfo { get => cultureInfo; }


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
        /// Check whether application is shutting down or not.
        /// </summary>
        public override bool IsShutdownStarted { get => isShutdownStarted; }


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
        /// Called when Avalonia framework initialized.
        /// </summary>
        public override void OnFrameworkInitializationCompleted()
        {
            // call base
            base.OnFrameworkInitializationCompleted();

            // attach to lifetime
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
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


        // Called when main window closed.
        async void OnMainWindowClosed(object? sender, EventArgs e)
        {
            // detach from main window
            if (sender is not Window mainWindow)
                return;
            if (!this.mainWindows.Remove(mainWindow))
                return;
            mainWindow.Closed -= this.OnMainWindowClosed;

            this.Logger.LogDebug($"Main window closed, {this.mainWindows.Count} remains");

            // perform operations
            await this.OnMainWindowClosedAsync(mainWindow);

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
        /// Called to prepare application after Avalonia framework initialized.
        /// </summary>
        /// <returns>Task of preparation.</returns>
        protected virtual async Task OnPrepareStartingAsync()
        {
            // load persistent state and settings
            await this.LoadPersistentStateAsync();
            await this.LoadSettingsAsync();
        }


        /// <summary>
        /// Called to perform asynchronous operations before shutting down.
        /// </summary>
        /// <returns>Task of performing operations.</returns>
        protected virtual Task OnPrepareShuttingDownAsync() => Task.CompletedTask;


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
        /// Get persistent state of application.
        /// </summary>
        public override ISettings PersistentState { get => this.persistentState ?? throw new InvalidOperationException("Application is not initialized yet."); }


        /// <summary>
        /// Get version of <see cref="PersistentState"/>.
        /// </summary>
        protected virtual int PersistentStateVersion { get => 1; }


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
        protected bool ShowMainWindow(object? param = null)
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

            // create main window
            var mainWindow = this.OnCreateMainWindow(param);
            if (mainWindowCount != this.mainWindows.Count)
                throw new InternalStateCorruptedException("Nested main window showing found.");

            // attach to main window
            this.mainWindows.Add(mainWindow);
            mainWindow.Closed += this.OnMainWindowClosed;

            this.Logger.LogDebug($"Show main window, {this.mainWindows.Count} created");

            // show main window
            this.SynchronizationContext.Post(mainWindow.Show);
            return true;
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
    }
}
