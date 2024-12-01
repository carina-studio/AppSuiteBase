using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CarinaStudio.AppSuite.Controls.Highlighting;
using CarinaStudio.AppSuite.Controls.Presenters;
using CarinaStudio.Controls;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Base class of <see cref="ValueTextBox"/> which supports syntax highlighting.
/// </summary>
public abstract class SyntaxHighlightingValueTextBox : ValueTextBox
{
    /// <summary>
    /// Property of <see cref="IsMaxTokenCountReached"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlightingValueTextBox, bool> IsMaxTokenCountReachedProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlightingValueTextBox, bool>(nameof(IsMaxTokenCountReached), t => t.IsMaxTokenCountReached);
    /// <summary>
    /// Property of <see cref="IsSyntaxHighlightingEnabled"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlightingValueTextBox, bool> IsSyntaxHighlightingEnabledProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlightingValueTextBox, bool>(nameof(IsSyntaxHighlightingEnabled), tb => tb.isSyntaxHighlightingEnabled, (tb, e) => tb.IsSyntaxHighlightingEnabled = e);
    /// <summary>
    /// Property of <see cref="MaxTokenCount"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlightingValueTextBox, int> MaxTokenCountProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlightingValueTextBox, int>(nameof(MaxTokenCount), t => t.maxTokenCount, (t, c) => t.MaxTokenCount = c);
    
    
    // Fields.
    bool isSyntaxHighlightingEnabled = true;
    int maxTokenCount = -1;
    SyntaxHighlightingTextPresenter? textPresenter;
    
    
    /// <summary>
    /// Initialize new <see cref="SyntaxHighlightingValueTextBox"/> instance.
    /// </summary>
    protected SyntaxHighlightingValueTextBox()
    {
	    this.PseudoClasses.Add(":syntaxHighlighted");
    }
    
    
    /**
     * Check whether maximum number of token to be highlighted reached or not.
     */
    public bool IsMaxTokenCountReached => this.textPresenter?.IsMaxTokenCountReached ?? false;
    
    
    /// <summary>
    /// Get or set whether syntax highlighting is enabled or not.
    /// </summary>
    public bool IsSyntaxHighlightingEnabled
    {
        get => this.isSyntaxHighlightingEnabled;
        set 
        {
            this.VerifyAccess();
            if (this.isSyntaxHighlightingEnabled == value)
                return;
            this.SetAndRaise(IsSyntaxHighlightingEnabledProperty, ref this.isSyntaxHighlightingEnabled, value);
            if (this.textPresenter is not null)
            {
                this.textPresenter.DefinitionSet = this.isSyntaxHighlightingEnabled
                    ? this.SyntaxHighlightingDefinitionSet
                    : null;
            }
        }
    }
    
    
    /// <summary>
    /// Get or set maximum number of token should be highlighted. Negative value if there is no limitation.
    /// </summary>
    public int MaxTokenCount
    {
        get => this.maxTokenCount;
        set
        {
            if (this.maxTokenCount == value)
                return;
            this.maxTokenCount = value;
            this.SetAndRaise(MaxTokenCountProperty, ref this.maxTokenCount, value);
            if (this.textPresenter is not null)
                this.textPresenter.MaxTokenCount = this.maxTokenCount;
        }
    }
    
    
    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        // detach from previous text presenter
        if (this.textPresenter is not null)
        {
            this.textPresenter.PropertyChanged -= this.OnTextPresenterPropertyChanged;
            if (this.textPresenter.IsMaxTokenCountReached)
                RaisePropertyChanged(IsMaxTokenCountReachedProperty, true, false);
        }
        
        // call base
        base.OnApplyTemplate(e);
        
        // attach to text presenter
        this.textPresenter = e.NameScope.Find<SyntaxHighlightingTextPresenter>("PART_TextPresenter");
        if (this.textPresenter is not null)
        {
            if (this.isSyntaxHighlightingEnabled)
                this.textPresenter.DefinitionSet = this.SyntaxHighlightingDefinitionSet;
            this.textPresenter.MaxTokenCount = this.maxTokenCount;
            this.textPresenter.PropertyChanged += this.OnTextPresenterPropertyChanged;
            if (this.textPresenter.IsMaxTokenCountReached)
                this.RaisePropertyChanged(IsMaxTokenCountReachedProperty, false, true);
        }
    }
    
    
    // Called when property of syntax highlighting text presenter changed.
    void OnTextPresenterPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == SyntaxHighlightingTextPresenter.IsMaxTokenCountReachedProperty)
            this.RaisePropertyChanged(IsMaxTokenCountReachedProperty, (bool)e.OldValue!, (bool)e.NewValue!);
    }
    
    
    /**
     * Get set of definition of syntax highlighting.
     */
    protected abstract SyntaxHighlightingDefinitionSet SyntaxHighlightingDefinitionSet { get; }
}


/// <summary>
/// Base class of <see cref="ValueTextBox"/> which supports syntax highlighting.
/// </summary>
/// <typeparam name="T">Type of value.</typeparam>
public abstract class SyntaxHighlightingValueTextBox<T> : SyntaxHighlightingValueTextBox where T : struct
{
    /// <summary>
	/// Initialize new <see cref="SyntaxHighlightingValueTextBox{T}"/> instance.
	/// </summary>
	protected SyntaxHighlightingValueTextBox()
	{
		this.SetValue(DefaultValueProperty, default(T));
	}


	/// <inheritdoc/>
	protected sealed override bool CheckValueEquality(object? x, object? y)
	{
		var valueX = x is T tx ? (T?)tx : null;
		var valueY = y is T ty ? (T?)ty : null;
		return this.CheckValueEquality(valueX, valueY);
	}


	/// <summary>
	/// Check equality of values.
	/// </summary>
	/// <param name="x">First value.</param>
	/// <param name="y">Second value.</param>
	/// <returns>True if two values are equivalent.</returns>
	protected virtual bool CheckValueEquality(T? x, T? y) => x?.Equals(y) ?? y == null;


	/// <inheritdoc/>
	protected sealed override object CoerceValue(object value) =>
		this.CoerceValue((T)value);


	/// <summary>
	/// Coerce the set value.
	/// </summary>
	/// <param name="value">Set value.</param>
	/// <returns>Coerced value.</returns>
	protected virtual T CoerceValue(T value) => value;


	/// <inheritdoc/>
	protected sealed override string? ConvertToText(object value) =>
		this.ConvertToText((T)value);


	/// <summary>
	/// Convert value to text.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <returns>Converted text.</returns>
	protected virtual string? ConvertToText(T value) => value.ToString();


	/// <inheritdoc cref="ValueTextBox.DefaultValue"/>
	public new T DefaultValue
	{
		get => (T)base.DefaultValue.AsNonNull();
		set => base.DefaultValue = value;
	}


	/// <inheritdoc/>
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == ValueProperty)
			this.RaiseValueChanged((T?)change.OldValue, (T?)change.NewValue);
		base.OnPropertyChanged(change);
	}


	/// <summary>
	/// Raise property changed event of <see cref="Value"/>.
	/// </summary>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	protected abstract void RaiseValueChanged(T? oldValue, T? newValue);


	/// <inheritdoc/>
	protected sealed override bool TryConvertToValue(string text, out object? value)
	{
		if (this.TryConvertToValue(text, out var t))
		{
			value = t;
			return true;
		}
		value = null;
		return false;
	}


	/// <summary>
	/// Try converting text to value.
	/// </summary>
	/// <param name="text">Text.</param>
	/// <param name="value">Converted value.</param>
	/// <returns>True if conversion succeeded.</returns>
	protected abstract bool TryConvertToValue(string text, out T? value);


	/// <summary>
	/// Get or set value.
	/// </summary>
	public new abstract T? Value { get; set; }
}