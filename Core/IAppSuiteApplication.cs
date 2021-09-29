using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Interface of AppSuite application.
    /// </summary>
    public interface IAppSuiteApplication<TApp> : IApplication where TApp : class, IAppSuiteApplication<TApp>
    {
        /// <summary>
        /// Get key of application culture setting.
        /// </summary>
        SettingKey<ApplicationCulture> CultureSettingKey { get; }


        /// <summary>
        /// Check whether application is running in debug mode or not.
        /// </summary>
        bool IsDebugMode { get; }


        /// <summary>
        /// Check whether restarting all main windows is needed or not.
        /// </summary>
        bool IsRestartingMainWindowsNeeded { get; }


        /// <summary>
        /// Check whether <see cref="ThemeMode.System"/> is supported or not.
        /// </summary>
        bool IsSystemThemeModeSupported { get; }


        /// <summary>
        /// Get options to launch application which is converted by arguments passed to application.
        /// </summary>
        IDictionary<string, object> LaunchOptions { get; }


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
        /// Get list of main windows.
        /// </summary>
        IList<Window<TApp>> MainWindows { get; }


        /// <summary>
        /// Save <see cref="CarinaStudio.IApplication.PersistentState"/> to file.
        /// </summary>
        /// <returns>Task of saving.</returns>
        Task SavePersistentStateAsync();


        /// <summary>
        /// Save <see cref="CarinaStudio.IApplication.Settings"/> to file.
        /// </summary>
        /// <returns>Task of saving.</returns>
        Task SaveSettingsAsync();


        /// <summary>
        /// Create and show main window.
        /// </summary>
        /// <param name="param">Parameter to create main window.</param>
        /// <returns>True if main window created and shown successfully.</returns>
        bool ShowMainWindow(object? param = null);


        /// <summary>
        /// Close all main windows and shut down application.
        /// </summary>
        void Shutdown();


        /// <summary>
        /// Get key of theme mode setting.
        /// </summary>
        SettingKey<ThemeMode> ThemeModeSettingKey { get; }
    }
}
