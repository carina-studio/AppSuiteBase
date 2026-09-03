using Avalonia.Media;
using System;
using System.Text.RegularExpressions;

namespace CarinaStudio.AppSuite.Controls.Highlighting;

/// <summary>
/// Syntax highlighting for regular expression.
/// </summary>
public static class RegexSyntaxHighlighting
{
    // Constants.
    internal const string EndOfLineComment = "End-of-Line Comment";
    internal const string InlineComment = "Inline Comment";
    
    
    // Static fields.
    static Regex? AlternationPattern;
    static Regex? AnchorsPattern;
    static Regex? BracketsPattern;
    static Regex? CharacterClassesEndPattern;
    static Regex? CharacterRangesPattern;
    static Regex? CharacterClassesStartPattern;
    static Regex? EndOfLineCommentPattern;
    static Regex? EscapeCharactersPattern;
    static Regex? InlineCommentPattern;
    static Regex? QuantifiersPattern;
    static Regex? SpecialCharactersPattern;
    static Regex? UnicodeCategoriesPattern;


    /// <summary>
    /// Get number of continuous backslashes before given position.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="index">Index of position in the text.</param>
    /// <returns>Number of continuous backslashes before given position.</returns>
    public static int CountContinuousBackslashesBefore(string? text, int index) =>
        text is not null ? CountContinuousBackslashesBefore(text.AsSpan(), index) : 0;
    
    
    /// <summary>
    /// Get number of continuous backslashes before given position.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="index">Index of position in the text.</param>
    /// <returns>Number of continuous backslashes before given position.</returns>
    public static int CountContinuousBackslashesBefore(ReadOnlySpan<char> text, int index)
    {
        // check position
        if (index > text.Length)
            index = text.Length;
        --index;
        if (index < 0)
            return 0;
        
        // count continuous backslashes backward
        var count = 0;
        while (index >= 0 && text[index] == '\\')
        {
            ++count;
            --index;
        }
        return count;
    }


    /// <summary>
    /// Create definition set.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <returns>Definition set for regular expression.</returns>
    public static SyntaxHighlightingDefinitionSet CreateDefinitionSet(IAvaloniaApplication app)
    {
        // create patterns
        AlternationPattern ??= new(@"\|", RegexOptions.Compiled);
        AnchorsPattern ??= new(@"\^|\$|\\b|\\B|\\z|\\Z|\\A|\\G", RegexOptions.Compiled);
        BracketsPattern ??= new(@"\((\?(:|\!|<=|<\!|=)?(<[^-\>]+(\-[^-\>]+)?>|'[^-\']+(\-[^-\']+)?')?)?|\)", RegexOptions.Compiled);
        CharacterClassesEndPattern ??= new(@"(?<=[^\\](\\\\)*)\]", RegexOptions.Compiled);
        CharacterClassesStartPattern ??= new(@"(?<=(^|[^\\])(\\\\)*)\[(\^)?", RegexOptions.Compiled);
        CharacterRangesPattern ??= new(@"[0-9\w]\-[0-9\w]", RegexOptions.Compiled);
        EndOfLineCommentPattern ??= new(@"(?<=(^|[^\\])(\\\\)*)\#.*$", RegexOptions.Compiled);
        EscapeCharactersPattern ??= new(@"\\[^\sbBzZAG]", RegexOptions.Compiled);
        InlineCommentPattern ??= new(@"(?<=(^|[^\\])(\\\\)*)\(\?\#[^\)]*\)", RegexOptions.Compiled);
        QuantifiersPattern ??= new(@"\+(\?)?|\*(\?)?|\?(\?)?|\{\d+\}|\{\d+\,(\d+)?\}|\{(\d+)?\,\d+\}", RegexOptions.Compiled);
        SpecialCharactersPattern ??= new(@"\.", RegexOptions.Compiled);
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
        
        // comment
        var commentForeground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.Comment", Brushes.Gray);
        /*
        definitionSet.TokenDefinitions.Add(new(EndOfLineComment)
        {
            FontStyle = FontStyle.Italic,
            Foreground = commentForeground,
            Pattern = EndOfLineCommentPattern,
        });
        */
        definitionSet.TokenDefinitions.Add(new(InlineComment)
        {
            FontStyle = FontStyle.Italic,
            Foreground = commentForeground,
            Pattern = InlineCommentPattern,
        });

        // escape characters
        var escapeCharTokenDef = new SyntaxHighlightingToken(name: "Escape Characters")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.EscapeCharacters", Brushes.Gray),
            Pattern = EscapeCharactersPattern,
        };
        definitionSet.TokenDefinitions.Add(escapeCharTokenDef);

        // special characters
        var specialCharTokenDef = new SyntaxHighlightingToken(name: "Special Characters")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/RegexSyntaxHighlighting.SpecialCharacters", Brushes.Magenta),
            Pattern = SpecialCharactersPattern,
        };
        definitionSet.TokenDefinitions.Add(specialCharTokenDef);

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

            // special characters
            it.TokenDefinitions.Add(specialCharTokenDef);
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


    /// <summary>
    /// Find the range of character classes construct around given position.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="index">Index of position in the text.</param>
    /// <returns>The range of character classes construct.</returns>
    public static Range<int> FindCharacterClassesRange(string? text, int index) =>
        FindConstructRange(text, index, '[', ']');
    
    
    // Find range of construct with given beginning/ending characters.
    static Range<int> FindConstructRange(string? text, int index, char beginningChar, char endingChar)
    {
        // check parameters
        if (text is null)
            return default;
        var textLength = text.Length;
        if (index <= 0 || textLength <= 0 || index >= textLength)
            return default;
        var span = text.AsSpan();
        
        // find start of construct
        var start = index - 1;
        do
        {
            var c = span[start];
            if (c == beginningChar)
            {
                if ((CountContinuousBackslashesBefore(span, start) & 0x1) == 0)
                    break;
            }
            else if (c == endingChar)
            {
                if ((CountContinuousBackslashesBefore(span, start) & 0x1) == 0)
                    return default;
            }
            --start;
        } while (start >= 0);
        if (start < 0)
            return default;
        
        // find end of construct
        var end = index;
        do
        {
            var c = span[end];
            if (c == endingChar)
            {
                if ((CountContinuousBackslashesBefore(span, end) & 0x1) == 0)
                    break;
            }
            else if (c == beginningChar)
            {
                if ((CountContinuousBackslashesBefore(span, end) & 0x1) == 0)
                    return default;
            }
            ++end;
        } while (end < textLength);
        if (end >= textLength)
            return default;
        
        // complete
        return new(start, end + 1);
    }
    
    
    /// <summary>
    /// Find the range of name of group construct around given position.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="index">Index of position in the text.</param>
    /// <returns>The range of name of group construct.</returns>
    public static Range<int> FindGroupNameRange(string? text, int index)
    {
        // check parameters
        if (text is null)
            return default;
        var textLength = text.Length;
        var start = index - 1;
        if (start < 0 || start >= textLength)
            return default;
        var span = text.AsSpan();
        
        // find beginning of group name
        while (start >= 0 && span[start] != '<')
        {
            if (span[start] == '>')
                return default;
            --start;
        }
        if (start < 2 || span[start - 1] != '?' || span[start - 2] != '(')
            return default;
        
        // find end of group name
        for (var end = start + 1; end < textLength; ++end)
        {
            if (span[end] == '>')
                return (start + 1, end);
        }
        return new(start + 1, textLength);
    }


    /// <summary>
    /// Find the range of a phrase around given position.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="index">Index of position in the text.</param>
    /// <returns>The range of phrase.</returns>
    public static Range<int> FindPhraseRange(string? text, int index)
    {
        // check parameters
        if (text is null)
            return default;
        var textLength = text.Length;
        if (index < 0 || index > textLength)
            return default;
        var span = text.AsSpan();
        
        // find start of phrase
        var end = index;
        var start = end - 1;
        while (start >= 0)
        {
            var c = span[start];
            if (char.IsLetter(c) || char.IsDigit(c) || c == '_' || c == '-')
                --start;
            else
            {
                if (c == '\\' && (CountContinuousBackslashesBefore(span, start) & 0x1) == 0)
                    ++start;
                break;
            }
        }
        ++start;

        // find end of phrase
        if (start <= end)
        {
            while (end < textLength)
            {
                var c = span[end];
                if (char.IsLetter(c) || char.IsDigit(c) || c == '_' || c == '-')
                    ++end;
                else
                    break;
            }
        }
        
        // make sure that the phrase is started with letter
        while (start < end)
        {
            var c = span[start];
            if (char.IsLetter(c))
                break;
            ++start;
        }

        // get range
        if (start < end)
            return new(start, end);
        return default;
    }
    
    
    /// <summary>
    /// Find the range of quantifier construct around given position.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="index">Index of position in the text.</param>
    /// <returns>The range of quantifier construct.</returns>
    public static Range<int> FindQuantifierRange(string? text, int index) =>
        FindConstructRange(text, index, '{', '}');
}