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
    /// Define <see cref="BackdropEffect"/> property.
    /// </summary>
    public static readonly StyledProperty<BackdropEffect> BackdropEffectProperty = AvaloniaProperty.Register<Backdrop, BackdropEffect>(nameof(BackdropEffect), BackdropEffect.None);
    /// <summary>
    /// Define <see cref="IsBackdropEffectActive"/> property.
    /// </summary>
    public static readonly DirectProperty<Backdrop, bool> IsBackdropEffectActiveProperty = AvaloniaProperty.RegisterDirect<Backdrop, bool>(nameof(IsBackdropEffectActive), o => o.IsBackdropEffectActive);
    /// <summary>
    /// Define <see cref="Strength"/> property.
    /// </summary>
    public static readonly StyledProperty<double> StrengthProperty = AvaloniaProperty.Register<Backdrop, double>(nameof(Strength), SettingKeys.DefaultBackdropEffectStrength.DefaultValue, coerce: (_, strength) => CoerceStrength(strength));
    /// <summary>
    /// Define <see cref="Target"/> property.
    /// </summary>
    public static readonly StyledProperty<BackdropTarget?> TargetProperty = AvaloniaProperty.Register<Backdrop, BackdropTarget?>(nameof(Target));


    // Visual which draws the backdrop and applies the blur effect, kept behind the content so the content stays sharp.
    class BackdropLayer(Backdrop owner) : Control
    {
        /// <inheritdoc/>
        public override void Render(DrawingContext context)
        {
            if (this.Opacity > 0)
                owner.RenderBackdrop(context);
        }
    }


    // Constants.
    const double BlurRadius = 96;
    const double LayerOpacityGamma = 0.8;
    const double LensBlurExpansion = 16;
    const double MaxLayerOpacity = 0.5;


    // Static fields.
    static bool? isSupported;
    static readonly ImmutableBlurEffect SharedBlurEffect = new(BlurRadius);


    // Fields.
    readonly BackdropLayer backdropLayer;
    ISettings? configuration;
    IDisposable? defaultStrengthToken;
    bool isAttachedToVisualTree;
    bool isBackdropEffectActive;
    bool isBackdropEffectEnabled = ConfigurationKeys.EnableBackdropEffect.DefaultValue;
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

        // apply the initial strength as the opacity of the backdrop layer
        this.UpdateBackdropLayerOpacity();
    }


    // Apply the default strength from settings to the Strength property at Style priority, so an explicit Strength (LocalValue) on the consumer overrides it.
    void ApplyDefaultStrength()
    {
        this.defaultStrengthToken = this.defaultStrengthToken.DisposeAndReturnNull();
        if (this.settings is not null)
            this.defaultStrengthToken = this.SetValue(StrengthProperty, GetValidDefaultStrength(this.settings), BindingPriority.Style);
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
    /// Get or set the effect applied to the backdrop.
    /// </summary>
    public BackdropEffect BackdropEffect
    {
        get => this.GetValue(BackdropEffectProperty);
        set => this.SetValue(BackdropEffectProperty, value);
    }


    // Coerce given backdrop effect strength to a valid value within [0, 1].
    static double CoerceStrength(double strength)
    {
        if (!double.IsFinite(strength))
            return SettingKeys.DefaultBackdropEffectStrength.DefaultValue;
        return Math.Clamp(strength, 0, 1);
    }


    // Get a valid default strength from settings.
    static double GetValidDefaultStrength(ISettings settings) =>
        CoerceStrength(settings.GetValueOrDefault(SettingKeys.DefaultBackdropEffectStrength));


    /// <summary>
    /// Check whether the backdrop effect is active, i.e. <see cref="Target"/> is set, <see cref="BackdropEffect"/> is not <see cref="BackdropEffect.None"/>, and the application is rendering with the GPU pipeline.
    /// </summary>
    public bool IsBackdropEffectActive
    {
        get => this.isBackdropEffectActive;
        private set => this.SetAndRaise(IsBackdropEffectActiveProperty, ref this.isBackdropEffectActive, value);
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


    // Map strength [0, 1] to the internal opacity [0, MaxLayerOpacity] of the backdrop layer.
    static double MapStrengthToOpacity(double strength) =>
        Math.Pow(CoerceStrength(strength), LayerOpacityGamma) * MaxLayerOpacity;


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
            this.configuration = app.Configuration;
            this.configuration.SettingChanged += this.OnConfigChanged;
            this.settings = app.Settings;
            this.settings.SettingChanged += this.OnSettingChanged;
            this.isBackdropEffectEnabled = this.configuration.GetValueOrDefault(ConfigurationKeys.EnableBackdropEffect);
            this.ApplyDefaultStrength();
        }
        else
            this.isBackdropEffectEnabled = ConfigurationKeys.EnableBackdropEffect.DefaultValue;

        // update state
        this.UpdateIsBackdropEffectActive();
    }


    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // stop applying the default backdrop strength
        if (this.configuration is not null)
        {
            this.configuration.SettingChanged -= this.OnConfigChanged;
            this.configuration = null;
        }
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
        if (property == StrengthProperty)
            this.UpdateBackdropLayerOpacity();
        else if (property == TargetProperty)
        {
            this.UpdateRegistration();
            this.UpdateIsBackdropEffectActive();
        }
        else if (property == BackdropEffectProperty)
        {
            this.UpdateBackdropLayer();
            this.UpdateIsBackdropEffectActive();
        }
    }


    // Called when an application config is changed.
    void OnConfigChanged(object? sender, SettingChangedEventArgs e)
    {
        if (this.CheckAccess())
            this.OnConfigChanged(e);
        else
            Dispatcher.UIThread.Post(() => this.OnConfigChanged(e));
    }


    // Called when an application config is changed.
    void OnConfigChanged(SettingChangedEventArgs e)
    {
        if (e.Key == ConfigurationKeys.EnableBackdropEffect)
        {
            this.isBackdropEffectEnabled = (bool)e.Value;
            this.UpdateIsBackdropEffectActive();
            this.backdropLayer.InvalidateVisual();
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
        if (e.Key == SettingKeys.DefaultBackdropEffectStrength)
            this.ApplyDefaultStrength();
    }


    // Draw the backdrop into the backdrop layer according to the current backdrop effect.
    void RenderBackdrop(DrawingContext context)
    {
        // skip if there is nothing to draw, or the GPU pipeline is not used (snapshot + blur is only worthwhile on the GPU)
        var target = this.registeredTarget;
        if (target is null || this.BackdropEffect == BackdropEffect.None || !IsSupported || !this.isBackdropEffectEnabled)
            return;

        // extend the region on all sides so the blur effect has real content to sample at the edges, otherwise the blur weakens towards the edges (the layer does not clip to bounds, so the extra drawing is included in the effect)
        var destRect = new Rect(this.backdropLayer.Bounds.Size).Inflate(BlurRadius);

        // sample a larger region for lens blur, otherwise sample the region directly behind
        var srcRect = this.BackdropEffect == BackdropEffect.LensBlur ? destRect.Inflate(LensBlurExpansion) : destRect;

        // draw (the target fills its background color first, then the blur is applied by the layer's effect, so the content drawn on top stays sharp)
        target.DrawBackdrop(context, this.backdropLayer, srcRect, destRect);
    }


    /// <summary>
    /// Get or set the strength of the backdrop effect, in range [0, 1]. Defaults to the application's <see cref="SettingKeys.DefaultBackdropEffectStrength"/> setting until set explicitly.
    /// </summary>
    public double Strength
    {
        get => this.GetValue(StrengthProperty);
        set => this.SetValue(StrengthProperty, value);
    }


    /// <summary>
    /// Get or set the <see cref="BackdropTarget"/> which provides the backdrop.
    /// </summary>
    public BackdropTarget? Target
    {
        get => this.GetValue(TargetProperty);
        set => this.SetValue(TargetProperty, value);
    }


    // Update the effect of the backdrop layer according to the current backdrop effect, and request it to redraw.
    void UpdateBackdropLayer()
    {
        this.backdropLayer.Effect = this.BackdropEffect switch
        {
            BackdropEffect.Blur or BackdropEffect.LensBlur => SharedBlurEffect,
            _ => null,
        };
        this.backdropLayer.InvalidateVisual();
    }


    // Update the opacity of the backdrop layer from the current strength.
    void UpdateBackdropLayerOpacity() =>
        this.backdropLayer.Opacity = MapStrengthToOpacity(this.Strength);


    // Update IsBackdropEffectActive according to the current target, effect and rendering pipeline.
    void UpdateIsBackdropEffectActive() =>
        this.IsBackdropEffectActive = this.Target is not null && this.BackdropEffect != BackdropEffect.None && IsSupported && this.isBackdropEffectEnabled;


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
}
