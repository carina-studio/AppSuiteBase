using System.Text.RegularExpressions;

namespace CarinaStudio.AppSuite.Controls.Highlighting;

/// <summary>
/// Token of syntax highlighting.
/// </summary>
public class SyntaxHighlightingToken : SyntaxHighlightingDefinition
{
    // Fields.
    Regex? pattern;


    /// <summary>
    /// Initialize new <see cref="SyntaxHighlightingToken"/> instance.
    /// </summary>
    /// <param name="name">Name.</param>
    public SyntaxHighlightingToken(string? name = null) : base(name)
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


    /// <inheritdoc/>
    protected override bool OnValidate() =>
        base.OnValidate()
        && this.pattern is not null;


    /// <summary>
    /// Get or set pattern of the token.
    /// </summary>
    public Regex? Pattern
    {
        get => this.pattern;
        set
        {
            if (ArePatternsEqual(this.pattern, value))
                return;
            this.pattern = value;
            this.Validate();
            this.OnPropertyChanged(nameof(Pattern));
        }
    }


    /// <inheritdoc/>
    public override string ToString() =>
        string.IsNullOrEmpty(this.Name)
            ? $"{{{this.pattern}}}"
            : $"[{this.Name}]{{{this.pattern}}}";
}