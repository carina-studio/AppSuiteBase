using Avalonia.Media;
using CarinaStudio.Controls;
using System;
using System.Text.RegularExpressions;

namespace CarinaStudio.AppSuite.Controls.Highlighting;

/// <summary>
/// Syntax highlighting for format of <see cref="TimeSpan"/>.
/// </summary>
public static class TimeSpanFormatSyntaxHighlighting
{
    // Fields.
    static Regex? ConstantFormatPattern;
    static Regex? DaysPattern;
    static Regex? EscapeCharacterPattern;
    static Regex? HoursPattern;
    static Regex? LongFormatPattern;
    static Regex? MinutesPattern;
    static Regex? SecondsPattern;
    static Regex? ShortFormatPattern;
    static Regex? SubSecondsPattern;


    /// <summary>
    /// Create definition set of syntax highlighting.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <returns>Definition set of syntax highlighting.</returns>
    public static SyntaxHighlightingDefinitionSet CreateDefinitionSet(IAvaloniaApplication app)
    {
        // create patterns
        ConstantFormatPattern ??= new(@"(?<!(^|[^\\])(\\\\)*c)c", RegexOptions.Compiled);
        DaysPattern ??= new(@"(?<!(^|[^\\])(\\\\)*d)d{1,8}", RegexOptions.Compiled);
        EscapeCharacterPattern ??= new(@"\\.", RegexOptions.Compiled);
        HoursPattern ??= new(@"(?<!(^|[^\\])(\\\\)*h)h{1,2}", RegexOptions.Compiled);
        LongFormatPattern ??= new(@"(?<!(^|[^\\])(\\\\)*G)G", RegexOptions.Compiled);
        MinutesPattern ??= new(@"(?<!(^|[^\\])(\\\\)*m)m{1,2}", RegexOptions.Compiled);
        SecondsPattern ??= new(@"(?<!(^|[^\\])(\\\\)*s)s{1,2}", RegexOptions.Compiled);
        ShortFormatPattern ??= new(@"(?<!(^|[^\\])(\\\\)*g)g", RegexOptions.Compiled);
        SubSecondsPattern ??= new(@"(?<!(^|[^\\])(\\\\)*f)f{1,7}|(?<!(^|[^\\])(\\\\)*F)F{1,7}", RegexOptions.Compiled);

        // create definition set
        var definitionSet = new SyntaxHighlightingDefinitionSet(name: "TimeSpan Format");
        definitionSet.TokenDefinitions.Add(new(name: "Constant Format")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/TimeSpanFormatSyntaxHighlighting.ConstantFormat", Brushes.Green),
            Pattern = ConstantFormatPattern,
        });
        definitionSet.TokenDefinitions.Add(new(name: "Short Format")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/TimeSpanFormatSyntaxHighlighting.ShortFormat", Brushes.Green),
            Pattern = ShortFormatPattern,
        });
        definitionSet.TokenDefinitions.Add(new(name: "Long Format")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/TimeSpanFormatSyntaxHighlighting.LongFormat", Brushes.Green),
            Pattern = LongFormatPattern,
        });
        definitionSet.TokenDefinitions.Add(new(name: "Days")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/TimeSpanFormatSyntaxHighlighting.Days", Brushes.Blue),
            Pattern = DaysPattern,
        });
        definitionSet.TokenDefinitions.Add(new(name: "Hours")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/TimeSpanFormatSyntaxHighlighting.Hours", Brushes.Blue),
            Pattern = HoursPattern,
        });
        definitionSet.TokenDefinitions.Add(new(name: "Minutes")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/TimeSpanFormatSyntaxHighlighting.Minutes", Brushes.Blue),
            Pattern = MinutesPattern,
        });
        definitionSet.TokenDefinitions.Add(new(name: "Seconds")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/TimeSpanFormatSyntaxHighlighting.Seconds", Brushes.Blue),
            Pattern = SecondsPattern,
        });
        definitionSet.TokenDefinitions.Add(new(name: "Sub-Seconds")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/TimeSpanFormatSyntaxHighlighting.SubSeconds", Brushes.Blue),
            Pattern = SubSecondsPattern,
        });
        definitionSet.TokenDefinitions.Add(new("Escape Character")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/TimeSpanFormatSyntaxHighlighting.EscapeCharacter", Brushes.Magenta),
            Pattern = EscapeCharacterPattern,
        });

        // complete
        return definitionSet;
    }
}