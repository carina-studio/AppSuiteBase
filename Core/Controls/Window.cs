using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Base class of window of AppSuite.
/// </summary>
public abstract class Window : CarinaStudio.Controls.ApplicationWindow<IAppSuiteApplication>, ITutorialPresenter
{
    /// <summary>
    /// Property of <see cref="CurrentTutorial"/>.
    /// </summary>
    public static readonly DirectProperty<Window, Tutorial?> CurrentTutorialProperty = AvaloniaProperty.RegisterDirect<Window, Tutorial?>(nameof(CurrentTutorial), w => w.currentTutorial);
    /// <summary>
    /// Property of <see cref="IsSystemChromeVisibleInClientArea"/>.
    /// </summary>
    public static readonly DirectProperty<Window, bool> IsSystemChromeVisibleInClientAreaProperty = AvaloniaProperty.RegisterDirect<Window, bool>(nameof(IsSystemChromeVisibleInClientArea), w => w.isSystemChromeVisibleInClientArea);


    // Static fields.
    internal static readonly Stopwatch Stopwatch = new Stopwatch().Also(it => it.Start());


    // Fields.
    readonly ScheduledAction checkSystemChromeVisibilityAction;
    // ReSharper disable once NotAccessedField.Local
    readonly WindowContentFadingHelper contentFadingHelper;
    Tutorial? currentTutorial;
    IDisposable? currentTutorialObserverToken;
    bool isSystemChromeVisibleInClientArea;
    double taskbarIconProgress;
    TaskbarIconProgressState taskbarIconProgressState = TaskbarIconProgressState.None;
    INameScope? templateNameScope;
    TutorialPresenter? tutorialPresenter;
    readonly ScheduledAction updateTransparencyLevelAction;


    /// <summary>
    /// Initialize new <see cref="Window{TApp}"/> instance.
    /// </summary>
    protected Window()
    {
        this.Application.HardwareInfo.PropertyChanged += this.OnHardwareInfoPropertyChanged;
        this.Settings.SettingChanged += this.OnSettingChanged;
        this.contentFadingHelper = new WindowContentFadingHelper(this).Also(it =>
        {
            it.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(WindowContentFadingHelper.IsFadingContent))
                    this.InvalidateTransparencyLevelHint();
            };
        });
        this.checkSystemChromeVisibilityAction = new ScheduledAction(() =>
        {
            this.SetAndRaise(IsSystemChromeVisibleInClientAreaProperty, 
                ref this.isSystemChromeVisibleInClientArea, 
                Global.Run(() =>
                {
                    if (this.SystemDecorations != SystemDecorations.Full)
                        return false;
                    if (!this.ExtendClientAreaToDecorationsHint)
                        return false;
                    if (this.WindowState == WindowState.FullScreen)
                        return ExtendedClientAreaWindowConfiguration.IsSystemChromeVisibleInFullScreen;
                    return true;
                }));
        });
        this.updateTransparencyLevelAction = new ScheduledAction(() =>
        {
            if (!this.IsClosed)
                this.TransparencyLevelHint = [ this.OnSelectTransparentLevelHint() ];
        });
        this.checkSystemChromeVisibilityAction.Schedule();
        this.updateTransparencyLevelAction.Schedule();
    }


    /// <inheritdoc/>
    public void CancelTutorial() =>
        this.tutorialPresenter?.CancelTutorial();


    /// <inheritdoc/>
    public Tutorial? CurrentTutorial => this.currentTutorial;


    /// <inheritdoc/>
    public void DismissTutorial() =>
        this.tutorialPresenter?.DismissTutorial();


    /// <summary>
    /// Invalidate and update <see cref="TopLevel.TransparencyLevelHint"/>.
    /// </summary>
    protected void InvalidateTransparencyLevelHint() => this.updateTransparencyLevelAction.Schedule();


    /// <summary>
    /// Check whether system chrome is visible in client area or not.
    /// </summary>
    public bool IsSystemChromeVisibleInClientArea => this.isSystemChromeVisibleInClientArea;


    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        this.templateNameScope = e.NameScope;
        this.tutorialPresenter = null;
    }


    /// <summary>
    /// Called when window closed.
    /// </summary>
    /// <param name="e">Event data.</param>
    protected override void OnClosed(EventArgs e)
    {
        this.checkSystemChromeVisibilityAction.Cancel();
        this.updateTransparencyLevelAction.Cancel();
        this.Application.HardwareInfo.PropertyChanged -= this.OnHardwareInfoPropertyChanged;
        this.Settings.SettingChanged -= this.OnSettingChanged;
        base.OnClosed(e);
        
        // [Workaround] Prevent Window leak by child controls
        this.SynchronizationContext.Post(() => this.Content = null);
    }


    // Called when property of hardware info changed.
    void OnHardwareInfoPropertyChanged(object? sender, PropertyChangedEventArgs e) => this.OnHardwareInfoPropertyChanged(e);


    /// <summary>
    /// Called when property of hardware info changed.
    /// </summary>
    /// <param name="e">Event data.</param>
    protected void OnHardwareInfoPropertyChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HardwareInfo.HasDedicatedGraphicsCard))
            this.InvalidateTransparencyLevelHint();
    }


    /// <summary>
    /// Called when window opened.
    /// </summary>
    /// <param name="e">Event data.</param>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (Platform.IsWindows)
        {
            (this.Application as AppSuiteApplication)?.Let(app =>
            {
                app.UpdateWindowsTaskBarProgress(this);
                app.UpdateWindowsTaskBarProgressState(this);
            });
        }
        this.checkSystemChromeVisibilityAction.Execute();
        this.updateTransparencyLevelAction.ExecuteIfScheduled();
    }


    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var property = change.Property;
        if (property == CurrentTutorialProperty)
            this.updateTransparencyLevelAction.Schedule();
        else if (property == ExtendClientAreaToDecorationsHintProperty || property == SystemDecorationsProperty || property == WindowStateProperty)
            this.checkSystemChromeVisibilityAction.Schedule();
        else if (property == IsActiveProperty)
            this.updateTransparencyLevelAction.Schedule();
    }


    /// <summary>
    /// Called to select transparency level to apply on window.
    /// </summary>
    /// <returns>Transparency level.</returns>
    protected virtual WindowTransparencyLevel OnSelectTransparentLevelHint() =>
        WindowTransparencyLevel.None;


    // Called when application setting changed.
    void OnSettingChanged(object? sender, SettingChangedEventArgs e) => this.OnSettingChanged(e);


    /// <summary>
    /// Called when application setting changed.
    /// </summary>
    /// <param name="e">Event data.</param>
    protected virtual void OnSettingChanged(SettingChangedEventArgs e)
    { }


    /// <inheritdoc/>
    public void RequestSkippingAllTutorials() =>
        this.tutorialPresenter?.RequestSkippingAllTutorials();
    

    /// <inheritdoc/>
    public bool ShowTutorial(Tutorial tutorial)
    {
        return this.TutorialPresenter?.Let(it =>
        {
            it.IsVisible = true;
            return it.ShowTutorial(tutorial);
        }) ?? false;
    }


    /// <summary>
    /// Get or set progress shown on taskbar icon. The range is [0.0, 1.0].
    /// </summary>
    internal protected double TaskbarIconProgress
    {
        get => this.taskbarIconProgress;
        protected set
        {
            this.VerifyAccess();
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (value < 0)
                value = 0;
            else if (value > 1)
                value = 1;
            this.taskbarIconProgress = value;
            if (Platform.IsWindows)
                (this.Application as AppSuiteApplication)?.UpdateWindowsTaskBarProgress(this);
            else if (Platform.IsMacOS)
                (this.Application as AppSuiteApplication)?.UpdateMacOSDockTileProgressState();
        }
    }


    /// <summary>
    /// Get or set progress state of taskbar icon.
    /// </summary>
    internal protected TaskbarIconProgressState TaskbarIconProgressState
    {
        get => this.taskbarIconProgressState;
        protected set
        {
            this.VerifyAccess();
            if (this.taskbarIconProgressState == value)
                return;
            this.taskbarIconProgressState = value;
            if (Platform.IsWindows)
                (this.Application as AppSuiteApplication)?.UpdateWindowsTaskBarProgressState(this);
            else if (Platform.IsMacOS)
                (this.Application as AppSuiteApplication)?.UpdateMacOSDockTileProgressState();
        }
    }


    /// <summary>
    /// Get <see cref="TutorialPresenter"/> of this window.
    /// </summary>
    protected TutorialPresenter? TutorialPresenter
    {
        get
        {
            this.tutorialPresenter ??= this.templateNameScope?.Find<TutorialPresenter>("PART_TutorialPresenter").Also(it =>
            {
                this.currentTutorialObserverToken = this.currentTutorialObserverToken.DisposeAndReturnNull();
                if (it != null)
                {
                    this.currentTutorialObserverToken = it.GetObservable(TutorialPresenter.CurrentTutorialProperty).Subscribe(tutorial =>
                        this.SetAndRaise(CurrentTutorialProperty, ref this.currentTutorial, tutorial));
                }
                else
                    this.SetAndRaise(CurrentTutorialProperty, ref this.currentTutorial, null);
            });
            return this.tutorialPresenter;
        }
    }
}


/// <summary>
/// Base class of window of AppSuite.
/// </summary>
/// <typeparam name="TApp">Type of application.</typeparam>
public abstract class Window<TApp> : Window where TApp : class, IAppSuiteApplication
{
    /// <summary>
    /// Get application instance.
    /// </summary>
    public new TApp Application => (TApp)base.Application;
}