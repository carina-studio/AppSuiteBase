using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Styling;
using CarinaStudio.Windows.Input;
using ColorTextBlock.Avalonia;
using Markdown.Avalonia;
using System;
using System.Linq;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Viewer to show document in Markdown.
/// </summary>
public unsafe class MarkdownViewer : TemplatedControl
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
    ScrollViewer? scrollViewer;


    // Static initializer.
    static MarkdownViewer()
    {
        var handCursor = new Cursor(StandardCursorType.Hand);
        CHyperlink.IsUnderlineProperty.Changed.Subscribe(e =>
        {
            if (e.Sender is CHyperlink hyperlink)
            {
                hyperlink.FindLogicalAncestorOfType<Control>()?.Let(control =>
                    control.Cursor = e.NewValue.GetValueOrDefault() ? handCursor : Cursor.Default);
            }
        });
    }


    /// <summary>
    /// Initialize new <see cref="MarkdownViewer"/> instance.
    /// </summary>
    public MarkdownViewer()
    { 
        this.GetObservable(HorizontalScrollBarVisibilityProperty).Subscribe(visibility =>
        {
            if (this.scrollViewer != null)
                this.scrollViewer.HorizontalScrollBarVisibility = visibility;
        });
        this.GetObservable(PaddingProperty).Subscribe(padding =>
        {
            (this.scrollViewer?.Content as Control)?.Let(it =>
                it.Margin = padding);
        });
        this.GetObservable(VerticalScrollBarVisibilityProperty).Subscribe(visibility =>
        {
            if (this.scrollViewer != null)
                this.scrollViewer.VerticalScrollBarVisibility = visibility;
        });
    }


    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        this.presenter = e.NameScope.Find<MarkdownScrollViewer>("PART_MarkdownPresenter");
        if (this.presenter is not null)
        {
            // [Workaround] Need to use separate styles for each MarkdownScrollViewer to prevent crashing after changing theme mode.
            var baseUri = new Uri($"avares://{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}/");
            if (this.presenter.Engine is IMarkdownEngine markdownEngine)
                markdownEngine.HyperlinkCommand = new Command<object?>(this.OnHyperlinkClicked);
#pragma warning disable IL2026
            this.presenter.MarkdownStyle = new StyleInclude(baseUri)
            {
                Source = new(baseUri, "/Themes/Base-Styles-Markdown.axaml"),
            };
#pragma warning restore IL2026
            this.presenter.GetObservable(MarkdownScrollViewer.MarkdownProperty).Subscribe(markdown =>
            {
                var startsWithHeading = false;
                if (!string.IsNullOrEmpty(markdown))
                {
                    var length = markdown.Length;
                    fixed (char* p = markdown)
                    {
                        var cPtr = p;
                        for (var i = 0; i < length; ++i)
                        {
                            var c = *(cPtr++);
                            if (char.IsWhiteSpace(c))
                                continue;
                            if (c == '#')
                                startsWithHeading = true;
                            break;
                        }
                    }
                }
                if (!startsWithHeading)
                    this.PseudoClasses.Remove(":startsWithHeading");
                else if (!this.PseudoClasses.Contains(":startsWithHeading"))
                    this.PseudoClasses.Add(":startsWithHeading");
            });
            var fieldInfo = typeof(MarkdownScrollViewer).GetField("_viewer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            this.scrollViewer = fieldInfo?.GetValue(this.presenter) as ScrollViewer;
        }
        if (this.scrollViewer is not null)
        {
            this.scrollViewer.GetObservable(ScrollViewer.ContentProperty).Subscribe(content =>
            {
                if (content is Control control)
                    control.Margin = Padding;
            });
            this.scrollViewer.HorizontalScrollBarVisibility = this.GetValue(HorizontalScrollBarVisibilityProperty);
            this.scrollViewer.VerticalScrollBarVisibility = this.GetValue(VerticalScrollBarVisibilityProperty);
        }
    }


    // Called when user clicked a hyperlink.
    void OnHyperlinkClicked(object? parameter)
    {
        // check target
        var target = parameter as string;
        if (string.IsNullOrEmpty(target))
            return;
        
        // open link directly
        if (target[0] != '#')
        {
            Platform.OpenLink(target);
            return;
        }
        
        // scroll to heading text
        (this.scrollViewer?.Content as Panel)?.Let(panel =>
        {
            target = target[1..];
            var headingControl = panel.Children.Let(children =>
            {
                foreach (var child in children)
                {
                    if (child is CTextBlock cTextBlock && cTextBlock.Classes?.FirstOrDefault(it => it.StartsWith("Heading")) is not null)
                    {
                        var anchorName = cTextBlock.Text.ToLower().Replace(' ', '-');
                        if (anchorName == target || $"-{anchorName}" == target /* [Workaround] Extra '-' may be added by markdown editor */)
                            return cTextBlock;
                    }
                }
                return null;
            });
            if (headingControl is not null)
            {
                var bounds = headingControl.Bounds.Inflate(headingControl.Margin);
                this.scrollViewer!.SmoothScrollTo(new(0, bounds.Y + this.scrollViewer!.Padding.Top));
            }
        });
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