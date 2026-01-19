using Avalonia;
using Avalonia.Controls;
using CarinaStudio.Animation;
using CarinaStudio.Controls;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Extensions for <see cref="ScrollViewer"/>.
/// </summary>
public static class ScrollViewerExtensions
{
    // Static fields.
    static readonly Func<double, double> SmoothScrollingInterpolator = Interpolators.CreateCubicBezierInterpolator(0.7, 0, 0.3, 1);


    /// <summary>
    /// Scroll given content smoothly into viewport of <see cref="ScrollViewer"/>.
    /// </summary>
    /// <param name="scrollViewer"><see cref="ScrollViewer"/>.</param>
    /// <param name="visual">Content.</param>
    /// <param name="scrollToCenter">True to scroll content to center of viewport.</param>
    /// <param name="onCompleted">Function to be called when scrolling completed.</param>
    /// <param name="onCancelled">Function to be called when scrolling cancelled.</param>
    /// <returns>True if the scrolling has been started or completed successfully.</returns>
    public static bool SmoothScrollIntoView(this ScrollViewer scrollViewer, Visual visual, bool scrollToCenter = true, Action? onCompleted = null, Action? onCancelled = null) =>
        scrollViewer.SmoothScrollIntoView(visual, 
            IAppSuiteApplication.CurrentOrNull?.FindResourceOrDefault<TimeSpan?>("TimeSpan/Animation") ?? TimeSpan.FromMilliseconds(500),
            SmoothScrollingInterpolator,
            scrollToCenter,
            onCompleted,
            onCancelled);
    
    
    /// <summary>
    /// Scroll given content smoothly into viewport of <see cref="ScrollViewer"/>.
    /// </summary>
    /// <param name="scrollViewer"><see cref="ScrollViewer"/>.</param>
    /// <param name="visual">Content.</param>
    /// <param name="duration">Duration of scrolling.</param>
    /// <param name="scrollToCenter">True to scroll content to center of viewport.</param>
    /// <param name="onCompleted">Function to be called when scrolling completed.</param>
    /// <param name="onCancelled">Function to be called when scrolling cancelled.</param>
    /// <returns>True if the scrolling has been started or completed successfully.</returns>
    public static bool SmoothScrollIntoView(this ScrollViewer scrollViewer, Visual visual, TimeSpan duration, bool scrollToCenter = true, Action? onCompleted = null, Action? onCancelled = null) =>
        scrollViewer.SmoothScrollIntoView(visual, duration, SmoothScrollingInterpolator, scrollToCenter, onCompleted, onCancelled);


    /// <summary>
    /// Scroll to given offset smoothly.
    /// </summary>
    /// <param name="scrollViewer"><see cref="ScrollViewer"/>.</param>
    /// <param name="offset">Target offset.</param>
    /// <param name="onCompleted">Function to be called when scrolling completed.</param>
    /// <param name="onCancelled">Function to be called when scrolling cancelled.</param>
    /// <returns>True if the scrolling has been started or completed successfully.</returns>
    public static bool SmoothScrollTo(this ScrollViewer scrollViewer, Vector offset, Action? onCompleted = null, Action? onCancelled = null) =>
        scrollViewer.SmoothScrollTo(offset,
            IAppSuiteApplication.CurrentOrNull?.FindResourceOrDefault<TimeSpan?>("TimeSpan/Animation") ?? TimeSpan.FromMilliseconds(500),
            SmoothScrollingInterpolator,
            onCompleted, 
            onCancelled);
}