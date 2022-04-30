using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using CarinaStudio.Animation;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="TutorialPresenter"/> which shows tutorial in full window mode.
/// </summary>
public class FullWindowTutorialPresenter : TutorialPresenter, IStyleable
{
    // Drawing of background.
    class BackgroundDrawing : Drawing
    {
        // Fields.
        readonly Pen anchorBorderPen = new Pen();
        readonly FullWindowTutorialPresenter presneter;

        // Constructor.
        public BackgroundDrawing(FullWindowTutorialPresenter presenter)
        {
            this.anchorBorderPen.Bind(Pen.BrushProperty, presenter.GetResourceObservable("Brush/Accent"));
            this.anchorBorderPen.Thickness = 2;
            this.presneter = presenter;
        }

        // Get bounds.
        public override Rect GetBounds()
        {
            var bounds = this.presneter.Bounds;
            return new Rect(0, 0, bounds.Width, bounds.Height);
        }

        // Draw.
        public override void Draw(DrawingContext context)
        {
            // get state
            var brush = this.presneter.Background;
            if (brush == null)
                return;
            var bounds = this.presneter.Bounds;
            var anchorBounds = this.presneter.anchorBounds;
            
            // draw 
            if (anchorBounds.IsEmpty)
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
    }


    // Fields.
    Rect anchorBounds;
    DoubleAnimator? backgroundAnimator;
#pragma warning disable CS0618
    DrawingPresenter? backgroundPresenter;
#pragma warning restore CS0618
    Control? dismissControl;
    Control? root;
    Control? skipAllTutorialsControl;
    Control? tutorialContainer;
    readonly ScheduledAction updateTutorialPositionAction;


    /// <summary>
    /// Initialize new <see cref="FullWindowTutorialPresenter"/> instance.
    /// </summary>
    public FullWindowTutorialPresenter()
    {
        this.updateTutorialPositionAction = new(this.UpdateTutorialPosition);
        this.GetObservable(BoundsProperty).Subscribe(_ =>
        {
            if (this.CurrentTutorial != null)
                this.updateTutorialPositionAction.Schedule();
        });
    }


    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
#pragma warning disable CS0618
        this.backgroundPresenter = e.NameScope.Find<DrawingPresenter>("PART_BackgroundPresenter")?.Also(it =>
        {
            it.Drawing = new BackgroundDrawing(this);
        });
#pragma warning restore CS0618
        this.dismissControl = e.NameScope.Find<Control>("PART_Dismiss");
        this.root = e.NameScope.Find<Control>("PART_Root").Also(it =>
        {
            it.IsVisible = false;
            it.Opacity = 0;
        });
        this.skipAllTutorialsControl = e.NameScope.Find<Control>("PART_SkipAllTutorials");
        this.tutorialContainer = e.NameScope.Find<Control>("PART_TutorialContainer");
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
                animator.Completed += (_, e) =>
                {
                    this.root.IsVisible = false;
                    this.root.Opacity = animator.EndValue;
                    this.tutorialContainer?.Let(it =>
                        it.DataContext = null);
                };
                animator.Duration = (TimeSpan)this.FindResource("TimeSpan/Animation").AsNonNull();
                animator.Interpolator = Interpolators.Deceleration;
                animator.ProgressChanged += (_, e) => this.root.Opacity = animator.Value;
                animator.Start();
            });
        }
        else
        {
            this.tutorialContainer?.Let(it =>
                it.DataContext = null);
        }

        // remove key event handler
        this.RemoveHandler(KeyDownEvent, this.OnPreviewKeyDown);
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
                    animator.Completed += (_, e) => this.root.Opacity = animator.EndValue;
                    animator.Duration = (TimeSpan)this.FindResource("TimeSpan/Animation").AsNonNull();
                    animator.Interpolator = Interpolators.Deceleration;
                    animator.ProgressChanged += (_, e) => this.root.Opacity = animator.Value;
                    animator.Start();
                });
            }
        }

        // setup data context
        this.tutorialContainer?.Let(it =>
            it.DataContext = tutorial);

        // add key event handler
        this.AddHandler(KeyDownEvent, this.OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);

        // setup tutorial position
        this.updateTutorialPositionAction.Schedule();

        // setup focus
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
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
    }


    // Update position of tutorial.
    void UpdateTutorialPosition()
    {
        var tutorial = this.CurrentTutorial;
        if (tutorial == null)
            return;
        this.tutorialContainer?.Let(it =>
        {
            var anchor = tutorial.Anchor;
            if (anchor != null)
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
                    this.anchorBounds = new();
                    it.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                    it.Margin = new();
                    it.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
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

                // keep anchor bounds
                this.anchorBounds = anchorBounds;
            }
            else
            {
                this.anchorBounds = new();
                it.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                it.Margin = new();
                it.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            }
        });
    }


    // Interface implementations.
    Type IStyleable.StyleKey { get => typeof(FullWindowTutorialPresenter); }
}