using Avalonia.Data.Converters;
using CarinaStudio.IO;
using System;
using System.Globalization;

namespace CarinaStudio.AppSuite.Converters
{
    /// <summary>
    /// <see cref="IValueConverter"/> to convert from file size in bytes to readable string.
    /// </summary>
    public class FileSizeConverter : IValueConverter
    {
        /// <summary>
        /// Default instance.
        /// </summary>
        public static readonly IValueConverter Default = new FileSizeConverter();


        // Constructor.
        FileSizeConverter()
        { }


        /// <summary>
        /// Convert value to string.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Target type which should be <see cref="string"/>.</param>
        /// <param name="parameter">Parameter which will be ignored.</param>
        /// <param name="culture"><see cref="CultureInfo"/>.</param>
        /// <returns>Converted string.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (targetType != typeof(string) && targetType != typeof(object))
                return null;
            if (value is long longValue)
                return longValue.ToFileSizeString();
            if (value is int intValue)
                return intValue.ToFileSizeString();
            return null;
        }


        /// <summary>
        /// Convert string back to value.
        /// </summary>
        /// <remarks>The method is not implemented.</remarks>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
    }
}
