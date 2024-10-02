using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CarinaStudio.AppSuite.Converters;

/// <summary>
/// Predefined <see cref="IValueConverter"/>s for <see cref="TextBox"/>.
/// </summary>
public static class TextBoxConverters
{
    /// <summary>
    /// Converter to convert from <see cref="TextBox"/> to <see cref="BoxShadows"/> for focused state.
    /// </summary>
    public static readonly IValueConverter FocusedBoxShadowConverter = new FuncValueConverter<TextBox, string, BoxShadows>((textBox, parameter) =>
    {
        if (textBox is null || !textBox.IsEnabled || !textBox.IsFocused)
            return default;
        var borderThickness = textBox.BorderThickness;
        if (borderThickness.Left <= 0 && borderThickness.Top <= 0 && borderThickness.Right <= 0 && borderThickness.Bottom <= 0)
            return default;
        if (IAppSuiteApplication.CurrentOrNull is not { } app || !app.TryFindResource("Double/TextBox.BoxShadow.Radius.Focused", out double? radius))
            return default;
        var color = parameter switch
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
        };
        return new BoxShadows(boxShadow);
    });
}