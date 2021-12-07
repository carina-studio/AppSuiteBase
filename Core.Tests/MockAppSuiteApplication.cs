using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Mock implementation of <see cref="IAppSuiteApplication"/> for testing purpose.
    /// </summary>
    public class MockAppSuiteApplication : IAppSuiteApplication
    {
        // Static fields.
        static volatile MockAppSuiteApplication? current;
        static readonly object initSyncLock = new object();
        static volatile SingleThreadSynchronizationContext? synchronizationContext;


        /// <summary>
        /// Initialize new <see cref="MockAppSuiteApplication"/> instance.
        /// </summary>
        protected internal MockAppSuiteApplication()
        {
            this.HardwareInfo = new HardwareInfo(this);
            this.ProcessInfo = new ProcessInfo(this);
            this.RootPrivateDirectoryPath = Path.Combine(Path.GetTempPath(), $"AppSuiteTest-{DateTime.Now.ToBinary()}");
            Directory.CreateDirectory(this.RootPrivateDirectoryPath);
        }


        /// <inheritdoc/>
        public virtual void AgreeUserAgreement()
        { }


        /// <summary>
        /// Get assembly of application.
        /// </summary>
        public Assembly Assembly { get; } = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();


        /// <summary>
        /// Check whether current thread is the thread which instance depends on or not.
        /// </summary>
        /// <returns>True if current thread is the thread which instance depends on.</returns>
        public bool CheckAccess() => Thread.CurrentThread == synchronizationContext?.ExecutionThread;


        /// <summary>
        /// Check application update information asynchronously.
        /// </summary>
        /// <returns>Task to wait for checking.</returns>
        public virtual Task<ApplicationUpdateInfo?> CheckUpdateInfoAsync() => Task.FromResult((ApplicationUpdateInfo?)null);


        /// <summary>
        /// Get current culture info of application.
        /// </summary>
        public virtual CultureInfo CultureInfo { get; } = CultureInfo.CurrentCulture;


        /// <summary>
        /// Get key of application culture setting.
        /// </summary>
        public virtual SettingKey<ApplicationCulture> CultureSettingKey { get; } = new SettingKey<ApplicationCulture>("Culture", ApplicationCulture.System);


        /// <summary>
        /// Get instance for current process.
        /// </summary>
        public static MockAppSuiteApplication Current { get => current ?? throw new InvalidOperationException("Application instance is not ready."); }


        /// <inheritdoc/>
        public double CustomScreenScaleFactor { get => double.NaN; set { } }


        /// <inheritdoc/>
        public double EffectiveCustomScreenScaleFactor { get => double.NaN; }


        /// <summary>
        /// Get theme mode which is currently applied to application.
        /// </summary>
        public virtual ThemeMode EffectiveThemeMode { get; } = ThemeMode.Dark;


        /// <summary>
        /// Get string from resources.
        /// </summary>
        /// <param name="key">Key of string.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>String from resources.</returns>
        public virtual string? GetString(string key, string? defaultValue = null) => defaultValue;


        /// <summary>
        /// Get information of hardware.
        /// </summary>
        public HardwareInfo HardwareInfo { get; }


        /// <summary>
        /// Initialize default <see cref="MockAppSuiteApplication"/> instance for current process.
        /// </summary>
        /// <returns><see cref="MockAppSuiteApplication"/> instance.</returns>
        public static MockAppSuiteApplication Initialize() => Initialize(() => new MockAppSuiteApplication());


        /// <summary>
        /// Initialize <see cref="MockAppSuiteApplication"/> instance for current process.
        /// </summary>
        /// <param name="creator">Method to create instance.</param>
        /// <returns><see cref="MockAppSuiteApplication"/> instance.</returns>
        public static MockAppSuiteApplication Initialize(Func<MockAppSuiteApplication> creator)
        {
            if (current != null)
                return current;
            lock (initSyncLock)
            {
                if (current != null)
                    return current;
                synchronizationContext = new SingleThreadSynchronizationContext();
                synchronizationContext.Send(() =>
                {
                    current = creator();
                });
            }
            return current.AsNonNull();
        }


        /// <summary>
        /// Check whether application is running in debug mode or not.
        /// </summary>
        public virtual bool IsDebugMode { get; } = true;


        /// <summary>
        /// Check whether restarting all main windows is needed or not.
        /// </summary>
        public bool IsRestartingMainWindowsNeeded { get; } = false;

        
        /// <inheritdoc/>
        public bool IsRunningAsAdministrator { get; } = false;


        /// <summary>
        /// Check whether application shutting down is started or not.
        /// </summary>
        public virtual bool IsShutdownStarted { get; } = false;


        /// <inheritdoc/>.
        public virtual bool IsUserAgreementAgreed { get; } = false;


        /// <inheritdoc/>.
        public virtual bool IsUserAgreementAgreedBefore { get; } = false;


        /// <summary>
        /// Check whether <see cref="ThemeMode.System"/> is supported or not.
        /// </summary>
        public virtual bool IsSystemThemeModeSupported { get; } = false;


        /// <summary>
        /// Get options to launch application which is converted by arguments passed to application.
        /// </summary>
        public virtual IDictionary<string, object> LaunchOptions { get; } = new Dictionary<string, object>().AsReadOnly();


        /// <summary>
        /// Load <see cref="IApplication.PersistentState"/> from file.
        /// </summary>
        /// <returns>Task of loading.</returns>
        public Task LoadPersistentStateAsync()
        {
            this.PersistentState.ResetValues();
            return Task.CompletedTask;
        }


        /// <summary>
        /// Load <see cref="IApplication.Settings"/> from file.
        /// </summary>
        /// <returns>Task of loading.</returns>
        public Task LoadSettingsAsync()
        {
            this.Settings.ResetValues();
            return Task.CompletedTask;
        }


        /// <summary>
        /// Get logger factory.
        /// </summary>
        public virtual ILoggerFactory LoggerFactory { get; } = new LoggerFactory();


        /// <summary>
        /// Get list of main windows.
        /// </summary>
        public IList<Window> MainWindows { get; } = new Window[0];


        /// <summary>
        /// Get name of application.
        /// </summary>
        public virtual string Name { get; } = "Mock AppSuite";


        /// <summary>
        /// Raise <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">Name of changed property.</param>
        protected virtual void OnPropertyChanged(string propertyName) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        /// <summary>
        /// Get URI of application package manifest.
        /// </summary>
        public virtual Uri? PackageManifestUri { get; } = null;


        /// <summary>
        /// Get persistent state.
        /// </summary>
        public ISettings PersistentState { get; } = new MemorySettings();


        /// <summary>
        /// Get information of current process.
        /// </summary>
        public ProcessInfo ProcessInfo { get; }


        /// <summary>
        /// Raised when property changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;


        /// <summary>
        /// Get type of application releasing.
        /// </summary>
        public virtual ApplicationReleasingType ReleasingType { get; } = ApplicationReleasingType.Development;


        /// <inheritdoc/>
        public bool Restart(string? args, bool asAdministrator) => false;


        /// <summary>
        /// Request restarting given main window.
        /// </summary>
        /// <param name="mainWindow">Main window to restart.</param>
        /// <returns>True if restarting has been accepted.</returns>
        public bool RestartMainWindow(Window mainWindow) => false;


        /// <summary>
        /// Request restarting all main windows.
        /// </summary>
        /// <returns>True if restarting has been accepted.</returns>
        public bool RestartMainWindows() => false;


        /// <summary>
        /// Get root path of private directory.
        /// </summary>
        public string RootPrivateDirectoryPath { get; }


        /// <summary>
        /// Save <see cref="IApplication.PersistentState"/> to file.
        /// </summary>
        /// <returns>Task of saving.</returns>
        public Task SavePersistentStateAsync() => Task.CompletedTask;


        /// <summary>
        /// Save <see cref="CarinaStudio.IApplication.Settings"/> to file.
        /// </summary>
        /// <returns>Task of saving.</returns>
        public Task SaveSettingsAsync() => Task.CompletedTask;


        /// <summary>
        /// Get application settings.
        /// </summary>
        public ISettings Settings { get; } = new MemorySettings();


        /// <inheritdoc/>
        public bool ShowMainWindow() => false;


        /// <summary>
        /// Close all main windows and shut down application.
        /// </summary>
        public void Shutdown()
        { }


        /// <summary>
        /// Raised when string resources updated.
        /// </summary>
        public event EventHandler? StringsUpdated;


        /// <summary>
        /// Get <see cref="SynchronizationContext"/> of the instance.
        /// </summary>
        public SynchronizationContext SynchronizationContext { get => synchronizationContext ?? throw new InvalidOperationException("Application instance is not ready."); }


        /// <summary>
        /// Get key of theme mode setting.
        /// </summary>
        public virtual SettingKey<ThemeMode> ThemeModeSettingKey { get; } = new SettingKey<ThemeMode>("ThemeMode", ThemeMode.System);


        /// <summary>
        /// Get latest checked application update information.
        /// </summary>
        public virtual ApplicationUpdateInfo? UpdateInfo { get; } = null;


        /// <inheritdoc/>.
        public virtual Version? UserAgreementVersion { get; } = null;


        // Interface implementations.
        bool Avalonia.Controls.IResourceNode.HasResources => false;
        bool Avalonia.Controls.IResourceNode.TryGetResource(object key, out object? value)
        {
            value = null;
            return false;
        }
    }
}
