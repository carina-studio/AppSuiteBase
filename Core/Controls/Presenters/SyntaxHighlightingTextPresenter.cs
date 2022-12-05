using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media.TextFormatting;
using CarinaStudio.AppSuite.Controls.Highlighting;
using CarinaStudio.Threading;

namespace CarinaStudio.AppSuite.Controls.Presenters;

/// <summary>
/// <see cref="Avalonia.Controls.Presenters.TextPresenter"/> which supports syntax highlighting.
/// </summary>
public class SyntaxHighlightingTextPresenter : Avalonia.Controls.Presenters.TextPresenter
{
    /// <summary>
    /// Property of <see cref="DefinitionSet"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlightingTextPresenter, SyntaxHighlightingDefinitionSet?> DefinitionSetProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlightingTextPresenter, SyntaxHighlightingDefinitionSet?>(nameof(DefinitionSet), t => t.syntaxHighlighter.DefinitionSet, (t, ds) => t.syntaxHighlighter.DefinitionSet = ds);


    // Fields.
    readonly ScheduledAction correctCaretIndexAction;
    bool isArranging;
    bool isMeasuring;
    readonly SyntaxHighlighter syntaxHighlighter = new()
    {
        TextTrimming = Avalonia.Media.TextTrimming.None,
    };


    /// <summary>
    /// Initialize new <see cref="SyntaxHighlightingTextPresenter"/> instance.
    /// </summary>
    public SyntaxHighlightingTextPresenter()
    {
        // setup actions
        this.correctCaretIndexAction = new(() =>
        {
            if (this.SelectionStart != this.SelectionEnd)
                return;
            if (this.CaretIndex != this.SelectionStart)
                this.CaretIndex = this.SelectionStart;
        });

        // attach to self members
        this.GetObservable(BackgroundProperty).Subscribe(brush =>
            this.syntaxHighlighter.Background = brush);
        this.GetObservable(FlowDirectionProperty).Subscribe(direction =>
            this.syntaxHighlighter.FlowDirection = direction);
        this.GetObservable(TextElement.FontFamilyProperty).Subscribe(fontFamily =>
            this.syntaxHighlighter.FontFamily = fontFamily);
        this.GetObservable(TextElement.FontSizeProperty).Subscribe(fontSize =>
            this.syntaxHighlighter.FontSize = fontSize);
        this.GetObservable(TextElement.FontStretchProperty).Subscribe(stretch =>
            this.syntaxHighlighter.FontStretch = stretch);
        this.GetObservable(TextElement.FontStyleProperty).Subscribe(fontStyle =>
            this.syntaxHighlighter.FontStyle = fontStyle);
        this.GetObservable(TextElement.FontWeightProperty).Subscribe(fontWeight =>
            this.syntaxHighlighter.FontWeight = fontWeight);
        this.GetObservable(TextElement.ForegroundProperty).Subscribe(brush =>
            this.syntaxHighlighter.Foreground = brush);
        this.GetObservable(LetterSpacingProperty).Subscribe(spacing =>
            this.syntaxHighlighter.LetterSpacing = spacing);
        this.GetObservable(LineHeightProperty).Subscribe(height =>
            this.syntaxHighlighter.LineHeight = height);
        this.GetObservable(PreeditTextProperty).Subscribe(text =>
            this.syntaxHighlighter.PreeditText = text);
        this.GetObservable(SelectionEndProperty).Subscribe(end =>
        {
            this.syntaxHighlighter.SelectionEnd = end;
            this.correctCaretIndexAction.Schedule();
        });
        this.GetObservable(SelectionForegroundBrushProperty).Subscribe(brush =>
            this.syntaxHighlighter.SelectionForeground = brush);
        this.GetObservable(SelectionStartProperty).Subscribe(start =>
        {
            this.syntaxHighlighter.SelectionStart = start;
            this.correctCaretIndexAction.Schedule();
        });
        this.GetObservable(TextProperty).Subscribe(text =>
            this.syntaxHighlighter.Text = text);
        this.GetObservable(TextAlignmentProperty).Subscribe(alignment =>
            this.syntaxHighlighter.TextAlignment = alignment);
        this.GetObservable(TextWrappingProperty).Subscribe(wrapping =>
            this.syntaxHighlighter.TextWrapping = wrapping);
        
        // attach to syntax highlighter
        this.syntaxHighlighter.PropertyChanged += (_, e) =>
        {
            var property = e.Property;
            if (property == SyntaxHighlighter.DefinitionSetProperty)
                this.RaisePropertyChanged(DefinitionSetProperty, new((SyntaxHighlightingDefinitionSet?)e.OldValue), new((SyntaxHighlightingDefinitionSet?)e.NewValue));
        };
        this.syntaxHighlighter.TextLayoutInvalidated += (_, e) =>
        {
            if (!this.isArranging && !this.isMeasuring)
                this.InvalidateTextLayout();
            if (!this.IsFocused)
                this.InvalidateVisual();
        };
    }


    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size availableSize)
    {
        this.isArranging = true;
        try
        {
            this.syntaxHighlighter.MaxWidth = availableSize.Width;
            this.syntaxHighlighter.MaxHeight = availableSize.Height;
            return base.ArrangeOverride(availableSize);
        }
        finally
        {
            this.isArranging = false;
        }
    }


    /// <inheritdoc/>
    protected override TextLayout CreateTextLayout()
    {
        if (this.syntaxHighlighter.DefinitionSet != null)
            return this.syntaxHighlighter.CreateTextLayout();
        return base.CreateTextLayout();
    }


    /// <summary>
    /// Get or set syntax highlighting definition set.
    /// </summary>
    public SyntaxHighlightingDefinitionSet? DefinitionSet
    {
        get => this.syntaxHighlighter.DefinitionSet;
        set => this.syntaxHighlighter.DefinitionSet = value;
    }


    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        this.isMeasuring = true;
        try
        {
            this.syntaxHighlighter.MaxWidth = availableSize.Width;
            this.syntaxHighlighter.MaxHeight = availableSize.Height;
            return base.MeasureOverride(availableSize);
        }
        finally
        {
            this.isMeasuring = false;
        }
    }
}