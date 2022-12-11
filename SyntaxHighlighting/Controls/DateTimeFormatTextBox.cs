using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using CarinaStudio.AppSuite.Controls.Highlighting;
using CarinaStudio.Controls;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="Avalonia.Controls.TextBox"/> which allows entering format of date time.
/// </summary>
public class DateTimeFormatTextBox : ObjectTextBox<string>
{
    /// <summary>
    /// Property of <see cref="IsSyntaxHighlightingEnabled"/>.
    /// </summary>
    public static readonly DirectProperty<DateTimeFormatTextBox, bool> IsSyntaxHighlightingEnabledProperty = AvaloniaProperty.RegisterDirect<DateTimeFormatTextBox, bool>(nameof(IsSyntaxHighlightingEnabled), tb => tb.isSyntaxHighlightingEnabled, (tb, e) => tb.IsSyntaxHighlightingEnabled = e);

    
    // Fields.
    bool isSyntaxHighlightingEnabled = true;
    TextPresenter? textPresenter;


    /// <summary>
    /// Initialize new <see cref="DateTimeFormatTextBox"/> instance.
    /// </summary>
    public DateTimeFormatTextBox()
    {
        SyntaxHighlighting.VerifyInitialization();
        this.PseudoClasses.Add(":syntaxHighlighted");
        this.PseudoClasses.Add(":dateTimeFormatTextBox");
        this.Bind(WatermarkProperty, this.GetResourceObservable("String/DateTimeFormatTextBox.Watermark"));
    }


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
            if (textPresenter is Presenters.SyntaxHighlightingTextPresenter shTextPresenter)
            {
                if (this.isSyntaxHighlightingEnabled)
                {
                    AppSuiteApplication.CurrentOrNull?.Let(app =>
                        shTextPresenter.DefinitionSet = DateTimeFormatSyntaxHighlighting.CreateDefinitionSet(app));
                }
                else
                    shTextPresenter.DefinitionSet = null;
            }
        }
    }


    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        this.textPresenter = e.NameScope.Find<TextPresenter>("PART_TextPresenter");
        if (this.isSyntaxHighlightingEnabled && textPresenter is Presenters.SyntaxHighlightingTextPresenter shTextPresenter)
        {
            AppSuiteApplication.CurrentOrNull?.Let(app =>
                shTextPresenter.DefinitionSet = DateTimeFormatSyntaxHighlighting.CreateDefinitionSet(app));
        }
    }


    /// <inheritdoc/>
    protected override bool TryConvertToObject(string text, out string? obj)
    {
        try
        {
            DateTime.UtcNow.ToString(text);
            obj = text;
            return true;
        }
        catch
        {
            obj = default;
            return false;
        }
    }
}