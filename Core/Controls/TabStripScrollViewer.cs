using Avalonia;
using Avalonia.Controls;
using CarinaStudio.Animation;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    internal class TabStripScrollViewer : ScrollViewer
    {
        // Fields.
        readonly ScheduledAction correctOffsetAction;
        VectorAnimator? offsetAnimator;


        // Constructor.
        public TabStripScrollViewer()
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
                    this.Offset = new Vector(extent.Width - viewport.Width, 0);
                }
            });
            this.GetObservable(ExtentProperty).Subscribe(_ => this.correctOffsetAction.Schedule());
            this.GetObservable(ViewportProperty).Subscribe(_ => this.correctOffsetAction.Schedule());
        }


        // Scroll by given offset.
        public void ScrollBy(double offset)
        {
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
            this.offsetAnimator = new VectorAnimator(currentOffset, new Vector(targetOffsetX, currentOffset.Y)).Also(it =>
            {
                it.Completed += (_, e) => this.offsetAnimator = null;
                it.Duration = duration;
                it.Interpolator = Interpolators.Deceleration;
                it.ProgressChanged += (_, e) => this.Offset = it.Value;
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
}
