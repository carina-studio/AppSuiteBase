using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Extensions for dialog.
/// </summary>
public static class DialogExtensions
{
    // Static fields.
    static BrushTransition? HeaderBackgroundTransition;
    static BrushTransition? HeaderBorderBrushTransition;
    static BrushTransition? TextBlockBackgroundTransition;


    /// <summary>
    /// Animate header of dialog.
    /// </summary>
    /// <param name="dialog">Dialog.</param>
    /// <param name="header">Header to animate.</param>
    public static void AnimateHeader<TDialog>(this TDialog dialog, Border header) where TDialog : Avalonia.Controls.Window, IApplicationObject
    {
        // get application
        if (dialog.Application is not IAvaloniaApplication avnApp)
            return;

        // check class of header
        var headerClass = default(string);
        if (header.Classes.Contains("Dialog_TextBlock_Header1"))
            headerClass = "Header1";
        else if (header.Classes.Contains("Dialog_TextBlock_Header2"))
            headerClass = "Header2";
        if (headerClass == null)
            return;
        
        // setup initial brushes
        if (avnApp.TryFindResource($"Brush/Dialog.TextBlock.Background.{headerClass}.Focused", out IBrush? brush))
            header.Background = brush;
        if (avnApp.TryFindResource($"Brush/Dialog.TextBlock.Border.{headerClass}.Focused", out brush))
            header.BorderBrush = brush;
        
        // prepare transitions
        if (!avnApp.TryFindResource("TimeSpan/Animation", out TimeSpan? duration))
            duration = default;
        if (HeaderBackgroundTransition == null || HeaderBorderBrushTransition == null)
        {
            HeaderBackgroundTransition ??= new BrushTransition() { Duration = duration.GetValueOrDefault(), Property = Border.BackgroundProperty };
            HeaderBorderBrushTransition ??= new BrushTransition() { Duration = duration.GetValueOrDefault(), Property = Border.BorderBrushProperty };
        }
        var transitions = header.Transitions;
        if (transitions == null)
        {
            transitions = new();
            header.Transitions = transitions;
        }
        transitions.Add(HeaderBackgroundTransition);
        transitions.Add(HeaderBorderBrushTransition);
        
        // animate
        if (avnApp.TryFindResource($"Brush/Dialog.TextBlock.Background.{headerClass}", out brush))
            header.Background = brush;
        if (avnApp.TryFindResource($"Brush/Dialog.TextBlock.Border.{headerClass}", out brush))
            header.BorderBrush = brush;
        avnApp.SynchronizationContext.PostDelayed(() =>
        {
            transitions.Remove(HeaderBackgroundTransition);
            transitions.Remove(HeaderBorderBrushTransition);
        }, (int)duration.GetValueOrDefault().TotalMilliseconds);
    }


    /// <summary>
    /// Animate text block in dialog.
    /// </summary>
    /// <param name="dialog">Dialog.</param>
    /// <param name="textBlock">Text block to animate.</param>
    public static void AnimateTextBlock<TDialog>(this TDialog dialog, Avalonia.Controls.TextBlock textBlock) where TDialog : Avalonia.Controls.Window, IApplicationObject
    {
        // get application
        if (dialog.Application is not IAvaloniaApplication avnApp)
            return;

        // check class of header
        if (!textBlock.Classes.Contains("Dialog_TextBlock"))
            return;
        
        // setup initial brushes
        if (avnApp.TryFindResource($"Brush/Dialog.TextBlock.Background.Focused", out IBrush? brush))
            textBlock.Background = brush;
        
        // prepare transitions
        if (!avnApp.TryFindResource("TimeSpan/Animation", out TimeSpan? duration))
            duration = default;
        TextBlockBackgroundTransition ??= new BrushTransition() { Duration = duration.GetValueOrDefault(), Property = Avalonia.Controls.TextBlock.BackgroundProperty };
        var transitions = textBlock.Transitions;
        if (transitions == null)
        {
            transitions = new();
            textBlock.Transitions = transitions;
        }
        transitions.Add(TextBlockBackgroundTransition);
        
        // animate
        if (brush is ISolidColorBrush solidColorBrush)
        {
            var color = solidColorBrush.Color;
            textBlock.Background = new SolidColorBrush(Color.FromArgb(0, color.R, color.G, color.B));
        }
        else
            textBlock.Background = Brushes.Transparent;
        avnApp.SynchronizationContext.PostDelayed(() =>
        {
            transitions.Remove(TextBlockBackgroundTransition);
            textBlock.Background = null;
        }, (int)duration.GetValueOrDefault().TotalMilliseconds);
    }


    /// <summary>
    /// Scroll to given control.
    /// </summary>
    /// <param name="dialog">Dialog.</param>
    /// <param name="control">Control to scroll to.</param>
    public static void ScrollToControl<TDialog>(this TDialog dialog, Control control) where TDialog : Avalonia.Controls.Window, IApplicationObject
    {
        var scrollViewer = dialog.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer != null)
            ScrollToControl(dialog, scrollViewer, control);
    }


    /// <summary>
    /// Scroll to given control.
    /// </summary>
    /// <param name="dialog">Dialog.</param>
    /// <param name="scrollViewer">Scroll viewer.</param>
    /// <param name="control">Control to scroll to.</param>
    public static void ScrollToControl<TDialog>(this TDialog dialog, ScrollViewer scrollViewer, Control control) where TDialog : Avalonia.Controls.Window, IApplicationObject
    {
        var scrollViewerOpacity = scrollViewer.Opacity;
        scrollViewer.Opacity = 0;
        dialog.SynchronizationContext.Post(() =>
        {
            // get position of control in scroll viewer
            var offsetY = control.Bounds.Top;
            var parent = control.Parent;
            while (parent != null && parent != scrollViewer)
            {
                offsetY += parent.Bounds.Top;
                parent = parent.Parent;
            }
            if (parent != scrollViewer)
            {
                scrollViewer.Opacity = scrollViewerOpacity;
                return;
            }
            if (control.Margin.Top <= 0.01)
                offsetY -= 10;

            // scroll to control
            var extent = scrollViewer.Extent;
            var viewport = scrollViewer.Viewport;
            if (offsetY + viewport.Height > extent.Height)
                offsetY = extent.Height - viewport.Height;
            else if (offsetY < 0)
                offsetY = 0;
            scrollViewer.Offset = new(0, offsetY);
            scrollViewer.Opacity = scrollViewerOpacity;
        });
    }
}