using Avalonia;
using Avalonia.Controls;
using CarinaStudio.AppSuite.Controls.Highlighting;
using CarinaStudio.Controls;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="Avalonia.Controls.TextBox"/> which allows entering format of time span.
/// </summary>
public class TimeSpanFormatTextBox : SyntaxHighlightingObjectTextBox<string>
{
    /// <summary>
    /// Property of <see cref="Object"/>.
    /// </summary>
    public static new readonly DirectProperty<TimeSpanFormatTextBox, string?> ObjectProperty = AvaloniaProperty.RegisterDirect<TimeSpanFormatTextBox, string?>(nameof(Object), t => t.Object, (t, o) => t.Object = o);


    /// <summary>
    /// Initialize new <see cref="TimeSpanFormatTextBox"/> instance.
    /// </summary>
    public TimeSpanFormatTextBox()
    {
        SyntaxHighlighting.VerifyInitialization();
        this.PseudoClasses.Add(":timeSpanFormatTextBox");
        this.Bind(WatermarkProperty, this.GetResourceObservable("String/TimeSpanFormatTextBox.Watermark"));
    }
    
    
    /// <inheritdoc/>
    public override string? Object
    {
        get => (string?)((ObjectTextBox)this).Object;
        set => ((ObjectTextBox)this).Object = value;
    }


    /// <inheritdoc/>
    protected override void RaiseObjectChanged(string? oldValue, string? newValue) =>
        this.RaisePropertyChanged(ObjectProperty, oldValue, newValue);


    /// <inheritdoc/>
    protected override SyntaxHighlightingDefinitionSet SyntaxHighlightingDefinitionSet => TimeSpanFormatSyntaxHighlighting.CreateDefinitionSet(IAvaloniaApplication.Current);


    /// <inheritdoc/>
    protected override bool TryConvertToObject(string text, out string? obj)
    {
        try
        {
            _ = TimeSpan.Zero.ToString(text);
            obj = text;
            return true;
        }
        catch
        {
            obj = null;
            return false;
        }
    }
}