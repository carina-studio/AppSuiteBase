using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to show document.
/// </summary>
class DocumentViewerDialogImpl : Dialog
{
    // Static fields.
    static readonly StyledProperty<FontFamily> DocumentFontFamilyProperty = AvaloniaProperty.Register<DocumentViewerDialogImpl, FontFamily>(nameof(DocumentFontFamily), FontFamily.Default);
    static readonly StyledProperty<Uri?> DocumentUriProperty = AvaloniaProperty.Register<DocumentViewerDialogImpl, Uri?>("DocumentUri");
    public static readonly StyledProperty<string?> MessageProperty = AvaloniaProperty.Register<DocumentViewerDialogImpl, string?>(nameof(Message));


    // Constructor.
    public DocumentViewerDialogImpl()
    {
        AvaloniaXamlLoader.Load(this);
        this.Title = this.Application.Name;
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
    /// Get or set message to be shown.
    /// </summary>
    public string? Message
    {
        get => this.GetValue(MessageProperty);
        set => this.SetValue(MessageProperty, value);
    }


    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (this.DocumentSource is null)
            this.SynchronizationContext.Post(this.Close);
    }


    /// <inheritdoc/>
    protected override void OnOpening(EventArgs e)
    {
        base.OnOpening(e);
        this.DocumentSource?.Let(source =>
        {
            source.SetToCurrentCulture();
            this.SetValue(DocumentUriProperty, source.Uri);
        });
    }
}
