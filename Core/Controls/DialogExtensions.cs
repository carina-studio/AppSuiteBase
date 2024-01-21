using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using CarinaStudio.Threading;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Extensions for dialog.
/// </summary>
public static class DialogExtensions
{
    // Static fields.
    static BrushTransition? HeaderBackgroundTransition;
    static BrushTransition? HeaderBorderBrushTransition;
    static readonly Dictionary<TextBlock, CancellationTokenSource> TextBlockAnimationCancellationTokenSources = new();
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
        if (headerClass is null)
            return;
        
        // setup initial brushes
        if (avnApp.TryFindResource($"Brush/Dialog.TextBlock.Background.{headerClass}.Focused", out IBrush? brush))
            header.Background = brush;
        if (avnApp.TryFindResource($"Brush/Dialog.TextBlock.Border.{headerClass}.Focused", out brush))
            header.BorderBrush = brush;
        
        // prepare transitions
        if (!avnApp.TryFindResource("TimeSpan/Animation", out TimeSpan? duration))
            duration = default;
        if (HeaderBackgroundTransition is null || HeaderBorderBrushTransition is null)
        {
            HeaderBackgroundTransition ??= new BrushTransition { Duration = duration.GetValueOrDefault(), Property = Border.BackgroundProperty };
            HeaderBorderBrushTransition ??= new BrushTransition { Duration = duration.GetValueOrDefault(), Property = Border.BorderBrushProperty };
        }
        var transitions = header.Transitions;
        if (transitions is null)
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
    public static void AnimateTextBlock<TDialog>(this TDialog dialog, TextBlock textBlock) where TDialog : Avalonia.Controls.Window, IApplicationObject
    {
        // get application
        if (dialog.Application is not IAvaloniaApplication avnApp)
            return;

        // check class of header
        if (!textBlock.Classes.Contains("Dialog_TextBlock")
            && !textBlock.Classes.Contains("Dialog_TextBlock_Label"))
        {
            return;
        }
        
        // cancel current animation
        if (TextBlockAnimationCancellationTokenSources.Remove(textBlock, out var cancellationTokenSource))
            cancellationTokenSource.Cancel();

        // setup initial brushes
        var brush = avnApp.FindResourceOrDefault<IBrush?>("Brush/Dialog.TextBlock.Background.Focused");
        var transparentBrush = (brush as ISolidColorBrush)?.Let(solidColorBrush =>
        {
            var color = solidColorBrush.Color;
            return (IBrush)new SolidColorBrush(Color.FromArgb(0, color.R, color.G, color.B));
        }) ?? Brushes.Transparent;
        textBlock.Background = transparentBrush;
        
        // prepare transitions
        var duration = avnApp.FindResourceOrDefault<TimeSpan>("TimeSpan/Animation.Fast");
        TextBlockBackgroundTransition ??= new BrushTransition { Duration = duration, Property = TextBlock.BackgroundProperty };
        var transitions = textBlock.Transitions;
        if (transitions is null)
        {
            transitions = new();
            textBlock.Transitions = transitions;
        }
        transitions.Add(TextBlockBackgroundTransition);
        
        // animate
        var durationMillis = (int)duration.TotalMilliseconds;
        cancellationTokenSource = new();
        TextBlockAnimationCancellationTokenSources[textBlock] = cancellationTokenSource;
        textBlock.Background = brush;
        avnApp.SynchronizationContext.PostDelayed(() =>
        {
            if (!cancellationTokenSource.IsCancellationRequested)
                textBlock.Background = transparentBrush;
        }, durationMillis);
        avnApp.SynchronizationContext.PostDelayed(() =>
        {
            if (!cancellationTokenSource.IsCancellationRequested)
                textBlock.Background = brush;
        }, durationMillis * 2);
        avnApp.SynchronizationContext.PostDelayed(() =>
        {
            if (!cancellationTokenSource.IsCancellationRequested)
                textBlock.Background = transparentBrush;
        }, durationMillis * 3);
        avnApp.SynchronizationContext.PostDelayed(() =>
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                transitions.Remove(TextBlockBackgroundTransition);
                textBlock.Background = null;
                TextBlockAnimationCancellationTokenSources.Remove(textBlock);
            }
            cancellationTokenSource.Dispose();
        }, durationMillis * 4);
    }
}