using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Window to show document.
/// </summary>
public class DocumentViewerWindow : Dialog
{
    /// <summary>
    /// Define <see cref="Message"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> MessageProperty = AvaloniaProperty.Register<DocumentViewerWindow, string?>(nameof(Message));
    
    
    // Static fields.
    static readonly StyledProperty<FontFamily> DocumentFontFamilyProperty = AvaloniaProperty.Register<DocumentViewerWindow, FontFamily>(nameof(DocumentFontFamily), FontFamily.Default);
    static readonly StyledProperty<Uri?> DocumentUriProperty = AvaloniaProperty.Register<DocumentViewerWindow, Uri?>("DocumentUri");


    /// <summary>
    /// Initialize new <see cref="DocumentViewerWindow"/> instance.
    /// </summary>
    public DocumentViewerWindow()
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
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public DocumentSource? DocumentSource { get; set; }


    /// <summary>
    /// Get or set message to be shown.
    /// </summary>
    public string? Message
    {
        get => this.GetValue(MessageProperty);
        set => this.SetValue(MessageProperty, value);
    }
    
    
    // Called when application strings updated.
    void OnApplicationStringsUpdated(object? sender, EventArgs e)
    {
        this.DocumentSource?.Let(source =>
        {
            source.SetToCurrentCulture();
            this.SetValue(DocumentUriProperty, source.Uri);
        });
    }


    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e)
    {
        this.Application.StringsUpdated -= this.OnApplicationStringsUpdated;
        base.OnClosed(e);
    }


    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (this.DocumentSource is null)
            this.SynchronizationContext.Post(this.Close);
        else
            this.Application.StringsUpdated += this.OnApplicationStringsUpdated;
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
