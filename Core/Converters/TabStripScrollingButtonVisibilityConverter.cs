using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CarinaStudio.AppSuite.Converters
{
    internal class TabStripScrollingButtonVisibilityConverter : IMultiValueConverter
    {
        // Static fields.
        public static readonly TabStripScrollingButtonVisibilityConverter Default = new TabStripScrollingButtonVisibilityConverter();


        /// <inheritdoc/>
        public object? Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (typeof(object) != targetType && typeof(bool) != targetType)
                return null;
            if (values.Count >= 3
                && values[0] is double offset
                && values[1] is double extentWidth
                && values[2] is double viewportWidth)
            {
                if ((parameter as string) == "Right")
                    return (extentWidth > viewportWidth && (offset + viewportWidth) < extentWidth - 1);
                return (extentWidth > viewportWidth && offset > 1);
            }
            return false;
        }


        /// <inheritdoc/>
        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
