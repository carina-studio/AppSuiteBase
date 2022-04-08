using Avalonia;
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
        Control? contentFadingOverlay;
        readonly ScheduledAction fadeInContentAction;


        // Constructor.
        public WindowContentFadingHelper(CarinaStudio.Controls.Window window)
        {
            // create scheduled action
            this.fadeInContentAction = new ScheduledAction(() =>
            {
                if (this.content == null && this.contentFadingOverlay == null)
                    return;
                this.contentFadingAnimator?.Cancel();
                this.contentFadingAnimator = this.contentFadingOverlay != null
                    ? new DoubleAnimator(this.contentFadingOverlay.Opacity, 0)
                    : new DoubleAnimator(this.content.AsNonNull().Opacity, 1.0);
                this.contentFadingAnimator.Let(it =>
                {
                    it.Completed += (_, e) =>
                    {
                        if (this.contentFadingOverlay != null)
                        {
                            this.contentFadingOverlay.IsVisible = false;
                            this.contentFadingOverlay.Opacity = it.EndValue;
                        }
                        else if (this.content != null)
                            this.content.Opacity = it.EndValue;
                        this.contentFadingAnimator = null;
                        this.IsFadingContent = false;
                        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFadingContent)));
                    };
                    it.Duration = ContentFadeInDuration;
                    it.Interpolator = Interpolators.Deceleration;
                    it.ProgressChanged += (_, e) =>
                    {
                        if (this.contentFadingOverlay != null)
                            this.contentFadingOverlay.Opacity = it.Value;
                        else if (this.content != null)
                            this.content.Opacity = it.Value;
                    };
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
            window.GetObservable(Window.ContentProperty).Subscribe(content =>
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
                this.content = content as Control;
                if (this.content != null && this.contentFadingOverlay == null)
                    this.content.Opacity = window.HasDialogs ? ContentFadeOutOpacity : 1.0;
            });
            window.GetObservable(CarinaStudio.Controls.Window.HasDialogsProperty).Subscribe(hasDialogs =>
            {
                if (this.content == null && this.contentFadingOverlay == null)
                    return;
                if (hasDialogs)
                {
                    this.fadeInContentAction.Cancel();
                    this.contentFadingAnimator?.Cancel();
                    this.contentFadingAnimator = this.contentFadingOverlay != null
                        ? new DoubleAnimator(this.contentFadingOverlay.Opacity, 1 - ContentFadeOutOpacity)
                        : new DoubleAnimator(this.content.AsNonNull().Opacity, ContentFadeOutOpacity);
                    this.contentFadingAnimator.Let(it =>
                    {
                        it.Completed += (_, e) =>
                        {
                            if (this.contentFadingOverlay != null)
                                this.contentFadingOverlay.Opacity = it.EndValue;
                            else if (this.content != null)
                                this.content.Opacity = it.EndValue;
                            this.contentFadingAnimator = null;
                            this.IsFadingContent = false;
                            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFadingContent)));
                        };
                        it.Duration = ContentFadeOutDuration;
                        it.Interpolator = Interpolators.Deceleration;
                        it.ProgressChanged += (_, e) =>
                        {
                            if (this.contentFadingOverlay != null)
                                this.contentFadingOverlay.Opacity = it.Value;
                            else if (this.content != null)
                                this.content.Opacity = it.Value;
                        };
                        it.Start();
                        if (this.contentFadingOverlay != null)
                            this.contentFadingOverlay.IsVisible = true;
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
            });
            window.TemplateApplied += (_, e) =>
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
                this.contentFadingOverlay = e.NameScope.Find<Control>("PART_ContentFadingOverlay");
                if (this.contentFadingOverlay != null)
                {
                    if (this.content != null)
                        this.content.Opacity = 1;
                    this.contentFadingOverlay.Opacity = window.HasDialogs ? 1 - ContentFadeOutOpacity : 0;
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
