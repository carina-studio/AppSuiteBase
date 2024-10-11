using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CarinaStudio.AppSuite.Converters;

/// <summary>
/// Predefined <see cref="IValueConverter"/>s for <see cref="TemplatedControl"/>.
/// </summary>
public static class TemplatedControlConverters
{
    // Convert from border thickness to box shadow.
    internal static BoxShadows ConvertBorderThicknessToBoxShadows(Thickness borderThickness, string? state)
    {
        if (borderThickness.Left <= 0 && borderThickness.Top <= 0 && borderThickness.Right <= 0 && borderThickness.Bottom <= 0)
            return default;
        if (IAppSuiteApplication.CurrentOrNull is not { } app 
            || !app.TryFindResource("Double/TextBox.BoxShadow.Radius.Focused", out double? radius)
            || !app.TryFindResource("Double/TextBox.BoxShadow.Spread.Focused", out double? spread))
            return default;
        var color = state switch
        {
            "Error" => app.TryFindResource("Color/TextBox.BoxShadow.Error", out Color? colorResource)
                ? colorResource.Value
                : default,
            _ => app.TryFindResource("Color/TextBox.BoxShadow.Focused", out Color? colorResource)
                ? colorResource.Value
                : default,
        };
        var boxShadow = new BoxShadow
        {
            Blur = radius.Value,
            Color = color,
            Spread = spread.Value,
        };
        return new BoxShadows(boxShadow);
    }
    
    
    /// <summary>
    /// Converter to convert from <see cref="TemplatedControl"/> to <see cref="BoxShadows"/> for focused state.
    /// </summary>
    public static readonly IValueConverter FocusedBoxShadow = new FuncValueConverter<TemplatedControl, string, BoxShadows>((control, parameter) =>
    {
        if (control is null)
            return default;
        return ConvertBorderThicknessToBoxShadows(control.BorderThickness, parameter);
    });
}