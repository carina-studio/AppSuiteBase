﻿using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CarinaStudio.AppSuite.Converters
{
    internal class ScrollingButtonVisibilityConverter : IMultiValueConverter
    {
        // Static fields.
        public static readonly IMultiValueConverter Default = new ScrollingButtonVisibilityConverter();


        /// <inheritdoc/>
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (typeof(object) != targetType && typeof(bool) != targetType)
                return null;
            if (values.Count >= 3
                && values[0] is double offset
                && values[1] is double extentSize
                && values[2] is double viewportSize)
            {
                return (parameter as string) switch
                {
                    "Right" or "Down" => (extentSize > viewportSize && (offset + viewportSize) < extentSize - 1),
                    _ => (extentSize > viewportSize && offset > 1),
                };
            }
            return false;
        }
    }
}
