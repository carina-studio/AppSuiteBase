using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using CarinaStudio.Animation;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using CarinaStudio.VisualTree;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="TutorialPresenter"/> which shows tutorial in full window mode.
/// </summary>
public class FullWindowTutorialPresenter : TutorialPresenter
{
    // Fields.
    readonly Pen anchorBorderPen = new();
    Rect anchorBounds;
    IDisposable? anchorBoundsObserverToken;
    DoubleAnimator? backgroundAnimator;
    IBrush? backgroundBrush;
    // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
    readonly EventHandler<AvaloniaPropertyChangedEventArgs> backgroundPropertyChangedHandler;
    // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable
    IDisposable? backgroundPropertyChangedHandlerToken;
    Avalonia.Controls.TextBlock? descriptionTextBlock1;
    Avalonia.Controls.TextBlock? descriptionTextBlock2;
    Control? dismissControl;
    bool isPointerMovedAfterShowingTutorial;
    Control? root;
    Control? skipAllTutorialsControl;
    Control? tutorialContainer;
    readonly ScheduledAction updateTutorialPositionAction;


    /// <summary>
    /// Initialize new <see cref="FullWindowTutorialPresenter"/> instance.
    /// </summary>
    public FullWindowTutorialPresenter()
    {
        this.anchorBorderPen.Bind(Pen.BrushProperty, this.GetResourceObservable("Brush/Accent"));
        this.anchorBorderPen.Thickness = 2;
        this.backgroundPropertyChangedHandler = this.OnBackgroundPropertyChanged;
        this.updateTutorialPositionAction = new(this.UpdateTutorialPosition);
        this.GetObservable(BackgroundProperty).Subscribe(background =>
        {
            this.backgroundPropertyChangedHandlerToken = this.backgroundPropertyChangedHandlerToken.DisposeAndReturnNull();
            if (background is Brush brush)
                this.backgroundPropertyChangedHandlerToken = brush.AddWeakEventHandler(nameof(PropertyChanged), this.backgroundPropertyChangedHandler);
            this.CloneBackgroundBrush();
        });
        this.GetObservable(BoundsProperty).Subscribe(_ =>
        {
            if (this.CurrentTutorial != null)
                this.updateTutorialPositionAction.Schedule();
        });
    }


    // Clone background brush to local instance.
    void CloneBackgroundBrush()
    {
        var background = this.Background;
        if (background is not Brush brush)
        {
            this.backgroundBrush = null;
            return;
        }
        if (brush is ISolidColorBrush solidColorBrush)
        {
            if (this.backgroundBrush is SolidColorBrush localSolidColorBrush)
                localSolidColorBrush.Color = solidColorBrush.Color;
            else
                this.backgroundBrush = new SolidColorBrush(solidColorBrush.Color);
        }
        else
            this.backgroundBrush = null;
    }


    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        var nameScope = e.NameScope;
        this.descriptionTextBlock1 = nameScope.Find<Avalonia.Controls.TextBlock>("PART_Description");
        if (this.descriptionTextBlock1 is not null)
        {
            this.descriptionTextBlock2 = nameScope.Find<Avalonia.Controls.TextBlock>("PART_AlternativeDescription");
            if (this.descriptionTextBlock2 is null)
                this.descriptionTextBlock1 = null;
        }
        this.dismissControl = nameScope.Find<Control>("PART_Dismiss");
        this.root = nameScope.Find<Control>("PART_Root").AsNonNull().Also(it =>
        {
            it.IsVisible = false;
            it.Opacity = 0;
        });
        this.skipAllTutorialsControl = nameScope.Find<Control>("PART_SkipAllTutorials");
        this.tutorialContainer = nameScope.Find<Control>("PART_TutorialContainer")?.Also(it =>
        {
            it.LayoutUpdated += (_, _) =>
            {
                // [Workaround] Make sure that description with CJK characters won't be clipped unexpectedly
                if (this.descriptionTextBlock1 is null || this.descriptionTextBlock2 is null)
                    return;
                if (this.descriptionTextBlock1.Bounds.Height >= this.descriptionTextBlock2.Bounds.Height)
                {
                    this.descriptionTextBlock1.Opacity = 1;
                    this.descriptionTextBlock2.Opacity = 0;
                }
                else
                {
                    this.descriptionTextBlock1.Opacity = 0;
                    this.descriptionTextBlock2.Opacity = 1;
                }
            };
        });
        this.CurrentTutorial?.Let(this.OnShowTutorial);
    }


    // Called when property pf background brush changed.
    void OnBackgroundPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        this.CloneBackgroundBrush();
        if (this.CurrentTutorial is not null)
            this.InvalidateVisual();
    }


    /// <inheritdoc/>
    protected override void OnDismissTutorial(Tutorial tutorial)
    {
        // hide
        this.backgroundAnimator?.Cancel();
        if (this.root != null)
        {
            this.backgroundAnimator = new DoubleAnimator(this.root.Opacity, 0).Also(animator =>
            {
                animator.Cancelled += (_, _) =>
                {
                    this.tutorialContainer?.Let(it =>
                        it.DataContext = null);
                    this.InvalidateVisual();
                };
                animator.Completed += (_, _) =>
                {
                    this.root.IsVisible = false;
                    this.root.Opacity = animator.EndValue;
                    this.tutorialContainer?.Let(it =>
                        it.DataContext = null);
                    this.InvalidateVisual();
                };
                animator.Duration = (TimeSpan)this.FindResource("TimeSpan/Animation").AsNonNull();
                animator.Interpolator = Interpolators.Deceleration;
                animator.ProgressChanged += (_, _) => 
                {
                    this.root.Opacity = animator.Value;
                    this.InvalidateVisual();
                };
                animator.Start();
            });
        }
        else
        {
            this.tutorialContainer?.Let(it =>
                it.DataContext = null);
        }
        // [Workaround] Clear brush transitions of button
        this.dismissControl?.FindDescendantOfTypeAndName<Avalonia.Controls.Presenters.ContentPresenter>("PART_ContentPresenter")?.Let(it => 
        {
            var transitions = it.Transitions;
            it.Transitions = null;
            it.Background = null;
            it.BorderBrush = null;
            if (transitions != null)
                this.SynchronizationContext.Post(() => it.Transitions = transitions);
        });
        
        // stop monitor bounds change of anchor
        this.anchorBoundsObserverToken = this.anchorBoundsObserverToken.DisposeAndReturnNull();

        // remove key event handler
        this.RemoveHandler(KeyDownEvent, this.OnPreviewKeyDown);
        this.RemoveHandler(PointerMovedEvent, this.OnPreviewPointerMoved);
    }


    // Handle key down event.
    void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab && !e.Handled && this.CurrentTutorial != null)
        {
            Global.Run(() =>
            {
                if (this.dismissControl?.IsFocused == true)
                {
                    if (this.skipAllTutorialsControl?.IsEffectivelyVisible == true
                        && this.skipAllTutorialsControl.IsEffectivelyEnabled)
                    {
                        return this.skipAllTutorialsControl;
                    }
                }
                else if (this.dismissControl?.IsEffectivelyVisible == true
                    && this.dismissControl.IsEffectivelyEnabled)
                {
                    return this.dismissControl;
                }
                return this.tutorialContainer ?? this;
            }).Focus();
            e.Handled = true;
        }
    }


    // Handle pointer moved event.
    void OnPreviewPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!this.isPointerMovedAfterShowingTutorial && this.CurrentTutorial != null)
        {
            this.isPointerMovedAfterShowingTutorial = true;
            this.dismissControl?.FindDescendantOfTypeAndName<Avalonia.Controls.Presenters.ContentPresenter>("PART_ContentPresenter")?.Let(it => 
            {
                // [Workaround] Restore brush transitions of button
                it.ClearValue(BackgroundProperty);
                it.ClearValue(BorderBrushProperty);
            });
        }
    }


    /// <inheritdoc/>
    protected override void OnShowTutorial(Tutorial tutorial)
    {
        // show
        this.backgroundAnimator?.Cancel();
        if (this.root != null)
        {
            this.root.IsVisible = true;
            if (this.root.Opacity >= 0.95)
                this.root.Opacity = 1;
            else
            {
                this.backgroundAnimator = new DoubleAnimator(this.root.Opacity, 1).Also(animator =>
                {
                    animator.Completed += (_, _) => this.root.Opacity = animator.EndValue;
                    animator.Duration = (TimeSpan)this.FindResource("TimeSpan/Animation").AsNonNull();
                    animator.Interpolator = Interpolators.Deceleration;
                    animator.ProgressChanged += (_, _) =>
                    {
                        this.root.Opacity = animator.Value;
                        this.InvalidateVisual();
                    };
                    animator.Start();
                });
            }
        }
        (this.tutorialContainer?.RenderTransform as ScaleTransform)?.Let(transform =>
        {
            var transitions = transform.Transitions;
            transform.Transitions = null;
            transform.ScaleX = this.FindResourceOrDefault("Double/FullWindowTutorialPresenter.Tutorial.InitScaleX", 0.5);
            transform.ScaleY = this.FindResourceOrDefault("Double/FullWindowTutorialPresenter.Tutorial.InitScaleY", 0.5);
            transform.Transitions = transitions;
            transform.ScaleX = 1;
            transform.ScaleY = 1;
        });

        // add input event handler
        this.AddHandler(KeyDownEvent, this.OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        this.AddHandler(PointerMovedEvent, this.OnPreviewPointerMoved, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        this.isPointerMovedAfterShowingTutorial = false;

        // setup tutorial and its position
        if (this.tutorialContainer != null)
        {
            this.tutorialContainer.Opacity = 0;
            this.updateTutorialPositionAction.Schedule();
            this.SynchronizationContext.Post(() =>
            {
                this.dismissControl?.Let(it => it.IsVisible = true);
                this.tutorialContainer.DataContext = tutorial;
                this.tutorialContainer.Opacity = 1;
            });
        }
        
        // monitor bounds change of anchor
        this.anchorBoundsObserverToken = tutorial.Anchor?.GetObservable(BoundsProperty).Subscribe(_ =>
        {
            this.updateTutorialPositionAction.Schedule();
        });

        // setup focus
        this.SynchronizationContext.Post(() =>
        {
            if (this.CurrentTutorial == null)
                return;
            if (this.dismissControl?.IsEffectivelyVisible == true 
                && this.dismissControl.IsEffectivelyEnabled)
            {
                this.dismissControl.Focus();
            }
            else if (this.skipAllTutorialsControl?.IsEffectivelyVisible == true 
                && this.skipAllTutorialsControl.IsEffectivelyEnabled)
            {
                this.skipAllTutorialsControl.Focus();
            }
            else if (this.tutorialContainer != null)
                this.tutorialContainer.Focus();
            else
                this.Focus();
        });
        
        // invalidate
        this.InvalidateVisual();
    }


    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        // call base
        base.Render(context);
        if (this.root?.IsVisible != true)
            return;

        // get state
        var brush = this.backgroundBrush;
        if (brush is null)
            return;
        var bounds = this.Bounds;
        var anchorBounds = this.anchorBounds;
        (brush as Brush)?.Let(it =>
        {
            var baseOpacity = (this.Background as Brush)?.Opacity ?? 1.0;
            it.Opacity = (this.backgroundAnimator?.Value ?? 1.0) * baseOpacity;
        });

        // draw 
        if (anchorBounds.Width <= 0 || anchorBounds.Height <= 0)
            context.DrawRectangle(brush, null, bounds);
        else
        {
            context.DrawRectangle(brush, null, new(bounds.X, bounds.Y, bounds.Width, anchorBounds.Y));
            context.DrawRectangle(brush, null, new(bounds.X, anchorBounds.Y, anchorBounds.X, anchorBounds.Height));
            context.DrawRectangle(brush, null, new(anchorBounds.Right, anchorBounds.Y, bounds.Width - anchorBounds.Right, anchorBounds.Height));
            context.DrawRectangle(brush, null, new(bounds.X, anchorBounds.Bottom, bounds.Width, bounds.Height - anchorBounds.Bottom));
            context.DrawRectangle(Brushes.Transparent, null, anchorBounds);
            context.DrawRectangle(null, this.anchorBorderPen, anchorBounds);
        }
    }


    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(FullWindowTutorialPresenter);


    // Update position of tutorial.
    void UpdateTutorialPosition()
    {
        var tutorial = this.CurrentTutorial;
        if (tutorial == null)
            return;
        this.tutorialContainer?.Let(it =>
        {
            var anchor = tutorial.Anchor;
            if (anchor != null && anchor.IsEffectivelyVisible)
            {
                // find self bounds on window
                var selfBounds = this.Bounds;
                var x = selfBounds.X;
                var y = selfBounds.Y;
                var parent = this.GetVisualParent();
                while (parent != null && parent is not Window)
                {
                    var parentBounds = parent.Bounds;
                    x += parentBounds.X;
                    y += parentBounds.Y;
                    parent = parent.GetVisualParent();
                }
                selfBounds = new Rect(x, y, selfBounds.Width, selfBounds.Height);

                // find anchor bounds on window
                var anchorBounds = anchor.Bounds;
                x = anchorBounds.X;
                y = anchorBounds.Y;
                if (anchor is not Window)
                {
                    parent = anchor.GetVisualParent();
                    while (parent != null && parent is not Window)
                    {
                        var parentBounds = parent.Bounds;
                        x += parentBounds.X;
                        y += parentBounds.Y;
                        parent = parent.GetVisualParent();
                    }
                    anchorBounds = new Rect(x, y, anchorBounds.Width, anchorBounds.Height);
                }

                // show tutorial at center if anchor is not visible in bounds
                if (!selfBounds.Intersects(anchorBounds))
                {
                    it.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                    it.Margin = new();
                    it.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                    this.anchorBounds = new();
                    this.InvalidateVisual();
                    return;
                }
                anchorBounds = new(anchorBounds.X - selfBounds.X, anchorBounds.Y - selfBounds.Y, anchorBounds.Width, anchorBounds.Height);
                selfBounds = new(0, 0, selfBounds.Width, selfBounds.Height);

                // setup horizontal position
                var offset = this.TryFindResource<double>("Double/FullWindowTutorialPresenter.Tutorial.Offset", out var res) ? res.GetValueOrDefault() : 0.0;
                var anchorCenter = anchorBounds.Center;
                var selfCenter = selfBounds.Center;
                var marginLeft = 0.0;
                var marginTop = 0.0;
                var marginRight = 0.0;
                var marginBottom = 0.0;
                var placeVertically = false;
                if (anchorBounds.Left < selfCenter.X && anchorBounds.Right > selfCenter.X)
                {
                    placeVertically = true;
                    if ((selfCenter.X - anchorBounds.Left + 1) >= (anchorBounds.Right - selfCenter.X))
                    {
                        marginLeft = Math.Max(offset, anchorBounds.Left);
                        it.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                    }
                    else
                    {
                        marginRight = Math.Max(offset, selfBounds.Width - anchorBounds.Right);
                        it.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
                    }
                }
                else if (anchorBounds.Right <= selfCenter.X)
                {
                    marginLeft = Math.Max(offset, anchorBounds.Right + offset);
                    it.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                }
                else
                {
                    marginRight = Math.Max(offset, selfBounds.Width - anchorBounds.Left + offset);
                    it.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
                }

                // setup vertical position
                if (anchorCenter.Y <= selfCenter.Y)
                {
                    if (placeVertically)
                        marginTop = anchorBounds.Bottom + offset;
                    else
                        marginTop = Math.Max(offset, anchorBounds.Y);
                    it.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
                }
                else
                {
                    if (placeVertically)
                        marginBottom = (selfBounds.Height - anchorBounds.Y + offset);
                    else
                        marginBottom = Math.Max(offset, selfBounds.Height - anchorBounds.Bottom);
                    it.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
                }
                it.Margin = new(marginLeft, marginTop, marginRight, marginBottom);

                // update background
                this.anchorBounds = anchorBounds;
                this.InvalidateVisual();
            }
            else
            {
                it.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                it.Margin = new();
                it.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                this.anchorBounds = new();
                this.InvalidateVisual();
            }
        });
    }
}