using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Base class of window of AppSuite.
    /// </summary>
    public abstract class Window : CarinaStudio.Controls.Window<IAppSuiteApplication>, ITutorialPresenter
    {
        /// <summary>
        /// Property of <see cref="CurrentTutorial"/>.
        /// </summary>
        public static readonly AvaloniaProperty<Tutorial?> CurrentTutorialProperty = AvaloniaProperty.RegisterDirect<Window, Tutorial?>(nameof(CurrentTutorial), w => w.currentTutorial);
        /// <summary>
        /// Property of <see cref="IsSystemChromeVisibleInClientArea"/>.
        /// </summary>
        public static readonly AvaloniaProperty<bool> IsSystemChromeVisibleInClientAreaProperty = AvaloniaProperty.Register<Window, bool>(nameof(IsSystemChromeVisibleInClientArea), false);


        // Static fields.
        internal static readonly Stopwatch Stopwatch = new Stopwatch().Also(it => it.Start());


        // Fields.
        readonly ScheduledAction checkSystemChromeVisibilityAction;
        ContentPresenter? contentPresenter;
        readonly WindowContentFadingHelper contentFadingHelper;
        Tutorial? currentTutorial;
        IDisposable? currentTutorialObserverToken;
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
                this.SetValue<bool>(IsSystemChromeVisibleInClientAreaProperty, Global.Run(() =>
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
                    this.TransparencyLevelHint = this.OnSelectTransparentLevelHint();
            });
            this.checkSystemChromeVisibilityAction.Schedule();
            this.updateTransparencyLevelAction.Schedule();
        }


        /// <inheritdoc/>
        public void CancelTutorial() =>
            this.tutorialPresenter?.CancelTutorial();


        /// <inheritdoc/>
        public Tutorial? CurrentTutorial { get => this.currentTutorial; }


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
        public bool IsSystemChromeVisibleInClientArea { get => this.GetValue<bool>(IsSystemChromeVisibleInClientAreaProperty); }


        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            this.contentPresenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter")?.Also(it =>
            {
                // [Workaround] Force relayout content to make sure that layout will be correct after changing window size by code
                it.GetObservable(BoundsProperty).Subscribe(_ =>
                {
                    if (it.Margin != new Thickness())
                        this.SynchronizationContext.Post(() => it.Margin = new Thickness());
                });
            });
            this.tutorialPresenter = e.NameScope.Find<TutorialPresenter>("PART_TutorialPresenter").Also(it =>
            {
                this.currentTutorialObserverToken = this.currentTutorialObserverToken.DisposeAndReturnNull();
                if (it != null)
                {
                    this.currentTutorialObserverToken = it.GetObservable(TutorialPresenter.CurrentTutorialProperty).Subscribe(tutorial =>
                        this.SetAndRaise<Tutorial?>(CurrentTutorialProperty, ref this.currentTutorial, tutorial));
                }
                else
                    this.SetAndRaise<Tutorial?>(CurrentTutorialProperty, ref this.currentTutorial, null);
            });
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
            this.checkSystemChromeVisibilityAction.Execute();
            this.updateTransparencyLevelAction.ExecuteIfScheduled();
        }


        /// <summary>
        /// Called when property changed.
        /// </summary>
        /// <typeparam name="T">Type of property.</typeparam>
        /// <param name="change">Change data.</param>
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);
            var property = change.Property;
            if (property == ActualTransparencyLevelProperty)
            {
                /*
                if (Platform.IsWindows11OrAbove)
                {
                    if (this.ActualTransparencyLevel == WindowTransparencyLevel.None)
                    {
                        var margins = new MARGINS();
                        DwmExtendFrameIntoClientArea(this.PlatformImpl.Handle.Handle, ref margins);
                    }
                    else
                    {
                        var margins = new MARGINS()
                        {
                            cxLeftWidth = -1,
                            cyTopHeight = -1,
                            cxRightWidth = -1,
                            cyBottomHeight = -1,
                        };
                        DwmExtendFrameIntoClientArea(this.PlatformImpl.Handle.Handle, ref margins);
                    }
                }
                */
            }
            else if (property == CurrentTutorialProperty
                || property == IsActiveProperty)
            {
                this.updateTransparencyLevelAction.Schedule();
            }
            else if (property == ExtendClientAreaToDecorationsHintProperty
                || property == SystemDecorationsProperty
                || property == WindowStateProperty)
            {
                this.checkSystemChromeVisibilityAction.Schedule();
            }
            else if (property == HeightProperty 
                || property == WidthProperty)
            {
                this.SynchronizationContext.Post(() => this.contentPresenter?.Let(it => it.Margin = new Thickness(0, 0, 0, 1)));
            }
        }


        /// <summary>
        /// Called to select transparency level to apply on window.
        /// </summary>
        /// <returns>Transparency level.</returns>
        protected virtual WindowTransparencyLevel OnSelectTransparentLevelHint()
        {
            if (!this.IsActive 
                || this.contentFadingHelper.IsFadingContent
                || !this.Settings.GetValueOrDefault(SettingKeys.EnableBlurryBackground)
                || this.currentTutorial != null)
            {
                return WindowTransparencyLevel.None;
            }
            if (Platform.IsLinux)
                return WindowTransparencyLevel.None;
            if (Platform.IsMacOS)
                return WindowTransparencyLevel.AcrylicBlur;
            if (Platform.IsWindows11OrAbove)
                return WindowTransparencyLevel.Mica;
            if (this.Application.HardwareInfo.HasDedicatedGraphicsCard != true)
                return WindowTransparencyLevel.None;
            return WindowTransparencyLevel.AcrylicBlur;
        }


        // Called when application setting changed.
        void OnSettingChanged(object? sender, SettingChangedEventArgs e) => this.OnSettingChanged(e);


        /// <summary>
        /// Called when application setting changed.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected virtual void OnSettingChanged(SettingChangedEventArgs e)
        {
            if (e.Key == SettingKeys.EnableBlurryBackground)
                this.InvalidateTransparencyLevelHint();
        }


        /// <inheritdoc/>
        public void RequestSkippingAllTutorials() =>
            this.tutorialPresenter?.RequestSkippingAllTutorials();
        

        /// <inheritdoc/>
        public bool ShowTutorial(Tutorial tutorial) =>
            this.tutorialPresenter?.ShowTutorial(tutorial) ?? false;
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
        public new TApp Application
        {
            get => (TApp)base.Application;
        }
    }
}
