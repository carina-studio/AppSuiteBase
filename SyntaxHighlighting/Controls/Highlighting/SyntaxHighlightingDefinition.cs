using Avalonia;
using Avalonia.Media;
using System;
using System.ComponentModel;

namespace CarinaStudio.AppSuite.Controls.Highlighting;

/// <summary>
/// Base class of definition of syntax highlighting.
/// </summary>
public abstract class SyntaxHighlightingDefinition : INotifyPropertyChanged
{
    // Fields.
    IBrush? background;
    FontFamily? fontFamily;
    double fontSize = double.NaN;
    FontStyle? fontStyle;
    FontWeight? fontWeight;
    IBrush? foreground;
    bool isValid;
    TextDecorationCollection? textDecorations;


    /// <summary>
    /// Initialize new <see cref="SyntaxHighlightingDefinition"/> instance.
    /// </summary>
    /// <param name="name">Name.</param>
    protected SyntaxHighlightingDefinition(string? name = null)
    { 
        this.Name = name;
    }


    // Check whether two font sizes are equalivent or not.
    static bool AreFontSizesEqual(double x, double y)
    {
        if (double.IsNaN(x))
            return double.IsNaN(y);
        if (double.IsNaN(y))
            return false;
        return Math.Abs(x - y) <= 0.01;
    }


    /// <summary>
    /// Get or set background brush of the definition.
    /// </summary>
    public IBrush? Background
    {
        get => this.background;
        set
        {
            if (this.background == value)
                return;
            (this.background as AvaloniaObject)?.Let(it => it.PropertyChanged -= this.OnBrushPropertyChanged);
            (value as AvaloniaObject)?.Let(it => it.PropertyChanged += this.OnBrushPropertyChanged);
            this.background = value;
            this.Validate();
            this.OnPropertyChanged(nameof(Background));
        }
    }


    /// <summary>
    /// Get or set font family of the definition.
    /// </summary>
    public FontFamily? FontFamily
    {
        get => this.fontFamily;
        set
        {
            if (this.fontFamily?.Equals(value) ?? value is null)
                return;
            this.fontFamily = value;
            this.Validate();
            this.OnPropertyChanged(nameof(FontFamily));
        }
    }


    /// <summary>
    /// Get or set font size of the definition.
    /// </summary>
    public double FontSize
    {
        get => this.fontSize;
        set
        {
            if (double.IsInfinity(value) || value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (AreFontSizesEqual(this.fontSize, value))
                return;
            this.fontSize = value;
            this.Validate();
            this.OnPropertyChanged(nameof(FontSize));
        }
    }


    /// <summary>
    /// Get or set font style of the definition.
    /// </summary>
    public FontStyle? FontStyle
    {
        get => this.fontStyle;
        set
        {
            if (this.fontStyle == value)
                return;
            this.fontStyle = value;
            this.Validate();
            this.OnPropertyChanged(nameof(FontStyle));
        }
    }


    /// <summary>
    /// Get or set font weight of the definition.
    /// </summary>
    public FontWeight? FontWeight
    {
        get => this.fontWeight;
        set
        {
            if (this.fontWeight == value)
                return;
            this.fontWeight = value;
            this.Validate();
            this.OnPropertyChanged(nameof(FontWeight));
        }
    }


    /// <summary>
    /// Get or set foreground brush of the definition.
    /// </summary>
    public IBrush? Foreground
    {
        get => this.foreground;
        set
        {
            if (this.foreground == value)
                return;
            (this.foreground as AvaloniaObject)?.Let(it => it.PropertyChanged -= this.OnBrushPropertyChanged);
            (value as AvaloniaObject)?.Let(it => it.PropertyChanged += this.OnBrushPropertyChanged);
            this.foreground = value;
            this.Validate();
            this.OnPropertyChanged(nameof(Foreground));
        }
    }


    /// <summary>
    /// Check whether the definition is valid or not.
    /// </summary>
    public bool IsValid { get => this.isValid; }


    /// <summary>
    /// Get name of definition.
    /// </summary>
    public string? Name { get; }


    // Called when property of attached brush has been changed.
    void OnBrushPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender == this.background)
            this.OnPropertyChanged(nameof(Background));
        else if (sender == this.foreground)
            this.OnPropertyChanged(nameof(Foreground));
    }


    /// <summary>
    /// Raise <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">Name of changed property.</param>
    protected virtual void OnPropertyChanged(string propertyName) =>
        this.PropertyChanged?.Invoke(this, new(propertyName));
    

    /// <summary>
    /// Called to validate whether the definition is valid or not.
    /// </summary>
    /// <returns>True if the definition is valid.</returns>
    protected virtual bool OnValidate() =>
        this.background is not null
        || this.fontFamily is not null
        || double.IsFinite(this.fontSize)
        || this.fontStyle.HasValue
        || this.fontWeight.HasValue
        || this.foreground is not null
        || this.textDecorations is not null;


    /// <summary>
    /// Raised when property of rule has been changed.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;


    /// <summary>
    /// Get or set text decorations of the definition.
    /// </summary>
    public TextDecorationCollection? TextDecorations
    {
        get => this.textDecorations;
        set
        {
            if (this.textDecorations == value)
                return;
            this.textDecorations = value;
            this.Validate();
            this.OnPropertyChanged(nameof(TextDecorations));
        }
    }


    /// <summary>
    /// Validate whether the definition is valid or not.
    /// </summary>
    protected void Validate()
    {
        if (this.isValid != this.OnValidate())
        {
            this.isValid = !this.isValid;
            this.OnPropertyChanged(nameof(IsValid));
        }
    }
}