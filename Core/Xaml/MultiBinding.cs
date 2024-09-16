using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.Xaml;

/// <summary>
/// Markup extension to generate <see cref="Avalonia.Data.MultiBinding"/>.
/// </summary>
/// <param name="bindings"></param>
public class MultiBinding(IList<IBinding> bindings): MarkupExtension
{
    /// <summary>
    /// Initialize new <see cref="MultiBinding"/> instance.
    /// </summary>
    /// <param name="binding1">1st binding.</param>
    /// <param name="binding2">2nd binding.</param>
    public MultiBinding(IBinding binding1, IBinding binding2): this([ binding1, binding2 ])
    { }
    
    
    /// <summary>
    /// Initialize new <see cref="MultiBinding"/> instance.
    /// </summary>
    /// <param name="binding1">1st binding.</param>
    /// <param name="binding2">2nd binding.</param>
    /// <param name="binding3">3rd binding.</param>
    public MultiBinding(IBinding binding1, IBinding binding2, IBinding binding3): this([ binding1, binding2, binding3 ])
    { }
    
    
    /// <summary>
    /// Initialize new <see cref="MultiBinding"/> instance.
    /// </summary>
    /// <param name="binding1">1st binding.</param>
    /// <param name="binding2">2nd binding.</param>
    /// <param name="binding3">3rd binding.</param>
    /// <param name="binding4">4th binding.</param>
    public MultiBinding(IBinding binding1, IBinding binding2, IBinding binding3, IBinding binding4): this([ binding1, binding2, binding3, binding4 ])
    { }
    
    
    /// <summary>
    /// Initialize new <see cref="MultiBinding"/> instance.
    /// </summary>
    /// <param name="binding1">1st binding.</param>
    /// <param name="binding2">2nd binding.</param>
    /// <param name="binding3">3rd binding.</param>
    /// <param name="binding4">4th binding.</param>
    /// <param name="binding5">5th binding.</param>
    public MultiBinding(IBinding binding1, IBinding binding2, IBinding binding3, IBinding binding4, IBinding binding5): this([ binding1, binding2, binding3, binding4, binding5 ])
    { }
    
    
    /// <summary>
    /// Get or set <see cref="IMultiValueConverter"/> to convert from multiple bindings to single binding.
    /// </summary>
    public IMultiValueConverter? Converter { get; set; }
    
    
    /// <summary>
    /// Get or set fallback value.
    /// </summary>
    public required object FallbackValue { get; set; }


    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider provider) =>
        new Avalonia.Data.MultiBinding
        {
            Bindings = bindings,
            Converter = this.Converter,
            FallbackValue = this.FallbackValue,
            StringFormat = this.StringFormat,
        };


    /// <summary>
    /// Get or set the string format.
    /// </summary>
    public string? StringFormat { get; set; }
}