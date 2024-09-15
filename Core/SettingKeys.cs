using CarinaStudio.AppSuite.Scripting;
using CarinaStudio.Configuration;
using System;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Default keys of setting.
/// </summary>
// ReSharper disable once ConvertToStaticClass
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SettingKeys
{
    /// <summary>
    /// Accept application update with non-stable version.
    /// </summary>
    public static readonly SettingKey<bool> AcceptNonStableApplicationUpdate = new(nameof(AcceptNonStableApplicationUpdate), true);


    /// <summary>
    /// Application culture.
    /// </summary>
    public static readonly SettingKey<ApplicationCulture> Culture = new(nameof(Culture), ApplicationCulture.System);


    /// <summary>
    /// Default language of script.
    /// </summary>
    public static readonly SettingKey<ScriptLanguage> DefaultScriptLanguage = new(nameof(DefaultScriptLanguage), ScriptLanguage.CSharp);


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
    public static readonly SettingKey<bool> LaunchWithSplashWindow = new(nameof(LaunchWithSplashWindow), true);


    /// <summary>
    /// Notify user when application update found.
    /// </summary>
    public static readonly SettingKey<bool> NotifyApplicationUpdate = new(nameof(NotifyApplicationUpdate), true);


    /// <summary>
    /// Show notification dialog when runtime error occurred by script.
    /// </summary>
    public static readonly SettingKey<bool> PromptWhenScriptRuntimeErrorOccurred = new(nameof(PromptWhenScriptRuntimeErrorOccurred), true);


    /// <summary>
    /// Show process info on UI or not.
    /// </summary>
    public static readonly SettingKey<bool> ShowProcessInfo = new(nameof(ShowProcessInfo), false);


    /// <summary>
    /// Theme mode.
    /// </summary>
    public static readonly SettingKey<ThemeMode> ThemeMode = new(nameof(ThemeMode), AppSuite.ThemeMode.System);


    /// <summary>
    /// Use compact layout for user interface.
    /// </summary>
    public static readonly SettingKey<bool> UseCompactUserInterface = new(nameof(UseCompactUserInterface), false);


    /// <summary>
    /// Use spaces instead of tab for indentation in script.
    /// </summary>
    public static readonly SettingKey<bool> UseSpacesForIndentationInScript = new(nameof(UseSpacesForIndentationInScript), true);


    // Constructor.
    SettingKeys()
    { }
}