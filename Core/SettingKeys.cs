using CarinaStudio.Configuration;
using System;

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Default keys of setting.
    /// </summary>
    public static class SettingKeys
    {
        /// <summary>
        /// Accept application update with non-stable version.
        /// </summary>
        public static readonly SettingKey<bool> AcceptNonStableApplicationUpdate = new SettingKey<bool>(nameof(AcceptNonStableApplicationUpdate), true);


        /// <summary>
        /// Application culture.
        /// </summary>
        public static readonly SettingKey<ApplicationCulture> Culture = new SettingKey<ApplicationCulture>(nameof(Culture), ApplicationCulture.System);


        /// <summary>
        /// Enable blurry window background if available.
        /// </summary>
        public static readonly SettingKey<bool> EnableBlurryBackground = new SettingKey<bool>(nameof(EnableBlurryBackground), true);


        /// <summary>
        /// Notify user when application update found.
        /// </summary>
        public static readonly SettingKey<bool> NotifyApplicationUpdate = new SettingKey<bool>(nameof(NotifyApplicationUpdate), true);


        /// <summary>
		/// Show process info on UI or not.
		/// </summary>
		public static readonly SettingKey<bool> ShowProcessInfo = new SettingKey<bool>(nameof(ShowProcessInfo), false);


        /// <summary>
        /// Theme mode.
        /// </summary>
        public static readonly SettingKey<ThemeMode> ThemeMode = new SettingKey<ThemeMode>(nameof(ThemeMode), AppSuite.ThemeMode.System);
    }
}
