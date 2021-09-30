using Avalonia.Animation;
using Avalonia.Controls;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Helper class to control content fading of window.
    /// </summary>
    class WindowContentFadingHelper
    {
        // Constants.
        const int FadeInContentDelay = 300;


        // Fields.
        readonly ScheduledAction fadeInContentAction;


        // Constructor.
        public WindowContentFadingHelper(CarinaStudio.Controls.Window window)
        {
            // create scheduled action
            this.fadeInContentAction = new ScheduledAction(() =>
            {
                (window.Content as Control)?.Let(it => it.Opacity = 1);
            });

            // attach to window
            window.Closed += (_, e) => this.fadeInContentAction.Cancel();
            window.PropertyChanged += (_, e) =>
            {
                if (e.Property == Window.ContentProperty)
                {
                    if (e.NewValue is Control control)
                    {
                        var transitions = control.Transitions ?? new Transitions().Also(it => control.Transitions = it);
                        transitions.Add(new DoubleTransition()
                        {
                            Duration = TimeSpan.FromMilliseconds(500),
                            Property = Control.OpacityProperty
                        });
                    }
                }
                else if (e.Property == CarinaStudio.Controls.Window.HasDialogsProperty)
                {
                    if (window.HasDialogs)
                    {
                        this.fadeInContentAction.Cancel();
                        (window.Content as Control)?.Let(it => it.Opacity = 0.2);
                    }
                    else
                        this.fadeInContentAction.Schedule(FadeInContentDelay);
                }
            };
        }
    }
}
