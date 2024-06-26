using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using CarinaStudio.AppSuite.Controls.Highlighting;
using CarinaStudio.Collections;
using CarinaStudio.Threading;
using System;
using System.Collections.Specialized;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="CarinaStudio.Controls.SelectableTextBlock"/> which supports syntax highlighting.
/// </summary>
public class SelectableSyntaxHighlightingTextBlock : CarinaStudio.Controls.SelectableTextBlock
{
    /// <summary>
    /// Property of <see cref="DefinitionSet"/>.
    /// </summary>
    public static readonly DirectProperty<SelectableSyntaxHighlightingTextBlock, SyntaxHighlightingDefinitionSet?> DefinitionSetProperty = AvaloniaProperty.RegisterDirect<SelectableSyntaxHighlightingTextBlock, SyntaxHighlightingDefinitionSet?>(nameof(DefinitionSet), t => t.syntaxHighlighter.DefinitionSet, (t, ds) => t.syntaxHighlighter.DefinitionSet = ds);
    

    // Fields.
    InlineCollection? attachedInlines;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    readonly ScheduledAction invalidateVisualAction;
    bool isArranging;
    bool isCreatingTextLayout;
    bool isMeasuring;
    readonly SyntaxHighlighter syntaxHighlighter = new();


    /// <summary>
    /// Initialize new <see cref="SelectableSyntaxHighlightingTextBlock"/> instance.
    /// </summary>
    public SelectableSyntaxHighlightingTextBlock()
    {
        // create actions
        this.invalidateVisualAction = new(this.InvalidateVisual);

        // attach to self members
        this.GetObservable(InlinesProperty).Subscribe(inlines =>
        {
            if (this.attachedInlines != null)
                this.attachedInlines.CollectionChanged -= this.OnInlinesChanged;
            this.attachedInlines = inlines;
            if (inlines != null)
            {
                inlines.CollectionChanged += this.OnInlinesChanged;
                if (inlines.IsNotEmpty())
                {
                    inlines.Clear();
                    throw new InvalidOperationException();
                }
            }
        });
        this.GetObservable(SelectionBrushProperty).Subscribe(brush =>
        {
            this.syntaxHighlighter.SelectionBackground = brush;
        });
        this.GetObservable(SelectionForegroundBrushProperty).Subscribe(brush =>
        {
            this.syntaxHighlighter.SelectionForeground = brush;
        });
        this.GetObservable(SelectionEndProperty).Subscribe(end =>
        {
            this.syntaxHighlighter.SelectionEnd = end;
            this.InvalidateTextLayout();
        });
        this.GetObservable(SelectionStartProperty).Subscribe(start =>
        {
            this.syntaxHighlighter.SelectionStart = start;
            this.InvalidateTextLayout();
        });
        this.GetObservable(TextProperty).Subscribe(text =>
            this.syntaxHighlighter.Text = text);
        
        // attach to syntax highlighter
        this.syntaxHighlighter.PropertyChanged += (_, e) =>
        {
            var property = e.Property;
            if (property == SyntaxHighlighter.DefinitionSetProperty)
                this.RaisePropertyChanged(DefinitionSetProperty, (SyntaxHighlightingDefinitionSet?)e.OldValue, (SyntaxHighlightingDefinitionSet?)e.NewValue);
            else if (property == SyntaxHighlighter.SelectionBackgroundProperty)
                this.SetValue(SelectionBrushProperty, (IBrush?)e.NewValue);
            else if (property == SyntaxHighlighter.SelectionForegroundProperty)
                this.SetValue(SelectionForegroundBrushProperty, (IBrush?)e.NewValue);
        };
        this.syntaxHighlighter.TextLayoutInvalidated += (_, _) =>
        {
            if (!this.isArranging && !this.isCreatingTextLayout && !this.isMeasuring)
                this.InvalidateTextLayout();
            this.invalidateVisualAction.Schedule();
        };
    }


    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size availableSize)
    {
        this.isArranging = true;
        try
        {
            var padding = this.Padding;
            this.syntaxHighlighter.MaxWidth = double.IsInfinity(availableSize.Width)
                ? double.PositiveInfinity
                : Math.Max(0, availableSize.Width - padding.Left - padding.Right);
            this.syntaxHighlighter.MaxHeight = double.IsInfinity(availableSize.Height)
                ? double.PositiveInfinity
                : Math.Max(0, availableSize.Height - padding.Top - padding.Bottom);
            return base.ArrangeOverride(availableSize);
        }
        finally
        {
            this.isArranging = false;
        }
    }


    /// <inheritdoc/>
    protected override TextLayout CreateTextLayout(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return base.CreateTextLayout(text);
        var syntaxHighlighter = this.syntaxHighlighter;
        var definitionSet = syntaxHighlighter.DefinitionSet;
        if (this.SelectionStart == this.SelectionEnd)
        {
            if (definitionSet == null || !definitionSet.HasValidDefinitions)
                return base.CreateTextLayout(text);
        }
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
            syntaxHighlighter.MaxLines = this.MaxLines;
            syntaxHighlighter.TextAlignment = this.TextAlignment;
            syntaxHighlighter.TextDecorations = this.TextDecorations;
            syntaxHighlighter.TextTrimming = this.TextTrimming;
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


    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        this.isMeasuring = true;
        try
        {
            var scale = LayoutHelper.GetLayoutScale(this);
            var padding = LayoutHelper.RoundLayoutThickness(this.Padding, scale, scale);
            var availableTextBounds = availableSize.Deflate(padding);
            this.syntaxHighlighter.MaxWidth = availableTextBounds.Width;
            this.syntaxHighlighter.MaxHeight = availableTextBounds.Height;
            return base.MeasureOverride(availableSize);
        }
        finally
        {
            this.isMeasuring = false;
        }
    }


    // Called when collection of inlines changed.
    void OnInlinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (this.Inlines?.IsNotEmpty() == true)
        {
            this.Inlines.Clear();
            throw new InvalidOperationException();
        }
    }
    
    
    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(SelectableSyntaxHighlightingTextBlock);


    /// <summary>
    /// Get <see cref="SyntaxHighlighter"/> used by the control.
    /// </summary>
    protected SyntaxHighlighter SyntaxHighlighter => this.syntaxHighlighter;
}