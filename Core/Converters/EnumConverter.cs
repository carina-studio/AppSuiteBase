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
        readonly IApplication? app;
        readonly Type enumType;
        readonly string enumTypeName;


        /// <summary>
        /// Initialize new <see cref="EnumConverter"/> instance.
        /// </summary>
        /// <param name="app">Application.</param>
        /// <param name="enumType">Type of enumeration.</param>
        public EnumConverter(IApplication? app, Type enumType)
        {
            if (!enumType.IsEnum)
                throw new ArgumentException($"{enumType} is not a type of enumeration.");
            this.app = app;
            this.enumType = enumType;
            this.enumTypeName = enumType.Name;
        }


        /// <inheritdoc/>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (targetType != typeof(string) || value == null)
                return null;
            if (value.GetType() == this.enumType)
            {
                if (this.app != null)
                    return this.app.GetString($"{this.enumTypeName}.{value}", value.ToString());
                return value.ToString();
            }
            return null;
        }


        /// <inheritdoc/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (targetType != this.enumType)
                return null;
            if (value is string str)
            {
                if (this.app != null)
                {
                    var enumValues = Enum.GetValues(this.enumType);
                    foreach (var enumValue in enumValues)
                    {
                        if (this.app.GetString($"{this.enumTypeName}.{value}") == str)
                            return enumValue;
                    }
                }
                else if (Enum.TryParse(this.enumType, str, out var enumValue))
                    return enumValue;
            }
            else if (value?.GetType() == this.enumType)
                return value;
            return null;
        }
    }
}
