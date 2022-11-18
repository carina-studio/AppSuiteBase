using System;
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
    /// Converter to keep left corner radius and excludes given border thickness.
    /// </summary>
    public static readonly IMultiValueConverter InnerLeft = new FuncMultiValueConverter<object, CornerRadius>(values =>
    {
        var borderThickness = default(Thickness);
        var cornerRadius = default(CornerRadius);
        foreach (var value in values)
        {
            if (value is UnsetValueType)
                continue;
            if (value is Thickness t)
                borderThickness = t;
            else if (value is CornerRadius cr)
                cornerRadius = cr;
        }
        return new(Math.Max(0, cornerRadius.TopLeft - (borderThickness.Top + borderThickness.Left) / 4), 
            0,
            0,
            Math.Max(0, cornerRadius.BottomLeft - (borderThickness.Bottom + borderThickness.Left) / 4)
        );
    });


    /// <summary>
    /// Converter to keep right corner radius and excludes given border thickness.
    /// </summary>
    public static readonly IMultiValueConverter InnerRight = new FuncMultiValueConverter<object, CornerRadius>(values =>
    {
        var borderThickness = default(Thickness);
        var cornerRadius = default(CornerRadius);
        foreach (var value in values)
        {
            if (value is UnsetValueType)
                continue;
            if (value is Thickness t)
                borderThickness = t;
            else if (value is CornerRadius cr)
                cornerRadius = cr;
        }
        return new(0, 
            Math.Max(0, cornerRadius.TopRight - (borderThickness.Top + borderThickness.Right) / 4),
            Math.Max(0, cornerRadius.BottomRight - (borderThickness.Bottom + borderThickness.Right) / 4),
            0
        );
    });
    

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