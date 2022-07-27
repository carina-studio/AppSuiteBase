using CarinaStudio.AppSuite.Scripting;
using CarinaStudio.Configuration;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Default keys of setting.
/// </summary>
public sealed class SettingKeys
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
    /// Default language of script.
    /// </summary>
    public static readonly SettingKey<ScriptLanguage> DefaultScriptLanguage = new(nameof(DefaultScriptLanguage), ScriptLanguage.JavaScript);


    /// <summary>
    /// Enable blurry window background if available.
    /// </summary>
    public static readonly SettingKey<bool> EnableBlurryBackground = new SettingKey<bool>(nameof(EnableBlurryBackground), true);


    /// <summary>
    /// Enable running script or not.
    /// </summary>
    public static readonly SettingKey<bool> EnableRunningScript = new(nameof(EnableRunningScript), false);


    /// <summary>
    /// Indentation size of script.
    /// </summary>
    public static readonly SettingKey<int> IndentationSizeInScript = new(nameof(IndentationSizeInScript), 4);


    /// <summary>
    /// Whether splash window should be shown when launching application or not.
    /// </summary>
    public static readonly SettingKey<bool> LaunchWithSplashWindow = new SettingKey<bool>(nameof(LaunchWithSplashWindow), true);


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


    /// <summary>
    /// Use spaces instead of tab for indentation in script.
    /// </summary>
    public static readonly SettingKey<bool> UseSpacesForIndentationInScript = new(nameof(UseSpacesForIndentationInScript), true);


    // Constructor.
    SettingKeys()
    { }
}