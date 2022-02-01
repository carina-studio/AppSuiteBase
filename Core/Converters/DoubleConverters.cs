using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CarinaStudio.AppSuite.Converters
{
    /// <summary>
    /// Predefined <see cref="IMultiValueConverter"/> for operations on <see cref="double"/>.
    /// </summary>
    public static class DoubleConverters
    {
        /// <summary>
        /// <see cref="IMultiValueConverter"/> to add <see cref="double"/> values.
        /// </summary>
        public static readonly IMultiValueConverter Add = new AddConverter();


        // Add.
        class AddConverter : IMultiValueConverter
        {
            // Convert.
            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                if (typeof(object) != targetType && typeof(double) != targetType)
                    return null;
                var result = 0.0;
                foreach (var value in values)
                {
                    if (value is double doubleValue)
                        result += doubleValue;
                    else if (value is IConvertible convertible)
                        result += convertible.ToDouble(culture);
                }
                return result;
            }

            // Convert back.
            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
        }
    }
}