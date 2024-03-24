using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using CarinaStudio.Animation;
using CarinaStudio.Threading;
using System;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using CarinaStudio.Controls;

namespace CarinaStudio.AppSuite.Controls;

class TabStripScrollViewer : ScrollViewer
{
    // Fields.
    ScrollContentPresenter? contentPresenter;
    Thickness itemsPanelMargin;
    double maxEdgeFadingSize;
    VectorRenderingAnimator? offsetAnimator;
    readonly LinearGradientBrush opacityMackBrush = new LinearGradientBrush().Also(it =>
    {
        it.EndPoint = new(1.0, 0.0, RelativeUnit.Relative);
        it.GradientStops.Add(new(Colors.Transparent, 0.0));
        it.GradientStops.Add(new(Colors.Transparent, 0.0));
        it.GradientStops.Add(new(Colors.Black, 0.0));
        it.GradientStops.Add(new(Colors.Black, 1.0));
        it.GradientStops.Add(new(Colors.Transparent, 0.0));
        it.GradientStops.Add(new(Colors.Transparent, 1.0));
        it.StartPoint = new(0.0, 0.0, RelativeUnit.Relative);
    });
    TopLevel? topLevel;


    // Constructor.
    public TabStripScrollViewer()
    {
        // prepare actions
        var correctOffsetAction = new ScheduledAction(() =>
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
                this.Offset = new Vector(extent.Width - viewport.Width, 0);
            }
        });
        var updateOpacityMaskAction = new ScheduledAction(() =>
        {
            var size = this.Bounds.Size;
            if (size.Width <= 0 || size.Height <= 0)
                return;
            var brush = this.opacityMackBrush;
            if (this.CanHorizontallyScroll)
            {
                if (this.CanVerticallyScroll)
                {
                    brush.GradientStops[1].Offset = 0.0;
                    brush.GradientStops[2].Offset = 0.0;
                    brush.GradientStops[3].Offset = 1.0;
                    brush.GradientStops[4].Offset = 1.0;
                }
                else
                {
                    var offset = this.Offset;
                    var extent = this.Extent;
                    var viewport = this.Viewport;
                    brush.EndPoint = new(1.0, 0.0, RelativeUnit.Relative);
                    if (offset.X > 1)
                    {
                        brush.GradientStops[1].Offset = Math.Min(0.4, this.itemsPanelMargin.Left / size.Width);
                        brush.GradientStops[2].Offset = Math.Min(0.4, (this.itemsPanelMargin.Left + Math.Min(offset.X, this.maxEdgeFadingSize)) / size.Width);
                    }
                    else
                    {
                        brush.GradientStops[1].Offset = 0.0;
                        brush.GradientStops[2].Offset = 0.0;
                    }
                    if (offset.X + viewport.Width < extent.Width - 1)
                    {
                        brush.GradientStops[3].Offset = 1.0 - Math.Min(0.4, (this.itemsPanelMargin.Right + Math.Min(extent.Width - (offset.X + viewport.Width), this.maxEdgeFadingSize)) / size.Width);
                        brush.GradientStops[4].Offset = 1.0 - Math.Min(0.4, this.itemsPanelMargin.Right / size.Width);
                    }
                    else
                    {
                        brush.GradientStops[3].Offset = 1.0;
                        brush.GradientStops[4].Offset = 1.0;
                    }
                }
            }
            else if (this.CanVerticallyScroll)
            {
                var offset = this.Offset;
                var extent = this.Extent;
                var viewport = this.Viewport;
                brush.EndPoint = new(0.0, 1.0, RelativeUnit.Relative);
                if (offset.Y > 1)
                {
                    brush.GradientStops[1].Offset = Math.Min(0.4, this.itemsPanelMargin.Top / size.Height);
                    brush.GradientStops[2].Offset = Math.Min(0.4, (this.itemsPanelMargin.Top + Math.Min(offset.Y, this.maxEdgeFadingSize)) / size.Height);
                }
                else
                {
                    brush.GradientStops[1].Offset = 0.0;
                    brush.GradientStops[2].Offset = 0.0;
                }
                if (offset.Y + viewport.Height < extent.Height - 1)
                {
                    brush.GradientStops[3].Offset = 1.0 - Math.Min(0.4, (this.itemsPanelMargin.Bottom + Math.Min(extent.Height - (offset.Y + viewport.Height), this.maxEdgeFadingSize)) / size.Height);
                    brush.GradientStops[4].Offset = 1.0 - Math.Min(0.4, this.itemsPanelMargin.Bottom / size.Height);
                }
                else
                {
                    brush.GradientStops[3].Offset = 1.0;
                    brush.GradientStops[4].Offset = 1.0;
                }
            }
            else
            {
                brush.GradientStops[1].Offset = 0.0;
                brush.GradientStops[2].Offset = 0.0;
                brush.GradientStops[3].Offset = 1.0;
                brush.GradientStops[4].Offset = 1.0;
            }
            this.contentPresenter?.InvalidateVisual();
        });
        
        // attach to self
        this.GetObservable(ExtentProperty).Subscribe(_ =>
        {
            correctOffsetAction.Schedule();
            updateOpacityMaskAction.Schedule();
        });
        this.GetObservable(HorizontalScrollBarVisibilityProperty).Subscribe(visibility =>
        {
            if (this.contentPresenter is not null)
                this.contentPresenter.CanHorizontallyScroll = visibility == ScrollBarVisibility.Visible;
            updateOpacityMaskAction.Schedule();
        });
        this.GetObservable(OffsetProperty).Subscribe(_ => updateOpacityMaskAction.Schedule());
        this.SizeChanged += (_, _) => updateOpacityMaskAction.Schedule();
        this.GetObservable(VerticalScrollBarVisibilityProperty).Subscribe(visibility =>
        {
            if (this.contentPresenter is not null)
                this.contentPresenter.CanVerticallyScroll = visibility == ScrollBarVisibility.Visible;
            updateOpacityMaskAction.Schedule();
        });
        this.GetObservable(ViewportProperty).Subscribe(_ =>
        {
            correctOffsetAction.Schedule();
            updateOpacityMaskAction.Schedule();
        });
    }
    
    
    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        this.contentPresenter = e.NameScope.Find<ScrollContentPresenter>("PART_ContentPresenter")?.Also(it =>
        {
            it.CanHorizontallyScroll = this.HorizontalScrollBarVisibility == ScrollBarVisibility.Visible;
            it.CanVerticallyScroll = this.VerticalScrollBarVisibility == ScrollBarVisibility.Visible;
            it.OpacityMask = this.opacityMackBrush;
        });
        this.itemsPanelMargin = this.FindResourceOrDefault<Thickness>("Thickness/TabControl.TabStrip.Panel.Margin");
    }


    /// <inheritdoc/>
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        this.maxEdgeFadingSize = this.FindResourceOrDefault("Double/TabControl.TabStrip.MaxEdgeFadingSize", 20.0);
    }
    
    
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
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var delta = e.Delta;
        if (Math.Abs(delta.Y) > 0 && Math.Abs(delta.X) < 0.01
            && this.CanHorizontallyScroll
            && !this.CanVerticallyScroll)
        {
            this.ScrollBy(delta.Y * -16);
            e.Handled = true;
        }
        base.OnPointerWheelChanged(e);
    }


    // Scroll by given offset.
    public void ScrollBy(double offset)
    {
        // check state
        if (this.topLevel is null)
            return;
        
        // get current offset
        var currentOffset = this.offsetAnimator?.EndValue ?? this.Offset;

        // cancel current animation
        this.offsetAnimator?.Cancel();

        // calculate target offset
        var targetOffsetX = currentOffset.X + offset;
        var extent = this.Extent;
        var viewport = this.Viewport;
        if (targetOffsetX < 0)
            targetOffsetX = 0;
        else if (targetOffsetX + viewport.Width > extent.Width)
            targetOffsetX = extent.Width - viewport.Width;

        // animate offset
        var duration = AppSuiteApplication.CurrentOrNull?.FindResource("TimeSpan/Animation.Fast").Let(it =>
        {
            if (it is TimeSpan timeSpan)
                return timeSpan;
            return TimeSpan.FromMilliseconds(250);
        }) ?? TimeSpan.FromMilliseconds(250);
        this.offsetAnimator = new VectorRenderingAnimator(this.topLevel, currentOffset, new Vector(targetOffsetX, currentOffset.Y)).Also(it =>
        {
            it.Completed += (_, _) => this.offsetAnimator = null;
            it.Duration = duration;
            it.Interpolator = Interpolators.Deceleration;
            it.ProgressChanged += (_, _) => this.Offset = it.Value;
            it.Start();
        });
    }


    /// <summary>
    /// Scroll left.
    /// </summary>
    public void ScrollLeft() => ScrollBy(-100);


    /// <summary>
    /// Scroll right.
    /// </summary>
    public void ScrollRight() => ScrollBy(100);
}
