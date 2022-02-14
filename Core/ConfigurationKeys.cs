using CarinaStudio.Configuration;
using System;

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Predefined keys of configuration.
    /// </summary>
    public sealed class ConfigurationKeys
    {
        /// <summary>
        /// Interval of checking application update info in milliseconds.
        /// </summary>
        public static readonly SettingKey<int> AppUpdateInfoCheckingInterval = new(nameof(AppUpdateInfoCheckingInterval), 3600000 /* 1 hr */);
        /// <summary>
        /// Whether retrieved application update info should be always accepted or not.
        /// </summary>
        public static readonly SettingKey<bool> ForceAcceptingAppUpdateInfo = new(nameof(ForceAcceptingAppUpdateInfo), false);


        // Constructor.
        ConfigurationKeys()
        { }
    }
}