using Avalonia.Media;
using CarinaStudio.Controls;
using System.Text.RegularExpressions;

namespace CarinaStudio.AppSuite.Controls.Highlighting;

/// <summary>
/// Syntax highlighting for regular expression.
/// </summary>
public static class RegexSyntaxHighlighting
{
    // Static fields.
    static Regex? AlternationPattern;
    static Regex? AnchorsPattern;
    static Regex? BracketsPattern;
    static Regex? CharacterClassesEndPattern;
    static Regex? CharacterRangesPattern;
    static Regex? CharacterClassesStartPattern;
    static Regex? EscapeCharactersPattern;
    static Regex? QuantifiersPattern;
    static Regex? UnicodeCategoriesPattern;


    /// <summary>
    /// Create definition set.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <returns>Definition set for regular expression.</returns>
    public static SyntaxHighlightingDefinitionSet CreateDefinitionSet(CarinaStudio.IAvaloniaApplication app)
    {
        // create patterns
        AlternationPattern ??= new(@"\|", RegexOptions.Compiled);
        AnchorsPattern ??= new(@"\^|\$|\\b|\\B|\\z|\\Z|\\A|\\G", RegexOptions.Compiled);
        BracketsPattern ??= new(@"\((\?(:|\!|<=|>=)?(<[^-\>]+(\-[^-\>]+)?>|'[^-\']+(\-[^-\']+)?')?)?|\)", RegexOptions.Compiled);
        CharacterClassesEndPattern ??= new(@"(?<=(\\\\)*[^\\]?)\]", RegexOptions.Compiled);
        CharacterClassesStartPattern ??= new(@"(^|(?<=(\\\\)*[^\\]?))\[(\^)?", RegexOptions.Compiled);
        CharacterRangesPattern ??= new(@"[0-9\w]\-[0-9\w]", RegexOptions.Compiled);
        EscapeCharactersPattern ??= new(@"\\[^\sbBzZAG]", RegexOptions.Compiled);
        QuantifiersPattern ??= new(@"\+(\?)?|\*(\?)?|\?(\?)?|\{\d+\}|\{\d+\,(\d+)?\}|\{(\d+)?\,\d+\}", RegexOptions.Compiled);
        UnicodeCategoriesPattern ??= new(@"\\[pP]\{\w+\}", RegexOptions.Compiled);

        // create definition set
        var definitionSet = new SyntaxHighlightingDefinitionSet("Regular Expression");

        // alternation
        definitionSet.TokenDefinitions.Add(new(name: "Alternation")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.Alternation", Brushes.Gray),
            Pattern = AlternationPattern,
        });

        // anchors
        var anchorsTokenDef = new SyntaxHighlightingToken("Anchors")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.Anchors", Brushes.Magenta),
            Pattern = AnchorsPattern,
        };
        definitionSet.TokenDefinitions.Add(anchorsTokenDef);

        // brackets and grouping
        definitionSet.TokenDefinitions.Add(new(name: "Brackets")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.Brackets", Brushes.Yellow),
            Pattern = BracketsPattern,
        });

        // escape characters
        var escapeCharTokenDef = new SyntaxHighlightingToken(name: "Escape Characters")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.EscapeCharacters", Brushes.Gray),
            Pattern = EscapeCharactersPattern,
        };
        definitionSet.TokenDefinitions.Add(escapeCharTokenDef);

        // character classes
        definitionSet.SpanDefinitions.Add(new SyntaxHighlightingSpan("Character Classes").Also(it =>
        {
            // span
            it.EndPattern = CharacterClassesEndPattern;
            it.Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.CharacterClasses", Brushes.Cyan);
            it.StartPattern = CharacterClassesStartPattern;

            // anchors
            it.TokenDefinitions.Add(anchorsTokenDef);

            // character ranges
            it.TokenDefinitions.Add(new(name: "Character Ranges")
            {
                Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.CharacterRanges", Brushes.Green),
                Pattern = CharacterRangesPattern,
            });
            
            // escape characters
            it.TokenDefinitions.Add(escapeCharTokenDef);
        }));

        // quantifiers
        definitionSet.TokenDefinitions.Add(new(name: "Quantifiers")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.Quantifiers", Brushes.Red),
            Pattern = QuantifiersPattern,
        });

        // unicode categories
        definitionSet.TokenDefinitions.Add(new(name: "Unicode Categories")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.UnicodeCategories", Brushes.LightBlue),
            Pattern = UnicodeCategoriesPattern,
        });

        // complete
        return definitionSet;
    }
}