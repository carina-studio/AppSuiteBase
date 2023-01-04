using Avalonia.Controls;
using Avalonia.Styling;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
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
        // Empty implementation of IDIsposable.
        class EmptyDisposable : IDisposable
        {
            public void Dispose()
            { }
        }


        // Static fields.
        static volatile MockAppSuiteApplication? current;
        static readonly object initSyncLock = new();
        static volatile SingleThreadSynchronizationContext? synchronizationContext;


        /// <summary>
        /// Initialize new <see cref="MockAppSuiteApplication"/> instance.
        /// </summary>
        protected internal MockAppSuiteApplication()
        {
            this.HardwareInfo = new HardwareInfo(this);
            this.ProcessInfo = new ProcessInfo(this);
            this.ProductManager = new Product.MockProductManager(this);
            this.RootPrivateDirectoryPath = Path.Combine(Path.GetTempPath(), $"AppSuiteTest-{DateTime.Now.ToBinary()}");
            Directory.CreateDirectory(this.RootPrivateDirectoryPath);
        }


        /// <inheritdoc/>
        public virtual Task ActivateProVersionAsync(Avalonia.Controls.Window? window) =>
            Task.CompletedTask;


        /// <inheritdoc/>
        public virtual IDisposable AddCustomResource(Avalonia.Controls.IResourceProvider resource) =>
            new EmptyDisposable();
        

        /// <inheritdoc/>
        public virtual IDisposable AddCustomStyle(IStyle style) =>
            new EmptyDisposable();
        

        /// <inheritdoc/>
        public virtual Version? AgreedPrivacyPolicyVersion { get; }


        /// <inheritdoc/>
        public virtual Version? AgreedUserAgreementVersion { get; }


        /// <inheritdoc/>
        public virtual void AgreePrivacyPolicy()
        { }


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


        /// <inheritdoc/>
        public virtual DocumentSource? ChangeList { get; }


        /// <inheritdoc/>
        public virtual Task<bool> CheckForApplicationUpdateAsync(Avalonia.Controls.Window? owner, bool forceShowingDialog) =>
            Task.FromResult(false);


        /// <summary>
        /// Check application update information asynchronously.
        /// </summary>
        /// <returns>Task to wait for checking.</returns>
        public virtual Task<ApplicationUpdateInfo?> CheckForApplicationUpdateAsync() => Task.FromResult((ApplicationUpdateInfo?)null);


        /// <inheritdoc/>
        public virtual Avalonia.Input.Platform.IClipboard? Clipboard { get => null; }


        /// <inheritdoc/>
        public ISettings Configuration { get; } = new MemorySettings();


        /// <inheritdoc/>
        public virtual ApplicationArgsBuilder CreateApplicationArgsBuilder() =>
            new()
            {
                IsDebugMode = this.IsDebugMode,
                IsTestingMode = this.IsTestingMode
            };


        /// <inheritdoc/>
        public virtual ViewModels.ApplicationInfo CreateApplicationInfoViewModel() =>
            new();
        

        /// <inheritdoc/>
        public virtual ViewModels.ApplicationOptions CreateApplicationOptionsViewModel() =>
            new();


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


        /// <inheritdoc/>
        public virtual IEnumerable<ExternalDependency> ExternalDependencies { get; } = Array.Empty<ExternalDependency>();


        /// <inheritdoc/>
        public virtual int ExternalDependenciesVersion { get; } = 1;


        /// <inheritdoc/>
        public virtual IObservable<string?> GetObservableString(string key) =>
            new FixedObservableValue<string?>(null);


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


        /// <inheritdoc/>
        public virtual bool IsBackgroundMode { get => false; }


        /// <summary>
        /// Check whether application is running in debug mode or not.
        /// </summary>
        public virtual bool IsDebugMode { get; } = true;


        /// <inheritdoc/>
        public virtual bool IsFirstLaunch { get; } = true;


        /// <inheritdoc/>
        public virtual bool IsPrivacyPolicyAgreed { get; } = false;


        /// <inheritdoc/>
        public virtual bool IsPrivacyPolicyAgreedBefore { get; } = false;


        /// <summary>
        /// Check whether restarting all main windows is needed or not.
        /// </summary>
        public bool IsRestartingRootWindowsNeeded { get; } = false;

        
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


        /// <inheritdoc/>
        public virtual bool IsTestingMode { get; } = true;


        /// <inheritdoc/>
        public virtual CarinaStudio.Controls.Window? LatestActiveMainWindow { get; }


        /// <inheritdoc/>
        public virtual Avalonia.Controls.Window? LatestActiveWindow { get; }


        /// <summary>
        /// Get options to launch application which is converted by arguments passed to application.
        /// </summary>
        public virtual IDictionary<string, object> LaunchOptions { get; } = DictionaryExtensions.AsReadOnly(new Dictionary<string, object>());


        /// <inheritdoc/>
        public virtual void LayoutMainWindows(Avalonia.Platform.Screen screen, Controls.MultiWindowLayout layout, CarinaStudio.Controls.Window? activeMainWindow)
        { }


        /// <inheritdoc/>
#pragma warning disable CS0067
        public event EventHandler<IAppSuiteApplication, CultureInfo>? LoadingStrings;
#pragma warning restore CS0067


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


        /// <inheritdoc/>
        public virtual Avalonia.Controls.IResourceProvider? LoadStringResource(Uri uri) => null;


        /// <summary>
        /// Get logger factory.
        /// </summary>
        public virtual ILoggerFactory LoggerFactory { get; } = new LoggerFactory();


        /// <summary>
        /// Get list of main windows.
        /// </summary>
        public IList<CarinaStudio.Controls.Window> MainWindows { get; } = Array.Empty<CarinaStudio.Controls.Window>();


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


        /// <inheritdoc/>
        public virtual DocumentSource? PrivacyPolicy { get; }


        /// <inheritdoc/>
        public virtual Version? PrivacyPolicyVersion { get; } = null;


        /// <summary>
        /// Get information of current process.
        /// </summary>
        public ProcessInfo ProcessInfo { get; }


        /// <inheritdoc/>
        public virtual Product.IProductManager ProductManager { get; }


        /// <summary>
        /// Raised when property changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;


        /// <inheritdoc/>
        public virtual void PurchaseProVersionAsync(Avalonia.Controls.Window? window)
        { }


        /// <summary>
        /// Get type of application releasing.
        /// </summary>
        public virtual ApplicationReleasingType ReleasingType { get; } = ApplicationReleasingType.Development;


        /// <inheritdoc/>
#pragma warning disable CS0067
        public event EventHandler<ResourcesChangedEventArgs>? ResourcesChanged;
#pragma warning restore CS0067


        /// <inheritdoc/>
        public bool Restart(ApplicationArgsBuilder argsBuilder, bool asAdministrator) => false;


        /// <inheritdoc/>
        public Task<bool> RestartMainWindowAsync(CarinaStudio.Controls.Window mainWindow) => 
            Task.FromResult(false);


        /// <inheritdoc/>
        public Task<bool> RestartRootWindowsAsync() => 
            Task.FromResult(false);


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
        public virtual Task ShowApplicationInfoDialogAsync(Avalonia.Controls.Window? owner) =>
            Task.CompletedTask;
        

        /// <inheritdoc/>
        public virtual Task ShowApplicationOptionsDialogAsync(Avalonia.Controls.Window? owner, string? section = null) =>
            Task.CompletedTask;


        /// <inheritdoc/>
        public Task<bool> ShowMainWindowAsync(Action<CarinaStudio.Controls.Window>? windowCreatedAction = null) => 
            Task.FromResult(false);


        /// <summary>
        /// Close all main windows and shut down application.
        /// </summary>
        public void Shutdown(int delay)
        { }


        /// <summary>
        /// Raised when string resources updated.
        /// </summary>
#pragma warning disable CS0067
        public event EventHandler? StringsUpdated;
#pragma warning restore CS0067


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


        /// <inheritdoc/>
        public virtual DocumentSource? UserAgreement { get; }


        /// <inheritdoc/>
        public virtual Version? UserAgreementVersion { get; } = null;


        /// <inheritdoc/>
        public virtual IList<Avalonia.Controls.Window> Windows { get; } = Array.Empty<Avalonia.Controls.Window>();


        // Interface implementations.
        void IResourceHost.NotifyHostedResourcesChanged(ResourcesChangedEventArgs e)
        { }
        bool IResourceNode.HasResources => false;
        bool IResourceNode.TryGetResource(object key, out object? value)
        {
            value = null;
            return false;
        }
    }
}
