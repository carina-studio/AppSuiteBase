using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;

namespace CarinaStudio.AppSuite.Converters;

/// <summary>
/// Value converters for <see cref="ButtonSpinner"/>.
/// </summary>
static class ButtonSpinnerConverters
{
    /// <summary>
    /// <see cref="IValueConverter"/> to convert from <see cref="ButtonSpinner"/> to <see cref="CornerRadius"/> of button at left hand side.
    /// </summary>
    public static readonly IValueConverter LeftButtonCornerRadius = new FuncValueConverter<ButtonSpinner, CornerRadius>(buttonSpinner =>
    {
        if (buttonSpinner is null)
            return default;
        var borderThickness = buttonSpinner.BorderThickness;
        var cornerRadius = buttonSpinner.CornerRadius;
        return new(Math.Max(0, cornerRadius.TopLeft - (borderThickness.Top + borderThickness.Left) / 4), 
            0,
            0,
            Math.Max(0, cornerRadius.BottomLeft - (borderThickness.Bottom + borderThickness.Left) / 4)
        );
    });
    
    
    /// <summary>
    /// <see cref="IValueConverter"/> to convert from <see cref="ButtonSpinner"/> to <see cref="CornerRadius"/> of button at right hand side.
    /// </summary>
    public static readonly IValueConverter RightButtonCornerRadius = new FuncValueConverter<ButtonSpinner, CornerRadius>(buttonSpinner =>
    {
        if (buttonSpinner is null)
            return default;
        var borderThickness = buttonSpinner.BorderThickness;
        var cornerRadius = buttonSpinner.CornerRadius;
        return new(0, 
            Math.Max(0, cornerRadius.TopRight - (borderThickness.Top + borderThickness.Right) / 4),
            Math.Max(0, cornerRadius.BottomRight - (borderThickness.Bottom + borderThickness.Right) / 4),
            0
        );
    });
}