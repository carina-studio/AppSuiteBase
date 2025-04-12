using Avalonia.Controls;
using Avalonia.Styling;
using CarinaStudio.AppSuite.Controls;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Window = Avalonia.Controls.Window;
// ReSharper disable InvokeAsExtensionMethod

namespace CarinaStudio.AppSuite;

/// <summary>
/// Mock implementation of <see cref="IAppSuiteApplication"/> for testing purpose.
/// </summary>
public class MockAppSuiteApplication : IAppSuiteApplication
{
    // Empty implementation of IDisposable.
    class EmptyDisposable : IDisposable
    {
        public void Dispose()
        { }
    }


    // Static fields.
    static volatile MockAppSuiteApplication? current;
    static readonly Lock initSyncLock = new();
    static volatile SingleThreadSynchronizationContext? synchronizationContext;


    /// <summary>
    /// Initialize new <see cref="MockAppSuiteApplication"/> instance.
    /// </summary>
    internal protected MockAppSuiteApplication()
    {
        this.HardwareInfo = new HardwareInfo(this);
        this.ProcessInfo = new ProcessInfo(this);
        this.ProductManager = new Product.MockProductManager(this);
        this.RootPrivateDirectoryPath = Path.Combine(Path.GetTempPath(), $"AppSuiteTest-{DateTime.Now.ToBinary()}");
        Directory.CreateDirectory(this.RootPrivateDirectoryPath);
    }


    /// <inheritdoc/>
    public virtual Task ActivateProVersionAsync(Window? window) =>
        Task.CompletedTask;


    /// <inheritdoc/>
    public virtual IDisposable AddCustomResource(IResourceProvider resource) =>
        new EmptyDisposable();
    

    /// <inheritdoc/>
    public virtual IDisposable AddCustomStyle(IStyle style) =>
        new EmptyDisposable();


    /// <inheritdoc/>
    public virtual Version? AgreedPrivacyPolicyVersion => null;


    /// <inheritdoc/>
    public virtual Version? AgreedUserAgreementVersion => null;


    /// <inheritdoc/>
    public virtual void AgreePrivacyPolicy()
    { }


    /// <inheritdoc/>
    public virtual void AgreeUserAgreement()
    { }


    /// <inheritdoc/>
    public virtual WindowIcon ApplicationIcon =>
        throw new NotImplementedException();
    
    
    /// <inheritdoc/>
    public Version AvaloniaVersion { get; } = typeof(Avalonia.Application).Assembly.GetName().Version ?? throw new NotSupportedException("Unable to get version of Avalonia.");


    /// <inheritdoc/>
    public Assembly Assembly { get; } = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();


    /// <inheritdoc/>
    public bool CheckAccess() => Thread.CurrentThread == synchronizationContext?.ExecutionThread;


    /// <inheritdoc/>
    public Task DeactivateProVersionAndRemoveDeviceAsync(Window? window) =>
        Task.CompletedTask;


    /// <inheritdoc/>
    public virtual DocumentSource? ChangeList => null;


    /// <inheritdoc/>
    public virtual Task<bool> CheckForApplicationUpdateAsync(Window? owner, bool forceShowingDialog) =>
        Task.FromResult(false);


    /// <inheritdoc/>
    public virtual Task<ApplicationUpdateInfo?> CheckForApplicationUpdateAsync() => Task.FromResult((ApplicationUpdateInfo?)null);


    /// <inheritdoc/>
    public ChineseVariant ChineseVariant => ChineseVariant.Default;


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


    /// <inheritdoc/>
    public virtual CultureInfo CultureInfo { get; } = CultureInfo.CurrentCulture;
    

    /// <summary>
    /// Get instance for current process.
    /// </summary>
    public static MockAppSuiteApplication Current => current ?? throw new InvalidOperationException("Application instance is not ready.");


    /// <inheritdoc/>
    public double CustomScreenScaleFactor { get => double.NaN; set { } }


    /// <inheritdoc/>
    public double EffectiveCustomScreenScaleFactor => double.NaN;
    
    
    /// <inheritdoc/>
#pragma warning disable CS0067
    public virtual event EventHandler<IAppSuiteApplication.ExceptionEventArgs>? ExceptionOccurredInApplicationLifetime;
#pragma warning restore CS0067
    

    /// <inheritdoc/>
    public virtual ThemeMode EffectiveThemeMode => ThemeMode.Dark;


    /// <inheritdoc/>
    public virtual IEnumerable<ExternalDependency> ExternalDependencies { get; } = Array.Empty<ExternalDependency>();


    /// <inheritdoc/>
    public virtual int ExternalDependenciesVersion => 1;


    /// <inheritdoc/>
    public virtual IObservable<object?> GetResourceObservable(object key, Func<object?, object?>? converter = null) =>
        new FixedObservableValue<object?>(null);


    /// <inheritdoc/>
    public virtual IObservable<string?> GetObservableString(string key) =>
        new FixedObservableValue<string?>(null);


    /// <inheritdoc/>
    public virtual string? GetString(string key, string? defaultValue = null) => defaultValue;


    /// <inheritdoc/>
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
        using (var _ = initSyncLock.EnterScope())
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
    public virtual bool IsBackgroundMode => false;


    /// <inheritdoc/>
    public virtual bool IsCleanMode => false;
    
    
    /// <inheritdoc/>
    public virtual bool IsCriticalShutdownStarted => false;


    /// <inheritdoc/>
    public virtual bool IsDebugMode => true;


    /// <inheritdoc/>
    public virtual bool IsFirstLaunch => true;


    /// <inheritdoc/>
    public virtual bool IsPrivacyPolicyAgreed => false;


    /// <inheritdoc/>
    public virtual bool IsPrivacyPolicyAgreedBefore => false;


    /// <inheritdoc/>
    public bool IsRestartingRootWindowsNeeded => false;

    
    /// <inheritdoc/>
    public bool IsRunningAsAdministrator => false;


    /// <inheritdoc/>
    public virtual bool IsShutdownStarted => false;


    /// <inheritdoc/>.
    public virtual bool IsUserAgreementAgreed => false;


    /// <inheritdoc/>.
    public virtual bool IsUserAgreementAgreedBefore => false;


    /// <inheritdoc/>
    public virtual bool IsUserInteractive => false;


    /// <inheritdoc/>
    public virtual bool IsSystemThemeModeSupported => false;


    /// <inheritdoc/>
    public virtual bool IsTestingMode => true;


    /// <inheritdoc/>
    public virtual MainWindow? LatestActiveMainWindow => null;


    /// <inheritdoc/>
    public virtual Window? LatestActiveWindow => null;
    
    
    /// <inheritdoc/>
    public ChineseVariant LaunchChineseVariant => ChineseVariant.Default;


    /// <inheritdoc/>
    public virtual IDictionary<string, object> LaunchOptions { get; } = DictionaryExtensions.AsReadOnly(new Dictionary<string, object>());


    /// <inheritdoc/>
    public virtual void LayoutMainWindows(Avalonia.Platform.Screen screen, MultiWindowLayout layout, MainWindow? activeMainWindow)
    { }


    /// <inheritdoc/>
#pragma warning disable CS0067
    public event EventHandler<IAppSuiteApplication, CultureInfo>? LoadingStrings;
#pragma warning restore CS0067


    /// <inheritdoc/>
    public Task LoadPersistentStateAsync()
    {
        this.PersistentState.ResetValues();
        return Task.CompletedTask;
    }


    /// <inheritdoc/>
    public Task LoadSettingsAsync()
    {
        this.Settings.ResetValues();
        return Task.CompletedTask;
    }


    /// <inheritdoc/>
    public virtual IResourceProvider? LoadStringResource(Uri uri) => null;


    /// <inheritdoc/>
    public virtual ILoggerFactory LoggerFactory { get; } = new LoggerFactory();


    /// <inheritdoc/>
    public IList<MainWindow> MainWindows { get; } = Array.Empty<MainWindow>();


    /// <inheritdoc/>
    public virtual string Name => "Mock AppSuite";


    /// <summary>
    /// Raise <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">Name of changed property.</param>
    protected virtual void OnPropertyChanged(string propertyName) =>
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


    /// <inheritdoc/>
    public virtual IEnumerable<Uri> PackageManifestUris { get; } = Array.Empty<Uri>();


    /// <inheritdoc/>
    public virtual void PerformGC(GCCollectionMode collectionMode)
    { }


    /// <inheritdoc/>
    public ISettings PersistentState { get; } = new MemorySettings();


    /// <inheritdoc/>
    public virtual Version? PreviousVersion => null;


    /// <inheritdoc/>
    public virtual DocumentSource? PrivacyPolicy => null;


    /// <inheritdoc/>
    public virtual Version? PrivacyPolicyVersion { get; } = null;


    /// <inheritdoc/>
    public ProcessInfo ProcessInfo { get; }


    /// <inheritdoc/>
    public virtual Product.IProductManager ProductManager { get; }


    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;


    /// <inheritdoc/>
    public virtual void PurchaseProVersionAsync(Window? window)
    { }


    /// <inheritdoc/>
    public virtual ApplicationReleasingType ReleasingType => ApplicationReleasingType.Development;


    /// <inheritdoc/>
    public bool Restart(ApplicationArgsBuilder argsBuilder, bool asAdministrator, bool isCritical = false) => false;


    /// <inheritdoc/>
    public Task<bool> RestartMainWindowAsync(MainWindow mainWindow) => 
        Task.FromResult(false);


    /// <inheritdoc/>
    public Task<bool> RestartRootWindowsAsync() => 
        Task.FromResult(false);


    /// <inheritdoc/>
    public string RootPrivateDirectoryPath { get; }


    /// <inheritdoc/>
    public Task SavePersistentStateAsync(bool isCritical = false) => Task.CompletedTask;


    /// <inheritdoc/>
    public Task SaveSettingsAsync(bool isCritical = false) => Task.CompletedTask;


    /// <inheritdoc/>
    public ISettings Settings { get; } = new MemorySettings();


    /// <inheritdoc/>
    public virtual Task ShowApplicationInfoDialogAsync(Window? owner) =>
        Task.CompletedTask;
    

    /// <inheritdoc/>
    public virtual Task ShowApplicationOptionsDialogAsync(Window? owner, string? section = null) =>
        Task.CompletedTask;


    /// <inheritdoc/>
    public Task<bool> ShowMainWindowAsync(Action<MainWindow>? windowCreatedAction = null) => 
        Task.FromResult(false);


    /// <inheritdoc/>
    public void Shutdown(int delay, bool isCritical = false)
    { }


    /// <inheritdoc/>
    public virtual IDisposable StartTracing(string outputFileName) =>
        new EmptyDisposable();


    /// <summary>
    /// Raised when string resources updated.
    /// </summary>
#pragma warning disable CS0067
    public event EventHandler? StringsUpdated;
#pragma warning restore CS0067


    /// <inheritdoc/>
    public virtual SynchronizationContext SynchronizationContext => synchronizationContext ?? throw new InvalidOperationException("Application instance is not ready.");
    
    
    /// <inheritdoc/>
    public virtual bool TakeMemorySnapshot(string outputFileName) =>
        false;
    

    /// <inheritdoc/>
    public virtual bool TryFindResource(object key, ThemeVariant? theme, [NotNullWhen(true)] out object? value)
    {
        value = null;
        return false;
    }


    /// <inheritdoc/>
    public virtual ApplicationUpdateInfo? UpdateInfo => null;


    /// <inheritdoc/>
    public virtual DocumentSource? UserAgreement => null;


    /// <inheritdoc/>
    public virtual Version? UserAgreementVersion => null;


    /// <inheritdoc/>
    public virtual IList<Window> Windows { get; } = Array.Empty<Window>();
}
