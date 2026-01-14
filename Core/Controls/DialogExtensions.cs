using Avalonia;
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
    /// <param name="itemName">Name of item to animate.</param>
    /// <returns>Ture if item found and animated successfully.</returns>
    public static bool AnimateItem(this Avalonia.Controls.Window dialog, string itemName)
    {
        if (dialog.Find<Control>(itemName) is { } item)
        {
            dialog.AnimateItem(item);
            return true;
        }
        return false;
    }

    
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
            Property = Visual.OpacityProperty
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


    /// <summary>
    /// Hint user to input value into specific item.
    /// </summary>
    /// <param name="dialog">Dialog.</param>
    /// <param name="scrollViewer"><see cref="ScrollViewer"/> which contains the item to input.</param>
    /// <param name="animatedItem">Item to be animated.</param>
    /// <param name="focusedControl">Control to be focused.</param>
    public static void HintForInput(this Avalonia.Controls.Window dialog, ScrollViewer? scrollViewer, Control? animatedItem, Control? focusedControl)
    {
        if ((animatedItem ?? focusedControl) is { } itemToScrollTo)
            scrollViewer?.SmoothScrollIntoView(itemToScrollTo);
        if (animatedItem is not null)
            dialog.AnimateItem(animatedItem);
        (focusedControl ?? animatedItem)?.Focus();
    }
}