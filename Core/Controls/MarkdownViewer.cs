using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Styling;
using Markdown.Avalonia;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Viewer to show document in Markdown.
/// </summary>
public class MarkdownViewer : TemplatedControl
{
    /// <summary>
    /// Define <see cref="HorizontalScrollBarVisibility"/> property.
    /// </summary>
    public static readonly StyledProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty = AvaloniaProperty.Register<MarkdownViewer, ScrollBarVisibility>(nameof(HorizontalScrollBarVisibility), ScrollBarVisibility.Auto);
    /// <summary>
    /// Define <see cref="Source"/> property.
    /// </summary>
    public static readonly StyledProperty<Uri?> SourceProperty = AvaloniaProperty.Register<MarkdownViewer, Uri?>(nameof(Source));
    /// <summary>
    /// Define <see cref="VerticalScrollBarVisibility"/> property.
    /// </summary>
    public static readonly StyledProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty = AvaloniaProperty.Register<MarkdownViewer, ScrollBarVisibility>(nameof(VerticalScrollBarVisibility), ScrollBarVisibility.Auto);


    // Fields.
    MarkdownScrollViewer? presenter;


    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        this.presenter = e.NameScope.Find<MarkdownScrollViewer>("PART_MarkdownPresenter");
        if (this.presenter != null)
        {
            // [Workaround] Need to use separate styles for each MarkdownScrollViewer to prevent crashing after changing theme mode.
            var baseUri = new Uri($"avares://{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}/");
            this.presenter.MarkdownStyle = new StyleInclude(baseUri)
            {
                Source = new(baseUri, "/Themes/Base-Styles-Markdown.axaml"),
            };
        }
    }


    /// <summary>
    /// Get or set visibility of horizontal scroll bar.
    /// </summary>
    public ScrollBarVisibility HorizontalScrollBarVisibility
    {
        get => this.GetValue(HorizontalScrollBarVisibilityProperty);
        set => this.SetValue(HorizontalScrollBarVisibilityProperty, value);
    }


    /// <summary>
    /// Get or set URI of document.
    /// </summary>
    public Uri? Source
    {
        get => this.GetValue(SourceProperty);
        set => this.SetValue(SourceProperty, value);
    }


    /// <summary>
    /// Get or set visibility of vertical scroll bar.
    /// </summary>
    public ScrollBarVisibility VerticalScrollBarVisibility
    {
        get => this.GetValue(VerticalScrollBarVisibilityProperty);
        set => this.SetValue(VerticalScrollBarVisibilityProperty, value);
    }
}