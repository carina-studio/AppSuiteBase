using CarinaStudio.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CarinaStudio.AppSuite.Controls.Highlighting;

/// <summary>
/// Span of syntax highlighting.
/// </summary>
public class SyntaxHighlightingSpan : SyntaxHighlightingDefinition
{
    // Fields.
    Regex? endPattern;
    Regex? startPattern;


    /// <summary>
    /// Initialize new <see cref="SyntaxHighlightingSpan"/> instance.
    /// </summary>
    /// <param name="name">Name.</param>
    public SyntaxHighlightingSpan(string? name = null) : base(name)
    { }


    // Check equality of two patterns.
    static bool ArePatternsEqual(Regex? x, Regex? y)
    {
        if (x is null)
            return y is null;
        if (y is null)
            return false;
        return x.ToString() == y.ToString() && x.Options == y.Options;
    }


    /// <summary>
    /// Get or set end pattern of the span.
    /// </summary>
    public Regex? EndPattern
    {
        get => this.endPattern;
        set
        {
            if (ArePatternsEqual(this.endPattern, value))
                return;
            this.endPattern = value;
            this.Validate();
            this.OnPropertyChanged(nameof(EndPattern));
        }
    }


    /// <inheritdoc/>
    protected override bool OnValidate() =>
        base.OnValidate()
        && this.endPattern is not null
        && this.startPattern is not null;


    /// <summary>
    /// Get or set start pattern of the span.
    /// </summary>
    public Regex? StartPattern
    {
        get => this.startPattern;
        set
        {
            if (ArePatternsEqual(this.startPattern, value))
                return;
            this.startPattern = value;
            this.Validate();
            this.OnPropertyChanged(nameof(StartPattern));
        }
    }


    /// <summary>
    /// Get list of definitions of tokens inside the span.
    /// </summary>
    public IList<SyntaxHighlightingToken> TokenDefinitions { get; } = new ObservableList<SyntaxHighlightingToken>();


    /// <inheritdoc/>
    public override string ToString() =>
        string.IsNullOrEmpty(this.Name)
            ? $"{{{this.startPattern?.ToString()}}}-{{{this.endPattern?.ToString()}}}"
            : $"[{this.Name}]{{{this.startPattern?.ToString()}}}-{{{this.endPattern?.ToString()}}}";
}