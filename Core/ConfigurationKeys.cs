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
        /// Name of default font family for showing agreement.
        /// </summary>
        public static readonly SettingKey<string> DefaultFontFamilyOfAgreement = new(nameof(DefaultFontFamilyOfAgreement), "Arial");
        /// <summary>
        /// Whether retrieved application update info should be always accepted or not.
        /// </summary>
        public static readonly SettingKey<bool> ForceAcceptingAppUpdateInfo = new(nameof(ForceAcceptingAppUpdateInfo), false);
        /// <summary>
        /// Interval of checking whether script running is completed or not.
        /// </summary>
        public static readonly SettingKey<int> ScriptCompletionCheckingInterval = new(nameof(ScriptCompletionCheckingInterval), 3000);
        /// <summary>
        /// Timeout before notifying user that network connection is needed for product activation.
        /// </summary>
        public static readonly SettingKey<int> TimeoutToNotifyNetworkConnectionForProductActivation = new(nameof(TimeoutToNotifyNetworkConnectionForProductActivation), 3 * 60 * 1000);
        /// <summary>
        /// Size of working area in logical pixels to suggest using compact user interface.
        /// </summary>
        public static readonly SettingKey<int> WorkingAreaSizeToSuggestUsingCompactUI = new(nameof(WorkingAreaSizeToSuggestUsingCompactUI), 768);


        // Constructor.
        ConfigurationKeys()
        { }
    }
}