using Avalonia.Controls;
using CarinaStudio.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Interface of AppSuite application.
    /// </summary>
    public interface IAppSuiteApplication : CarinaStudio.IApplication
    {
        /// <summary>
        /// Get key of application culture setting.
        /// </summary>
        SettingKey<ApplicationCulture> CultureSettingKey { get; }


        /// <summary>
        /// Load <see cref="CarinaStudio.IApplication.PersistentState"/> from file.
        /// </summary>
        /// <returns>Task of loading.</returns>
        Task LoadPersistentStateAsync();


        /// <summary>
        /// Load <see cref="CarinaStudio.IApplication.Settings"/> from file.
        /// </summary>
        /// <returns>Task of loading.</returns>
        Task LoadSettingsAsync();


        /// <summary>
        /// Get list of main windows.
        /// </summary>
        IList<Window> MainWindows { get; }


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
