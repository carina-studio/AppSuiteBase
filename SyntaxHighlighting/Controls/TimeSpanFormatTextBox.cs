using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using CarinaStudio.AppSuite.Controls.Highlighting;
using CarinaStudio.Controls;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="Avalonia.Controls.TextBox"/> which allows entering format of time span.
/// </summary>
public class TimeSpanFormatTextBox : ObjectTextBox<string>
{
    /// <summary>
    /// Property of <see cref="IsSyntaxHighlightingEnabled"/>.
    /// </summary>
    public static readonly DirectProperty<TimeSpanFormatTextBox, bool> IsSyntaxHighlightingEnabledProperty = AvaloniaProperty.RegisterDirect<TimeSpanFormatTextBox, bool>(nameof(IsSyntaxHighlightingEnabled), tb => tb.isSyntaxHighlightingEnabled, (tb, e) => tb.IsSyntaxHighlightingEnabled = e);
    /// <summary>
    /// Property of <see cref="Object"/>.
    /// </summary>
    public static new readonly DirectProperty<TimeSpanFormatTextBox, string?> ObjectProperty = AvaloniaProperty.RegisterDirect<TimeSpanFormatTextBox, string?>(nameof(Object), t => t.Object, (t, o) => t.Object = o);

    
    // Fields.
    bool isSyntaxHighlightingEnabled = true;
    TextPresenter? textPresenter;


    /// <summary>
    /// Initialize new <see cref="TimeSpanFormatTextBox"/> instance.
    /// </summary>
    public TimeSpanFormatTextBox()
    {
        SyntaxHighlighting.VerifyInitialization();
        this.PseudoClasses.Add(":syntaxHighlighted");
        this.PseudoClasses.Add(":timeSpanFormatTextBox");
        this.Bind(WatermarkProperty, this.GetResourceObservable("String/TimeSpanFormatTextBox.Watermark"));
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
                    IAppSuiteApplication.CurrentOrNull?.Let(app =>
                        shTextPresenter.DefinitionSet = TimeSpanFormatSyntaxHighlighting.CreateDefinitionSet(app));
                }
                else
                    shTextPresenter.DefinitionSet = null;
            }
        }
    }
    
    
    /// <inheritdoc/>
    public override string? Object
    {
        get => (string?)((ObjectTextBox)this).Object;
        set => ((ObjectTextBox)this).Object = value;
    }


    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        this.textPresenter = e.NameScope.Find<TextPresenter>("PART_TextPresenter");
        if (this.isSyntaxHighlightingEnabled && textPresenter is Presenters.SyntaxHighlightingTextPresenter shTextPresenter)
        {
            IAppSuiteApplication.CurrentOrNull?.Let(app =>
                shTextPresenter.DefinitionSet = TimeSpanFormatSyntaxHighlighting.CreateDefinitionSet(app));
        }
    }


    /// <inheritdoc/>
    protected override void RaiseObjectChanged(string? oldValue, string? newValue) =>
        this.RaisePropertyChanged(ObjectProperty, oldValue, newValue);


    /// <inheritdoc/>
    protected override bool TryConvertToObject(string text, out string? obj)
    {
        try
        {
            _ = TimeSpan.Zero.ToString(text);
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