using Avalonia.Media;
using CarinaStudio.Controls;
using System;
using System.Text.RegularExpressions;

namespace CarinaStudio.AppSuite.Controls.Highlighting;

/// <summary>
/// Syntax highlighting for format of <see cref="DateTime"/>.
/// </summary>
static partial class DateTimeFormatSyntaxHighlighting
{
    // Fields.
    static Regex? AmPmDesignatorPattern;
    static Regex? DayPattern;
    static Regex? EraPattern;
    static Regex? EscapeCharacterPattern;
    static Regex? HourPattern;
    static Regex? MinutePattern;
    static Regex? MonthPattern;
    static Regex? SecondPattern;
    static Regex? SeparatorPattern;
    static Regex? SubSecondPattern;
    static Regex? TimeZoneOffsetPattern;
    static Regex? TimeZonePattern;
    static Regex? YearPattern;


    /// <summary>
    /// Create definition set of syntax highlighting.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <returns>Definition set of syntax highlighting.</returns>
    public static SyntaxHighlightingDefinitionSet CreateDefinitionSet(IAvaloniaApplication app)
    {
        // create patterns
        AmPmDesignatorPattern ??= new(@"(?<=(^|[^\\])(\\\\)*)t+", RegexOptions.Compiled);
        DayPattern ??= new(@"(?<=(^|[^\\])(\\\\)*)d+", RegexOptions.Compiled);
        EraPattern ??= new(@"(?<=(^|[^\\])(\\\\)*)g+", RegexOptions.Compiled);
        EscapeCharacterPattern ??= new(@"\\.", RegexOptions.Compiled);
        HourPattern ??= new(@"(?<=(^|[^\\])(\\\\)*)h+|(?<=(^|[^\\])(\\\\)*)H+", RegexOptions.Compiled);
        MinutePattern ??= new(@"(?<=(^|[^\\])(\\\\)*)m+", RegexOptions.Compiled);
        MonthPattern ??= new(@"(?<=(^|[^\\])(\\\\)*)M+", RegexOptions.Compiled);
        SecondPattern ??= new(@"(?<=(^|[^\\])(\\\\)*)s+", RegexOptions.Compiled);
        SeparatorPattern ??= new(@"[:/]", RegexOptions.Compiled);
        SubSecondPattern ??= new(@"(?<!(^|[^\\])(\\\\)*f)f{1,7}|(?<!(^|[^\\])(\\\\)*F)F{1,7}", RegexOptions.Compiled);
        TimeZoneOffsetPattern ??= new(@"(?<=(^|[^\\])(\\\\)*)z+", RegexOptions.Compiled);
        TimeZonePattern ??= new(@"(?<=(^|[^\\])(\\\\)*)K+", RegexOptions.Compiled);
        YearPattern ??= new(@"(?<=(^|[^\\])(\\\\)*)y+", RegexOptions.Compiled);

        // create definition set
        var definitionSet = new SyntaxHighlightingDefinitionSet("DateTime Format");
        definitionSet.TokenDefinitions.Add(new("Year")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/DateTimeFormatSyntaxHighlighting.Year", Brushes.Red),
            Pattern = YearPattern,
        });
        definitionSet.TokenDefinitions.Add(new("Month")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/DateTimeFormatSyntaxHighlighting.Month", Brushes.Orange),
            Pattern = MonthPattern,
        });
        definitionSet.TokenDefinitions.Add(new("Day")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/DateTimeFormatSyntaxHighlighting.Day", Brushes.Yellow),
            Pattern = DayPattern,
        });
        definitionSet.TokenDefinitions.Add(new("Hour")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/DateTimeFormatSyntaxHighlighting.Hour", Brushes.Green),
            Pattern = HourPattern,
        });
        definitionSet.TokenDefinitions.Add(new("Minute")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/DateTimeFormatSyntaxHighlighting.Minute", Brushes.Blue),
            Pattern = MinutePattern,
        });
        definitionSet.TokenDefinitions.Add(new(name: "Second")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/DateTimeFormatSyntaxHighlighting.Second", Brushes.Indigo),
            Pattern = SecondPattern,
        });
        definitionSet.TokenDefinitions.Add(new("Sub-Second")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/DateTimeFormatSyntaxHighlighting.SubSecond", Brushes.Purple),
            Pattern = SubSecondPattern,
        });
        definitionSet.TokenDefinitions.Add(new("AM/PM Designator")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/DateTimeFormatSyntaxHighlighting.AmPmDesignator", Brushes.LightGreen),
            Pattern = AmPmDesignatorPattern,
        });
        definitionSet.TokenDefinitions.Add(new("Era")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/DateTimeFormatSyntaxHighlighting.Era", Brushes.DarkRed),
            Pattern = EraPattern,
        });
        definitionSet.TokenDefinitions.Add(new("Escape Character")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/DateTimeFormatSyntaxHighlighting.EscapeCharacter", Brushes.LightYellow),
            Pattern = EscapeCharacterPattern,
        });
        definitionSet.TokenDefinitions.Add(new("Separator")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/DateTimeFormatSyntaxHighlighting.Separator", Brushes.Magenta),
            Pattern = SeparatorPattern,
        });
        definitionSet.TokenDefinitions.Add(new("Time Zone")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/DateTimeFormatSyntaxHighlighting.TimeZone", Brushes.Navy),
            Pattern = TimeZonePattern,
        });
        definitionSet.TokenDefinitions.Add(new("Time Zone Offset")
        {
            Foreground = app.FindResourceOrDefault<IBrush>("Brush/DateTimeFormatSyntaxHighlighting.TimeZoneOffset", Brushes.Navy),
            Pattern = TimeZoneOffsetPattern,
        });

        // complete
        return definitionSet;
    }
}