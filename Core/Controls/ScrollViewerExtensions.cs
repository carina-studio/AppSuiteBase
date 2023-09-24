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
    static readonly Func<double, double> SmoothScrollingInterpolator = Interpolators.CreateCubicBezierInterpolator(0.6, 0, 0.4, 1);


    /// <summary>
    /// Scroll given content smoothly into viewport of <see cref="ScrollViewer"/>.
    /// </summary>
    /// <param name="scrollViewer"><see cref="ScrollViewer"/>.</param>
    /// <param name="visual">Content.</param>
    /// <param name="scrollToCenter">True to scroll content to center of viewport.</param>
    public static void SmoothScrollIntoView(this ScrollViewer scrollViewer, Visual visual, bool scrollToCenter = true) =>
        scrollViewer.SmoothScrollIntoView(visual, 
            IAppSuiteApplication.CurrentOrNull?.FindResourceOrDefault<TimeSpan?>("TimeSpan/Animation") ?? TimeSpan.FromMilliseconds(500),
            SmoothScrollingInterpolator,
            scrollToCenter);
    
    
    /// <summary>
    /// Scroll given content smoothly into viewport of <see cref="ScrollViewer"/>.
    /// </summary>
    /// <param name="scrollViewer"><see cref="ScrollViewer"/>.</param>
    /// <param name="visual">Content.</param>
    /// <param name="duration">Duration of scrolling.</param>
    /// <param name="scrollToCenter">True to scroll content to center of viewport.</param>
    public static void SmoothScrollIntoView(this ScrollViewer scrollViewer, Visual visual, TimeSpan duration, bool scrollToCenter = true) =>
        scrollViewer.SmoothScrollIntoView(visual, duration, SmoothScrollingInterpolator, scrollToCenter);
}