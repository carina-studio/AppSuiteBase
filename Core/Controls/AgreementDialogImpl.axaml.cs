using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CarinaStudio.Collections;
using CarinaStudio.Threading;
using System;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to show document of agreement.
/// </summary>
class AgreementDialogImpl : Dialog
{
    /// <summary>
    /// Converter to convert from application culture to string.
    /// </summary>
    public static readonly IValueConverter CultureConverter = new Converters.EnumConverter(AppSuiteApplication.CurrentOrNull, typeof(ApplicationCulture));


    // Static fields.
    static readonly StyledProperty<ApplicationCulture> CultureProperty = AvaloniaProperty.Register<AgreementDialogImpl, ApplicationCulture>("Culture", ApplicationCulture.System);
    static readonly StyledProperty<IList<ApplicationCulture>> CulturesProperty = AvaloniaProperty.Register<AgreementDialogImpl, IList<ApplicationCulture>>("Cultures", Array.Empty<ApplicationCulture>());
    static readonly StyledProperty<FontFamily> DocumentFontFamilyProperty = AvaloniaProperty.Register<AgreementDialogImpl, FontFamily>(nameof(DocumentFontFamily), FontFamily.Default);
    static readonly StyledProperty<Uri?> DocumentUriProperty = AvaloniaProperty.Register<AgreementDialogImpl, Uri?>("DocumentUri");
    public static readonly StyledProperty<bool> IsAgreedBeforeProperty = AvaloniaProperty.Register<AgreementDialogImpl, bool>(nameof(IsAgreedBefore));
    public static readonly StyledProperty<string?> MessageProperty = AvaloniaProperty.Register<AgreementDialogImpl, string?>(nameof(Message));


    // Fields.
    bool hasResult;


    // Constructor.
    public AgreementDialogImpl()
    {
        AvaloniaXamlLoader.Load(this);
        this.GetObservable(CultureProperty).Subscribe(culture =>
        {
            if (this.DocumentSource != null)
            {
                this.DocumentSource.Culture = culture;
                this.SetValue(DocumentUriProperty, this.DocumentSource.Uri);
            }
        });
        this.Title = this.Application.Name;
    }


    /// <summary>
    /// Agree the user agreement.
    /// </summary>
    public void Agree()
    {
        this.hasResult = true;
        this.Close(AgreementDialogResult.Agreed);
    }


    /// <summary>
    /// Decline the user agreement.
    /// </summary>
    public void Decline()
    {
        this.hasResult = true;
        this.Close(AgreementDialogResult.Declined);
    }


    /// <summary>
    /// Get or set font family for showing document.
    /// </summary>
    public FontFamily DocumentFontFamily
    {
        get => this.GetValue(DocumentFontFamilyProperty);
        set => this.SetValue(DocumentFontFamilyProperty, value);
    }


    /// <summary>
    /// Get or set source of document to be shown.
    /// </summary>
    public DocumentSource? DocumentSource { get; set; }


    /// <summary>
    /// Get or set whether agreement was agreed before or not.
    /// </summary>
    public bool IsAgreedBefore
    {
        get => this.GetValue(IsAgreedBeforeProperty);
        set => this.SetValue(IsAgreedBeforeProperty, value);
    }


    /// <summary>
    /// Get or set message to be shown.
    /// </summary>
    public string? Message
    {
        get => this.GetValue(MessageProperty);
        set => this.SetValue(MessageProperty, value);
    }


    // Called when closing.
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!this.hasResult && !this.IsAgreedBefore)
            e.Cancel = true;
        base.OnClosing(e);
    }


    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        var source = this.DocumentSource;
        if (source is not null)
        {
            // setup initial focus
            this.SynchronizationContext.Post(() =>
            {
                if (!this.IsAgreedBefore)
                    this.Get<Button>("agreeButton").Focus();
            });
        }
        else
            this.SynchronizationContext.Post(this.Decline);
    }


    /// <inheritdoc/>
    protected override void OnOpening(EventArgs e)
    {
        base.OnOpening(e);
        this.DocumentSource?.Let(source =>
        {
            // setup cultures
            var cultures = source.SupportedCultures;
            this.SetValue(CulturesProperty, cultures);
            if (cultures.IsNotEmpty())
            {
                if (cultures.Contains(source.Culture))
                    this.SetValue(CultureProperty, source.Culture);
                else
                    this.SetValue(CultureProperty, cultures[0]);
            }
            else
                this.Get<ComboBox>("cultureComboBox").IsVisible = false;
            
            // setup document Uri
            this.SetValue(DocumentUriProperty, source.Uri);
        });
    }
}
