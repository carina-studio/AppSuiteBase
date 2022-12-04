using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Styling;
using CarinaStudio.AppSuite.Controls.Highlighting;
using CarinaStudio.Collections;
using System;
using System.Collections.Specialized;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="CarinaStudio.Controls.SelectableTextBlock"/> which supports syntax highlighting.
/// </summary>
public class SelectableSyntaxHighlightingTextBlock : CarinaStudio.Controls.SelectableTextBlock, IStyleable
{
    /// <summary>
    /// Property of <see cref="DefinitionSet"/>.
    /// </summary>
    public static readonly DirectProperty<SelectableSyntaxHighlightingTextBlock, SyntaxHighlightingDefinitionSet?> DefinitionSetProperty = AvaloniaProperty.RegisterDirect<SelectableSyntaxHighlightingTextBlock, SyntaxHighlightingDefinitionSet?>(nameof(DefinitionSet), t => t.syntaxHighlighter.DefinitionSet, (t, ds) => t.syntaxHighlighter.DefinitionSet = ds);
    /// <summary>
    /// Property of <see cref="SelectionForegroundBrush"/>.
    /// </summary>
    public static readonly DirectProperty<SelectableSyntaxHighlightingTextBlock, IBrush?> SelectionForegroundBrushProperty = AvaloniaProperty.RegisterDirect<SelectableSyntaxHighlightingTextBlock, IBrush?>(nameof(SelectionForegroundBrush), t => t.syntaxHighlighter.SelectionForeground, (t, b) => t.syntaxHighlighter.SelectionForeground = b);


    // Fields.
    InlineCollection? attachedInlines;
    readonly SyntaxHighlighter syntaxHighlighter = new();


    /// <summary>
    /// Initialize new <see cref="SelectableSyntaxHighlightingTextBlock"/> instance.
    /// </summary>
    public SelectableSyntaxHighlightingTextBlock()
    {
        // attach to self members
        this.GetObservable(BackgroundProperty).Subscribe(brush =>
            this.syntaxHighlighter.Background = brush);
        this.GetObservable(FlowDirectionProperty).Subscribe(direction =>
            this.syntaxHighlighter.FlowDirection = direction);
        this.GetObservable(FontFamilyProperty).Subscribe(fontFamily =>
            this.syntaxHighlighter.FontFamily = fontFamily);
        this.GetObservable(FontSizeProperty).Subscribe(fontSize =>
            this.syntaxHighlighter.FontSize = fontSize);
        this.GetObservable(FontStretchProperty).Subscribe(stretch =>
            this.syntaxHighlighter.FontStretch = stretch);
        this.GetObservable(FontStyleProperty).Subscribe(fontStyle =>
            this.syntaxHighlighter.FontStyle = fontStyle);
        this.GetObservable(FontWeightProperty).Subscribe(fontWeight =>
            this.syntaxHighlighter.FontWeight = fontWeight);
        this.GetObservable(ForegroundProperty).Subscribe(brush =>
            this.syntaxHighlighter.Foreground = brush);
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
        this.GetObservable(LetterSpacingProperty).Subscribe(spacing =>
            this.syntaxHighlighter.LetterSpacing = spacing);
        this.GetObservable(LineHeightProperty).Subscribe(height =>
            this.syntaxHighlighter.LineHeight = height);
        this.GetObservable(MaxLinesProperty).Subscribe(maxLines =>
            this.syntaxHighlighter.MaxLines = maxLines);
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
        this.GetObservable(TextAlignmentProperty).Subscribe(alignment =>
            this.syntaxHighlighter.TextAlignment = alignment);
        this.GetObservable(TextDecorationsProperty).Subscribe(decorations =>
            this.syntaxHighlighter.TextDecorations = decorations);
        this.GetObservable(TextTrimmingProperty).Subscribe(trimming =>
            this.syntaxHighlighter.TextTrimming = trimming);
        this.GetObservable(TextWrappingProperty).Subscribe(wrapping =>
            this.syntaxHighlighter.TextWrapping = wrapping);
        
        // attach to syntax highlighter
        this.syntaxHighlighter.PropertyChanged += (_, e) =>
        {
            var property = e.Property;
            if (property == SyntaxHighlighter.DefinitionSetProperty)
                this.RaisePropertyChanged(DefinitionSetProperty, new((SyntaxHighlightingDefinitionSet?)e.OldValue), new((SyntaxHighlightingDefinitionSet?)e.NewValue));
            else if (property == SyntaxHighlighter.SelectionForegroundProperty)
                this.RaisePropertyChanged(SelectionForegroundBrushProperty, new((IBrush?)e.OldValue), new((IBrush?)e.NewValue));
        };
        this.syntaxHighlighter.TextLayoutInvalidated += (_, e) =>
            this.InvalidateTextLayout();
    }


    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size availableSize)
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


    /// <inheritdoc/>
    protected override TextLayout CreateTextLayout(string? text)
    {
        if (string.IsNullOrEmpty(text) || this.syntaxHighlighter.DefinitionSet == null)
            return base.CreateTextLayout(text);
        return this.syntaxHighlighter.CreateTextLayout();
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
        var scale = LayoutHelper.GetLayoutScale(this);
        var padding = LayoutHelper.RoundLayoutThickness(this.Padding, scale, scale);
        var availableTextBounds = availableSize.Deflate(padding);
        this.syntaxHighlighter.MaxWidth = availableTextBounds.Width;
        this.syntaxHighlighter.MaxHeight = availableTextBounds.Height;
        this._textLayout = null;
        return this.TextLayout.Bounds.Inflate(padding).Size;
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


    /// <summary>
    /// Get or set brush of foreground of selected text.
    /// </summary>
    public IBrush? SelectionForegroundBrush
    {
        get => this.syntaxHighlighter.SelectionForeground;
        set => this.syntaxHighlighter.SelectionForeground = value;
    }


    /// <inheritdoc/>
    Type IStyleable.StyleKey => typeof(SelectableSyntaxHighlightingTextBlock);
}