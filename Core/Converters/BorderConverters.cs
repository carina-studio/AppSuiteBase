using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CarinaStudio.AppSuite.Converters;

/// <summary>
/// Predefined <see cref="IValueConverter"/>s for <see cref="Border"/>.
/// </summary>
public static class BorderConverters
{
    /// <summary>
    /// Converter to convert from <see cref="Border"/> to <see cref="BoxShadows"/> for focused state.
    /// </summary>
    public static readonly IValueConverter FocusedBoxShadow = new FuncValueConverter<Border, string, BoxShadows>((border, parameter) =>
    {
        if (border is null)
            return default;
        return TemplatedControlConverters.ConvertBorderThicknessToBoxShadows(border.BorderThickness, parameter);
    });
}