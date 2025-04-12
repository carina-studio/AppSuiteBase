using System.Globalization;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Extension methods for <see cref="CultureInfo"/>.
/// </summary>
public static class CultureInfoExtensions
{
    /// <summary>
    /// Get corresponding variant of Chinese.
    /// </summary>
    /// <param name="cultureInfo"><see cref="CultureInfo"/>.</param>
    /// <returns>Variant of Chinese.</returns>
    public static ChineseVariant GetChineseVariant(this CultureInfo cultureInfo)
    {
        var name = cultureInfo.Name;
        return name.StartsWith("zh") && name.EndsWith("TW")
            ? ChineseVariant.Taiwan
            : ChineseVariant.Default;
    }
}