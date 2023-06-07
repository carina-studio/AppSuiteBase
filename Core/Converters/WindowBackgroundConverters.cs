using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CarinaStudio.Controls;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.Converters;

static class WindowBackgroundConverters
{
    /// <summary>
    /// Window with default class.
    /// </summary>
    public static readonly IValueConverter Default = new FuncValueConverter<IReadOnlyList<WindowTransparencyLevel>?, IBrush?>(levels =>
    {
        var app = AppSuiteApplication.CurrentOrNull;
        if (app is null)
            return null;
        if (levels is null || levels.Count == 0 || levels[0] == WindowTransparencyLevel.None)
            return app.FindResourceOrDefault<IBrush?>("Brush/Window.Background");
        return app.FindResourceOrDefault<IBrush?>("Brush/Window.Background.Transparent");
    });


    /// <summary>
    /// Window with "Tabbed" class.
    /// </summary>
    public static readonly IValueConverter Tabbed = new FuncValueConverter<IReadOnlyList<WindowTransparencyLevel>?, IBrush?>(levels =>
    {
        var app = AppSuiteApplication.CurrentOrNull;
        if (app is null)
            return null;
        if (levels is null || levels.Count == 0 || levels[0] == WindowTransparencyLevel.None)
            return app.FindResourceOrDefault<IBrush?>("Brush/Window.Background.Tabbed");
        return app.FindResourceOrDefault<IBrush?>("Brush/Window.Background.Tabbed.Transparent");
    });
}