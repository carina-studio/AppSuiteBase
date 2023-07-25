using Avalonia;
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
    bool isArranging;
    bool isCreatingTextLayout;
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
        var correctCaretIndexAction = new ScheduledAction(() =>
        {
            if (this.SelectionStart != this.SelectionEnd)
                return;
            if (this.CaretIndex != this.SelectionStart)
                this.CaretIndex = this.SelectionStart;
        });
        var invalidateVisualAction = new ScheduledAction(this.InvalidateVisual);

        // attach to self members
        this.GetObservable(PreeditTextProperty).Subscribe(text =>
            this.syntaxHighlighter.PreeditText = text);
        this.GetObservable(SelectionEndProperty).Subscribe(end =>
        {
            System.Diagnostics.Debug.WriteLine("SelectionEnd: " + end);
            this.syntaxHighlighter.SelectionEnd = end;
            correctCaretIndexAction.Schedule();
        });
        this.GetObservable(SelectionForegroundBrushProperty).Subscribe(brush =>
            this.syntaxHighlighter.SelectionForeground = brush);
        this.GetObservable(SelectionStartProperty).Subscribe(start =>
        {
            this.syntaxHighlighter.SelectionStart = start;
            correctCaretIndexAction.Schedule();
        });
        this.GetObservable(TextProperty).Subscribe(text =>
            this.syntaxHighlighter.Text = text);
        
        // attach to syntax highlighter
        this.syntaxHighlighter.PropertyChanged += (_, e) =>
        {
            var property = e.Property;
            if (property == SyntaxHighlighter.DefinitionSetProperty)
                this.RaisePropertyChanged(DefinitionSetProperty, (SyntaxHighlightingDefinitionSet?)e.OldValue, (SyntaxHighlightingDefinitionSet?)e.NewValue);
        };
        this.syntaxHighlighter.TextLayoutInvalidated += (_, _) =>
        {
            if (!this.isArranging && !this.isCreatingTextLayout && !this.isMeasuring)
                this.InvalidateTextLayout();
            invalidateVisualAction.Schedule();
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
        var syntaxHighlighter = this.syntaxHighlighter;
        var definitionSet = this.syntaxHighlighter.DefinitionSet;
        if (definitionSet == null || !definitionSet.HasValidDefinitions)
            return base.CreateTextLayout();
        this.isCreatingTextLayout = true;
        try
        {
            syntaxHighlighter.FlowDirection = this.FlowDirection;
            syntaxHighlighter.FontFamily = this.FontFamily;
            syntaxHighlighter.FontSize = this.FontSize;
            syntaxHighlighter.FontStretch = this.FontStretch;
            syntaxHighlighter.FontStyle = this.FontStyle;
            syntaxHighlighter.FontWeight = this.FontWeight;
            syntaxHighlighter.Foreground = this.Foreground;
            syntaxHighlighter.LetterSpacing = this.LetterSpacing;
            syntaxHighlighter.LineHeight = this.LineHeight;
            syntaxHighlighter.TextAlignment = this.TextAlignment;
            syntaxHighlighter.TextWrapping = this.TextWrapping;
            return syntaxHighlighter.CreateTextLayout();
        }
        finally
        {
            this.isCreatingTextLayout = false;
        }
    }


    /// <summary>
    /// Get or set syntax highlighting definition set.
    /// </summary>
    public SyntaxHighlightingDefinitionSet? DefinitionSet
    {
        get => this.syntaxHighlighter.DefinitionSet;
        set => this.syntaxHighlighter.DefinitionSet = value;
    }


    /// <summary>
    /// Find corresponding span and token which contains the character at specific position.
    /// </summary>
    /// <param name="characterIndex">Index of character.</param>
    /// <param name="span">Span which contains the character.</param>
    /// <param name="token">Token which contains the character.</param>
    public void FindSpanAndToken(int characterIndex, out SyntaxHighlightingSpan? span, out SyntaxHighlightingToken? token) =>
        this.syntaxHighlighter.FindSpanAndToken(characterIndex, out span, out token);


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


    /// <summary>
    /// Get <see cref="SyntaxHighlighter"/> used by the control.
    /// </summary>
    protected SyntaxHighlighter SyntaxHighlighter => this.syntaxHighlighter;
}