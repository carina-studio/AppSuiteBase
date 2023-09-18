using CarinaStudio.Configuration;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Predefined keys of configuration.
/// </summary>
public abstract class ConfigurationKeys
{
    /// <summary>
    /// Interval of checking application update info in milliseconds.
    /// </summary>
    public static readonly SettingKey<int> AppUpdateInfoCheckingInterval = new(nameof(AppUpdateInfoCheckingInterval), 3600000 /* 1 hr */);
    /// <summary>
    /// Name of default font family for showing agreement.
    /// </summary>
    public static readonly SettingKey<string> DefaultFontFamilyOfAgreement = new(nameof(DefaultFontFamilyOfAgreement), "Roboto");
    /// <summary>
    /// Name of default font family for showing document.
    /// </summary>
    public static readonly SettingKey<string> DefaultFontFamilyOfDocument = new(nameof(DefaultFontFamilyOfDocument), "Roboto");
    /// <summary>
    /// Whether verbose of avalonia logging is enabled or not.
    /// </summary>
    public static readonly SettingKey<bool> EnableAvaloniaVerboseLogging = new(nameof(EnableAvaloniaVerboseLogging), false);
    /// <summary>
    /// Whether retrieved application update info should be always accepted or not.
    /// </summary>
    public static readonly SettingKey<bool> ForceAcceptingAppUpdateInfo = new(nameof(ForceAcceptingAppUpdateInfo), false);
    /// <summary>
    /// Default timeout to dismiss notification in milliseconds.
    /// </summary>
    public static readonly SettingKey<int> DefaultTimeoutOfNotification = new(nameof(DefaultTimeoutOfNotification), 5000);
    /// <summary>
    /// Delay before performing full GC when application is in background mode.
    /// </summary>
    public static readonly SettingKey<int> DelayToPerformFullGCInBackgroundMode = new(nameof(DelayToPerformFullGCInBackgroundMode), 5000);
    /// <summary>
    /// Delay before performing full GC when user interaction stopped.
    /// </summary>
    public static readonly SettingKey<int> DelayToPerformFullGCWhenUserInteractionStopped = new(nameof(DelayToPerformFullGCWhenUserInteractionStopped), 60000);
    /// <summary>
    /// Delay before updating navigation bar in dialog.
    /// </summary>
    public static readonly SettingKey<int> DialogNavigationBarUpdateDelay = new(nameof(DialogNavigationBarUpdateDelay), 100);
    /// <summary>
    /// Interval of checking whether script running is completed or not.
    /// </summary>
    public static readonly SettingKey<int> ScriptCompletionCheckingInterval = new(nameof(ScriptCompletionCheckingInterval), 3000);
    /// <summary>
    /// Timeout before notifying user that network connection is needed for product activation.
    /// </summary>
    public static readonly SettingKey<int> TimeoutToNotifyNetworkConnectionForProductActivation = new(nameof(TimeoutToNotifyNetworkConnectionForProductActivation), 3 * 60 * 1000);
    /// <summary>
    /// Timeout of keeping in user interactive mode in milliseconds.
    /// </summary>
    public static readonly SettingKey<int> UserInteractionTimeout = new(nameof(UserInteractionTimeout), 3000);
    /// <summary>
    /// Size of working area in logical pixels to suggest using compact user interface.
    /// </summary>
    public static readonly SettingKey<int> WorkingAreaSizeToSuggestUsingCompactUI = new(nameof(WorkingAreaSizeToSuggestUsingCompactUI), 700);


    // Constructor.
    ConfigurationKeys()
    { }
}