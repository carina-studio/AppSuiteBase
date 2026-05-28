using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace CarinaStudio.AppSuite.Converters;

/// <summary>
/// Predefined converters to convert from double value.
/// </summary>
public static class DoubleConverters
{
    /// <summary>
    /// <see cref="IValueConverter"/> to convert from double value to <see cref="GridLength"/> with <see cref="GridUnitType.Auto"/>.
    /// </summary>
    public static readonly IValueConverter ToGridLengthAuto = new FuncValueConverter<double, GridLength>(value => new(value, GridUnitType.Auto));
    /// <summary>
    /// <see cref="IValueConverter"/> to convert from double value to <see cref="GridLength"/> with <see cref="GridUnitType.Pixel"/>.
    /// </summary>
    public static readonly IValueConverter ToGridLengthPixel = new FuncValueConverter<double, GridLength>(value => new(value, GridUnitType.Pixel));
    /// <summary>
    /// <see cref="IValueConverter"/> to convert from double value to <see cref="GridLength"/> with <see cref="GridUnitType.Star"/>.
    /// </summary>
    public static readonly IValueConverter ToGridLengthStar = new FuncValueConverter<double, GridLength>(value => new(value, GridUnitType.Star));
}