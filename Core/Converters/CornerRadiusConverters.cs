using Avalonia;
using Avalonia.Data.Converters;

namespace CarinaStudio.AppSuite.Converters;

/// <summary>
/// <see cref="IValueConverter"/> to convert <see cref="CornerRadius"/>.
/// </summary>
public static class CornerRadiusConverters
{
    /// <summary>
    /// Converter to keep bottom corner radius.
    /// </summary>
    public static readonly IValueConverter Bottom = new FuncValueConverter<CornerRadius, CornerRadius>(cr =>
        new(0, 0, cr.BottomRight, cr.BottomLeft));
    

    /// <summary>
    /// Converter to keep left corner radius.
    /// </summary>
    public static readonly IValueConverter Left = new FuncValueConverter<CornerRadius, CornerRadius>(cr =>
        new(cr.TopLeft, 0, 0, cr.BottomLeft));
    

    /// <summary>
    /// Converter to keep right corner radius.
    /// </summary>
    public static readonly IValueConverter Right = new FuncValueConverter<CornerRadius, CornerRadius>(cr =>
        new(0, cr.TopRight, cr.BottomRight, 0));

    
    /// <summary>
    /// Converter to keep top corner radius.
    /// </summary>
    public static readonly IValueConverter Top = new FuncValueConverter<CornerRadius, CornerRadius>(cr =>
        new(cr.TopLeft, cr.TopRight, 0, 0));
}