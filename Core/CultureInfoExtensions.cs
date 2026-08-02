using System.Globalization;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Extension methods for <see cref="CultureInfo"/>.
/// </summary>
public static class CultureInfoExtensions
{
    extension(CultureInfo cultureInfo)
    {
        /// <summary>
        /// Get corresponding variant of Chinese.
        /// </summary>
        public ChineseVariant ChineseVariant
        {
            get
            {
                var name = cultureInfo.Name;
                return name.StartsWith("zh") && name.EndsWith("TW")
                    ? ChineseVariant.Taiwan
                    : ChineseVariant.Default;
            }
        }


        /// <summary>
        /// Check whether the culture represents Chinese or not.
        /// </summary>
        public bool IsChinese => cultureInfo.Name.StartsWith("zh");


        /// <summary>
        /// Check whether the culture represents Japanese or not.
        /// </summary>
        public bool IsJapanese => cultureInfo.Name.StartsWith("ja");
    }
}