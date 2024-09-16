using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using System;

namespace CarinaStudio.AppSuite.Xaml;

/// <summary>
/// Markup extension for dynamic string resource.
/// </summary>
/// <param name="stringResKey">Key of string resource.</param>
public class StringResource(string stringResKey): MarkupExtension
{
    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new DynamicResourceExtension($"String/{stringResKey}").ProvideValue(serviceProvider);
}