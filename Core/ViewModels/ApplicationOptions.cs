using CarinaStudio.Configuration;
using CarinaStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CarinaStudio.AppSuite.ViewModels
{
    /// <summary>
    /// View-model for application options UI.
    /// </summary>
    public class ApplicationOptions : ViewModel<IAppSuiteApplication>
    {
        /// <summary>
        /// Initialize new <see cref="ApplicationOptions"/> instance.
        /// </summary>
        public ApplicationOptions() : base(AppSuiteApplication.Current)
        {
            this.ThemeModes = new List<ThemeMode>(Enum.GetValues<ThemeMode>()).Also(it =>
            {
                if (!this.Application.IsSystemThemeModeSupported)
                    it.Remove(ThemeMode.System);
            }).AsReadOnly();
        }


        /// <summary>
        /// Get or set application culture.
        /// </summary>
        public ApplicationCulture Culture
        {
            get => this.Settings.GetValueOrDefault(this.Application.CultureSettingKey);
            set => this.Settings.SetValue<ApplicationCulture>(this.Application.CultureSettingKey, value);
        }


        /// <summary>
        /// Get available values of <see cref="Culture"/>.
        /// </summary>
        public IList<ApplicationCulture> Cultures { get; } = new List<ApplicationCulture>(Enum.GetValues<ApplicationCulture>()).AsReadOnly();


        /// <summary>
        /// Check whether restarting all main windows is needed or not.
        /// </summary>
        public bool IsRestartingMainWindowsNeeded
        {
            get => this.Application.IsRestartingMainWindowsNeeded;
        }


        /// <summary>
        /// Called when property of application changed.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnApplicationPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnApplicationPropertyChanged(e);
            if (e.PropertyName == nameof(IAppSuiteApplication.IsRestartingMainWindowsNeeded))
                this.OnPropertyChanged(nameof(IsRestartingMainWindowsNeeded));
        }


        /// <summary>
        /// Called when setting changed.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnSettingChanged(SettingChangedEventArgs e)
        {
            base.OnSettingChanged(e);
            var key = e.Key;
            if (key == this.Application.CultureSettingKey)
                this.OnPropertyChanged(nameof(Culture));
            else if (key == this.Application.ThemeModeSettingKey)
                this.OnPropertyChanged(nameof(ThemeMode));
        }


        /// <summary>
        /// Get or set theme mode.
        /// </summary>
        public ThemeMode ThemeMode
        {
            get => this.Settings.GetValueOrDefault(this.Application.ThemeModeSettingKey);
            set
            {
                if (value == ThemeMode.System && !this.Application.IsSystemThemeModeSupported)
                    return;
                this.Settings.SetValue<ThemeMode>(this.Application.ThemeModeSettingKey, value);
            }
        }


        /// <summary>
        /// Get available values of <see cref="ThemeMode"/>.
        /// </summary>
        public IList<ThemeMode> ThemeModes { get; } 
    }
}
