using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CarinaStudio.AppSuite.Controls.Presenters;

/// <summary>
/// Extended <see cref="Avalonia.Controls.Presenters.TextPresenter"/>.
/// </summary>
public class TextPresenter : Avalonia.Controls.Presenters.TextPresenter
{
    /// <summary>
    /// Property of <see cref="PreeditTextForegroundBrush"/>.
    /// </summary>
    public static readonly StyledProperty<IBrush?> PreeditTextForegroundBrushProperty = AvaloniaProperty.Register<TextPresenter, IBrush?>(nameof(PreeditTextForegroundBrush));


    // Delegate of internal methods.
    delegate TextLayout CreateTextLayoutInternalDelegate(Size constraint, string? text, Typeface typeface, IReadOnlyList<ValueSpan<TextRunProperties>>? textStyleOverrides);
    delegate string? GetTextDelegate();


    /// <summary>
    /// Initialize new <see cref="TextPresenter"/> instance.
    /// </summary>
    public TextPresenter()
    {
        this.GetObservable(PreeditTextForegroundBrushProperty).Subscribe(_ =>
        {
            if (!string.IsNullOrEmpty(this.PreeditText))
                this.InvalidateTextLayout();
        });
    }


    // Fields.
    FieldInfo? constraintField;
    CreateTextLayoutInternalDelegate? createTextLayoutInternalDelegate;
    GetTextDelegate? getTextDelegate;


    /// <inheritdoc/>
    // Please refer to https://github.com/AvaloniaUI/Avalonia/blob/release/11.0.0-preview4/src/Avalonia.Controls/Presenters/TextPresenter.cs
    protected override TextLayout CreateTextLayout()
    {
        int CoerceCaretIndex(int value)
        {
            var text = Text;
            var length = text?.Length ?? 0;
            return Math.Max(0, Math.Min(length, value));
        }

        TextLayout result;

        var text = GetText();

        var typeface = new Typeface(FontFamily, FontStyle, FontWeight);

        var selectionStart = CoerceCaretIndex(SelectionStart);
        var selectionEnd = CoerceCaretIndex(SelectionEnd);
        var start = Math.Min(selectionStart, selectionEnd);
        var length = Math.Max(selectionStart, selectionEnd) - start;

        IReadOnlyList<ValueSpan<TextRunProperties>>? textStyleOverrides = null;

        var foreground = Foreground;
        var preeditText = this.PreeditText;

        if (!string.IsNullOrEmpty(preeditText))
        {
            var preeditHighlight = new ValueSpan<TextRunProperties>(this.CaretIndex, preeditText.Length,
                    new GenericTextRunProperties(typeface, FontSize,
                    foregroundBrush: this.PreeditTextForegroundBrush,
                    textDecorations: TextDecorations.Underline));

            textStyleOverrides = new[]
            {
                preeditHighlight
            };
        }
        else
        {
            if (length > 0 && SelectionForegroundBrush != null)
            {
                textStyleOverrides = new[]
                {
                    new ValueSpan<TextRunProperties>(start, length,
                    new GenericTextRunProperties(typeface, FontSize,
                        foregroundBrush: SelectionForegroundBrush))
                };
            }
        }

        if (PasswordChar != default(char) && !RevealPassword)
        {
            result = CreateTextLayoutInternal(this.Constraint, new string(PasswordChar, text?.Length ?? 0), typeface,
                textStyleOverrides);
        }
        else
        {
            result = CreateTextLayoutInternal(this.Constraint, text, typeface, textStyleOverrides);
        }

        return result;
    }


    // Get _constraint.
    Size Constraint
    {
        get
        {
            this.constraintField ??= typeof(Avalonia.Controls.Presenters.TextPresenter).GetField("_constraint", BindingFlags.Instance | BindingFlags.NonPublic).AsNonNull();
            return (Size)this.constraintField.GetValue(this)!;
        }
    }


    // Invoke CreateTextLayoutInternal().
    TextLayout CreateTextLayoutInternal(Size constraint, string? text, Typeface typeface, IReadOnlyList<ValueSpan<TextRunProperties>>? textStyleOverrides)
    {
        if (this.createTextLayoutInternalDelegate != null)
            return this.createTextLayoutInternalDelegate(constraint, text, typeface, textStyleOverrides);
        var methodInfo = typeof(Avalonia.Controls.Presenters.TextPresenter).GetMethod(nameof(CreateTextLayoutInternal), BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { typeof(Size), typeof(string), typeof(Typeface), typeof(IReadOnlyList<ValueSpan<TextRunProperties>>) });
        this.createTextLayoutInternalDelegate = methodInfo!.CreateDelegate<CreateTextLayoutInternalDelegate>(this);
        return this.createTextLayoutInternalDelegate(constraint, text, typeface, textStyleOverrides);
    }


    // Invoke GetText().
    string? GetText()
    {
        if (this.getTextDelegate != null)
            return this.getTextDelegate();
        var methodInfo = typeof(Avalonia.Controls.Presenters.TextPresenter).GetMethod(nameof(GetText), BindingFlags.Instance | BindingFlags.NonPublic);
        this.getTextDelegate = methodInfo!.CreateDelegate<GetTextDelegate>(this);
        return this.getTextDelegate();
    }


    /// <summary>
    /// Get or set brush for foreground of preedit text.
    /// </summary>
    public IBrush? PreeditTextForegroundBrush
    {
        get => this.GetValue(PreeditTextForegroundBrushProperty);
        set => this.SetValue(PreeditTextForegroundBrushProperty, value);
    }
}