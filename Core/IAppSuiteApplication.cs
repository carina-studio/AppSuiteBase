using Avalonia.Controls;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Interface of AppSuite application.
    /// </summary>
    public interface IAppSuiteApplication : IAvaloniaApplication
    {
        /// <summary>
        /// Activate Pro version.
        /// </summary>
        /// <param name="window">Window.</param>
        /// <returns>Task of Pro version activation.</returns>
		Task ActivateProVersionAsync(Window? window);


        /// <summary>
        /// Add custom resource to application resource dictionary.
        /// </summary>
        /// <param name="resource">Resource.</param>
        /// <returns><see cref="IDisposable"/> which represents token of added resource.</returns>
        IDisposable AddCustomResource(IResourceProvider resource);


        /// <summary>
        /// Add custom style to application styles.
        /// </summary>
        /// <param name="style">Resource.</param>
        /// <returns><see cref="IDisposable"/> which represents token of added style.</returns>
        IDisposable AddCustomStyle(IStyle style);


        /// <summary>
        /// Get version of Privacy Policy which was agreed by user.
        /// </summary>
        Version? AgreedPrivacyPolicyVersion { get; }


        /// <summary>
        /// Get version of User Agreement which was agreed by user.
        /// </summary>
        Version? AgreedUserAgreementVersion { get; }


        /// <summary>
        /// Called when user agree the Privacy Policy.
        /// </summary>
        void AgreePrivacyPolicy();


        /// <summary>
        /// Called when user agree the User Agreement.
        /// </summary>
        void AgreeUserAgreement();


        /// <summary>
        /// Get document of change list of application.
        /// </summary>
        DocumentSource? ChangeList { get; }


        /// <summary>
		/// Check for application update asynchronously.
		/// </summary>
        /// <param name="owner">Owner window.</param>
        /// <param name="forceShowingDialog">True to show dialog no matter application update is available or not.</param> 
        /// <returns>True if application update has been found and update has been started.</returns>     
		Task<bool> CheckForApplicationUpdateAsync(Window? owner, bool forceShowingDialog);


        /// <summary>
        /// Check application update information asynchronously.
        /// </summary>
        /// <returns>Task to wait for checking.</returns>
        Task<ApplicationUpdateInfo?> CheckForApplicationUpdateAsync();


        /// <summary>
        /// Get application configuration.
        /// </summary>
        Configuration.ISettings Configuration { get; }


        /// <summary>
        /// Create builder to build arguments to launch application.
        /// </summary>
        /// <returns>Builder.</returns>
        ApplicationArgsBuilder CreateApplicationArgsBuilder();


        /// <summary>
        /// Create view-model of application info.
        /// </summary>
        /// <returns>View-model of application info.</returns>
        ViewModels.ApplicationInfo CreateApplicationInfoViewModel();


        /// <summary>
        /// Create view-model of application options.
        /// </summary>
        /// <returns>View-model of application options.</returns>
        ViewModels.ApplicationOptions CreateApplicationOptionsViewModel();


        /// <summary>
        /// Get instance of <see cref="IAppSuiteApplication"/> of current process.
        /// </summary>
        public static IAppSuiteApplication Current => (IAppSuiteApplication)Avalonia.Application.Current.AsNonNull();


        /// <summary>
        /// Get instance of <see cref="IAppSuiteApplication"/> of current process, or Null if instance doesn't exist.
        /// </summary>
        public static IAppSuiteApplication? CurrentOrNull => Avalonia.Application.Current as IAppSuiteApplication;


        /// <summary>
        /// Get or set custom screen scale factor for Linux.
        /// </summary>
        double CustomScreenScaleFactor { get; set; }
        
        
        /// <summary>
        /// Deactivate Pro version and remove current device from product.
        /// </summary>
        /// <param name="window">Window.</param>
        /// <returns>Task of deactivating Pro version.</returns>
        Task DeactivateProVersionAndRemoveDeviceAsync(Window? window);


        /// <summary>
        /// Get effective custom screen scale factor for Linux.
        /// </summary>
        double EffectiveCustomScreenScaleFactor { get; }


        /// <summary>
        /// Get theme mode which is currently applied to application.
        /// </summary>
        ThemeMode EffectiveThemeMode { get; }


        /// <summary>
        /// Get all external dependencies of application.
        /// </summary>
        IEnumerable<ExternalDependency> ExternalDependencies { get; }


        /// <summary>
        /// Get version to identify the collection of external dependencies.
        /// </summary>
        int ExternalDependenciesVersion { get; }


        /// <summary>
        /// Get information of hardware.
        /// </summary>
        HardwareInfo HardwareInfo { get; }


        /// <summary>
        /// Check whether application is running in background mode or not.
        /// </summary>
        bool IsBackgroundMode { get; }


        /// <summary>
        /// Check whether application is launched in clean mode or not.
        /// </summary>
        bool IsCleanMode { get; }


        /// <summary>
        /// Check whether application is running in debug mode or not.
        /// </summary>
        bool IsDebugMode { get; }


        /// <summary>
        /// Check whether this is the first time application launch or not.
        /// </summary>
        bool IsFirstLaunch { get; }


        /// <summary>
        /// Check whether the current Privacy Policy has been agreed by user or not.
        /// </summary>
        bool IsPrivacyPolicyAgreed { get; }


        /// <summary>
        /// Check whether the Privacy Policy has been agreed by user before or not.
        /// </summary>
        bool IsPrivacyPolicyAgreedBefore { get; }


        /// <summary>
        /// Check whether restarting all root windows is needed or not.
        /// </summary>
        bool IsRestartingRootWindowsNeeded { get; }


        /// <summary>
        /// Check whether application is running as Administrator/Superuser or not.
        /// </summary>
        bool IsRunningAsAdministrator { get; }


        /// <summary>
        /// Check whether <see cref="ThemeMode.System"/> is supported or not.
        /// </summary>
        bool IsSystemThemeModeSupported { get; }


        /// <summary>
        /// Check whether the current User Agreement has been agreed by user or not.
        /// </summary>
        bool IsUserAgreementAgreed { get; }


        /// <summary>
        /// Check whether the User Agreement has been agreed by user before or not.
        /// </summary>
        bool IsUserAgreementAgreedBefore { get; }


        /// <summary>
        /// Check whether application is user interactive or not.
        /// </summary>
        bool IsUserInteractive { get; }


        /// <summary>
        /// Check whether application is running in testing mode or not.
        /// </summary>
        bool IsTestingMode { get; }


        /// <summary>
        /// Get latest active main window.
        /// </summary>
        Controls.MainWindow? LatestActiveMainWindow { get; }


        /// <summary>
        /// Get latest active window.
        /// </summary>
        Window? LatestActiveWindow { get; }


        /// <summary>
        /// Get options to launch application which is converted by arguments passed to application.
        /// </summary>
        IDictionary<string, object> LaunchOptions { get; }


        /// <summary>
        /// Layout existing main windows.
        /// </summary>
        /// <param name="screen"><see cref="Avalonia.Platform.Screen"/> to layout main windows.</param>
        /// <param name="layout">Layout.</param>
        /// <param name="activeMainWindow">Main window which should be active one after layout.</param>
        void LayoutMainWindows(Avalonia.Platform.Screen screen, Controls.MultiWindowLayout layout, Controls.MainWindow? activeMainWindow);


        /// <summary>
        /// Raised when start loading string resources for given culture.
        /// </summary>
        event EventHandler<IAppSuiteApplication, CultureInfo> LoadingStrings;


        /// <summary>
        /// Load <see cref="IApplication.PersistentState"/> from file.
        /// </summary>
        /// <returns>Task of loading.</returns>
        Task LoadPersistentStateAsync();


        /// <summary>
        /// Load <see cref="IApplication.Settings"/> from file.
        /// </summary>
        /// <returns>Task of loading.</returns>
        Task LoadSettingsAsync();


        /// <summary>
        /// Load string resource in XAML format.
        /// </summary>
        /// <param name="uri">URI of string resource.</param>
        /// <returns>Loaded string resource, or Null if failed to load.</returns>
        IResourceProvider? LoadStringResource(Uri uri);


        /// <summary>
        /// Get list of main windows.
        /// </summary>
        IList<Controls.MainWindow> MainWindows { get; }


        /// <summary>
        /// Get name of application.
        /// </summary>
        string Name { get; }


        /// <summary>
        /// Get URIs of application package manifest.
        /// </summary>
        IEnumerable<Uri> PackageManifestUris { get; }


        /// <summary>
        /// Perform garbage collection.
        /// </summary>
        /// <param name="collectionMode">GC mode.</param>
        void PerformGC(GCCollectionMode collectionMode = GCCollectionMode.Default);


        /// <summary>
        /// Get version of application in previous launch.
        /// </summary>
        Version? PreviousVersion { get; }


        /// <summary>
        /// Get latest document of Privacy Policy.
        /// </summary>
        DocumentSource? PrivacyPolicy { get; }


        /// <summary>
        /// Get version of the Privacy Policy. Null means there is no Privacy Policy.
        /// </summary>
        Version? PrivacyPolicyVersion { get; }


        /// <summary>
        /// Get information of current process.
        /// </summary>
        ProcessInfo ProcessInfo { get; }


        /// <summary>
        /// Get <see cref="Product.IProductManager"/> for product management.
        /// </summary>
        Product.IProductManager ProductManager { get; }


        /// <summary>
        /// Start purchasing Pro version asynchronously.
        /// </summary>
        /// <param name="window">Window.</param>
        void PurchaseProVersionAsync(Window? window);


        /// <summary>
        /// Get type of application releasing.
        /// </summary>
        ApplicationReleasingType ReleasingType { get; }


        /// <summary>
        /// Restart application.
        /// </summary>
        /// <param name="argsBuilder">Builder to build arguments to restart.</param>
        /// <param name="asAdministrator">True to restart application as Administrator/Superuser.</param>
        /// <returns>True if restarting has been accepted.</returns>
        bool Restart(ApplicationArgsBuilder argsBuilder, bool asAdministrator = false);


        /// <summary>
        /// Request restarting given main window asynchronously.
        /// </summary>
        /// <param name="mainWindow">Main window to restart.</param>
        /// <returns>Task of restarting main window. The result will be True if restarting has been accepted.</returns>
        Task<bool> RestartMainWindowAsync(Controls.MainWindow mainWindow);


        /// <summary>
        /// Request restarting all root windows asynchronously.
        /// </summary>
        /// <returns>Task of restarting all root windows. The result will be True if restarting has been accepted.</returns>
        Task<bool> RestartRootWindowsAsync();


        /// <summary>
        /// Save <see cref="IApplication.PersistentState"/> to file.
        /// </summary>
        /// <returns>Task of saving.</returns>
        Task SavePersistentStateAsync();


        /// <summary>
        /// Save <see cref="IApplication.Settings"/> to file.
        /// </summary>
        /// <returns>Task of saving.</returns>
        Task SaveSettingsAsync();


        /// <summary>
        /// Show dialog of application information.
        /// </summary>
        /// <param name="owner">Owner window.</param>
        /// <returns>Task of showing dialog.</returns>
        Task ShowApplicationInfoDialogAsync(Window? owner);


        /// <summary>
        /// Show dialog of application options.
        /// </summary>
        /// <param name="owner">Owner window.</param>
        /// <param name="section">Section of options to show.</param>
        /// <returns>Task of showing dialog.</returns>
        Task ShowApplicationOptionsDialogAsync(Window? owner, string? section = null);


        /// <summary>
        /// Create and show main window asynchronously.
        /// </summary>
        /// <param name="windowCreatedAction">Action to perform when window created.</param>
        /// <returns>Task of showing main window. The result will be True if main window created and shown successfully.</returns>
        Task<bool> ShowMainWindowAsync(Action<Controls.MainWindow>? windowCreatedAction = null);


        /// <summary>
        /// Close all main windows and shut down application.
        /// </summary>
        /// <param name="delay">Delay before start process of shutting down in milliseconds.</param>
        void Shutdown(int delay = 0);
        
        
        /// <summary>
        /// Take single memory snapshot.
        /// </summary>
        /// <param name="outputFileName">Name of output snapshot file.</param>
        /// <returns>True if memory snapshot has been taken successfully.</returns>
        bool TakeMemorySnapshot(string outputFileName);


        /// <summary>
        /// Get latest checked application update information.
        /// </summary>
        ApplicationUpdateInfo? UpdateInfo { get; }


        /// <summary>
        /// Get latest document of User Agreement.
        /// </summary>
        DocumentSource? UserAgreement { get; }


        /// <summary>
        /// Get version of the User Agreement. Null means there is no User Agreement.
        /// </summary>
        Version? UserAgreementVersion { get; }


        /// <summary>
        /// Get all windows of application.
        /// </summary>
        IList<Window> Windows { get; }
    }


    /// <summary>
    /// Builder for arguments to launch application.
    /// </summary>
    public class ApplicationArgsBuilder : ICloneable, IEquatable<ApplicationArgsBuilder>
    {
        /// <summary>
        /// Initialize new <see cref="ApplicationArgsBuilder"/> instance.
        /// </summary>
        public ApplicationArgsBuilder()
        { }


        /// <summary>
        /// Initialize new <see cref="ApplicationArgsBuilder"/> instance.
        /// </summary>
        /// <param name="template">Template.</param>
        public ApplicationArgsBuilder(ApplicationArgsBuilder template)
        {
            this.IsCleanMode = template.IsCleanMode;
            this.IsDebugMode = template.IsDebugMode;
            this.IsTestingMode = template.IsTestingMode;
            this.RestoringMainWindows = template.RestoringMainWindows;
        }


        /// <summary>
        /// Clone the builder.
        /// </summary>
        /// <returns>Cloned builder.</returns>
        public virtual ApplicationArgsBuilder Clone() => 
            new(this);
        

        /// <inheritdoc/>
        object ICloneable.Clone() =>
            this.Clone();


        /// <inheritdoc/>
        public virtual bool Equals(ApplicationArgsBuilder? builder) =>
            builder is not null
            && this.IsCleanMode == builder.IsCleanMode
            && this.IsDebugMode == builder.IsDebugMode
            && this.IsTestingMode == builder.IsTestingMode
            && this.RestoringMainWindows == builder.RestoringMainWindows;


        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            obj is ApplicationArgsBuilder builder && this.Equals(builder);


        /// <inheritdoc/>
        // ReSharper disable NonReadonlyMemberInGetHashCode
        public override int GetHashCode() =>
            ((this.IsDebugMode ? 1 : 0) << 31) | ((this.IsTestingMode ? 1 : 0) << 30);
        // ReSharper restore NonReadonlyMemberInGetHashCode
        

        /// <summary>
        /// Get or set whether application should be launched in clean mode or not.
        /// </summary>
        public bool IsCleanMode { get; set; }


        /// <summary>
        /// Get or set whether application should be launched in debug mode or not.
        /// </summary>
        public bool IsDebugMode { get; set; }


        /// <summary>
        /// Get or set whether application should be launched in testing mode or not.
        /// </summary>
        public bool IsTestingMode { get; set; }


        /// <summary>
        /// Get or set whether main windows should be restored after launching application or not.
        /// </summary>
        public bool RestoringMainWindows { get; set; }


        /// <inheritdoc/>
        public override string ToString()
        {
            var buffer = new StringBuilder();
            if (this.IsCleanMode)
            {
                if (buffer.Length > 0)
                    buffer.Append(' ');
                buffer.Append(AppSuiteApplication.CleanModeArgument);
            }
            if (this.IsDebugMode)
            {
                if (buffer.Length > 0)
                    buffer.Append(' ');
                buffer.Append(AppSuiteApplication.DebugArgument);
            }
            if (this.IsTestingMode)
            {
                if (buffer.Length > 0)
                    buffer.Append(' ');
                buffer.Append(AppSuiteApplication.TestingArgument);
            }
            if (this.RestoringMainWindows)
            {
                if (buffer.Length > 0)
                    buffer.Append(' ');
                buffer.Append(AppSuiteApplication.RestoreMainWindowsArgument);
            }
            return buffer.ToString();
        }
    }


    /// <summary>
    /// Extensions for <see cref="IAppSuiteApplication"/>.
    /// </summary>
    public static class AppSuiteApplicationExtensions
    {
        // Fields.
        static Uri? baseAvaloniaResourceUri;


        /// <summary>
        /// Create URI of avalonia resource in assembly of application.
        /// </summary>
        /// <param name="app">Application.</param>
        /// <param name="path">Path to resource.</param>
        /// <returns>URI of avalonia resource.</returns>
        public static Uri CreateAvaloniaResourceUri(this IAppSuiteApplication app, string path)
        {
            baseAvaloniaResourceUri ??= new($"avares://{app.Assembly.GetName().Name}");
            return new(baseAvaloniaResourceUri, path);
        }
    }
}
