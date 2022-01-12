using Avalonia.Controls;
using CarinaStudio.Animation;
using CarinaStudio.Threading;
using System;
using System.ComponentModel;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Helper class to control content fading of window.
    /// </summary>
    class WindowContentFadingHelper : INotifyPropertyChanged
    {
        // Constants.
        const double ContentFadeOutOpacity = 0.2;


        // Static fields.
        static readonly TimeSpan ContentFadeInDelay = TimeSpan.FromMilliseconds(300);
        static readonly TimeSpan ContentFadeInDuration = TimeSpan.FromMilliseconds(500);
        static readonly TimeSpan ContentFadeOutDuration = ContentFadeInDuration;


        // Fields.
        Control? content;
        DoubleAnimator? contentFadingAnimator;
        readonly ScheduledAction fadeInContentAction;


        // Constructor.
        public WindowContentFadingHelper(CarinaStudio.Controls.Window window)
        {
            // create scheduled action
            this.fadeInContentAction = new ScheduledAction(() =>
            {
                if (this.content == null)
                    return;
                this.contentFadingAnimator?.Cancel();
                this.contentFadingAnimator = new DoubleAnimator(this.content.Opacity, 1.0).Also(it =>
                {
                    it.Completed += (_, e) =>
                    {
                        this.contentFadingAnimator = null;
                        this.IsFadingContent = false;
                        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFadingContent)));
                    };
                    it.Duration = ContentFadeInDuration;
                    it.Interpolator = Interpolators.Deceleration;
                    it.ProgressChanged += (_, e) => this.content.Opacity = it.Value;
                    it.Start();
                });
                if (!this.IsFadingContent)
                {
                    this.IsFadingContent = true;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFadingContent)));
                }
            });

            // attach to window
            window.Closed += (_, e) => this.fadeInContentAction.Cancel();
            window.PropertyChanged += (_, e) =>
            {
                if (e.Property == ContentControl.ContentProperty)
                {
                    this.fadeInContentAction.Cancel();
                    if (this.contentFadingAnimator != null)
                    {
                        this.contentFadingAnimator.Cancel();
                        this.contentFadingAnimator = null;
                    }
                    if (this.IsFadingContent)
                    {
                        this.IsFadingContent = false;
                        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFadingContent)));
                    }
                    this.content = e.NewValue as Control;
                    if (this.content != null)
                        this.content.Opacity = window.HasDialogs ? ContentFadeOutOpacity : 1.0;
                }
                else if (e.Property == CarinaStudio.Controls.Window.HasDialogsProperty)
                {
                    if (this.content == null)
                        return;
                    if (window.HasDialogs)
                    {
                        this.fadeInContentAction.Cancel();
                        this.contentFadingAnimator?.Cancel();
                        this.contentFadingAnimator = new DoubleAnimator(this.content.Opacity, ContentFadeOutOpacity).Also(it =>
                        {
                            it.Completed += (_, e) =>
                            {
                                this.contentFadingAnimator = null;
                                this.IsFadingContent = false;
                                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFadingContent)));
                            };
                            it.Duration = ContentFadeOutDuration;
                            it.Interpolator = Interpolators.Deceleration;
                            it.ProgressChanged += (_, e) => this.content.Opacity = it.Value;
                            it.Start();
                        });
                        if (!this.IsFadingContent)
                        {
                            this.IsFadingContent = true;
                            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFadingContent)));
                        }
                    }
                    else
                    {
                        if (!this.IsFadingContent)
                        {
                            this.IsFadingContent = true;
                            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFadingContent)));
                        }
                        this.fadeInContentAction.Schedule(ContentFadeInDelay);
                    }
                }
            };
        }


        /// <summary>
        /// Check whether content of window is being fading in/out or not.
        /// </summary>
        public bool IsFadingContent { get; private set; }


        /// <summary>
        /// Raised when property changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
