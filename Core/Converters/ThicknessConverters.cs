using Avalonia;
using Avalonia.Data.Converters;

namespace CarinaStudio.AppSuite.Converters;

/// <summary>
/// <see cref="IValueConverter"/> to convert <see cref="Thickness"/>.
/// </summary>
public static class ThicknessConverters
{
    /// <summary>
    /// Converter to keep bottom of thickness.
    /// </summary>
    public static readonly IValueConverter Bottom = new FuncValueConverter<Thickness, Thickness>(t =>
        new(0, 0, 0, t.Bottom));
    

    /// <summary>
    /// Converter to keep left of thickness.
    /// </summary>
    public static readonly IValueConverter Left = new FuncValueConverter<Thickness, Thickness>(t =>
        new(t.Left, 0, 0, 0));
    
    
    /// <summary>
    /// Converter to exclude bottom of thickness.
    /// </summary>
    public static readonly IValueConverter NoBottom = new FuncValueConverter<Thickness, Thickness>(t =>
        new(t.Left, t.Top, t.Right, 0));
    

    /// <summary>
    /// Converter to exclude left of thickness.
    /// </summary>
    public static readonly IValueConverter NoLeft = new FuncValueConverter<Thickness, Thickness>(t =>
        new(0, t.Top, t.Right, t.Bottom));
    

    /// <summary>
    /// Converter to exclude right of thickness.
    /// </summary>
    public static readonly IValueConverter NoRight = new FuncValueConverter<Thickness, Thickness>(t =>
        new(t.Left, t.Top, 0, t.Bottom));
    

    /// <summary>
    /// Converter to exclude top of thickness.
    /// </summary>
    public static readonly IValueConverter NoTop = new FuncValueConverter<Thickness, Thickness>(t =>
        new(t.Left, 0, t.Right, t.Bottom));
    

    /// <summary>
    /// Converter to keep right of thickness.
    /// </summary>
    public static readonly IValueConverter Right = new FuncValueConverter<Thickness, Thickness>(t =>
        new(0, 0, t.Right, 0));
    

    /// <summary>
    /// Converter to keep top of thickness.
    /// </summary>
    public static readonly IValueConverter Top = new FuncValueConverter<Thickness, Thickness>(t =>
        new(0, t.Top, 0, 0));
}