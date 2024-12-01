using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CarinaStudio.AppSuite.Controls.Highlighting;
using CarinaStudio.AppSuite.Controls.Presenters;
using CarinaStudio.Controls;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Base class of <see cref="ObjectTextBox"/> which supports syntax highlighting.
/// </summary>
public abstract class SyntaxHighlightingObjectTextBox : ObjectTextBox
{
    /// <summary>
    /// Property of <see cref="IsMaxTokenCountReached"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlightingObjectTextBox, bool> IsMaxTokenCountReachedProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlightingObjectTextBox, bool>(nameof(IsMaxTokenCountReached), t => t.IsMaxTokenCountReached);
    /// <summary>
    /// Property of <see cref="IsSyntaxHighlightingEnabled"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlightingObjectTextBox, bool> IsSyntaxHighlightingEnabledProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlightingObjectTextBox, bool>(nameof(IsSyntaxHighlightingEnabled), tb => tb.isSyntaxHighlightingEnabled, (tb, e) => tb.IsSyntaxHighlightingEnabled = e);
    /// <summary>
    /// Property of <see cref="MaxTokenCount"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlightingObjectTextBox, int> MaxTokenCountProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlightingObjectTextBox, int>(nameof(MaxTokenCount), t => t.maxTokenCount, (t, c) => t.MaxTokenCount = c);
    
    
    // Fields.
    bool isSyntaxHighlightingEnabled = true;
    int maxTokenCount = -1;
    SyntaxHighlightingTextPresenter? textPresenter;
    
    
    /// <summary>
    /// Initialize new <see cref="SyntaxHighlightingObjectTextBox"/> instance.
    /// </summary>
    protected SyntaxHighlightingObjectTextBox()
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
/// Base class of <see cref="ObjectTextBox"/> which supports syntax highlighting.
/// </summary>
/// <typeparam name="T">Type of object.</typeparam>
public abstract class SyntaxHighlightingObjectTextBox<T> : SyntaxHighlightingObjectTextBox where T : class
{
    /// <summary>
    /// Initialize new <see cref="SyntaxHighlightingObjectTextBox{T}"/> instance.
    /// </summary>
    protected SyntaxHighlightingObjectTextBox()
    { }


    /// <inheritdoc/>
    protected sealed override bool CheckObjectEquality(object? x, object? y) =>
        this.CheckObjectEquality(x as T, y as T);


    /// <summary>
    /// Check equality of objects.
    /// </summary>
    /// <param name="x">First object.</param>
    /// <param name="y">Second object.</param>
    /// <returns>True if two objects are equivalent.</returns>
    protected virtual bool CheckObjectEquality(T? x, T? y) => x?.Equals(y) ?? y == null;
    
    
    /// <inheritdoc/>
    protected sealed override string? ConvertToText(object obj) =>
        obj is T t ? this.ConvertToText(t) : null;
	

    /// <summary>
    /// Convert object to text.
    /// </summary>
    /// <param name="obj">Object.</param>
    /// <returns>Converted text.</returns>
    protected virtual string? ConvertToText(T obj) => obj.ToString();
    
    
    /// <summary>
    /// Get or set object.
    /// </summary>
    public new abstract T? Object { get; set; }
    
    
    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ObjectProperty)
            this.RaiseObjectChanged((T?)change.OldValue, (T?)change.NewValue);
        base.OnPropertyChanged(change);
    }
    
    
    /// <summary>
    /// Raise property changed event of <see cref="Object"/>.
    /// </summary>
    /// <param name="oldValue">Old value.</param>
    /// <param name="newValue">New value.</param>
    protected abstract void RaiseObjectChanged(T? oldValue, T? newValue);
    
    
    /// <inheritdoc/>
    protected sealed override bool TryConvertToObject(string text, out object? obj)
    {
        if (this.TryConvertToObject(text, out var t))
        {
            obj = t;
            return true;
        }
        obj = null;
        return false;
    }


    /// <summary>
    /// Try converting text to object.
    /// </summary>
    /// <param name="text">Text.</param>
    /// <param name="obj">Converted object.</param>
    /// <returns>True if conversion succeeded.</returns>
    protected abstract bool TryConvertToObject(string text, out T? obj);
}