using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using CarinaStudio.Animation;
using System;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Extensions for <see cref="ScrollViewer"/>.
/// </summary>
public static class ScrollViewerExtensions
{
    // Static fields.
    static readonly Dictionary<ScrollViewer, DoubleAnimator> SmoothScrollingAnimators = new();
    static readonly Dictionary<ScrollViewer, Vector> SmoothScrollingTargetOffsets = new();
    
    
    /// <summary>
    /// Scroll given content into viewport of <see cref="ScrollViewer"/>.
    /// </summary>
    /// <param name="scrollViewer"><see cref="ScrollViewer"/>.</param>
    /// <param name="content">Content.</param>
    /// <param name="scrollToCenter">True to scroll content to center of viewport.</param>
    public static void ScrollToContent(this ScrollViewer scrollViewer, Visual content, bool scrollToCenter = true) =>
        ScrollToContent(scrollViewer, content, TimeSpan.Zero, scrollToCenter);
    
    
    // Scroll given content into viewport ofScrollViewer.
    static void ScrollToContent(ScrollViewer scrollViewer, Visual content, TimeSpan duration, bool scrollToCenter)
    {
        // check parameter
        if (scrollViewer == content)
            return;

        // find position in scroll viewer
        var offset = scrollViewer.Offset;
        var contentBounds = content.Bounds;
        var leftInScrollViewer = contentBounds.Left;
        var topInScrollViewer = contentBounds.Top;
        var parent = content.GetVisualParent();
        while (parent != scrollViewer && parent is not null)
        {
            var parentBounds = parent.Bounds;
            leftInScrollViewer += parentBounds.Left;
            topInScrollViewer += parentBounds.Top;
            parent = parent.GetVisualParent();
        }
        if (parent is null)
            return;
        leftInScrollViewer += offset.X;
        topInScrollViewer += offset.Y;
        var rightInScrollViewer = leftInScrollViewer + contentBounds.Width;
        var bottomInScrollViewer = topInScrollViewer + contentBounds.Height;

        // check whether scrolling is needed or not
        var extent = scrollViewer.Extent;
        var viewportSize = scrollViewer.Viewport;
        var viewportCenter = new Point(offset.X + viewportSize.Width / 2, offset.Y + viewportSize.Height / 2);
        var scrollHorizontally = scrollViewer.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled && Global.Run(() =>
        {
            if (contentBounds.Width > viewportSize.Width)
            {
                return scrollViewer.FlowDirection switch
                {
                    FlowDirection.RightToLeft => Math.Abs(rightInScrollViewer - (offset.X + viewportSize.Width)) > double.Epsilon * 2,
                    _ => Math.Abs(leftInScrollViewer - offset.X) > double.Epsilon * 2,
                };
            }
            if (!scrollToCenter)
                return leftInScrollViewer < offset.X || rightInScrollViewer > offset.X + viewportSize.Width;
            return leftInScrollViewer > viewportCenter.X || rightInScrollViewer < viewportCenter.X;
        });
        var scrollVertically = scrollViewer.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled && Global.Run(() =>
        {
            if (contentBounds.Height > viewportSize.Height)
                return Math.Abs(topInScrollViewer - offset.Y) > double.Epsilon * 2;
            if (!scrollToCenter)
                return topInScrollViewer < offset.Y || bottomInScrollViewer > offset.Y + viewportSize.Height;
            return topInScrollViewer > viewportCenter.Y || bottomInScrollViewer < viewportCenter.Y;
        });
        if (!scrollHorizontally && !scrollVertically)
            return;

        // calculate position to scroll
        var newOffsetX = Global.Run(() =>
        {
            if (!scrollHorizontally)
                return offset.X;
            if (contentBounds.Width > viewportSize.Width)
            {
                return scrollViewer.FlowDirection switch
                {
                    FlowDirection.RightToLeft => rightInScrollViewer - viewportSize.Width,
                    _ => leftInScrollViewer,
                };
            }
            if (scrollToCenter)
                return (leftInScrollViewer + rightInScrollViewer) / 2 - viewportSize.Width / 2;
            if (leftInScrollViewer < offset.X)
                return leftInScrollViewer;
            return rightInScrollViewer - viewportSize.Width;
        });
        var newOffsetY = Global.Run(() =>
        {
            if (!scrollVertically)
                return offset.Y;
            if (contentBounds.Height > viewportSize.Height)
                return topInScrollViewer;
            if (scrollToCenter)
                return (topInScrollViewer + bottomInScrollViewer) / 2 - viewportSize.Height / 2;
            if (topInScrollViewer < offset.Y)
                return topInScrollViewer;
            return bottomInScrollViewer - viewportSize.Height;
        });
        newOffsetX = Math.Max(0, Math.Min(newOffsetX, extent.Width - viewportSize.Width));
        newOffsetY = Math.Max(0, Math.Min(newOffsetY, extent.Height - viewportSize.Height));
        var diffX = (newOffsetX - offset.X);
        var diffY = (newOffsetY - offset.Y);
        if (Math.Abs(diffX) < Double.Epsilon * 2 && Math.Abs(diffY) < double.Epsilon * 2)
            return;

        // cancel previous scrolling
        if (SmoothScrollingAnimators.TryGetValue(scrollViewer, out var prevAnimator))
            prevAnimator.Cancel();
        SmoothScrollingTargetOffsets.Remove(scrollViewer);

        // scroll to content
        if (duration.TotalMilliseconds > 0)
        {
            var animator = default(DoubleAnimator);
            void OnPointerPressed(object? sender, RoutedEventArgs e) =>
                animator?.Cancel();
            void OnPointerWheelChanged(object? sender, RoutedEventArgs e) =>
                animator?.Cancel();
            var extentChangedObserverToken = scrollViewer.GetObservable(ScrollViewer.ExtentProperty).Subscribe(_ => animator?.Cancel());
            var viewportChangedObserverToken = scrollViewer.GetObservable(ScrollViewer.ViewportProperty).Subscribe(_ => animator?.Cancel());
            animator = new DoubleAnimator(0, 1).Also(it =>
            {
                it.Cancelled += (_, _) =>
                {
                    if (SmoothScrollingAnimators.TryGetValue(scrollViewer, out var currentAnimator) && currentAnimator == it)
                    {
                        SmoothScrollingAnimators.Remove(scrollViewer);
                        SmoothScrollingTargetOffsets.Remove(scrollViewer);
                    }
                    scrollViewer.RemoveHandler(ScrollViewer.PointerPressedEvent, OnPointerPressed);
                    scrollViewer.RemoveHandler(ScrollViewer.PointerWheelChangedEvent, OnPointerWheelChanged);
                    extentChangedObserverToken.Dispose();
                    viewportChangedObserverToken.Dispose();
                };
                it.Completed += (_, _) =>
                {
                    if (SmoothScrollingAnimators.TryGetValue(scrollViewer, out var currentAnimator) && currentAnimator == it)
                    {
                        SmoothScrollingAnimators.Remove(scrollViewer);
                        SmoothScrollingTargetOffsets.Remove(scrollViewer);
                        scrollViewer.Offset = new(newOffsetX, newOffsetY);
                    }
                    scrollViewer.RemoveHandler(ScrollViewer.PointerPressedEvent, OnPointerPressed);
                    scrollViewer.RemoveHandler(ScrollViewer.PointerWheelChangedEvent, OnPointerWheelChanged);
                    extentChangedObserverToken.Dispose();
                    viewportChangedObserverToken.Dispose();
                };
                it.Duration = duration;
                it.Interpolator = SmoothScrollingInterpolation;
                it.ProgressChanged += (_, _) => { scrollViewer.Offset = new(offset.X + diffX * it.Progress, offset.Y + diffY * it.Progress); };
            });
            scrollViewer.AddHandler(ScrollViewer.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            scrollViewer.AddHandler(ScrollViewer.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
            SmoothScrollingAnimators[scrollViewer] = animator;
            SmoothScrollingTargetOffsets[scrollViewer] = new(newOffsetX, newOffsetY);
            animator.Start();
        }
        else
            scrollViewer.Offset = new(newOffsetX, newOffsetY);
    }


    // Interpolation of smooth scrolling (cubic bezier).
    static double SmoothScrollingInterpolation(double p)
    {
        // start: (0, 0)
        // control-1: (0.5, 0)
        // control-2: (0.5, 1)
        // end: (1, 1)
        return (3 * p * p * (1 - p)) + (p * p * p);
    }


    /// <summary>
    /// Scroll given content smoothly into viewport of <see cref="ScrollViewer"/>.
    /// </summary>
    /// <param name="scrollViewer"><see cref="ScrollViewer"/>.</param>
    /// <param name="content">Content.</param>
    /// <param name="scrollToCenter">True to scroll content to center of viewport.</param>
    public static void SmoothScrollToContent(this ScrollViewer scrollViewer, Visual content, bool scrollToCenter = true) =>
        ScrollToContent(scrollViewer, 
            content, 
            IAppSuiteApplication.CurrentOrNull?.FindResourceOrDefault<TimeSpan?>("TimeSpan/Animation") ?? TimeSpan.FromMilliseconds(500), 
            scrollToCenter);
    
    
    /// <summary>
    /// Scroll given content smoothly into viewport of <see cref="ScrollViewer"/>.
    /// </summary>
    /// <param name="scrollViewer"><see cref="ScrollViewer"/>.</param>
    /// <param name="content">Content.</param>
    /// <param name="duration">Duration of scrolling.</param>
    /// <param name="scrollToCenter">True to scroll content to center of viewport.</param>
    public static void SmoothScrollToContent(this ScrollViewer scrollViewer, Visual content, TimeSpan duration, bool scrollToCenter = true) =>
        ScrollToContent(scrollViewer, content, duration, scrollToCenter);


    /// <summary>
    /// Try getting target offset of smooth scrolling.
    /// </summary>
    /// <param name="scrollViewer"><see cref="ScrollViewer"/>.</param>
    /// <param name="offset">Target offset.</param>
    /// <returns>True if tar offset got successfully.</returns>
    public static bool TryGetSmoothScrollingTargetOffset(this ScrollViewer scrollViewer, out Vector offset) =>
        SmoothScrollingTargetOffsets.TryGetValue(scrollViewer, out offset);
}