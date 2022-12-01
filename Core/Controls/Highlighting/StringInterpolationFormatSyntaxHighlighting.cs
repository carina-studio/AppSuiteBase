using Avalonia.Media;
using CarinaStudio.Controls;
using System.Text.RegularExpressions;

namespace CarinaStudio.AppSuite.Controls.Highlighting;

/// <summary>
/// Syntax highlighting for string interpolation format.
/// </summary>
public static class StringInterpolationFormatSyntaxHighlighting
{
    // Static fields.
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
        StringFormatPattern ??= new(@":[^\}]+", RegexOptions.Compiled);
        SpecialTokensPattern ??= new(@"\{\{|\}\}", RegexOptions.Compiled);
        VarEndPattern ??= new(@"\}", RegexOptions.Compiled);
        VarExpressionPattern ??= new(@"[^:\}]+(?=[:\}])", RegexOptions.Compiled);
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
}