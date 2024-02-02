using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;

namespace CarinaStudio.AppSuite.Converters;

/// <summary>
/// Converters for <see cref="TextAlignment"/>.
/// </summary>
static class TextAlignmentConverters
{
    /// <summary>
    /// Convert to <see cref="HorizontalAlignment"/> in Left-to-Right direction.
    /// </summary>
    public static readonly IValueConverter ToHorizontalAlignment = new FuncValueConverter<TextAlignment, HorizontalAlignment>(textAlignment => textAlignment switch
    {
        TextAlignment.Center => HorizontalAlignment.Center,
        TextAlignment.Left or TextAlignment.Start => HorizontalAlignment.Left,
        TextAlignment.Right or TextAlignment.End => HorizontalAlignment.Right,
        _ => HorizontalAlignment.Stretch,
    });
    
    
    /// <summary>
    /// Convert to <see cref="HorizontalAlignment"/> in Right-to-Left direction.
    /// </summary>
    public static readonly IValueConverter ToHorizontalAlignmentRTL = new FuncValueConverter<TextAlignment, HorizontalAlignment>(textAlignment => textAlignment switch
    {
        TextAlignment.Center => HorizontalAlignment.Center,
        TextAlignment.Left or TextAlignment.End => HorizontalAlignment.Left,
        TextAlignment.Right or TextAlignment.Start => HorizontalAlignment.Right,
        _ => HorizontalAlignment.Stretch,
    });
}