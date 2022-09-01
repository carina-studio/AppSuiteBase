using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Avalonia.VisualTree;
using CarinaStudio.Animation;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// <see cref="ScrollViewer"/> which is designed for tool bar.
    /// </summary>
    public class ToolBarScrollViewer : ScrollViewer, IStyleable
    {
        // Fields.
        readonly ScheduledAction correctOffsetAction;
        VectorAnimator? offsetAnimator;
        Control? scrollDownButton;
        Control? scrollLeftButton;
        Control? scrollRightButton;
        Control? scrollUpButton;


        /// <summary>
        /// Initialize new <see cref="ToolBarScrollViewer"/>.
        /// </summary>
        public ToolBarScrollViewer()
        {
            this.correctOffsetAction = new ScheduledAction(() =>
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
            });
            this.GetObservable(ExtentProperty).Subscribe(_ => this.correctOffsetAction.Schedule());
            this.GetObservable(ViewportProperty).Subscribe(_ => this.correctOffsetAction.Schedule());
        }


         /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            this.scrollDownButton = e.NameScope.Find<Control>("PART_ScrollDownButton");
            this.scrollLeftButton = e.NameScope.Find<Control>("PART_ScrollLeftButton");
            this.scrollRightButton = e.NameScope.Find<Control>("PART_ScrollRightButton");
            this.scrollUpButton = e.NameScope.Find<Control>("PART_ScrollUpButton");
        }


        // Scroll by given offset.
        void ScrollBy(double offsetX, double offsetY)
        {
            // check thread
            this.VerifyAccess();

            // get current offset
            var currentOffset = this.offsetAnimator?.EndValue ?? this.Offset;

            // cancel current animation
            if (this.offsetAnimator != null)
                this.offsetAnimator.Cancel();
            
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
            var duration = AppSuiteApplication.CurrentOrNull?.FindResource("TimeSpan/Animation.Fast").Let(it =>
            {
                if (it is TimeSpan timeSpan)
                    return timeSpan;
                return TimeSpan.FromMilliseconds(250);
            }) ?? TimeSpan.FromMilliseconds(250);
            this.offsetAnimator = new VectorAnimator(currentOffset, new Vector(targetOffsetX, targetOffsetY)).Also(it =>
            {
                it.Completed += (_, e) => this.offsetAnimator = null;
                it.Duration = duration;
                it.Interpolator = Interpolators.Deceleration;
                it.ProgressChanged += (_, e) => this.Offset = it.Value;
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
        public void ScrollIntoView(IVisual visual)
        {
            // check state
            this.VerifyAccess();
            if (visual == this)
                return;

            // calculate relative bounds
            var bounds = visual.Bounds;
            var x = bounds.Left;
            var y = bounds.Top;
            visual = visual.GetVisualParent();
            while (visual != null && visual != this)
            {
                var parentBounds = visual.Bounds;
                x += parentBounds.Left;
                y += parentBounds.Top;
                visual = visual.GetVisualParent();
            }
            if (visual == null)
                return;

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


        // Interface implementation.
        Type IStyleable.StyleKey { get; } = typeof(ToolBarScrollViewer);
    }
}
