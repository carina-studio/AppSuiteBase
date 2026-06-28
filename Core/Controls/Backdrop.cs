using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using CarinaStudio.Configuration;
using System;
using System.Reflection;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="Decorator"/> which draws the backdrop provided by a <see cref="BackdropTarget"/> behind its content.
/// </summary>
public class Backdrop : Decorator
{
    /// <summary>
    /// Define <see cref="DefaultOpacity"/> property.
    /// </summary>
    public static readonly DirectProperty<Backdrop, double> DefaultOpacityProperty = AvaloniaProperty.RegisterDirect<Backdrop, double>(nameof(DefaultOpacity), o => o.defaultOpacity);
    /// <summary>
    /// Define <see cref="IsBackdropActive"/> property.
    /// </summary>
    public static readonly DirectProperty<Backdrop, bool> IsBackdropActiveProperty = AvaloniaProperty.RegisterDirect<Backdrop, bool>(nameof(IsBackdropActive), o => o.IsBackdropActive);
    /// <summary>
    /// Define <see cref="Target"/> property.
    /// </summary>
    public static readonly StyledProperty<BackdropTarget?> TargetProperty = AvaloniaProperty.Register<Backdrop, BackdropTarget?>(nameof(Target));
    /// <summary>
    /// Define <see cref="Type"/> property.
    /// </summary>
    public static readonly StyledProperty<BackdropType> TypeProperty = AvaloniaProperty.Register<Backdrop, BackdropType>(nameof(Type), BackdropType.None);


    // Constants.
    const double BlurRadius = 96;
    const double LensBlurExpansion = 16;


    // Static fields.
    static bool? isSupported;
    static readonly ImmutableBlurEffect SharedBlurEffect = new(BlurRadius);


    // Fields.
    readonly BackdropLayer backdropLayer;
    double defaultOpacity = SettingKeys.DefaultBackdropStrength.DefaultValue;
    IDisposable? defaultStrengthToken;
    bool isAttachedToVisualTree;
    bool isBackdropActive;
    BackdropTarget? registeredTarget;
    ISettings? settings;


    /// <summary>
    /// Initialize new <see cref="Backdrop"/> instance.
    /// </summary>
    public Backdrop()
    {
        // create the backdrop layer behind the content (added before Child, which Decorator appends)
        this.backdropLayer = new(this);
        this.VisualChildren.Add(this.backdropLayer);
        this.UpdateBackdropLayer();
    }
    
    
    // Apply the default Opacity at Style priority, so an explicit Opacity binding/value (LocalValue) on the consumer overrides it.
    // Skip while a custom OpacityMask is set: the consumer is shaping transparency itself, and the uniform default Opacity on top would attenuate the backdrop twice.
    void ApplyDefaultOpacity()
    {
        this.defaultStrengthToken = this.defaultStrengthToken.DisposeAndReturnNull();
        if (this.OpacityMask is null)
            this.defaultStrengthToken = this.SetValue(OpacityProperty, this.defaultOpacity, BindingPriority.Style);
    }


    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // arrange content
        var size = base.ArrangeOverride(finalSize);

        // arrange the backdrop layer to fill the whole control
        this.backdropLayer.Arrange(new(finalSize));
        return size;
    }


    /// <summary>
    /// Get default opacity of backdrop according to settings.
    /// </summary>
    public double DefaultOpacity => this.defaultOpacity;


    /// <summary>
    /// Check whether the backdrop is active, i.e. <see cref="Target"/> is set, <see cref="Type"/> is not <see cref="BackdropType.None"/>, and the application is rendering with the GPU pipeline.
    /// </summary>
    public bool IsBackdropActive
    {
        get => this.isBackdropActive;
        private set => this.SetAndRaise(IsBackdropActiveProperty, ref this.isBackdropActive, value);
    }


    /// <summary>
    /// Check whether the backdrop is supported by the current rendering pipeline or not. The backdrop requires the GPU pipeline and is not supported with software rendering.
    /// </summary>
    // The GPU pipeline registers an IPlatformGraphics; software rendering registers none. The locator is internal in 11.3, so it is reached by reflection (fail-fast on Avalonia upgrade).
    public static bool IsSupported
    {
        get
        {
            if (isSupported.HasValue)
                return isSupported.Value;
            var resolver = typeof(AvaloniaLocator).GetProperty("Current", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).AsNonNull().GetValue(null).AsNonNull();
            var getService = typeof(IAvaloniaDependencyResolver).GetMethod("GetService", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).AsNonNull();
            isSupported = getService.Invoke(resolver, [typeof(IPlatformGraphics)]) is not null;
            return isSupported.Value;
        }
    }


    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // measure content
        var size = base.MeasureOverride(availableSize);

        // measure the backdrop layer
        this.backdropLayer.Measure(availableSize);
        return size;
    }


    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // register to the target
        base.OnAttachedToVisualTree(e);
        this.isAttachedToVisualTree = true;
        this.UpdateRegistration();

        // start applying the default backdrop strength from the application setting
        if (IAppSuiteApplication.CurrentOrNull is { } app)
        {
            this.settings = app.Settings;
            this.settings.SettingChanged += this.OnSettingChanged;
            var defaultOpacity = this.settings.GetValueOrDefault(SettingKeys.DefaultBackdropStrength);
            this.SetAndRaise(DefaultOpacityProperty, ref this.defaultOpacity, double.IsFinite(defaultOpacity) 
                ? Math.Clamp(defaultOpacity, 0, 1) 
                : SettingKeys.DefaultBackdropStrength.DefaultValue);
            this.ApplyDefaultOpacity();
        }
    }


    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // stop applying the default backdrop strength
        if (this.settings is not null)
        {
            this.settings.SettingChanged -= this.OnSettingChanged;
            this.settings = null;
        }
        this.defaultStrengthToken = this.defaultStrengthToken.DisposeAndReturnNull();

        // unregister from the target
        this.isAttachedToVisualTree = false;
        this.UpdateRegistration();
        base.OnDetachedFromVisualTree(e);
    }


    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var property = change.Property;
        if (property == DefaultOpacityProperty || property == OpacityMaskProperty)
            this.ApplyDefaultOpacity();
        else if (property == TargetProperty)
        {
            this.UpdateRegistration();
            this.UpdateIsBackdropActive();
        }
        else if (property == TypeProperty)
        {
            this.UpdateBackdropLayer();
            this.UpdateIsBackdropActive();
        }
    }


    // Called when an application setting is changed.
    void OnSettingChanged(object? sender, SettingChangedEventArgs e)
    {
        if (this.CheckAccess())
            this.OnSettingChanged(e);
        else
            Dispatcher.UIThread.Post(() => this.OnSettingChanged(e));
    }
    
    
    // Called when an application setting is changed.
    void OnSettingChanged(SettingChangedEventArgs e)
    {
        if (e.Key == SettingKeys.DefaultBackdropStrength && this.settings is not null)
        {
            var defaultOpacity = (double)e.Value;
            this.SetAndRaise(DefaultOpacityProperty, ref this.defaultOpacity, double.IsFinite(defaultOpacity) 
                ? Math.Clamp(defaultOpacity, 0, 1) 
                : SettingKeys.DefaultBackdropStrength.DefaultValue);
        }
    }


    // Draw the backdrop into the backdrop layer according to the current type.
    void RenderBackdrop(DrawingContext context)
    {
        // skip if there is nothing to draw, or the GPU pipeline is not used (snapshot + blur is only worthwhile on the GPU)
        var target = this.registeredTarget;
        if (target is null || this.Type == BackdropType.None || !IsSupported)
            return;

        // extend the region on all sides so the blur effect has real content to sample at the edges, otherwise the blur weakens towards the edges (the layer does not clip to bounds, so the extra drawing is included in the effect)
        var destRect = new Rect(this.backdropLayer.Bounds.Size).Inflate(BlurRadius);

        // sample a larger region for lens blur, otherwise sample the region directly behind
        var srcRect = this.Type == BackdropType.LensBlur ? destRect.Inflate(LensBlurExpansion) : destRect;

        // draw (the target fills its background color first, then the blur is applied by the layer's effect, so the content drawn on top stays sharp)
        target.DrawBackdrop(context, this.backdropLayer, srcRect, destRect);
    }


    /// <summary>
    /// Get or set the <see cref="BackdropTarget"/> which provides the backdrop.
    /// </summary>
    public BackdropTarget? Target
    {
        get => this.GetValue(TargetProperty);
        set => this.SetValue(TargetProperty, value);
    }


    /// <summary>
    /// Get or set the type of backdrop to draw.
    /// </summary>
    public BackdropType Type
    {
        get => this.GetValue(TypeProperty);
        set => this.SetValue(TypeProperty, value);
    }


    // Update the effect of the backdrop layer according to the current type, and request it to redraw.
    void UpdateBackdropLayer()
    {
        this.backdropLayer.Effect = this.Type switch
        {
            BackdropType.Blur or BackdropType.LensBlur => SharedBlurEffect,
            _ => null,
        };
        this.backdropLayer.InvalidateVisual();
    }


    // Update IsBackdropActive according to the current target, type and rendering pipeline.
    void UpdateIsBackdropActive() =>
        this.IsBackdropActive = this.Target is not null && this.Type != BackdropType.None && IsSupported;


    // Register the backdrop layer to or unregister it from the target according to the current attachment state and Target property.
    void UpdateRegistration()
    {
        // resolve the target to register to (only while attached to the visual tree)
        var target = this.isAttachedToVisualTree ? this.Target : null;
        if (this.registeredTarget == target)
            return;

        // re-register
        this.registeredTarget?.Unregister(this.backdropLayer);
        this.registeredTarget = target;
        target?.Register(this.backdropLayer);

        // redraw with the new target
        this.backdropLayer.InvalidateVisual();
    }


    // Visual which draws the backdrop and applies the blur effect, kept behind the content so the content stays sharp.
    class BackdropLayer(Backdrop owner) : Control
    {
        /// <inheritdoc/>
        public override void Render(DrawingContext context) =>
            owner.RenderBackdrop(context);
    }
}
