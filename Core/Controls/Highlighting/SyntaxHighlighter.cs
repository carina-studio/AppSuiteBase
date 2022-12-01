using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using CarinaStudio.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    /// Property of <see cref="TextTrimming"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, TextTrimming> TextTrimmingProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, TextTrimming>(nameof(TextTrimming), sh => sh.textTrimming, (sh, t) => sh.TextTrimming = t);
    /// <summary>
    /// Property of <see cref="TextWrapping"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlighter, TextWrapping> TextWrappingProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlighter, TextWrapping>(nameof(TextWrapping), sh => sh.textWrapping, (sh, w) => sh.TextWrapping = w);


    // Span.
    record class Span(SyntaxHighlightingSpan Definition, int Start, int End, int InnerStart, int InnerEnd);


    // Implementation of ITextSource.
    class TextSourceImpl : ITextSource
    {
        // Fields.
        readonly int textLength;
        readonly IList<(int, TextRun)> textRuns;

        // Constructor.
        public TextSourceImpl(IList<TextRun> textRuns)
        {
            var textRunCount = textRuns.Count;
            if (textRunCount > 0)
            {
                this.textRuns = new List<(int, TextRun)>(textRunCount);
                var textSourceIndex = 0;
                for (var i = 0; i < textRunCount; ++i)
                {
                    var textRun = textRuns[i];
                    if (textRun.TextSourceLength == 0)
                        continue;
                    this.textRuns.Add((textSourceIndex, textRun));
                    textSourceIndex += textRun.TextSourceLength;
                }
                this.textLength = textSourceIndex;
            }
            else
                this.textRuns = Array.Empty<(int, TextRun)>();
        }

        // Find text run.
        bool FindTextRun(int textSourceIndex, int searchRangeStart, int searchRangeEnd, out int textSourceIndexOfTextRun, [NotNullWhen(true)] out TextRun? textRun)
        {
            if (searchRangeStart >= searchRangeEnd)
            {
                textSourceIndexOfTextRun = -1;
                textRun = null;
                return false;
            }
            var searchIndex = (searchRangeStart + searchRangeEnd) >> 1;
            var candidateTextRun = this.textRuns[searchIndex];
            if (candidateTextRun.Item1 > textSourceIndex)
                return this.FindTextRun(textSourceIndex, searchRangeStart, searchIndex, out textSourceIndexOfTextRun, out textRun);
            if (candidateTextRun.Item1 + candidateTextRun.Item2.TextSourceLength <= textSourceIndex)
                return this.FindTextRun(textSourceIndex, searchIndex + 1, searchRangeEnd, out textSourceIndexOfTextRun, out textRun);
            textSourceIndexOfTextRun = candidateTextRun.Item1;
            textRun = candidateTextRun.Item2;
            return true;
        }
        
        /// <inheritdoc/>
        public TextRun? GetTextRun(int textSourceIndex)
        {
            if (textRuns.IsEmpty() || textSourceIndex >= this.textLength)
                return null;
            if (!this.FindTextRun(textSourceIndex, 0, this.textRuns.Count, out var textSourceIndexOfTextRun, out var textRun))
                return null;
            if (textSourceIndexOfTextRun == textSourceIndex)
                return textRun;
            if (textRun is TextCharacters textCharacters)
                return new TextCharacters(textCharacters.Text.Skip((textSourceIndex - textSourceIndexOfTextRun)), textCharacters.Properties);
            return textRun;
        }
    }


    // Token.
    record class Token(SyntaxHighlightingSpan Definition, int Start, int End);


    // Static fields.
    static readonly IList<SyntaxHighlightingToken> EmptyTokenDefinitions = Array.Empty<SyntaxHighlightingToken>();


    // Fields.
    IBrush? background;
    SyntaxHighlightingDefinitionSet? definitionSet;
    FlowDirection flowDirection = FlowDirection.LeftToRight;
    FontFamily fontFamily = FontManager.Current.DefaultFontFamilyName;
    double fontSize = 12;
    FontStretch fontStretch = FontStretch.Normal;
    FontStyle fontStyle = FontStyle.Normal;
    FontWeight fontWeight = FontWeight.Normal;
    IBrush? foreground;
    double letterSpacing;
    double lineHeight = double.NaN;
    double maxHeight = double.PositiveInfinity;
    int maxLines;
    double maxWidth = double.PositiveInfinity;
    string? preeditText;
    int selectionEnd;
    IBrush? selectionForeground;
    int selectionStart;
    string? text;
    TextAlignment textAlignment = TextAlignment.Left;
    TextLayout? textLayout;
    IList<TextRun>? textRuns;
    ITextSource? textSource;
    TextTrimming textTrimming = TextTrimming.CharacterEllipsis;
    TextWrapping textWrapping = TextWrapping.NoWrap;


    /// <summary>
    /// Initialize new <see cref="SyntaxHighlighter"/> instance.
    /// </summary>
    public SyntaxHighlighter()
    { }


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
            this.SetAndRaise(BackgroundProperty, ref this.background, value);
            this.InvalidateTextRuns();
        }
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
        
        // create typr face
        var typeface = new Typeface(this.fontFamily, this.fontStyle, this.fontWeight, this.fontStretch);

        // prepare base run properties
        var defaultRunProperties = new GenericTextRunProperties(
            typeface,
            this.fontSize,
            null,
            this.foreground
        );

        // prepare paragraph properties
        var paragraphProperties = new GenericTextParagraphProperties(
            this.flowDirection, 
            this.textAlignment, 
            true, 
            false,
            defaultRunProperties, 
            this.textWrapping, 
            this.lineHeight, 
            0, 
            this.letterSpacing);
        
        // create text runs and source
        this.textRuns ??= this.CreateTextRuns(defaultRunProperties);
        this.textSource ??= new TextSourceImpl(this.textRuns);

        // create text layout
        this.textLayout = new TextLayout(
            this.textSource,
            paragraphProperties,
            this.textTrimming,
            this.maxWidth,
            this.maxHeight,
            maxLines: this.maxLines,
            lineHeight: this.lineHeight);
        return this.textLayout;
    }


    // Create text runs.
    IList<TextRun> CreateTextRuns(TextRunProperties defaultRunProperties)
    {
        // check text
        var text = this.text;
        var preeditText = this.preeditText;
        if (string.IsNullOrEmpty(text))
        {
            if (string.IsNullOrEmpty(preeditText))
                return Array.Empty<TextRun>();
            text = "";
        }
        
        // setup default run properties for selected text
        var defaultSelectionRunProperties = new GenericTextRunProperties(
            defaultRunProperties.Typeface,
            defaultRunProperties.FontRenderingEmSize,
            null,
            this.selectionForeground ?? defaultRunProperties.ForegroundBrush
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
            result = this.definitionSet?.SpanDefinitions?.Let(it =>
            {
                return it.IndexOf(rhs.Definition) - it.IndexOf(lhs.Definition);
            }) ?? 0;
            return result != 0 ? result : (rhs.GetHashCode() - lhs.GetHashCode());
        });
        this.definitionSet?.SpanDefinitions?.Let(it =>
        {
            foreach (var spanDefinition in it)
            {
                if (!spanDefinition.IsValid)
                    continue;
                var startMatch = spanDefinition.StartPattern!.Match(text);
                if (!startMatch.Success)
                    continue;
                var endMatch = spanDefinition.EndPattern!.Match(text, startMatch.Index + startMatch.Length);
                if (endMatch.Success)
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
        
        // create text runs
        var textRuns = new List<TextRun>();
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
            var endMatch = default(Match);
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
                    candidateSpans.Add(new(
                        removingSpan.Definition, 
                        startMatch.Index, 
                        endMatch.Index + endMatch.Length,
                        startMatch.Index + startMatch.Length,
                        endMatch.Index));
                }
            }

            // create text runs
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
                    null,
                    span.Definition.Foreground ?? defaultRunProperties.ForegroundBrush
                );
                runPropertiesMap[span.Definition] = runProperties;
            }
            if (!selectionRunPropertiesMap.TryGetValue(span.Definition, out var selectionRunProperties))
            {
                selectionRunProperties = new GenericTextRunProperties(
                    runProperties.Typeface,
                    runProperties.FontRenderingEmSize,
                    null,
                    this.selectionForeground ?? runProperties.ForegroundBrush
                );
                selectionRunPropertiesMap[span.Definition] = selectionRunProperties;
            }
            if (textStartIndex < span.Start)
                CreateTextRunsInSpan(text, textStartIndex, span.Start, defaultTokenDefinitions, defaultRunProperties, defaultSelectionRunProperties, textRuns);
            CreateTextRunsInSpan(text, span.Start, span.InnerStart, EmptyTokenDefinitions, runProperties, selectionRunProperties, textRuns);
            CreateTextRunsInSpan(text, span.InnerStart, span.InnerEnd, span.Definition.TokenDefinitions, runProperties, selectionRunProperties, textRuns);
            CreateTextRunsInSpan(text, span.InnerEnd, span.End, EmptyTokenDefinitions, runProperties, selectionRunProperties, textRuns);
            textStartIndex = span.End;
        }
        if (textStartIndex < text.Length)
            CreateTextRunsInSpan(text, textStartIndex, text.Length, defaultTokenDefinitions, defaultRunProperties, defaultSelectionRunProperties, textRuns);
        
        // insert text run for preedit text
        if (!string.IsNullOrEmpty(preeditText))
        {
            var caretIndex = Math.Min(this.selectionStart, this.selectionEnd);
            var runProperties = new GenericTextRunProperties(
                defaultRunProperties.Typeface,
                defaultRunProperties.FontRenderingEmSize,
                TextDecorations.Underline,
                defaultRunProperties.ForegroundBrush
            );
            if (caretIndex <= 0)
                textRuns.Insert(0, new TextCharacters(preeditText.AsMemory(), runProperties));
            else if (caretIndex >= text.Length || textRuns.IsEmpty())
                textRuns.Add(new TextCharacters(preeditText.AsMemory(), runProperties));
            else
            {
                var indexOfTextRunToInsert = textRuns.Count - 1;
                var textRunToInsert = textRuns[indexOfTextRunToInsert];
                var textSourceIndexOfTextRun = text.Length - textRunToInsert.TextSourceLength;
                if (textSourceIndexOfTextRun > caretIndex)
                {
                    for (var i = textRuns.Count - 2; i >= 0; --i)
                    {
                        var textRun = textRuns[i];
                        textSourceIndexOfTextRun -= textRun.TextSourceLength;
                        if (textSourceIndexOfTextRun <= caretIndex)
                        {
                            indexOfTextRunToInsert = i;
                            textRunToInsert = textRun;
                            break;
                        }
                    }
                }
                if (textSourceIndexOfTextRun < 0)
                    textSourceIndexOfTextRun = 0;
                if (textSourceIndexOfTextRun == caretIndex)
                    textRuns.Insert(indexOfTextRunToInsert, new TextCharacters(preeditText.AsMemory(), runProperties));
                else
                {
                    textRuns[indexOfTextRunToInsert] = new TextCharacters(textRunToInsert.Text.Take(textSourceIndexOfTextRun + textRunToInsert.TextSourceLength - caretIndex), textRunToInsert.Properties!);
                    textRuns.Insert(indexOfTextRunToInsert + 1, new TextCharacters(preeditText.AsMemory(), runProperties));
                    textRuns.Insert(indexOfTextRunToInsert + 2, new TextCharacters(textRunToInsert.Text.Skip(caretIndex - textSourceIndexOfTextRun), textRunToInsert.Properties!));
                }
            }
        }

        // complete
        return textRuns;
    }
    void CreateTextRunsInSpan(string text, int start, int end, IList<SyntaxHighlightingToken> tokenDefinitions, TextRunProperties defaultRunProperties, TextRunProperties defaultSelectionRunProperties, IList<TextRun> textRuns)
    {
        // setup initial candidate tokens
        var tokenComparison = new Comparison<(int, int, SyntaxHighlightingToken)>((lhs, rhs) =>
        {
            var result = (rhs.Item1 - lhs.Item1);
            if (result != 0)
                return result;
            result = (lhs.Item2 - rhs.Item2);
            if (result != 0)
                return result;
            result = tokenDefinitions.IndexOf(rhs.Item3) - tokenDefinitions.IndexOf(lhs.Item3);
            return result != 0 ? result : (rhs.GetHashCode() - lhs.GetHashCode());
        });
        var candidateTokens = new SortedObservableList<(int, int, SyntaxHighlightingToken)>(tokenComparison);
        foreach (var tokenDefinition in tokenDefinitions)
        {
            if (!tokenDefinition.IsValid)
                continue;
            var match = tokenDefinition.Pattern!.Match(text, start);
            if (match.Success)
            {
                var endIndex = match.Index + match.Length;
                if (endIndex <= end)
                    candidateTokens.Add((match.Index, endIndex, tokenDefinition));
            }
        }

        // create text runs
        var textMemory = text.AsMemory();
        var textStartIndex = start;
        var runPropertiesMap = new Dictionary<SyntaxHighlightingToken, TextRunProperties>();
        var selectionRunPropertiesMap = new Dictionary<SyntaxHighlightingToken, TextRunProperties>();
        while (candidateTokens.IsNotEmpty())
        {
            // get current token
            var token = candidateTokens[^1];
            var tokenStartIndex = token.Item1;
            var tokenEndIndex = token.Item2;
            var tokenDefinition = token.Item3;
            candidateTokens.RemoveAt(candidateTokens.Count - 1);

            // find next token
            while (true)
            {
                var match = tokenDefinition.Pattern!.Match(text, tokenEndIndex);
                if (!match.Success)
                    break;
                var endIndex = match.Index + match.Length;
                if (endIndex > end)
                    break;
                if (match.Index == tokenEndIndex) // combine into single token
                {
                    var nextTokenIndex = candidateTokens.BinarySearch((match.Index, match.Index + match.Length, tokenDefinition), tokenComparison);
                    if (nextTokenIndex == ~candidateTokens.Count)
                    {
                        tokenEndIndex = endIndex;
                        continue;
                    }
                }
                candidateTokens.Add((match.Index, endIndex, tokenDefinition));
                break;
            }

            // remove tokens which overlaps with current token
            for (var i = candidateTokens.Count - 1; i >= 0; --i)
            {
                // check overlapping
                var removingToken = candidateTokens[i];
                if (removingToken.Item1 >= tokenEndIndex && removingToken.Item2 >= tokenEndIndex)
                    continue;
                candidateTokens.RemoveAt(i);

                // find next token
                var match = removingToken.Item3.Pattern!.Match(text, tokenEndIndex);
                if (match.Success)
                {
                    var endIndex = match.Index + match.Length;
                    if (endIndex <= end)
                        candidateTokens.Add((match.Index, endIndex, removingToken.Item3));
                }
            }

            // create text runs
            if (!runPropertiesMap.TryGetValue(tokenDefinition, out var runProperties))
            {
                var typeface = new Typeface(
                    tokenDefinition.FontFamily ?? defaultRunProperties.Typeface.FontFamily, 
                    tokenDefinition.FontStyle ?? defaultRunProperties.Typeface.Style,
                    tokenDefinition.FontWeight ?? defaultRunProperties.Typeface.Weight, 
                    this.fontStretch
                );
                runProperties = new GenericTextRunProperties(
                    typeface,
                    double.IsNaN(tokenDefinition.FontSize) ? defaultRunProperties.FontRenderingEmSize : tokenDefinition.FontSize,
                    null,
                    tokenDefinition.Foreground ?? defaultRunProperties.ForegroundBrush
                );
                runPropertiesMap[tokenDefinition] = runProperties;
            }
            if (!selectionRunPropertiesMap.TryGetValue(tokenDefinition, out var selectionRunProperties))
            {
                selectionRunProperties = new GenericTextRunProperties(
                    defaultSelectionRunProperties.Typeface,
                    defaultSelectionRunProperties.FontRenderingEmSize,
                    null,
                    this.selectionForeground ?? defaultSelectionRunProperties.ForegroundBrush
                );
                selectionRunPropertiesMap[tokenDefinition] = selectionRunProperties;
            }
            if (textStartIndex < tokenStartIndex)
                this.CreateTextRunsWithLineBreaks(text, textStartIndex, tokenStartIndex, defaultRunProperties, defaultSelectionRunProperties, textRuns);
            this.CreateTextRunsWithLineBreaks(text, tokenStartIndex, tokenEndIndex, runProperties, selectionRunProperties, textRuns);
            textStartIndex = tokenEndIndex;
        }
        if (textStartIndex < end)
            this.CreateTextRunsWithLineBreaks(text, textStartIndex, end, defaultRunProperties, defaultSelectionRunProperties, textRuns);
    }
    unsafe void CreateTextRunsWithLineBreaks(string text, int start, int end, TextRunProperties runProperties, TextRunProperties selectionRunProperties, IList<TextRun> textRuns)
    {
        var textStartIndex = start;
        var textEndIndex = start;
        fixed (char* textPtr = text)
        {
            var cPtr = (textPtr + start);
            while (textEndIndex < end)
            {
                var c = *(cPtr++);
                ++textEndIndex;
                switch (c)
                {
                    case '\n':
                        if (textEndIndex - 1 > textStartIndex)
                            this.CreateTextRunsWithoutLineBreaks(text, textStartIndex, textEndIndex - 1, runProperties, selectionRunProperties, textRuns);
                        textRuns.Add(new TextCharacters("\n".AsMemory(), runProperties));
                        textStartIndex = textEndIndex;
                        break;
                    case '\b':
                    case '\r':
                        textStartIndex = textEndIndex;
                        break;
                    default:
                        break;
                }
            }
            if (textStartIndex < textEndIndex)
                this.CreateTextRunsWithoutLineBreaks(text, textStartIndex, textEndIndex, runProperties, selectionRunProperties, textRuns);
        }
    }
    void CreateTextRunsWithoutLineBreaks(string text, int start, int end, TextRunProperties runProperties, TextRunProperties selectionRunProperties, IList<TextRun> textRuns)
    {
        var selectionStart = this.selectionStart;
        var selectionEnd = this.selectionEnd;
        if (selectionEnd < selectionStart)
            (selectionStart, selectionEnd) = (selectionEnd, selectionStart);
        if (selectionStart == selectionEnd || start >= selectionEnd || end <= selectionStart)
            textRuns.Add(new TextCharacters(new(text.AsMemory(start), 0, end - start), runProperties));
        else if (start < selectionStart)
        {
            textRuns.Add(new TextCharacters(new(text.AsMemory(start), 0, selectionStart - start), runProperties));
            if (end <= selectionEnd)
                textRuns.Add(new TextCharacters(new(text.AsMemory(selectionStart), 0, end - selectionStart), selectionRunProperties));
            else
            {
                textRuns.Add(new TextCharacters(new(text.AsMemory(selectionStart), 0, selectionEnd - selectionStart), selectionRunProperties));
                textRuns.Add(new TextCharacters(new(text.AsMemory(selectionEnd), 0, end - selectionEnd), runProperties));
            }
        }
        else
        {
            if (end <= selectionEnd)
                textRuns.Add(new TextCharacters(new(text.AsMemory(start), 0, end - start), selectionRunProperties));
            else
            {
                textRuns.Add(new TextCharacters(new(text.AsMemory(start), 0, selectionEnd - start), selectionRunProperties));
                textRuns.Add(new TextCharacters(new(text.AsMemory(selectionEnd), 0, end - selectionEnd), runProperties));
            }
        }
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
            this.InvalidateTextRuns();
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
            this.InvalidateTextLayout();
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
            this.InvalidateTextRuns();
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
            this.InvalidateTextRuns();
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
            this.SetAndRaise(ForegroundProperty, ref this.foreground, value);
            this.InvalidateTextRuns();
        }
    }


    // Invalidate text layout.
    void InvalidateTextLayout() =>
        this.textLayout = null;
    

    // Invalidate text runs.
    void InvalidateTextRuns()
    {
        this.textRuns = null;
        this.textSource = null;
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


    // Called when definition set changed.
    void OnDefinitionSetChanged(object? sender, EventArgs e) =>
        this.InvalidateTextRuns();
    

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
            this.InvalidateTextRuns();
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
                this.InvalidateTextRuns();
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
            this.SetAndRaise(SelectionForegroundProperty, ref this.selectionForeground, value);
            if (this.selectionStart != this.selectionEnd)
                this.InvalidateTextRuns();
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
                this.InvalidateTextRuns();
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
            this.InvalidateTextRuns();
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
