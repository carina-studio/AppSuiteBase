using Avalonia;
using Avalonia.Media;
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
    /// <summary>
    /// Property of <see cref="IsMaxTokenCountReached"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlightingTextPresenter, bool> IsMaxTokenCountReachedProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlightingTextPresenter, bool>(nameof(IsMaxTokenCountReached), t => t.syntaxHighlighter.IsMaxTokenCountReached);
    /// <summary>
    /// Property of <see cref="MaxTokenCount"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlightingTextPresenter, int> MaxTokenCountProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlightingTextPresenter, int>(nameof(MaxTokenCount), t => t.syntaxHighlighter.MaxTokenCount, (t, count) => t.syntaxHighlighter.MaxTokenCount = count);


    // Fields.
    readonly ScheduledAction correctCaretIndexAction;
    bool isArranging;
    bool isCreatingTextLayout;
    bool isMeasuring;
    readonly SyntaxHighlighter syntaxHighlighter = new()
    {
        TextTrimming = TextTrimming.None,
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
        var invalidateVisualAction = new ScheduledAction(this.InvalidateVisual);
        
        // attach to syntax highlighter
        this.syntaxHighlighter.PropertyChanged += (_, e) =>
        {
            var property = e.Property;
            if (property == SyntaxHighlighter.DefinitionSetProperty)
                this.RaisePropertyChanged(DefinitionSetProperty, (SyntaxHighlightingDefinitionSet?)e.OldValue, (SyntaxHighlightingDefinitionSet?)e.NewValue);
            else if (property == SyntaxHighlighter.IsMaxTokenCountReachedProperty)
                this.RaisePropertyChanged(IsMaxTokenCountReachedProperty, (bool)e.OldValue!, (bool)e.NewValue!);
            else if (property == SyntaxHighlighter.MaxTokenCountProperty)
                this.RaisePropertyChanged(MaxTokenCountProperty, (int)e.OldValue!, (int)e.NewValue!);
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
        if (definitionSet is null || !definitionSet.HasValidDefinitions)
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


    /**
     * Check whether maximum number of token to be highlighted reached or not.
     */
    public bool IsMaxTokenCountReached => this.syntaxHighlighter.IsMaxTokenCountReached;


    /// <summary>
    /// Get or set maximum number of token should be highlighted. Negative value if there is no limitation.
    /// </summary>
    public int MaxTokenCount
    {
        get => this.syntaxHighlighter.MaxTokenCount;
        set => this.syntaxHighlighter.MaxTokenCount = value;
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


    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var property = change.Property;
        if (property == PreeditTextProperty)
            this.syntaxHighlighter.PreeditText = change.NewValue as string;
        else if (property == SelectionEndProperty)
        {
            this.syntaxHighlighter.SelectionEnd = (int)change.NewValue!;
            this.correctCaretIndexAction.Schedule();
        }
        else if (property == SelectionForegroundBrushProperty)
            this.syntaxHighlighter.SelectionForeground = change.NewValue as IBrush;
        else if (property == SelectionStartProperty)
        {
            this.syntaxHighlighter.SelectionStart = (int)change.NewValue!;
            this.correctCaretIndexAction.Schedule();
        }
        else if (property == TextProperty)
            this.syntaxHighlighter.Text = change.NewValue as string;
    }


    /// <summary>
    /// Get <see cref="SyntaxHighlighter"/> used by the control.
    /// </summary>
    protected SyntaxHighlighter SyntaxHighlighter => this.syntaxHighlighter;
}