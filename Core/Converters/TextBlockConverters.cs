using Avalonia.Data.Converters;
using System;

namespace CarinaStudio.AppSuite.Converters;

/// <summary>
/// <see cref="IValueConverter"/>s for <see cref="Avalonia.Controls.TextBlock"/>.
/// </summary>
public static class TextBlockConverters
{
    /// <summary>
    /// Converter to convert from font size to line height.
    /// </summary>
    public static readonly IValueConverter FontSizeToLineHeight;
    
    
    // Static constructor.
    static TextBlockConverters()
    {
        // [Workaround] Line height will be different between non-CJK and CJK texts
        FontSizeToLineHeight = IAppSuiteApplication.CurrentOrNull?.CheckAvaloniaVersion(11, 2) == true
            ? new FuncValueConverter<double, double>(fontSize => Math.Ceiling(fontSize * 1.28))
            : new FuncValueConverter<double, double>(_ => double.NaN);
    }
}