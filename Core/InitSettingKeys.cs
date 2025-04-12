using CarinaStudio.Configuration;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Keys of initial setting.
/// </summary>
// ReSharper disable once ConvertToStaticClass
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class InitSettingKeys
{
    /// <summary>
    /// Do not using ANGLE for rendering (only for Windows).
    /// </summary>
    public static readonly SettingKey<bool> DisableAngle = new(nameof(DisableAngle), false);
    
    
    // Constructor.
    InitSettingKeys()
    { }
}