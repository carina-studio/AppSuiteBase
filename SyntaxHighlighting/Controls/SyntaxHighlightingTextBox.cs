using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using CarinaStudio.AppSuite.Controls.Highlighting;
using CarinaStudio.AppSuite.Controls.Presenters;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="Avalonia.Controls.TextBox"/> which supports syntax highlighting.
/// </summary>
public class SyntaxHighlightingTextBox : TextBox
{
    /// <summary>
    /// Property of <see cref="DefinitionSet"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlightingTextBox, SyntaxHighlightingDefinitionSet?> DefinitionSetProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlightingTextBox, SyntaxHighlightingDefinitionSet?>(nameof(DefinitionSet), t => t.definitionSet, (t, ds) => t.DefinitionSet = ds);
    /// <summary>
    /// Property of <see cref="IsMaxTokenCountReached"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlightingTextBox, bool> IsMaxTokenCountReachedProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlightingTextBox, bool>(nameof(IsMaxTokenCountReached), t => t.IsMaxTokenCountReached);
    /// <summary>
    /// Property of <see cref="MaxTokenCount"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlightingTextBox, int> MaxTokenCountProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlightingTextBox, int>(nameof(MaxTokenCount), t => t.maxTokenCount, (t, count) => t.MaxTokenCount = count);


    // Fields.
    SyntaxHighlightingDefinitionSet? definitionSet;
    int maxTokenCount = -1;
    SyntaxHighlightingTextPresenter? textPresenter;


    /// <summary>
    /// Initialize new <see cref="SyntaxHighlightingTextBox"/> instance.
    /// </summary>
    public SyntaxHighlightingTextBox()
    {
        this.PseudoClasses.Add(":syntaxHighlighted");
        this.PseudoClasses.Add(":syntaxHighlightingTextBox");
        this.PastingFromClipboard += (_, e) =>
        {
            TopLevel.GetTopLevel(this)?.Clipboard?.LetAsync(async clipboard =>
            {
                this.OnPastingFromClipboard(await clipboard.TryGetTextAsync());
            });
            e.Handled = true;
        };
    }


    /// <summary>
    /// Get or set syntax highlighting definition set.
    /// </summary>
    public SyntaxHighlightingDefinitionSet? DefinitionSet
    {
        get => this.definitionSet;
        set
        {
            this.VerifyAccess();
            if (this.definitionSet == value)
                return;
            this.SetAndRaise(DefinitionSetProperty, ref this.definitionSet, value);
            if (this.textPresenter is not null)
                this.textPresenter.DefinitionSet = this.definitionSet;
        }
    }
    
    
    /**
     * Check whether maximum number of token to be highlighted reached or not.
     */
    public bool IsMaxTokenCountReached => this.textPresenter?.IsMaxTokenCountReached ?? false;
    
    
    /// <summary>
    /// Get or set maximum number of token should be highlighted. Negative value if there is no limitation.
    /// </summary>
    public int MaxTokenCount
    {
        get => this.maxTokenCount;
        set
        {
            this.VerifyAccess();
            if (this.maxTokenCount == value)
                return;
            this.SetAndRaise(MaxTokenCountProperty, ref this.maxTokenCount, value);
            if (this.textPresenter is not null)
                this.textPresenter.MaxTokenCount = this.maxTokenCount;
        }
    }


    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (this.textPresenter is not null && this.textPresenter.IsMaxTokenCountReached)
            RaisePropertyChanged(IsMaxTokenCountReachedProperty, true, false);
        base.OnApplyTemplate(e);
        this.textPresenter = e.NameScope.Find<SyntaxHighlightingTextPresenter>("PART_TextPresenter");
        if (this.textPresenter is not null)
        {
            this.textPresenter.DefinitionSet = this.definitionSet;
            this.textPresenter.MaxTokenCount = this.maxTokenCount;
            this.textPresenter.PropertyChanged += (_, e) =>
            {
                if (e.Property == SyntaxHighlightingTextPresenter.IsMaxTokenCountReachedProperty)
                    RaisePropertyChanged(IsMaxTokenCountReachedProperty, (bool)e.OldValue!, (bool)e.NewValue!);
            };
            if (this.textPresenter.IsMaxTokenCountReached)
                this.RaisePropertyChanged(IsMaxTokenCountReachedProperty, false, true);
        }
    }
    
    
    /// <summary>
    /// Called when pasting text from clipboard
    /// </summary>
    /// <param name="text">The text from clipboard.</param>
    protected virtual void OnPastingFromClipboard(string? text)
    {
        if (text is null)
            return;
        this.SelectedText = this.AcceptsReturn 
            ? text
            : text.RemoveLineBreaks();
    }


    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(TextBox);
}