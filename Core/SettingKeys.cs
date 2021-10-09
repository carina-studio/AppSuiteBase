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
        /// Application culture.
        /// </summary>
        public static readonly SettingKey<ApplicationCulture> Culture = new SettingKey<ApplicationCulture>(nameof(Culture), ApplicationCulture.System);


        /// <summary>
        /// Setting key of enabling blurry window background if available.
        /// </summary>
        public static readonly SettingKey<bool> EnableBlurryBackground = new SettingKey<bool>(nameof(EnableBlurryBackground), true);


        /// <summary>
        /// Theme mode.
        /// </summary>
        public static readonly SettingKey<ThemeMode> ThemeMode = new SettingKey<ThemeMode>(nameof(ThemeMode), AppSuite.ThemeMode.System);
    }
}
