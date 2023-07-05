using Avalonia.Data.Converters;
using Avalonia.Media;
using CarinaStudio.Data.Converters;
using System.Globalization;

namespace CarinaStudio.AppSuite.Converters;

/// <summary>
/// Implementation of <see cref="IValueConverter"/> to convert from enumeration value to image.
/// </summary>
public class EnumIconConverter : BaseValueConverter<object?, IImage?>
{
    // Fields.
    readonly IAppSuiteApplication? app;
    readonly string? resourceNamePostfix;
    readonly string resourceNamePrefix;


    /// <summary>
    /// Initialize new <see cref="EnumIconConverter"/> instance.
    /// </summary>
    /// <param name="resNamePrefix">Prefix of icon resource name.</param>
    /// <param name="resNamePostfix">Postfix of icon resource name.</param>
    protected EnumIconConverter(string resNamePrefix, string? resNamePostfix)
    {
        this.app = IAppSuiteApplication.CurrentOrNull;
        this.resourceNamePostfix = resNamePostfix;
        this.resourceNamePrefix = resNamePrefix;
    }


    /// <inheritdoc/>
    protected override IImage? Convert(object? value, object? parameter, CultureInfo culture)
    {
        if (value == null || this.app == null)
            return null;
        IImage? icon;
        var resName = string.IsNullOrEmpty(this.resourceNamePostfix)
            ? $"Image/{this.resourceNamePrefix}.{value}"
            : $"Image/{this.resourceNamePrefix}.{value}.{this.resourceNamePostfix}";
        if (parameter is string paramString && paramString.Length > 0)
        {
            if (app.TryFindResource($"{resName}.{paramString}", out icon))
                return icon;
        }
        app.TryFindResource(resName, out icon);
        return icon;
    }
}