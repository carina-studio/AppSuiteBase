using Avalonia.Data.Converters;
using System;

namespace CarinaStudio.AppSuite.Converters;

/// <summary>
/// <see cref="IValueConverter"/>s for <see cref="Avalonia.Controls.TextBlock"/>.
/// </summary>
public static class TextBlockConverters
{
    // [Workaround] To keep line height same across platforms.
    /// <summary>
    /// Converter to convert from font size to line height.
    /// </summary>
    public static readonly IValueConverter FontSizeToLineHeight = new FuncValueConverter<double, double>(fontSize => Math.Ceiling(fontSize * 1.28));
}