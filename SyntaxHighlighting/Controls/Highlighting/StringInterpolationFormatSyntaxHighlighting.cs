using Avalonia.Media;
using System.Text.RegularExpressions;

namespace CarinaStudio.AppSuite.Controls.Highlighting;

/// <summary>
/// Syntax highlighting for string interpolation format.
/// </summary>
public static class StringInterpolationFormatSyntaxHighlighting
{
    // Static fields.
    static Regex? AlignmentPattern;
    static Regex? StringFormatPattern;
    static Regex? SpecialTokensPattern;
    static Regex? VarEndPattern;
    static Regex? VarExpressionPattern;
    static Regex? VarStartPattern;


    /// <summary>
    /// Create definition set.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <returns>Definition set for string interpolation format.</returns>
    public static SyntaxHighlightingDefinitionSet CreateDefinitionSet(IAvaloniaApplication app)
    {
        // create patterns
        AlignmentPattern ??= new(@"\,[^,:\}]+(?=[:\}])", RegexOptions.Compiled);
        StringFormatPattern ??= new(@":[^,:\}]+(?=\})", RegexOptions.Compiled);
        SpecialTokensPattern ??= new(@"\{\{|\}\}", RegexOptions.Compiled);
        VarEndPattern ??= new(@"\}", RegexOptions.Compiled);
        VarExpressionPattern ??= new(@"[^,:\}]+(?=[,:\}])", RegexOptions.Compiled);
        VarStartPattern ??= new(@"(?<=(^|[^\{])(\{\{)*[^\{]?)\{(?!\{)", RegexOptions.Compiled);

        // create definition set
        var definitionSet = new SyntaxHighlightingDefinitionSet("String Interpolation");

        // special tokens
        definitionSet.TokenDefinitions.Add(new("Special Tokens")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/StringInterpolationFormatSyntaxHighlighting.SpecialTokens", Brushes.Magenta),
            Pattern = SpecialTokensPattern,
        });

        // variables
        definitionSet.SpanDefinitions.Add(new SyntaxHighlightingSpan("Variables").Also(it =>
        {
            // define span
            it.EndPattern = VarEndPattern;
            it.Foreground = app.FindResourceOrDefault<IBrush>("Brush/StringInterpolationFormatSyntaxHighlighting.Variables", Brushes.LightGreen);
            it.StartPattern = VarStartPattern;

            // variable expression
            it.TokenDefinitions.Add(new("Variable Expression")
            {
                Foreground = app.FindResourceOrDefault<IBrush>("Brush/StringInterpolationFormatSyntaxHighlighting.VariableExpression", Brushes.Yellow),
                Pattern = VarExpressionPattern,
            });

            // alignment
            it.TokenDefinitions.Add(new("Alignment")
            {
                Foreground = app.FindResourceOrDefault<IBrush>("Brush/StringInterpolationFormatSyntaxHighlighting.Alignment", Brushes.Magenta),
                Pattern = AlignmentPattern,
            });

            // string format
            it.TokenDefinitions.Add(new("String Format")
            {
                Foreground = app.FindResourceOrDefault<IBrush>("Brush/StringInterpolationFormatSyntaxHighlighting.StringFormat", Brushes.LightBlue),
                Pattern = StringFormatPattern,
            });
        }));

        // complete
        return definitionSet;
    }


    /// <summary>
    /// Find the range of variable name around given position.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="index">Index of position in the text.</param>
    /// <returns>The range of variable name.</returns>
    public static unsafe Range<int> FindVariableNameRange(string? text, int index)
    {
        if (text is null)
            return default;
        var textLength = text.Length;
        var start = index - 1;
        if (start < 0)
            return default;
        fixed (char* p = text)
        {
            if (p is null)
                return default;
            var cPtr = p;
            while (start >= 0 && cPtr[start] != '{')
            {
                var c = cPtr[start];
                if (c == '}' || c == ':' || c == ',')
                    return default;
                --start;
            }
            if (start < 0)
                return default;
            for (var end = start + 1; end < textLength; ++end)
            {
                var c = cPtr[end];
                if (c == '}' || c == ':' || c == ',')
                    return (start, end + 1);
            }
            return default;
        }
    }
}