using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using CarinaStudio.Animation;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using System;
using System.ComponentModel;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Helper class to control content fading of window.
/// </summary>
class WindowContentFadingHelper : INotifyPropertyChanged
{
    // Constants.
    const double ContentBlurRadius = 6;
    const double ContentFadeOutOpacity = 0.2;


    // Static fields.
    static readonly TimeSpan ContentFadeInDelay = TimeSpan.FromMilliseconds(300);
    static readonly TimeSpan ContentFadeInDuration = TimeSpan.FromMilliseconds(800);
    static readonly TimeSpan ContentFadeOutDelay = TimeSpan.FromMilliseconds(200);
    static readonly TimeSpan ContentFadeOutDuration = ContentFadeInDuration;


    // Fields.
    BlurEffect? contentBlurEffect;
    Control? content;
    DoubleRenderingAnimator? contentFadingAnimator;
    Control? contentFadingOverlay;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    readonly ScheduledAction fadeInContentAction;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    bool isContentBlurred;
    readonly CarinaStudio.Controls.Window window;


    // Constructor.
    public WindowContentFadingHelper(CarinaStudio.Controls.Window window)
    {
        // keep reference to window
        this.window = window;
        
        // create scheduled action
        this.fadeInContentAction = new(this.FadeInContent);

        // attach to window
        window.Closed += (_, _) => this.OnWindowClosed();
        window.GetObservable(ContentControl.ContentProperty).Subscribe(this.OnWindowContentChanged);
        window.GetObservable(CarinaStudio.Controls.Window.HasDialogsProperty).Subscribe(this.OnWindowHasDialogsChanged);
        window.TemplateApplied += (_, e) => this.OnWindowTemplateApplied(e);
    }


    // Fade in content of window.
    void FadeInContent()
    {
        if (this.content is null && this.contentFadingOverlay is null)
            return;
        this.contentFadingAnimator?.Cancel();
        this.contentFadingAnimator = this.contentFadingOverlay != null
            ? new DoubleRenderingAnimator(this.window, this.contentFadingOverlay.Opacity, 0)
            : new DoubleRenderingAnimator(this.window, this.content.AsNonNull().Opacity, 1.0);
        this.contentFadingAnimator.Let(it =>
        {
            it.Completed += (_, _) =>
            {
                if (this.contentFadingOverlay != null)
                {
                    this.contentFadingOverlay.IsVisible = false;
                    this.contentFadingOverlay.Opacity = it.EndValue;
                }
                else if (this.content != null)
                    this.content.Opacity = it.EndValue;
                if (this.isContentBlurred)
                {
                    if (ReferenceEquals(this.content?.Effect, this.contentBlurEffect))
                        this.content!.Effect = null;
                    this.contentBlurEffect!.Radius = 0;
                }
                this.contentFadingAnimator = null;
                this.IsFadingContent = false;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFadingContent)));
            };
            it.Duration = ContentFadeInDuration;
            it.Interpolator = Interpolators.Deceleration;
            it.ProgressChanged += (_, _) =>
            {
                if (this.contentFadingOverlay != null)
                    this.contentFadingOverlay.Opacity = it.Value;
                else if (this.content != null)
                    this.content.Opacity = it.Value;
                if (this.isContentBlurred)
                    this.contentBlurEffect!.Radius = 1 - (ContentBlurRadius * it.Progress);
            };
            it.Start();
        });
        if (!this.IsFadingContent)
        {
            this.IsFadingContent = true;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFadingContent)));
        }
    }


    /// <summary>
    /// Check whether content of window is being fading in/out or not.
    /// </summary>
    public bool IsFadingContent { get; private set; }
    
    
    // Called when window closed.
    void OnWindowClosed() =>
        this.fadeInContentAction.Cancel();
    
    
    // Called when content of window changed.
    void OnWindowContentChanged(object? content)
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
        if (this.content != null && this.contentFadingOverlay is null)
            this.content.Opacity = window.HasDialogs ? ContentFadeOutOpacity : 1.0;
    }
    
    
    // Called when dialog state of window changed.
    void OnWindowHasDialogsChanged(bool hasDialogs)
    {
        if (this.content is null && this.contentFadingOverlay is null)
            return;
        if (hasDialogs)
        {
            this.isContentBlurred = IAppSuiteApplication.CurrentOrNull?.Configuration.GetValueOrDefault(ConfigurationKeys.MakeContentBlurredWhenShowingDialog) ?? false;
            if (this.isContentBlurred)
                this.contentBlurEffect ??= new();
            this.fadeInContentAction.Cancel();
            this.contentFadingAnimator?.Cancel();
            this.contentFadingAnimator = this.contentFadingOverlay is not null
                ? new DoubleRenderingAnimator(window, this.contentFadingOverlay.Opacity, 1 - ContentFadeOutOpacity)
                : new DoubleRenderingAnimator(window, this.content.AsNonNull().Opacity, ContentFadeOutOpacity);
            this.contentFadingAnimator.Let(it =>
            {
                it.Completed += (_, _) =>
                {
                    if (this.contentFadingOverlay is not null)
                        this.contentFadingOverlay.Opacity = it.EndValue;
                    else if (this.content is not null)
                        this.content.Opacity = it.EndValue;
                    if (this.isContentBlurred)
                        this.contentBlurEffect!.Radius = ContentBlurRadius;
                    this.contentFadingAnimator = null;
                    this.IsFadingContent = false;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFadingContent)));
                };
                it.Delay = ContentFadeOutDelay;
                it.Duration = ContentFadeOutDuration;
                it.Interpolator = Interpolators.Deceleration;
                it.ProgressChanged += (_, _) =>
                {
                    if (this.contentFadingOverlay is not null)
                        this.contentFadingOverlay.Opacity = it.Value;
                    else if (this.content is not null)
                        this.content.Opacity = it.Value;
                    if (this.isContentBlurred)
                        this.contentBlurEffect!.Radius = ContentBlurRadius * it.Progress;
                };
                it.Start();
                if (this.contentFadingOverlay is not null)
                    this.contentFadingOverlay.IsVisible = true;
                if (this.content is not null && this.isContentBlurred)
                    this.content.Effect ??= this.contentBlurEffect;
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
    
    
    // Called when template applied on window.
    void OnWindowTemplateApplied(TemplateAppliedEventArgs e)
    {
        this.fadeInContentAction.Cancel();
        if (this.contentFadingAnimator is not null)
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
        if (this.contentFadingOverlay is not null)
        {
            if (this.content is not null)
                this.content.Opacity = 1;
            this.contentFadingOverlay.Opacity = window.HasDialogs ? 1 - ContentFadeOutOpacity : 0;
        }
    }


    /// <summary>
    /// Raised when property changed.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}