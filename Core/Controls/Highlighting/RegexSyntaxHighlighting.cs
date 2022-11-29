using Avalonia.Media;
using CarinaStudio.Controls;

namespace CarinaStudio.AppSuite.Controls.Highlighting;

/// <summary>
/// Syntax highlighting for regular expression.
/// </summary>
public static class RegexSyntaxHighlighting
{
    /// <summary>
    /// Create definition set.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <returns>Definition set for regular expression.</returns>
    public static SyntaxHighlightingDefinitionSet CreateDefinitionSet(CarinaStudio.IAvaloniaApplication app)
    {
        // create definition set
        var definitionSet = new SyntaxHighlightingDefinitionSet("Regular Expression");

        // alternation
        definitionSet.TokenDefinitions.Add(new(name: "Alternation")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.Alternation", Brushes.Gray),
            Pattern = new(@"\|"),
        });

        // anchors
        definitionSet.TokenDefinitions.Add(new("Anchors")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.Anchors", Brushes.Magenta),
            Pattern = new(@"\^|\$|\\b|\\B|\\z|\\Z|\\A|\\G"),
        });

        // brackets and grouping
        definitionSet.TokenDefinitions.Add(new(name: "Brackets")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.Brackets", Brushes.Yellow),
            Pattern = new(@"\((\?(:|\!|<=|>=)?(<[^-\>]+(\-[^-\>]+)?>|'[^-\']+(\-[^-\']+)?')?)?|\)"),
        });

        // character classes
        definitionSet.SpanDefinitions.Add(new SyntaxHighlightingSpan("Character Classes").Also(it =>
        {
            // span
            it.EndPattern = new(@"(?<=(\\\\)*[^\\]?)\]");
            it.Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.CharacterClasses", Brushes.Cyan);
            it.StartPattern = new(@"(^|(?<=(\\\\)*[^\\]?))\[(\^)?");

            // anchors
            it.TokenDefinitions.Add(new("Anchors")
            {
                Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.Anchors", Brushes.Magenta),
                Pattern = new(@"\^|\$|\\b|\\B|\\z|\\Z|\\A|\\G"),
            });

            // character ranges
            it.TokenDefinitions.Add(new(name: "Character Ranges")
            {
                Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.CharacterRanges", Brushes.Green),
                Pattern = new(@"[0-9\w]\-[0-9\w]"),
            });
            
            // escape characters
            it.TokenDefinitions.Add(new(name: "Escape Characters")
            {
                Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.EscapeCharacters", Brushes.Gray),
                Pattern = new(@"\\[^\sbBzZAG]"),
            });
        }));

        // escape characters
        definitionSet.TokenDefinitions.Add(new(name: "Escape Characters")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.EscapeCharacters", Brushes.Gray),
            Pattern = new(@"\\[^\sbBzZAG]"),
        });

        // quantifiers
        definitionSet.TokenDefinitions.Add(new(name: "Quantifiers")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.Quantifiers", Brushes.Red),
            Pattern = new(@"\+(\?)?|\*(\?)?|\?(\?)?|\{\d+\}|\{\d+\,(\d+)?\}|\{(\d+)?\,\d+\}"),
        });

        // unicode categories
        definitionSet.TokenDefinitions.Add(new(name: "Unicode Categories")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.UnicodeCategories", Brushes.LightBlue),
            Pattern = new(@"\\[pP]\{\w+\}"),
        });

        // complete
        return definitionSet;
    }
}