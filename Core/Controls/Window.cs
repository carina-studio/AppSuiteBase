using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CarinaStudio.AppSuite.Controls
{
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
        static readonly Version? AvaloniaVersion = typeof(Avalonia.Application).Assembly.GetName().Version;
        static readonly bool IsAvalonia_0_10_14_OrAbove = AvaloniaVersion?.Let(version =>
            version.Major >= 1 || version.Minor >= 11 || version.Build >= 14) ?? false;
        static MethodInfo? SetWindowsTaskbarProgressStateMethod;
        static MethodInfo? SetWindowsTaskbarProgressValueMethod;
        internal static readonly Stopwatch Stopwatch = new Stopwatch().Also(it => it.Start());
        static object? WindowsTaskbarManager;
        static Type? WindowsTaskbarProgressBarStateType;


        // Fields.
        readonly ScheduledAction checkSystemChromeVisibilityAction;
        ContentPresenter? contentPresenter;
        readonly WindowContentFadingHelper contentFadingHelper;
        Tutorial? currentTutorial;
        IDisposable? currentTutorialObserverToken;
        bool isSystemChromeVisibleInClientArea;
        double taskbarIconProgress;
        TaskbarIconProgressState taskbarIconProgressState = TaskbarIconProgressState.None;
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
                this.SetAndRaise<bool>(IsSystemChromeVisibleInClientAreaProperty, 
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
                    this.TransparencyLevelHint = this.OnSelectTransparentLevelHint();
            });
            this.checkSystemChromeVisibilityAction.Schedule();
            this.updateTransparencyLevelAction.Schedule();

            // observe self properties
            var isSubscribed = false;
            this.GetObservable(CurrentTutorialProperty).Subscribe(_ =>
            {
                if (isSubscribed)
                    this.updateTransparencyLevelAction.Schedule();
            });
            this.GetObservable(ExtendClientAreaToDecorationsHintProperty).Subscribe(_ =>
            {
                if (isSubscribed)
                    this.checkSystemChromeVisibilityAction.Schedule();
            });
            this.GetObservable(HeightProperty).Subscribe(_ =>
            {
                if (isSubscribed)
                    this.SynchronizationContext.Post(() => this.contentPresenter?.Let(it => it.Margin = new Thickness(0, 0, 0, 1)));
            });
            this.GetObservable(IsActiveProperty).Subscribe(_ =>
            {
                if (isSubscribed)
                    this.updateTransparencyLevelAction.Schedule();
            });
            this.GetObservable(SystemDecorationsProperty).Subscribe(_ =>
            {
                if (isSubscribed)
                    this.checkSystemChromeVisibilityAction.Schedule();
            });
            this.GetObservable(WidthProperty).Subscribe(_ =>
            {
                if (isSubscribed)
                    this.SynchronizationContext.Post(() => this.contentPresenter?.Let(it => it.Margin = new Thickness(0, 0, 0, 1)));
            });
            this.GetObservable(WindowStateProperty).Subscribe(_ =>
            {
                if (isSubscribed)
                    this.checkSystemChromeVisibilityAction.Schedule();
            });
            isSubscribed = true;
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
        public bool IsSystemChromeVisibleInClientArea { get => this.isSystemChromeVisibleInClientArea; }


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
            if (this.Application.HardwareInfo.HasDedicatedGraphicsCard == true)
            {
                if (IsAvalonia_0_10_14_OrAbove || !Platform.IsWindows11OrAbove)
                    return WindowTransparencyLevel.AcrylicBlur;
            }
            if (Platform.IsWindows11OrAbove)
                return WindowTransparencyLevel.Mica;
            return WindowTransparencyLevel.None;
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
        

        // Setup TaskbarManager for Windows is available.
        [MemberNotNullWhen(true, nameof(SetWindowsTaskbarProgressStateMethod))]
        [MemberNotNullWhen(true, nameof(SetWindowsTaskbarProgressValueMethod))]
        [MemberNotNullWhen(true, nameof(WindowsTaskbarManager))]
        [MemberNotNullWhen(true, nameof(WindowsTaskbarProgressBarStateType))]
        bool SetupWindowsTaskbarManager()
        {
            // check state
            if (Platform.IsNotWindows)
                return false;
#pragma warning disable CS8775
            if (WindowsTaskbarManager != null)
                return true;
#pragma warning restore CS8775
            var tbmType = this.TaskbarManagerType;
            if (tbmType == null)
                return false;
            
            // find type
            WindowsTaskbarProgressBarStateType = tbmType.Assembly.GetType("Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState");
            if (WindowsTaskbarProgressBarStateType == null)
            {
                this.Logger.LogError("Unable to find TaskbarProgressBarState type on Windows");
                return false;
            }
            
            // create taskbar manager
            object? taskbarManager;
            try
            {
                taskbarManager = tbmType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).AsNonNull().GetValue(null).AsNonNull();
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Unable to get TaskbarManager on Windows");
                return false;
            }

            // find methods
            SetWindowsTaskbarProgressStateMethod = taskbarManager.GetType().GetMethod("SetProgressState", new Type[]{ WindowsTaskbarProgressBarStateType, typeof(IntPtr) });
            if (SetWindowsTaskbarProgressStateMethod == null)
            {
                this.Logger.LogError("Unable to find TaskbarManager.SetProgressState() on Windows");
                return false;
            }
            SetWindowsTaskbarProgressValueMethod = taskbarManager.GetType().GetMethod("SetProgressValue", new Type[]{ typeof(int), typeof(int), typeof(IntPtr) });
            if (SetWindowsTaskbarProgressValueMethod == null)
            {
                this.Logger.LogError("Unable to find TaskbarManager.SetProgressValue() on Windows");
                return false;
            }

            // complete
            WindowsTaskbarManager = taskbarManager;
            return true;
        }
        

        /// <inheritdoc/>
        public bool ShowTutorial(Tutorial tutorial) =>
            this.tutorialPresenter?.ShowTutorial(tutorial) ?? false;
        

        /// <summary>
        /// Get or set progress shown on taskbar icon. The range is [0.0, 1.0].
        /// </summary>
        internal protected double TaskbarIconProgress
        {
            get => this.taskbarIconProgress;
            set
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
                {
                    if (this.SetupWindowsTaskbarManager())
                    {
                        (this.PlatformImpl?.Handle?.Handle)?.Let(hWnd =>
                            SetWindowsTaskbarProgressValueMethod.Invoke(WindowsTaskbarManager, new object?[]{ (int)(value * 100 + 0.5), 100, hWnd }));
                    }
                }
                else if (Platform.IsMacOS)
                    AppSuiteApplication.CurrentOrNull?.UpdateMacOSDockTileProgressState();
            }
        }


        /// <summary>
        /// Get or set progress state of taskbar icon.
        /// </summary>
        internal protected TaskbarIconProgressState TaskbarIconProgressState
        {
            get => this.taskbarIconProgressState;
            set
            {
                this.VerifyAccess();
                if (this.taskbarIconProgressState == value)
                    return;
                this.taskbarIconProgressState = value;
                if (Platform.IsWindows)
                {
                    if (this.SetupWindowsTaskbarManager())
                    {
                        (this.PlatformImpl?.Handle?.Handle)?.Let(hWnd =>
                            SetWindowsTaskbarProgressStateMethod.Invoke(WindowsTaskbarManager, new object?[]{ (int)value, hWnd }));
                    }
                }
                else if (Platform.IsMacOS)
                    AppSuiteApplication.CurrentOrNull?.UpdateMacOSDockTileProgressState();
            }
        }
        

        /// <summary>
        /// Get type of Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.
        /// </summary>
        protected virtual Type? TaskbarManagerType { get => null; }
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
