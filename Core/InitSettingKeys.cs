using CarinaStudio.Configuration;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Keys of initial setting.
/// </summary>
public sealed class InitSettingKeys
{
    /// <summary>
    /// Use embedded fonts for Chinese.
    /// </summary>
    public static readonly SettingKey<bool> UseEmbeddedFontsForChinese = new(nameof(UseEmbeddedFontsForChinese), Platform.IsNotMacOS);
    
    
    // Constructor.
    InitSettingKeys()
    { }
}