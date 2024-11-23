using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using CarinaStudio.Animation;
using CarinaStudio.Threading;
using System;
using Avalonia.Controls.Presenters;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="ScrollViewer"/> which is designed for tool bar.
/// </summary>
public class ToolBarScrollViewer : ScrollViewer
{
    // Fields.
    ScrollContentPresenter? contentPresenter;
    readonly ScheduledAction correctOffsetAction;
    VectorRenderingAnimator? offsetAnimator;
    Control? scrollDownButton;
    Control? scrollLeftButton;
    Control? scrollRightButton;
    Control? scrollUpButton;
    TopLevel? topLevel;


    /// <summary>
    /// Initialize new <see cref="ToolBarScrollViewer"/>.
    /// </summary>
    public ToolBarScrollViewer()
    {
        this.correctOffsetAction = new(this.CorrectOffset);
    }
    
    
    // Correct the offset.
    void CorrectOffset()
    {
        var extent = this.Extent;
        var viewport = this.Viewport;
        var offset = this.Offset;
        if (offset.X + viewport.Width > extent.Width && offset.X > 0)
        {
            if (this.offsetAnimator != null)
            {
                this.offsetAnimator.Cancel();
                this.offsetAnimator = null;
            }
            offset = new Vector(extent.Width - viewport.Width, offset.Y);
            this.Offset = offset;
        }
        if (offset.Y + viewport.Height > extent.Height && offset.Y > 0)
        {
            if (this.offsetAnimator != null)
            {
                this.offsetAnimator.Cancel();
                this.offsetAnimator = null;
            }
            this.Offset = new Vector(offset.X, extent.Height - viewport.Height);
        }
    }


    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        this.contentPresenter = e.NameScope.Find<ScrollContentPresenter>("PART_ContentPresenter")?.Also(it =>
        {
            it.CanHorizontallyScroll = this.HorizontalScrollBarVisibility == ScrollBarVisibility.Visible;
            it.CanVerticallyScroll = this.VerticalScrollBarVisibility == ScrollBarVisibility.Visible;
            //it.OpacityMask = this.opacityMackBrush;
        });
        this.scrollDownButton = e.NameScope.Find<Control>("PART_ScrollDownButton");
        this.scrollLeftButton = e.NameScope.Find<Control>("PART_ScrollLeftButton");
        this.scrollRightButton = e.NameScope.Find<Control>("PART_ScrollRightButton");
        this.scrollUpButton = e.NameScope.Find<Control>("PART_ScrollUpButton");
    }
    
    
    /*
    /// <inheritdoc/>
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        this.maxEdgeFadingSize = this.FindResourceOrDefault("Double/ToolBarScrollViewer.MaxEdgeFadingSize", 20.0);
    }
    */


    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.topLevel = TopLevel.GetTopLevel(this);
    }


    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        this.topLevel = null;
        base.OnDetachedFromVisualTree(e);
    }


    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var property = change.Property;
        if (property == ExtentProperty || property == ViewportProperty)
            this.correctOffsetAction.Schedule();
        else if (property == HorizontalScrollBarVisibilityProperty)
        {
            if (this.contentPresenter is not null)
                this.contentPresenter.CanHorizontallyScroll = (ScrollBarVisibility)change.NewValue! == ScrollBarVisibility.Visible;
        }
        else if (property == VerticalScrollBarVisibilityProperty)
        {
            if (this.contentPresenter is not null)
                this.contentPresenter.CanVerticallyScroll = (ScrollBarVisibility)change.NewValue! == ScrollBarVisibility.Visible;
        }
    }


    // Scroll by given offset.
    void ScrollBy(double offsetX, double offsetY)
    {
        // check state
        this.VerifyAccess();
        if (this.topLevel is null)
            return;

        // get current offset
        var currentOffset = this.offsetAnimator?.EndValue ?? this.Offset;

        // cancel current animation
        this.offsetAnimator?.Cancel();
        
        // prevent scrolling
        if (this.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled)
            offsetX = 0;
        if (this.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled)
            offsetY = 0;

        // calculate target offset
        var targetOffsetX = currentOffset.X + offsetX;
        var targetOffsetY = currentOffset.Y + offsetY;
        var extent = this.Extent;
        var viewport = this.Viewport;
        if (targetOffsetX < 0)
            targetOffsetX = 0;
        else if (targetOffsetX + viewport.Width > extent.Width)
            targetOffsetX = extent.Width - viewport.Width;
        if (targetOffsetY < 0)
            targetOffsetY = 0;
        else if (targetOffsetY + viewport.Height > extent.Height)
            targetOffsetY = extent.Height - viewport.Height;

        // animate offset
        var duration = Application.CurrentOrNull?.FindResource("TimeSpan/Animation.Fast").Let(it =>
        {
            if (it is TimeSpan timeSpan)
                return timeSpan;
            return TimeSpan.FromMilliseconds(250);
        }) ?? TimeSpan.FromMilliseconds(250);
        this.offsetAnimator = new VectorRenderingAnimator(this.topLevel, currentOffset, new Vector(targetOffsetX, targetOffsetY)).Also(it =>
        {
            it.Completed += (_, _) => this.offsetAnimator = null;
            it.Duration = duration;
            it.Interpolator = Interpolators.Deceleration;
            it.ProgressChanged += (_, _) => this.Offset = it.Value;
            it.Start();
        });
    }


    /// <summary>
    /// Scroll down.
    /// </summary>
    public void ScrollDown() => ScrollBy(0, 100);


    /// <summary>
    /// Scroll left.
    /// </summary>
    public void ScrollLeft() => ScrollBy(-100, 0);


    /// <summary>
    /// Scroll given child visual into viewport.
    /// </summary>
    /// <param name="visual">Child visual.</param>
    public void ScrollIntoView(Visual visual)
    {
        // check state
        this.VerifyAccess();
        if (visual == this)
            return;

        // calculate relative bounds
        var bounds = visual.Bounds;
        var x = bounds.Left;
        var y = bounds.Top;
        var parentVisual = visual.GetVisualParent();
        while (parentVisual != null)
        {
            var parentBounds = visual.Bounds;
            x += parentBounds.Left;
            y += parentBounds.Top;
            parentVisual = parentVisual.GetVisualParent();
        }

        // scroll into view
        var leftMargin = this.scrollLeftButton?.Bounds.Right ?? 0;
        var rightMargin = this.scrollRightButton?.Bounds.Left ?? this.Bounds.Width;
        var topMargin = this.scrollUpButton?.Bounds.Bottom ?? 0;
        var bottomMargin = this.scrollDownButton?.Bounds.Top ?? this.Bounds.Height;
        var scrollX = x < leftMargin ? x - leftMargin : x + bounds.Width - rightMargin;
        var scrollY = y < topMargin ? y - topMargin : y + bounds.Height - bottomMargin;
        this.ScrollBy(scrollX, scrollY);
    }


    /// <summary>
    /// Scroll right.
    /// </summary>
    public void ScrollRight() => ScrollBy(100, 0);


    /// <summary>
    /// Scroll up.
    /// </summary>
    public void ScrollUp() => ScrollBy(0, -100);


    /// <inheritdox/>
    protected override Type StyleKeyOverride => typeof(ToolBarScrollViewer);
}