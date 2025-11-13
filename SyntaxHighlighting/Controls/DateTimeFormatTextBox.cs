using Avalonia;
using Avalonia.Controls;
using CarinaStudio.AppSuite.Controls.Highlighting;
using CarinaStudio.Controls;
using System;
using Avalonia.Input;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="Avalonia.Controls.TextBox"/> which allows entering format of date time.
/// </summary>
public class DateTimeFormatTextBox : SyntaxHighlightingObjectTextBox<string>
{
    /// <summary>
    /// Property of <see cref="Object"/>.
    /// </summary>
    public static new readonly DirectProperty<DateTimeFormatTextBox, string?> ObjectProperty = AvaloniaProperty.RegisterDirect<DateTimeFormatTextBox, string?>(nameof(Object), t => t.Object, (t, o) => t.Object = o);


    /// <summary>
    /// Initialize new <see cref="DateTimeFormatTextBox"/> instance.
    /// </summary>
    public DateTimeFormatTextBox()
    {
        SyntaxHighlighting.VerifyInitialization();
        this.AcceptsWhiteSpaces = true;
        this.PseudoClasses.Add(":dateTimeFormatTextBox");
        this.Bind(WatermarkProperty, this.GetResourceObservable("String/DateTimeFormatTextBox.Watermark"));
    }
    
    
    /// <inheritdoc/>
    public override string? Object
    {
        get => (string?)((ObjectTextBox)this).Object;
        set => ((ObjectTextBox)this).Object = value;
    }


    /// <inheritdoc/>
    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Text) && Math.Min(this.SelectionStart, this.SelectionEnd) == 0)
            e.Handled = true;
        base.OnTextInput(e);
    }


    /// <inheritdoc/>
    protected override void RaiseObjectChanged(string? oldValue, string? newValue) =>
        this.RaisePropertyChanged(ObjectProperty, oldValue, newValue);


    /// <inheritdoc/>
    protected override SyntaxHighlightingDefinitionSet SyntaxHighlightingDefinitionSet => DateTimeFormatSyntaxHighlighting.CreateDefinitionSet(IAvaloniaApplication.Current);


    /// <inheritdoc/>
    protected override bool TryConvertToObject(string text, out string? obj)
    {
        try
        {
            _ = DateTime.UtcNow.ToString(text);
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