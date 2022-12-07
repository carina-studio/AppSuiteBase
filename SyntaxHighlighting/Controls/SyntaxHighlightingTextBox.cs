using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using CarinaStudio.AppSuite.Controls.Highlighting;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="Avalonia.Controls.TextBox"/> which supports syntax highlighting.
/// </summary>
public class SyntaxHighlightingTextBox : Avalonia.Controls.TextBox, IStyleable
{
    /// <summary>
    /// Property of <see cref="DefinitionSet"/>.
    /// </summary>
    public static readonly DirectProperty<SyntaxHighlightingTextBox, SyntaxHighlightingDefinitionSet?> DefinitionSetProperty = AvaloniaProperty.RegisterDirect<SyntaxHighlightingTextBox, SyntaxHighlightingDefinitionSet?>(nameof(DefinitionSet), t => t.definitionSet, (t, ds) => t.DefinitionSet = ds);


    // Fields.
    SyntaxHighlightingDefinitionSet? definitionSet;
    Presenters.SyntaxHighlightingTextPresenter? textPresenter;


    /// <summary>
    /// Initialize new <see cref="SyntaxHighlightingTextBox"/> instance.
    /// </summary>
    public SyntaxHighlightingTextBox()
    {
        this.PseudoClasses.Add(":syntaxHighlightingTextBox");
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
            if (this.textPresenter != null)
                this.textPresenter.DefinitionSet = this.definitionSet;
        }
    }


    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        this.textPresenter = e.NameScope.Find<Presenters.SyntaxHighlightingTextPresenter>("PART_TextPresenter");
        if (this.textPresenter != null)
            this.textPresenter.DefinitionSet = this.definitionSet;
    }


    /// <inheritdoc/>
    Type IStyleable.StyleKey => typeof(Avalonia.Controls.TextBox);
}