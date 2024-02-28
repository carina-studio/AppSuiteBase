using Avalonia.Animation;
using Avalonia.Controls;
using CarinaStudio.Controls;
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
    static readonly Dictionary<Control, double> ItemAnimationBaseOpacities = new();
    static readonly Dictionary<Control, CancellationTokenSource> ItemAnimationCancellationTokenSources = new();
    static DoubleTransition? ItemOpacityTransition;

    
    /// <summary>
    /// Animate item of dialog to get attention of user.
    /// </summary>
    /// <param name="dialog">Dialog.</param>
    /// <param name="item">Item to animate.</param>
    public static void AnimateItem(this Avalonia.Controls.Window dialog, Control item)
    {
        // cancel current animation
        if (ItemAnimationCancellationTokenSources.Remove(item, out var cancellationTokenSource))
            cancellationTokenSource.Cancel();
        
        // prepare transitions
        var duration = dialog.FindResourceOrDefault("TimeSpan/Animation.Fast", TimeSpan.FromMilliseconds(300));
        ItemOpacityTransition ??= new DoubleTransition
        {
            Duration = duration, 
            Property = Control.OpacityProperty
        };
        var transitions = item.Transitions ?? new Transitions().Also(it =>
        {
            item.Transitions = it;
        });
        if (!transitions.Contains(ItemOpacityTransition))
            transitions.Add(ItemOpacityTransition);
        
        // get base opacity
        if (!ItemAnimationBaseOpacities.TryGetValue(item, out var baseOpacity))
        {
            baseOpacity = item.Opacity;
            ItemAnimationBaseOpacities[item] = baseOpacity;
        }
        
        // animate
        var syncContext = DispatcherSynchronizationContext.UIThread;
        var durationMillis = (int)duration.TotalMilliseconds;
        cancellationTokenSource = new();
        ItemAnimationCancellationTokenSources[item] = cancellationTokenSource;
        item.Opacity = 0;
        syncContext.PostDelayed(() =>
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                // reset opacity
                item.Opacity = baseOpacity;
                
                // continue animation
                syncContext.PostDelayed(() =>
                {
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        // set opacity
                        item.Opacity = 0;
                        
                        // continue animation
                        syncContext.PostDelayed(() =>
                        {
                            if (!cancellationTokenSource.IsCancellationRequested)
                            {
                                // reset opacity
                                item.Opacity = baseOpacity;
                            
                                // complete animation
                                syncContext.PostDelayed(() =>
                                {
                                    if (!cancellationTokenSource.IsCancellationRequested)
                                    {
                                        transitions.Remove(ItemOpacityTransition);
                                        ItemAnimationBaseOpacities.Remove(item);
                                        ItemAnimationCancellationTokenSources.Remove(item);
                                    }
                                }, durationMillis);
                            }
                        }, durationMillis);
                    }
                }, durationMillis);
            }
        }, durationMillis);
    }
}