using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using CarinaStudio.Collections;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CarinaStudio.AppSuite.Controls.Highlighting;

/// <summary>
/// Syntax highlighter.
/// </summary>
public sealed class SyntaxHighlighter : AvaloniaObject
{
    /// <summary>
    /// Property of <see cref="Background"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, IBrush?> BackgroundProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, IBrush?>(nameof(Background), sh => sh.background, (sh, b) => sh.Background = b);
    /// <summary>
    /// Property of <see cref="FlowDirection"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, FlowDirection> FlowDirectionProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, FlowDirection>(nameof(FlowDirection), sh => sh.flowDirection, (sh, d) => sh.FlowDirection = d);
    /// <summary>
    /// Property of <see cref="FontFamily"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, FontFamily> FontFamilyProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, FontFamily>(nameof(FontFamily), sh => sh.fontFamily, (sh, f) => sh.FontFamily = f);
    /// <summary>
    /// Property of <see cref="FontStretch"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, FontStretch> FontStretchProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, FontStretch>(nameof(FontStretch), sh => sh.fontStretch, (sh, s) => sh.FontStretch = s);
    /// <summary>
    /// Property of <see cref="FontSize"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, double> FontSizeProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, double>(nameof(FontSize), sh => sh.fontSize, (sh, s) => sh.FontSize = s);
    /// <summary>
    /// Property of <see cref="FontStyle"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, FontStyle> FontStyleProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, FontStyle>(nameof(FontStyle), sh => sh.fontStyle, (sh, s) => sh.FontStyle = s);
    /// <summary>
    /// Property of <see cref="FontWeight"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, FontWeight> FontWeightProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, FontWeight>(nameof(FontWeight), sh => sh.fontWeight, (sh, w) => sh.FontWeight = w);
    /// <summary>
    /// Property of <see cref="Foreground"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, IBrush?> ForegroundProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, IBrush?>(nameof(Foreground), sh => sh.foreground, (sh, b) => sh.Foreground = b);
    /// <summary>
    /// Property of <see cref="DefinitionSet"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, SyntaxHighlightingDefinitionSet?> DefinitionSetProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, SyntaxHighlightingDefinitionSet?>(nameof(DefinitionSet), sh => sh.definitionSet, (sh, ds) => sh.DefinitionSet = ds);
    /// <summary>
    /// Property of <see cref="LetterSpacing"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, double> LetterSpacingProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, double>(nameof(LetterSpacing), sh => sh.letterSpacing, (sh, s) => sh.LetterSpacing = s);
    /// <summary>
    /// Property of <see cref="FlowDirection"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, double> LineHeightProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, double>(nameof(LineHeight), sh => sh.lineHeight, (sh, h) => sh.LineHeight = h);
    /// <summary>
    /// Property of <see cref="MaxHeight"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, double> MaxHeightProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, double>(nameof(MaxHeight), sh => sh.maxHeight, (sh, h) => sh.MaxHeight = h);
    /// <summary>
    /// Property of <see cref="MaxLines"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, int> MaxLinesProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, int>(nameof(MaxLines), sh => sh.maxLines, (sh, l) => sh.MaxLines = l);
    /// <summary>
    /// Property of <see cref="MaxWidth"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, double> MaxWidthProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, double>(nameof(MaxWidth), sh => sh.maxWidth, (sh, w) => sh.MaxWidth = w);
     /// <summary>
    /// Property of <see cref="PreeditText"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, string?> PreeditTextProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, string?>(nameof(PreeditText), sh => sh.preeditText, (sh, t) => sh.PreeditText = t);
    /// <summary>
    /// Property of <see cref="SelectionBackground"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, IBrush?> SelectionBackgroundProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, IBrush?>(nameof(SelectionBackground), sh => sh.selectionBackground, (sh, b) => sh.SelectionBackground = b);
    /// <summary>
    /// Property of <see cref="SelectionEnd"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, int> SelectionEndProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, int>(nameof(SelectionEnd), sh => sh.selectionEnd, (sh, i) => sh.SelectionEnd = i);
    /// <summary>
    /// Property of <see cref="SelectionForeground"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, IBrush?> SelectionForegroundProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, IBrush?>(nameof(SelectionForeground), sh => sh.selectionForeground, (sh, b) => sh.SelectionForeground = b);
    /// <summary>
    /// Property of <see cref="SelectionStart"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, int> SelectionStartProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, int>(nameof(SelectionStart), sh => sh.selectionStart, (sh, i) => sh.SelectionStart = i);
    /// <summary>
    /// Property of <see cref="Text"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, string?> TextProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, string?>(nameof(Text), sh => sh.text, (sh, t) => sh.Text = t);
    /// <summary>
    /// Property of <see cref="TextAlignment"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, TextAlignment> TextAlignmentProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, TextAlignment>(nameof(TextAlignment), sh => sh.textAlignment, (sh, a) => sh.TextAlignment = a);
    /// <summary>
    /// Property of <see cref="TextDecorations"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, TextDecorationCollection?> TextDecorationProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, TextDecorationCollection?>(nameof(TextDecorations), sh => sh.textDecorations, (sh, d) => sh.TextDecorations = d);
    /// <summary>
    /// Property of <see cref="TextTrimming"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, TextTrimming> TextTrimmingProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, TextTrimming>(nameof(TextTrimming), sh => sh.textTrimming, (sh, t) => sh.TextTrimming = t);
    /// <summary>
    /// Property of <see cref="TextWrapping"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, TextWrapping> TextWrappingProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, TextWrapping>(nameof(TextWrapping), sh => sh.textWrapping, (sh, w) => sh.TextWrapping = w);


    // Span.
    record Span(SyntaxHighlightingSpan Definition, int Start, int End, int InnerStart, int InnerEnd);


    // Token.
    record Token(SyntaxHighlightingToken Definition, int Start, int End);


    // Static fields.
    static readonly IList<SyntaxHighlightingToken> EmptyTokenDefinitions = Array.Empty<SyntaxHighlightingToken>();


    // Fields.
    IBrush? background;
    IDisposable backgroundPropertyChangedHandlerToken = EmptyDisposable.Default;
    SyntaxHighlightingDefinitionSet? definitionSet;
    FlowDirection flowDirection = FlowDirection.LeftToRight;
    FontFamily fontFamily = FontManager.Current.DefaultFontFamily;
    double fontSize = 12;
    FontStretch fontStretch = FontStretch.Normal;
    FontStyle fontStyle = FontStyle.Normal;
    FontWeight fontWeight = FontWeight.Normal;
    IBrush? foreground;
    IDisposable foregroundPropertyChangedHandlerToken = EmptyDisposable.Default;
    readonly bool isDebugMode = IAppSuiteApplication.CurrentOrNull?.IsDebugMode == true;
    double letterSpacing;
    double lineHeight = double.NaN;
    double maxHeight = double.PositiveInfinity;
    int maxLines;
    double maxWidth = double.PositiveInfinity;
    string? preeditText;
    IBrush? selectionBackground;
    IDisposable selectionBackgroundPropertyChangedHandlerToken = EmptyDisposable.Default;
    int selectionEnd;
    IBrush? selectionForeground;
    IDisposable selectionForegroundPropertyChangedHandlerToken = EmptyDisposable.Default;
    int selectionStart;
    string? text;
    TextAlignment textAlignment = TextAlignment.Left;
    TextDecorationCollection? textDecorations;
    TextLayout? textLayout;
    IReadOnlyList<ValueSpan<TextRunProperties>>? textProperties;
    TextTrimming textTrimming = TextTrimming.CharacterEllipsis;
    TextWrapping textWrapping = TextWrapping.NoWrap;


    /// <summary>
    /// Initialize new <see cref="SyntaxHighlighter"/> instance.
    /// </summary>
    public SyntaxHighlighter()
    { }


    /// <inheritdoc/>
    ~SyntaxHighlighter()
    {
        this.backgroundPropertyChangedHandlerToken.Dispose();
        this.foregroundPropertyChangedHandlerToken.Dispose();
        this.selectionBackgroundPropertyChangedHandlerToken.Dispose();
        this.selectionForegroundPropertyChangedHandlerToken.Dispose();
    }


    /// <summary>
    /// Get or set base background brush.
    /// </summary>
    public IBrush? Background
    {
        get => this.background;
        set
        {
            this.VerifyAccess();
            if (this.background == value)
                return;
            this.backgroundPropertyChangedHandlerToken.Dispose();
            (value as AvaloniaObject)?.Let(it => 
                this.backgroundPropertyChangedHandlerToken = it.AddWeakEventHandler<AvaloniaPropertyChangedEventArgs>(nameof(AvaloniaObject.PropertyChanged), this.OnBrushPropertyChanged));
            this.SetAndRaise(BackgroundProperty, ref this.background, value);
            this.InvalidateTextProperties();
        }
    }


    // Create text properties.
    IReadOnlyList<ValueSpan<TextRunProperties>> CreateTextProperties(TextRunProperties defaultRunProperties)
    {
        // check text
        var text = this.text;
        var preeditText = this.preeditText;
        if (string.IsNullOrEmpty(text))
        {
            if (string.IsNullOrEmpty(preeditText))
                return Array.Empty<ValueSpan<TextRunProperties>>();
            text = "";
        }
        
        // setup default run properties for selected text
        var defaultSelectionRunProperties = new GenericTextRunProperties(
            defaultRunProperties.Typeface,
            defaultRunProperties.FontRenderingEmSize,
            defaultRunProperties.TextDecorations,
            this.selectionForeground ?? defaultRunProperties.ForegroundBrush,
            this.selectionBackground ?? defaultRunProperties.BackgroundBrush
        );
        
        // setup initial candidate spans
        var candidateSpans = new SortedObservableList<Span>((lhs, rhs) =>
        {
            var result = (rhs.Start - lhs.Start);
            if (result != 0)
                return result;
            result = (lhs.End - rhs.End);
            if (result != 0)
                return result;
            result = this.definitionSet?.SpanDefinitions.Let(it => it.IndexOf(rhs.Definition) - it.IndexOf(lhs.Definition)) ?? 0;
            return result != 0 ? result : (rhs.GetHashCode() - lhs.GetHashCode());
        });
        this.definitionSet?.SpanDefinitions.Let(it =>
        {
            foreach (var spanDefinition in it)
            {
                if (!spanDefinition.IsValid)
                    continue;
                var startMatch = spanDefinition.StartPattern!.Match(text);
                if (!startMatch.Success || startMatch.Length == 0)
                    continue;
                var endMatch = spanDefinition.EndPattern!.Match(text, startMatch.Index + startMatch.Length);
                if (endMatch.Success && endMatch.Length > 0)
                {
                    candidateSpans.Add(new(
                        spanDefinition, 
                        startMatch.Index, 
                        endMatch.Index + endMatch.Length, 
                        startMatch.Index + startMatch.Length,
                        endMatch.Index));
                }
            }
        });
        
        // create text properties for each span
        var textProperties = new List<ValueSpan<TextRunProperties>>();
        var textStartIndex = 0;
        var runPropertiesMap = new Dictionary<SyntaxHighlightingSpan, TextRunProperties>();
        var selectionRunPropertiesMap = new Dictionary<SyntaxHighlightingSpan, TextRunProperties>();
        var defaultTokenDefinitions = this.definitionSet?.TokenDefinitions ?? Array.Empty<SyntaxHighlightingToken>();
        while (candidateSpans.IsNotEmpty())
        {
            // get current span
            var span = candidateSpans[^1];
            candidateSpans.RemoveAt(candidateSpans.Count - 1);

            // find next span
            var startMatch = span.Definition.StartPattern!.Match(text, span.End);
            Match? endMatch;
            if (startMatch.Success)
            {
                endMatch = span.Definition.EndPattern!.Match(text, startMatch.Index + startMatch.Length);
                if (endMatch.Success)
                {
                    candidateSpans.Add(new(
                        span.Definition,
                        startMatch.Index, 
                        endMatch.Index + endMatch.Length,
                        startMatch.Index + startMatch.Length,
                        endMatch.Index));
                }
            }

            // remove spans which overlaps with current span
            for (var i = candidateSpans.Count - 1; i >= 0; --i)
            {
                // check overlapping
                var removingSpan = candidateSpans[i];
                if (removingSpan.Start >= span.End)
                    continue;
                candidateSpans.RemoveAt(i);

                // find next span
                startMatch = removingSpan.Definition.StartPattern!.Match(text, span.End);
                if (!startMatch.Success)
                    continue;
                endMatch = removingSpan.Definition.EndPattern!.Match(text, startMatch.Index + startMatch.Length);
                if (endMatch.Success)
                {
                    var j = candidateSpans.Add(new(
                        removingSpan.Definition, 
                        startMatch.Index, 
                        endMatch.Index + endMatch.Length,
                        startMatch.Index + startMatch.Length,
                        endMatch.Index));
                    if (j < i)
                        ++i;
                }
            }

            // create text properties
            if (!runPropertiesMap.TryGetValue(span.Definition, out var runProperties))
            {
                var typeface = new Typeface(
                    span.Definition.FontFamily ?? defaultRunProperties.Typeface.FontFamily, 
                    span.Definition.FontStyle ?? defaultRunProperties.Typeface.Style,
                    span.Definition.FontWeight ?? defaultRunProperties.Typeface.Weight, 
                    this.fontStretch
                );
                runProperties = new GenericTextRunProperties(
                    typeface,
                    double.IsNaN(span.Definition.FontSize) ? defaultRunProperties.FontRenderingEmSize : span.Definition.FontSize,
                    span.Definition.TextDecorations ?? defaultRunProperties.TextDecorations,
                    span.Definition.Foreground ?? defaultRunProperties.ForegroundBrush,
                    span.Definition.Background ?? defaultRunProperties.BackgroundBrush
                );
                runPropertiesMap[span.Definition] = runProperties;
            }
            if (!selectionRunPropertiesMap.TryGetValue(span.Definition, out var selectionRunProperties))
            {
                selectionRunProperties = new GenericTextRunProperties(
                    runProperties.Typeface,
                    runProperties.FontRenderingEmSize,
                    runProperties.TextDecorations,
                    this.selectionForeground ?? runProperties.ForegroundBrush,
                    this.selectionBackground ?? runProperties.BackgroundBrush
                );
                selectionRunPropertiesMap[span.Definition] = selectionRunProperties;
            }
            if (textStartIndex < span.Start)
                CreateTextPropertiesInSpan(text, textStartIndex, span.Start, defaultTokenDefinitions, defaultRunProperties, defaultSelectionRunProperties, textProperties);
            CreateTextPropertiesInSpan(text, span.Start, span.InnerStart, EmptyTokenDefinitions, runProperties, selectionRunProperties, textProperties);
            CreateTextPropertiesInSpan(text, span.InnerStart, span.InnerEnd, span.Definition.TokenDefinitions, runProperties, selectionRunProperties, textProperties);
            CreateTextPropertiesInSpan(text, span.InnerEnd, span.End, EmptyTokenDefinitions, runProperties, selectionRunProperties, textProperties);
            textStartIndex = span.End;
        }
        if (textStartIndex < text.Length)
            CreateTextPropertiesInSpan(text, textStartIndex, text.Length, defaultTokenDefinitions, defaultRunProperties, defaultSelectionRunProperties, textProperties);
        
        // insert text style for preedit text
        if (!string.IsNullOrEmpty(preeditText))
        {
            var preeditTextLength = preeditText.Length;
            var caretIndex = Math.Min(this.selectionStart, this.selectionEnd);
            var runProperties = new GenericTextRunProperties(
                defaultRunProperties.Typeface,
                defaultRunProperties.FontRenderingEmSize,
                Avalonia.Media.TextDecorations.Underline,
                defaultRunProperties.ForegroundBrush
            );
            if (caretIndex <= 0)
            {
                textProperties.Add(new(0, preeditTextLength, runProperties));
                for (var i = textProperties.Count - 1; i > 0; --i)
                {
                    var properties = textProperties[i];
                    textProperties[i] = new(properties.Start + preeditTextLength, properties.Length, properties.Value);
                }
            }
            else if (caretIndex >= text.Length)
                textProperties.Add(new(text.Length, preeditTextLength, runProperties));
            else
            {
                var indexOfTextPropertiesToInsert = textProperties.Count - 1;
                var textPropertiesToInsert = textProperties[indexOfTextPropertiesToInsert];
                if (textPropertiesToInsert.Start > caretIndex)
                {
                    for (var i = textProperties.Count - 2; i >= 0; --i)
                    {
                        var properties = textProperties[i];
                        if (properties.Start <= caretIndex)
                        {
                            indexOfTextPropertiesToInsert = i;
                            textPropertiesToInsert = properties;
                            break;
                        }
                    }
                }
                if (textPropertiesToInsert.Start == caretIndex)
                    textProperties.Insert(indexOfTextPropertiesToInsert, new(caretIndex, preeditTextLength, runProperties));
                else
                {
                    textProperties[indexOfTextPropertiesToInsert++] = new(textPropertiesToInsert.Start, caretIndex - textPropertiesToInsert.Start, textPropertiesToInsert.Value);
                    textProperties.Insert(indexOfTextPropertiesToInsert++, new(caretIndex, preeditTextLength, runProperties));
                    textProperties.Insert(indexOfTextPropertiesToInsert, new(caretIndex + preeditTextLength, textPropertiesToInsert.Length - (caretIndex - textPropertiesToInsert.Start), textPropertiesToInsert.Value));
                }
                for (var i = textProperties.Count - 1; i > indexOfTextPropertiesToInsert; --i)
                {
                    var properties = textProperties[i];
                    textProperties[i] = new(properties.Start + preeditTextLength, properties.Length, properties.Value);
                }
            }
        }

        // complete
        return textProperties;
    }


    // Create text properties for given range of text.
    void CreateTextProperties(int start, int end, TextRunProperties runProperties, TextRunProperties selectionRunProperties, IList<ValueSpan<TextRunProperties>> textProperties)
    {
        textProperties.Add(new(start, end - start, runProperties));
        /*
        var selectionStart = this.selectionStart;
        var selectionEnd = this.selectionEnd;
        if (selectionEnd < selectionStart)
            (selectionStart, selectionEnd) = (selectionEnd, selectionStart);
        if (selectionStart == selectionEnd || start >= selectionEnd || end <= selectionStart)
            textProperties.Add(new(start, end - start, runProperties));
        else if (start < selectionStart)
        {
            textProperties.Add(new(start, selectionStart - start, runProperties));
            if (end <= selectionEnd)
                textProperties.Add(new(selectionStart, end - selectionStart, selectionRunProperties));
            else
            {
                textProperties.Add(new(selectionStart, selectionEnd - selectionStart, selectionRunProperties));
                textProperties.Add(new(selectionEnd, end - selectionEnd, runProperties));
            }
        }
        else
        {
            if (end <= selectionEnd)
                textProperties.Add(new(start, end - start, selectionRunProperties));
            else
            {
                textProperties.Add(new(start, selectionEnd - start, selectionRunProperties));
                textProperties.Add(new(selectionEnd, end - selectionEnd, runProperties));
            }
        }
        */
    }


    // Create text properties for a span.
    void CreateTextPropertiesInSpan(string text, int start, int end, IList<SyntaxHighlightingToken> tokenDefinitions, TextRunProperties defaultRunProperties, TextRunProperties defaultSelectionRunProperties, IList<ValueSpan<TextRunProperties>> textProperties)
    {
        // setup initial candidate tokens
        var tokenComparison = new Comparison<Token>((lhs, rhs) =>
        {
            var result = (rhs.Start - lhs.Start);
            if (result != 0)
                return result;
            result = (lhs.End - rhs.End);
            if (result != 0)
                return result;
            result = tokenDefinitions.IndexOf(rhs.Definition) - tokenDefinitions.IndexOf(lhs.Definition);
            return result != 0 ? result : (rhs.GetHashCode() - lhs.GetHashCode());
        });
        var candidateTokens = new SortedObservableList<Token>(tokenComparison);
        foreach (var tokenDefinition in tokenDefinitions)
        {
            if (!tokenDefinition.IsValid)
                continue;
            var match = tokenDefinition.Pattern!.Match(text, start);
            if (match.Success && match.Length > 0)
            {
                var endIndex = match.Index + match.Length;
                if (endIndex <= end)
                    candidateTokens.Add(new(tokenDefinition, match.Index, endIndex));
            }
        }

        // create text runs
        var textStartIndex = start;
        var runPropertiesMap = new Dictionary<SyntaxHighlightingToken, TextRunProperties>();
        var selectionRunPropertiesMap = new Dictionary<SyntaxHighlightingToken, TextRunProperties>();
        while (candidateTokens.IsNotEmpty())
        {
            // get current token
            var token = candidateTokens[^1];
            candidateTokens.RemoveAt(candidateTokens.Count - 1);

            // find and combine with next token if possible
            while (true)
            {
                var match = token.Definition.Pattern!.Match(text, token.End);
                if (!match.Success || match.Length <= 0)
                    break;
                var endIndex = match.Index + match.Length;
                if (endIndex > end)
                    break;
                var nextToken = new Token(token.Definition, match.Index, match.Index + match.Length);
                if (match.Index == token.End && match.Length > 0) // combine into single token
                {
                    var nextTokenIndex = candidateTokens.BinarySearch(nextToken, tokenComparison);
                    if (nextTokenIndex == ~candidateTokens.Count)
                    {
                        token = new(token.Definition, token.Start, nextToken.End);
                        continue;
                    }
                }
                candidateTokens.Add(nextToken);
                break;
            }

            // remove tokens which overlaps with current token
            for (var i = candidateTokens.Count - 1; i >= 0; --i)
            {
                // check overlapping
                var removingToken = candidateTokens[i];
                if (removingToken.Start >= token.End)
                    continue;
                candidateTokens.RemoveAt(i);

                // find next token
                var match = removingToken.Definition.Pattern!.Match(text, token.End);
                if (match.Success && match.Length > 0)
                {
                    var endIndex = match.Index + match.Length;
                    if (endIndex <= end)
                    {
                        var j = candidateTokens.Add(new(removingToken.Definition, match.Index, endIndex));
                        if (j < i)
                            ++i;
                    }
                }
            }

            // create text style
            if (!runPropertiesMap.TryGetValue(token.Definition, out var runProperties))
            {
                var typeface = new Typeface(
                    token.Definition.FontFamily ?? defaultRunProperties.Typeface.FontFamily, 
                    token.Definition.FontStyle ?? defaultRunProperties.Typeface.Style,
                    token.Definition.FontWeight ?? defaultRunProperties.Typeface.Weight, 
                    this.fontStretch
                );
                runProperties = new GenericTextRunProperties(
                    typeface,
                    double.IsNaN(token.Definition.FontSize) ? defaultRunProperties.FontRenderingEmSize : token.Definition.FontSize,
                    token.Definition.TextDecorations ?? defaultRunProperties.TextDecorations,
                    token.Definition.Foreground ?? defaultRunProperties.ForegroundBrush,
                    token.Definition.Background ?? defaultRunProperties.BackgroundBrush
                );
                runPropertiesMap[token.Definition] = runProperties;
            }
            if (!selectionRunPropertiesMap.TryGetValue(token.Definition, out var selectionRunProperties))
            {
                selectionRunProperties = new GenericTextRunProperties(
                    runProperties.Typeface,
                    runProperties.FontRenderingEmSize,
                    runProperties.TextDecorations,
                    this.selectionForeground ?? defaultRunProperties.ForegroundBrush,
                    this.selectionBackground ?? defaultRunProperties.BackgroundBrush
                );
                selectionRunPropertiesMap[token.Definition] = selectionRunProperties;
            }
            if (textStartIndex < token.Start)
                CreateTextProperties(textStartIndex, token.Start, defaultRunProperties, defaultSelectionRunProperties, textProperties);
            CreateTextProperties(token.Start, token.End, runProperties, selectionRunProperties, textProperties);
            textStartIndex = token.End;
        }
        if (textStartIndex < end)
            CreateTextProperties(textStartIndex, end, defaultRunProperties, defaultSelectionRunProperties, textProperties);
    }


    /// <summary>
    /// Create text layout.
    /// </summary>
    /// <returns>Text layout.</returns>
    public TextLayout CreateTextLayout()
    {
        // use created text layout
        if (this.textLayout != null)
            return this.textLayout;
        
        // get text
        var text = this.TextWithPreeditText;
        
        // create type face
        var typeface = new Typeface(this.fontFamily, this.fontStyle, this.fontWeight, this.fontStretch);

        // prepare base run properties
        var defaultRunProperties = new GenericTextRunProperties(
            typeface,
            this.fontSize,
            this.textDecorations,
            this.foreground,
            this.background
        );
        
        // create text runs and source
        this.textProperties ??= this.CreateTextProperties(defaultRunProperties);

        // create text layout
        this.textLayout = new TextLayout(
            text, 
            typeface, 
            this.fontSize, 
            this.foreground, 
            this.textAlignment,
            this.textWrapping, 
            this.textTrimming,
            maxLines: this.maxLines,
            maxWidth: this.maxWidth, 
            maxHeight: this.maxHeight, 
            textStyleOverrides: this.textProperties,
            flowDirection: this.flowDirection, 
            lineHeight: this.lineHeight, 
            letterSpacing: this.letterSpacing
        );
        if (this.isDebugMode)
        {
            var textLines = this.textLayout.TextLines;
            for (var lineIndex = textLines.Count - 1; lineIndex >= 0; --lineIndex)
            {
                var textRuns = textLines[lineIndex].TextRuns;
                for (var runIndex = textRuns.Count - 1; runIndex >= 0; --runIndex)
                {
                    var textRun = textRuns[runIndex];
                    if (textRun is ShapedTextRun shapedTextRun && textRun.Length > 0 && shapedTextRun.ShapedBuffer.Length == 0)
                        throw new InternalStateCorruptedException($"Text run with empty shaped buffer created at line {lineIndex} run {runIndex}, text: '{new string(textRun.Text.ToArray())}'.");
                }
            }
        }
        return this.textLayout;
    }


    /// <summary>
    /// Get or set syntax highlighting definition set.
    /// </summary>
    public SyntaxHighlightingDefinitionSet? DefinitionSet
    {
        get => this.definitionSet;
        set
        {
            this.VerifyAccess();
            if (this.definitionSet == value)
                return;
            if (this.definitionSet != null)
                this.definitionSet.Changed -= this.OnDefinitionSetChanged;
            if (value != null)
                value.Changed += this.OnDefinitionSetChanged;
            this.SetAndRaise(DefinitionSetProperty, ref this.definitionSet, value);
            this.InvalidateTextLayout();
        }
    }
    
    
    /// <summary>
    /// Find corresponding span and token which contains the character at specific position.
    /// </summary>
    /// <param name="characterIndex">Index of character.</param>
    /// <param name="span">Span which contains the character.</param>
    /// <param name="token">Token which contains the character.</param>
    public void FindSpanAndToken(int characterIndex, out SyntaxHighlightingSpan? span, out SyntaxHighlightingToken? token)
    {
        // check state and parameter
        span = default;
        token = default;
        if (this.definitionSet is null || characterIndex < 0)
            return;
        var text = this.TextWithPreeditText;
        if (characterIndex >= text.Length)
            return;
        
        // setup initial candidate spans
        var spanDefinitions = this.definitionSet.SpanDefinitions;
        var candidateSpans = new SortedObservableList<Span>((lhs, rhs) =>
        {
            var result = (rhs.Start - lhs.Start);
            if (result != 0)
                return result;
            result = (lhs.End - rhs.End);
            if (result != 0)
                return result;
            result = this.definitionSet?.SpanDefinitions.Let(it => it.IndexOf(rhs.Definition) - it.IndexOf(lhs.Definition)) ?? 0;
            return result != 0 ? result : (rhs.GetHashCode() - lhs.GetHashCode());
        });
        foreach (var spanDefinition in spanDefinitions)
        {
            if (!spanDefinition.IsValid)
                continue;
            var startMatch = spanDefinition.StartPattern!.Match(text);
            if (!startMatch.Success || startMatch.Length == 0)
                continue;
            var endMatch = spanDefinition.EndPattern!.Match(text, startMatch.Index + startMatch.Length);
            if (endMatch.Success && endMatch.Length > 0)
            {
                candidateSpans.Add(new(
                    spanDefinition,
                    startMatch.Index,
                    endMatch.Index + endMatch.Length,
                    startMatch.Index + startMatch.Length,
                    endMatch.Index));
            }
        }
        
        // find span and token
        var textStartIndex = 0;
        var defaultTokenDefinitions = this.definitionSet.TokenDefinitions;
        while (candidateSpans.IsNotEmpty())
        {
            // stop finding
            if (textStartIndex > characterIndex)
            {
                span = default;
                token = default;
                return;
            }
            
            // get current span
            var candidateSpan = candidateSpans[^1];
            candidateSpans.RemoveAt(candidateSpans.Count - 1);
            
            // find token in/before the span
            if (candidateSpan.Start > characterIndex)
            {
                this.FindToken(text, textStartIndex, candidateSpan.Start, characterIndex, defaultTokenDefinitions, out token);
                return;
            }
            if (candidateSpan.End > characterIndex)
            {
                span = candidateSpan.Definition;
                this.FindToken(text, candidateSpan.Start, candidateSpan.End, characterIndex, candidateSpan.Definition.TokenDefinitions, out token);
                return;
            }

            // find next span
            var startMatch = candidateSpan.Definition.StartPattern!.Match(text, candidateSpan.End);
            Match? endMatch;
            if (startMatch.Success)
            {
                endMatch = candidateSpan.Definition.EndPattern!.Match(text, startMatch.Index + startMatch.Length);
                if (endMatch.Success)
                {
                    candidateSpans.Add(new(
                        candidateSpan.Definition,
                        startMatch.Index, 
                        endMatch.Index + endMatch.Length,
                        startMatch.Index + startMatch.Length,
                        endMatch.Index));
                }
            }

            // remove spans which overlaps with current span
            for (var i = candidateSpans.Count - 1; i >= 0; --i)
            {
                // check overlapping
                var removingSpan = candidateSpans[i];
                if (removingSpan.Start >= candidateSpan.End)
                    continue;
                candidateSpans.RemoveAt(i);

                // find next span
                startMatch = removingSpan.Definition.StartPattern!.Match(text, candidateSpan.End);
                if (!startMatch.Success)
                    continue;
                endMatch = removingSpan.Definition.EndPattern!.Match(text, startMatch.Index + startMatch.Length);
                if (endMatch.Success)
                {
                    var j = candidateSpans.Add(new(
                        removingSpan.Definition, 
                        startMatch.Index, 
                        endMatch.Index + endMatch.Length,
                        startMatch.Index + startMatch.Length,
                        endMatch.Index));
                    if (j < i)
                        ++i;
                }
            }
            
            // move to next span
            textStartIndex = candidateSpan.End;
        }
        this.FindToken(text, textStartIndex, text.Length, characterIndex, defaultTokenDefinitions, out token);
    }
    
    
    // Find token at given position.
    void FindToken(string text, int start, int end, int index, IList<SyntaxHighlightingToken> tokenDefinitions, out SyntaxHighlightingToken? token)
    {
        // initialize
        token = default;

        // setup initial candidate tokens
        var tokenComparison = new Comparison<Token>((lhs, rhs) =>
        {
            var result = (rhs.Start - lhs.Start);
            if (result != 0)
                return result;
            result = (lhs.End - rhs.End);
            if (result != 0)
                return result;
            result = tokenDefinitions.IndexOf(rhs.Definition) - tokenDefinitions.IndexOf(lhs.Definition);
            return result != 0 ? result : (rhs.GetHashCode() - lhs.GetHashCode());
        });
        var candidateTokens = new SortedObservableList<Token>(tokenComparison);
        foreach (var tokenDefinition in tokenDefinitions)
        {
            if (!tokenDefinition.IsValid)
                continue;
            var match = tokenDefinition.Pattern!.Match(text, start);
            if (match.Success && match.Length > 0)
            {
                var endIndex = match.Index + match.Length;
                if (endIndex <= end)
                    candidateTokens.Add(new(tokenDefinition, match.Index, endIndex));
            }
        }

        // create text runs
        var textStartIndex = start;
        while (candidateTokens.IsNotEmpty())
        {
            // stop finding
            if (textStartIndex > index)
                return;
            
            // get current token
            var candidateToken = candidateTokens[^1];
            candidateTokens.RemoveAt(candidateTokens.Count - 1);
            if (candidateToken.Start > index)
                return;

            // find and combine with next token if possible
            while (true)
            {
                var match = candidateToken.Definition.Pattern!.Match(text, candidateToken.End);
                if (!match.Success || match.Length <= 0)
                    break;
                var endIndex = match.Index + match.Length;
                if (endIndex > end)
                    break;
                var nextToken = new Token(candidateToken.Definition, match.Index, match.Index + match.Length);
                if (match.Index == candidateToken.End && match.Length > 0) // combine into single token
                {
                    var nextTokenIndex = candidateTokens.BinarySearch(nextToken, tokenComparison);
                    if (nextTokenIndex == ~candidateTokens.Count)
                    {
                        candidateToken = new(candidateToken.Definition, candidateToken.Start, nextToken.End);
                        continue;
                    }
                }
                candidateTokens.Add(nextToken);
                break;
            }
            
            // use current token
            if (candidateToken.End > index)
            {
                token = candidateToken.Definition;
                return;
            }

            // remove tokens which overlaps with current token
            for (var i = candidateTokens.Count - 1; i >= 0; --i)
            {
                // check overlapping
                var removingToken = candidateTokens[i];
                if (removingToken.Start >= candidateToken.End)
                    continue;
                candidateTokens.RemoveAt(i);

                // find next token
                var match = removingToken.Definition.Pattern!.Match(text, candidateToken.End);
                if (match.Success && match.Length > 0)
                {
                    var endIndex = match.Index + match.Length;
                    if (endIndex <= end)
                    {
                        var j = candidateTokens.Add(new(removingToken.Definition, match.Index, endIndex));
                        if (j < i)
                            ++i;
                    }
                }
            }

            // move to next token
            textStartIndex = candidateToken.End;
        }
    }


    /// <summary>
    /// Get or set flow direction.
    /// </summary>
    public FlowDirection FlowDirection
    {
        get => this.flowDirection;
        set
        {
            this.VerifyAccess();
            if (this.flowDirection == value)
                return;
            this.SetAndRaise(FlowDirectionProperty, ref this.flowDirection, value);
            this.InvalidateTextProperties();
        }
    }


    /// <summary>
    /// Get or set base font family.
    /// </summary>
    public FontFamily FontFamily
    {
        get => this.fontFamily;
        set
        {
            this.VerifyAccess();
            if (this.fontFamily == value)
                return;
            this.SetAndRaise(FontFamilyProperty, ref this.fontFamily, value);
            this.InvalidateTextProperties();
        }
    }


    /// <summary>
    /// Get or set base font size.
    /// </summary>
    public double FontSize
    {
        get => this.fontSize;
        set
        {
            this.VerifyAccess();
            if (Math.Abs(this.fontSize - value) <= 0.01)
                return;
            this.SetAndRaise(FontSizeProperty, ref this.fontSize, value);
            this.InvalidateTextProperties();
        }
    }


    /// <summary>
    /// Get or set base font stretch.
    /// </summary>
    public FontStretch FontStretch
    {
        get => this.fontStretch;
        set
        {
            this.VerifyAccess();
            if (this.fontStretch == value)
                return;
            this.SetAndRaise(FontStretchProperty, ref this.fontStretch, value);
            this.InvalidateTextLayout();
        }
    }


    /// <summary>
    /// Get or set base font style.
    /// </summary>
    public FontStyle FontStyle
    {
        get => this.fontStyle;
        set
        {
            this.VerifyAccess();
            if (this.fontStyle == value)
                return;
            this.SetAndRaise(FontStyleProperty, ref this.fontStyle, value);
            this.InvalidateTextProperties();
        }
    }


    /// <summary>
    /// Get or set base font weight.
    /// </summary>
    public FontWeight FontWeight
    {
        get => this.fontWeight;
        set
        {
            this.VerifyAccess();
            if (this.fontWeight == value)
                return;
            this.SetAndRaise(FontWeightProperty, ref this.fontWeight, value);
            this.InvalidateTextLayout();
        }
    }


    /// <summary>
    /// Get or set base foreground brush.
    /// </summary>
    public IBrush? Foreground
    {
        get => this.foreground;
        set
        {
            this.VerifyAccess();
            if (this.foreground == value)
                return;
            this.foregroundPropertyChangedHandlerToken.Dispose();
            (value as AvaloniaObject)?.Let(it => 
                this.foregroundPropertyChangedHandlerToken = it.AddWeakEventHandler<AvaloniaPropertyChangedEventArgs>(nameof(AvaloniaObject.PropertyChanged), this.OnBrushPropertyChanged));
            this.SetAndRaise(ForegroundProperty, ref this.foreground, value);
            this.InvalidateTextProperties();
        }
    }


    // Invalidate text layout.
    void InvalidateTextLayout()
    {
        this.textLayout = null;
        this.TextLayoutInvalidated?.Invoke(this, EventArgs.Empty);
    }
    

    // Invalidate text properties.
    void InvalidateTextProperties()
    {
        this.textProperties = null;
        this.InvalidateTextLayout();
    }


    /// <summary>
    /// Get or set letter spacing.
    /// </summary>
    public double LetterSpacing
    {
        get => this.letterSpacing;
        set
        {
            this.VerifyAccess();
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (Math.Abs(this.letterSpacing - value) <= 0.01)
                return;
            this.SetAndRaise(LetterSpacingProperty, ref this.letterSpacing, value);
            this.InvalidateTextLayout();
        }
    }


    /// <summary>
    /// Get or set line height
    /// </summary>
    public double LineHeight
    {
        get => this.lineHeight;
        set
        {
            this.VerifyAccess();
            if (double.IsNaN(value))
            {
                if (double.IsNaN(this.lineHeight))
                    return;
            }
            else if (!double.IsFinite(value) || value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            else if (double.IsFinite(this.lineHeight) && Math.Abs(this.lineHeight - value) <= 0.01)
                return;
            this.SetAndRaise(LineHeightProperty, ref this.lineHeight, value);
            this.InvalidateTextLayout();
        }
    }


    /// <summary>
    /// Get or set maximum height of text layout.
    /// </summary>
    public double MaxHeight
    {
        get => this.maxHeight;
        set
        {
            this.VerifyAccess();
            if (double.IsInfinity(value))
            {
                if (value.Equals(this.maxHeight))
                    return;
            }
            else if (double.IsNaN(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            else if (double.IsFinite(this.maxHeight) && Math.Abs(this.maxHeight - value) <= 0.01)
                return;
            this.SetAndRaise(MaxHeightProperty, ref this.maxHeight, value);
            this.InvalidateTextLayout();
        }
    }


    /// <summary>
    /// Get or set maximum number of lines.
    /// </summary>
    public int MaxLines
    {
        get => this.maxLines;
        set
        {
            this.VerifyAccess();
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            else if (this.maxLines == value)
                return;
            this.SetAndRaise(MaxLinesProperty, ref this.maxLines, value);
            this.InvalidateTextLayout();
        }
    }


    /// <summary>
    /// Get or set maximum width of text layout.
    /// </summary>
    public double MaxWidth
    {
        get => this.maxWidth;
        set
        {
            this.VerifyAccess();
            if (double.IsInfinity(value))
            {
                if (value.Equals(this.maxWidth))
                    return;
            }
            else if (double.IsNaN(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            else if (double.IsFinite(this.maxWidth) && Math.Abs(this.maxWidth - value) <= 0.01)
                return;
            this.SetAndRaise(MaxWidthProperty, ref this.maxWidth, value);
            this.InvalidateTextLayout();
        }
    }


    // Called when property of attached brush has been changed.
    void OnBrushPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) =>
        this.InvalidateTextProperties();


    // Called when definition set changed.
    void OnDefinitionSetChanged(object? sender, EventArgs e) =>
        this.InvalidateTextProperties();
    

    /// <summary>
    /// Get or set preedit text.
    /// </summary>
    public string? PreeditText
    {
        get => this.preeditText;
        set
        {
            this.VerifyAccess();
            if (this.preeditText == value)
                return;
            this.SetAndRaise(PreeditTextProperty, ref this.preeditText, value);
            this.InvalidateTextProperties();
        }
    }


    /// <summary>
    /// Get or set background brush for selected text.
    /// </summary>
    public IBrush? SelectionBackground
    {
        get => this.selectionBackground;
        set
        {
            this.VerifyAccess();
            if (this.selectionBackground == value)
                return;
            this.selectionBackgroundPropertyChangedHandlerToken.Dispose();
            (value as AvaloniaObject)?.Let(it => 
                this.selectionBackgroundPropertyChangedHandlerToken = it.AddWeakEventHandler<AvaloniaPropertyChangedEventArgs>(nameof(AvaloniaObject.PropertyChanged), this.OnBrushPropertyChanged));
            this.SetAndRaise(SelectionBackgroundProperty, ref this.selectionBackground, value);
            if (this.selectionStart != this.selectionEnd)
                this.InvalidateTextProperties();
        }
    }
    

    /// <summary>
    /// Get or set end (exclusive) index of selected text.
    /// </summary>
    public int SelectionEnd
    {
        get => this.selectionEnd;
        set
        {
            this.VerifyAccess();
            if (this.selectionEnd == value)
                return;
            this.SetAndRaise(SelectionEndProperty, ref this.selectionEnd, value);
            if (this.selectionForeground != null)
                this.InvalidateTextProperties();
        }
    }


    /// <summary>
    /// Get or set foreground brush for selected text.
    /// </summary>
    public IBrush? SelectionForeground
    {
        get => this.selectionForeground;
        set
        {
            this.VerifyAccess();
            if (this.selectionForeground == value)
                return;
            this.selectionForegroundPropertyChangedHandlerToken.Dispose();
            (value as AvaloniaObject)?.Let(it => 
                this.selectionForegroundPropertyChangedHandlerToken = it.AddWeakEventHandler<AvaloniaPropertyChangedEventArgs>(nameof(AvaloniaObject.PropertyChanged), this.OnBrushPropertyChanged));
            this.SetAndRaise(SelectionForegroundProperty, ref this.selectionForeground, value);
            if (this.selectionStart != this.selectionEnd)
                this.InvalidateTextProperties();
        }
    }


    /// <summary>
    /// Get or set start (inclusive) index of selected text.
    /// </summary>
    public int SelectionStart
    {
        get => this.selectionStart;
        set
        {
            this.VerifyAccess();
            if (this.selectionStart == value)
                return;
            this.SetAndRaise(SelectionStartProperty, ref this.selectionStart, value);
            if (this.selectionForeground != null)
                this.InvalidateTextProperties();
        }
    }


    /// <summary>
    /// Get or set text.
    /// </summary>
    public string? Text
    {
        get => this.text;
        set
        {
            this.VerifyAccess();
            if ((this.text?.Length ?? 0) < 1024
                && (value?.Length ?? 0) < 1024
                && this.text == value)
            {
                return;
            }
            this.SetAndRaise(TextProperty, ref this.text, value);
            this.InvalidateTextProperties();
        }
    }


    /// <summary>
    /// Get or set text alignment.
    /// </summary>
    public TextAlignment TextAlignment
    {
        get => this.textAlignment;
        set
        {
            this.VerifyAccess();
            if (this.textAlignment == value)
                return;
            this.SetAndRaise(TextAlignmentProperty, ref this.textAlignment, value);
            this.InvalidateTextLayout();
        }
    }


    /// <summary>
    /// Get or set base text decorations.
    /// </summary>
    public TextDecorationCollection? TextDecorations
    {
        get => this.textDecorations;
        set
        {
            this.VerifyAccess();
            if (this.textDecorations == value)
                return;
            this.SetAndRaise(TextDecorationProperty, ref this.textDecorations, value);
            this.InvalidateTextProperties();
        }
    }


    /// <summary>
    /// Raised when text layout of the instance was invalidated.
    /// </summary>
    public event EventHandler? TextLayoutInvalidated;


    // Get text with pre-edit text.
    string TextWithPreeditText
    {
        get
        {
            var text = this.text;
            if (!string.IsNullOrEmpty(this.preeditText))
            {
                if (text == null)
                    text = this.preeditText;
                else
                {
                    var caretIndex = Math.Min(this.selectionStart, this.selectionEnd);
                    if (caretIndex < 0)
                        text = this.preeditText + text;
                    else if (caretIndex >= text.Length)
                        text += this.preeditText;
                    else
                        text = text[..caretIndex] + this.preeditText + text[caretIndex..];
                }
            }
            return text ?? "";
        }
    }


    /// <summary>
    /// Get or set text trimming.
    /// </summary>
    public TextTrimming TextTrimming
    {
        get => this.textTrimming;
        set
        {
            this.VerifyAccess();
            if (this.textTrimming == value)
                return;
            this.SetAndRaise(TextTrimmingProperty, ref this.textTrimming, value);
            this.InvalidateTextLayout();
        }
    }


    /// <summary>
    /// Get or set text wrapping.
    /// </summary>
    public TextWrapping TextWrapping
    {
        get => this.textWrapping;
        set
        {
            this.VerifyAccess();
            if (this.textWrapping == value)
                return;
            this.SetAndRaise(TextWrappingProperty, ref this.textWrapping, value);
            this.InvalidateTextLayout();
        }
    }
}
