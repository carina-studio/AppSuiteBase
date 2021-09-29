using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace CarinaStudio.AppSuite.Converters
{
    /// <summary>
    /// <see cref="IValueConverter"/> to convert enumeration to readable string.
    /// </summary>
    public class EnumConverter : IValueConverter
    {
        // Fields.
        readonly IApplication app;
        readonly Type enumType;
        readonly string enumTypeName;


        /// <summary>
        /// Initialize new <see cref="EnumConverter"/> instance.
        /// </summary>
        /// <param name="app">Application.</param>
        /// <param name="enumType">Type of enumeration.</param>
        public EnumConverter(IApplication app, Type enumType)
        {
            if (!enumType.IsEnum)
                throw new ArgumentException($"{enumType} is not a type of enumeration.");
            this.app = app;
            this.enumType = enumType;
            this.enumTypeName = enumType.Name;
        }


        /// <summary>
        /// Convert value to string.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Target type which should be <see cref="string"/>.</param>
        /// <param name="parameter">Parameter which will be ignored.</param>
        /// <param name="culture"><see cref="CultureInfo"/>.</param>
        /// <returns>Converted string.</returns>
        public object? Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (targetType != typeof(string))
                return null;
            if (value.GetType() == this.enumType)
                return this.app.GetString($"{this.enumTypeName}.{value}", value.ToString());
            return null;
        }


        /// <summary>
        /// Convert string back to value.
        /// </summary>
        /// <remarks>The method is not implemented.</remarks>
        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
