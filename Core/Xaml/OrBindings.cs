using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.Xaml;

/// <summary>
/// Markup extension to generate binding which use <see cref="Avalonia.Data.Converters.BoolConverters.Or"/> to combine multiple bindings.
/// </summary>
/// <param name="bindings">Bindings to be combined.</param>
public class OrBindings(IList<IBinding> bindings) : MarkupExtension
{
    /// <summary>
    /// Initialize new <see cref="OrBindings"/> instance.
    /// </summary>
    /// <param name="binding1">1st binding.</param>
    /// <param name="binding2">2nd binding.</param>
    public OrBindings(IBinding binding1, IBinding binding2): this([ binding1, binding2 ])
    { }
    
    
    /// <summary>
    /// Initialize new <see cref="OrBindings"/> instance.
    /// </summary>
    /// <param name="binding1">1st binding.</param>
    /// <param name="binding2">2nd binding.</param>
    /// <param name="binding3">3rd binding.</param>
    public OrBindings(IBinding binding1, IBinding binding2, IBinding binding3): this([ binding1, binding2, binding3 ])
    { }
    
    
    /// <summary>
    /// Initialize new <see cref="OrBindings"/> instance.
    /// </summary>
    /// <param name="binding1">1st binding.</param>
    /// <param name="binding2">2nd binding.</param>
    /// <param name="binding3">3rd binding.</param>
    /// <param name="binding4">4th binding.</param>
    public OrBindings(IBinding binding1, IBinding binding2, IBinding binding3, IBinding binding4): this([ binding1, binding2, binding3, binding4 ])
    { }
    
    
    /// <summary>
    /// Initialize new <see cref="OrBindings"/> instance.
    /// </summary>
    /// <param name="binding1">1st binding.</param>
    /// <param name="binding2">2nd binding.</param>
    /// <param name="binding3">3rd binding.</param>
    /// <param name="binding4">4th binding.</param>
    /// <param name="binding5">5th binding.</param>
    public OrBindings(IBinding binding1, IBinding binding2, IBinding binding3, IBinding binding4, IBinding binding5): this([ binding1, binding2, binding3, binding4, binding5 ])
    { }


    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var target = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget))!;
        if (target.TargetObject is Setter)
            throw new NotSupportedException($"Cannot use {nameof(OrBindings)} markup extension in style setter.");
        return new Avalonia.Data.MultiBinding
        {
            Bindings = bindings,
            Converter = Avalonia.Data.Converters.BoolConverters.Or,
        };
    }
}