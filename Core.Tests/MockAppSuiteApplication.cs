using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using CarinaStudio.AppSuite.Controls;
using CarinaStudio.AppSuite.UsageData;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Window = Avalonia.Controls.Window;
// ReSharper disable InvokeAsExtensionMethod

namespace CarinaStudio.AppSuite;

/// <summary>
/// Mock implementation of <see cref="IAppSuiteApplication"/> for testing purpose.
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class MockAppSuiteApplication : Application, IAppSuiteApplication
{
    // Static fields.
    static volatile MockAppSuiteApplication? current;
    static readonly Lock initSyncLock = new();


    /// <summary>
    /// Initialize new <see cref="MockAppSuiteApplication"/> instance.
    /// </summary>
    internal protected MockAppSuiteApplication()
    {
        this.ProductManager = new Product.MockProductManager(this);
        this.RootPrivateDirectoryPath = Path.Combine(Path.GetTempPath(), $"AppSuiteTest-{DateTime.Now.ToBinary()}");
        this.UsageManager = new MockUsageManager(this);
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
    public virtual ViewModels.ApplicationUpdater CreateApplicationUpdaterViewModel() => 
        new();


    /// <inheritdoc/>
    public override CultureInfo CultureInfo { get; } = CultureInfo.CurrentCulture;


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
    public new virtual IObservable<object?> GetResourceObservable(object key, Func<object?, object?>? converter = null) =>
        new FixedObservableValue<object?>(null);


    /// <inheritdoc/>
    public override IObservable<string?> GetObservableString(string key) =>
        new FixedObservableValue<string?>(null);


    /// <inheritdoc/>
    public override string? GetString(string key, string? defaultValue = null) => defaultValue;


    /// <inheritdoc/>
    public HardwareInfo HardwareInfo { get; private set; } = null!;


    /// <summary>
    /// Initialize default <see cref="MockAppSuiteApplication"/> instance for current process.
    /// </summary>
    /// <returns><see cref="MockAppSuiteApplication"/> instance.</returns>
    internal static new MockAppSuiteApplication Initialize() => Initialize(() => new MockAppSuiteApplication());


    /// <summary>
    /// Initialize <see cref="MockAppSuiteApplication"/> instance for current process.
    /// </summary>
    /// <param name="creator">Method to create instance.</param>
    /// <returns><see cref="MockAppSuiteApplication"/> instance.</returns>
    public static MockAppSuiteApplication Initialize(Func<MockAppSuiteApplication> creator)
    {
        if (current is not null)
        {
            if (IAppSuiteApplication.MockInstance is not null && !ReferenceEquals(IAppSuiteApplication.MockInstance, current))
                throw new InvalidOperationException("There is already a mock instance registered into IAppSuiteApplication.");
            return current;
        }
        using (var _ = initSyncLock.EnterScope())
        {
            if (current is not null)
            {
                if (IAppSuiteApplication.MockInstance is not null && !ReferenceEquals(IAppSuiteApplication.MockInstance, current))
                    throw new InvalidOperationException("There is already a mock instance registered into IAppSuiteApplication.");
                return current;
            }
            if (IAppSuiteApplication.MockInstance is not null)
                throw new InvalidOperationException("There is already a mock instance registered into IAppSuiteApplication.");

            // set up the headless application on a dedicated dispatcher thread with a real (Skia) render backend
            var appReadyEvent = new ManualResetEventSlim();
            var appLoopCts = new CancellationTokenSource();
            var appThread = new Thread(() =>
            {
                AppBuilder.Configure(creator)
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
                    .UseSkia()
                    .SetupWithoutStarting();
                current = (MockAppSuiteApplication)Avalonia.Application.Current.AsNonNull();
                appReadyEvent.Set();
                Dispatcher.UIThread.MainLoop(appLoopCts.Token);
            })
            {
                IsBackground = true,
                Name = "Mock application thread",
            };
            appThread.Start();
            appReadyEvent.Wait(CancellationToken.None);
            IAppSuiteApplication.MockInstance = current;
        }
        return current.AsNonNull();
    }


    /// <inheritdoc/>
    [ThreadSafe]
    public bool IsActive => false;


    /// <inheritdoc/>
    [ThreadSafe]
    public bool IsApplyingSystemTextScaleFactorSupported => false;


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
    public override bool IsShutdownStarted => false;
    
    
    /// <inheritdoc/>
    public virtual bool IsSystemThemeModeSupported => false;


    /// <inheritdoc/>
    public virtual bool IsTestingMode => true;


    /// <inheritdoc/>.
    public virtual bool IsUserAgreementAgreed => false;


    /// <inheritdoc/>.
    public virtual bool IsUserAgreementAgreedBefore => false;


    /// <inheritdoc/>
    public virtual bool IsUserInteractive => false;


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
    public override ILoggerFactory LoggerFactory { get; } = new LoggerFactory();


    /// <inheritdoc/>
    public IList<MainWindow> MainWindows { get; } = Array.Empty<MainWindow>();


    /// <inheritdoc/>
    public new virtual string Name => "Mock AppSuite";


    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        // base implementation sets up the synchronization context of the application thread
        base.OnFrameworkInitializationCompleted();

        // create infrastructure which depends on the application thread being ready
        this.HardwareInfo = new HardwareInfo(this);
        this.ProcessInfo = new ProcessInfo(this);
    }


    /// <inheritdoc/>
    public virtual IEnumerable<Uri> PackageManifestUris { get; } = Array.Empty<Uri>();


    /// <inheritdoc/>
    public virtual void PerformGC(GCCollectionMode collectionMode)
    { }


    /// <inheritdoc/>
    public override ISettings PersistentState { get; } = new MemorySettings();


    /// <inheritdoc/>
    public virtual Version? PreviousVersion => null;


    /// <inheritdoc/>
    public virtual DocumentSource? PrivacyPolicy => null;


    /// <inheritdoc/>
    public virtual Version? PrivacyPolicyVersion => null;


    /// <inheritdoc/>
    public ProcessInfo ProcessInfo { get; private set; } = null!;


    /// <inheritdoc/>
    public virtual Product.IProductManager ProductManager { get; }


    /// <inheritdoc/>
    public virtual void PurchaseProVersionAsync(Window? window)
    { }


    /// <inheritdoc/>
    public virtual ApplicationReleasingType ReleasingType => ApplicationReleasingType.Development;


    /// <inheritdoc/>
    public bool Restart(ApplicationArgsBuilder argsBuilder, bool asAdministrator, ApplicationShutdownReason reason) => false;


    /// <inheritdoc/>
    public Task<bool> RestartMainWindowAsync(MainWindow mainWindow) => 
        Task.FromResult(false);


    /// <inheritdoc/>
    public Task<bool> RestartRootWindowsAsync() => 
        Task.FromResult(false);


    /// <inheritdoc/>
    public sealed override string RootPrivateDirectoryPath { get; }


    /// <inheritdoc/>
    public Task SavePersistentStateAsync(bool isCritical = false) => Task.CompletedTask;


    /// <inheritdoc/>
    public Task SaveSettingsAsync(bool isCritical = false) => Task.CompletedTask;


    /// <inheritdoc/>
    public override ISettings Settings { get; } = new MemorySettings();


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
    public void Shutdown(int delay, ApplicationShutdownReason reason)
    { }


    /// <inheritdoc/>
    public ApplicationShutdownReason ShutdownReason => ApplicationShutdownReason.None;


    /// <inheritdoc/>
    public virtual IDisposable StartTracing(string outputFileName) =>
        new EmptyDisposable();


    /// <inheritdoc/>
    public virtual ThemeMode SystemThemeMode => ThemeMode.Dark;
    
    
    /// <inheritdoc/>
    public virtual bool TakeMemorySnapshot(string outputFileName) =>
        false;


    /// <inheritdoc/>
    public double TextScaleFactor => 1.0;
    

    /// <inheritdoc/>
    public new virtual bool TryFindResource(object key, ThemeVariant? theme, [NotNullWhen(true)] out object? value)
    {
        value = null;
        return false;
    }


    /// <inheritdoc/>
    public virtual ApplicationUpdateInfo? UpdateInfo => null;


    /// <inheritdoc/>
    public IUsageManager UsageManager { get; }


    /// <inheritdoc/>
    public virtual DocumentSource? UserAgreement => null;


    /// <inheritdoc/>
    public virtual Version? UserAgreementVersion => null;


    /// <inheritdoc/>
    public virtual IList<Window> Windows { get; } = Array.Empty<Window>();
}
