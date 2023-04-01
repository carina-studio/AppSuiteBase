﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Platform;
using CarinaStudio.Animation;
using CarinaStudio.AppSuite.Net;
using CarinaStudio.AppSuite.Product;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Base class of main window pf application.
    /// </summary>
    /// <typeparam name="TViewModel">Type of view-model.</typeparam>
    public abstract class MainWindow<TViewModel> : Window, IMainWindow where TViewModel : MainWindowViewModel
    {
        /// <summary>
        /// Property of <see cref="AreInitialDialogsClosed"/>.
        /// </summary>
        public static readonly DirectProperty<MainWindow<TViewModel>, bool> AreInitialDialogsClosedProperty = AvaloniaProperty.RegisterDirect<MainWindow<TViewModel>, bool>(nameof(AreInitialDialogsClosed), w => w.areInitialDialogsClosed);
        /// <summary>
        /// Property of <see cref="ContentPadding"/>.
        /// </summary>
        public static readonly DirectProperty<MainWindow<TViewModel>, Thickness> ContentPaddingProperty = AvaloniaProperty.RegisterDirect<MainWindow<TViewModel>, Thickness>(nameof(ContentPadding), w => w.contentPadding);
        /// <summary>
        /// Property of <see cref="HasMultipleMainWindows"/>.
        /// </summary>
        public static readonly DirectProperty<MainWindow<TViewModel>, bool> HasMultipleMainWindowsProperty = AvaloniaProperty.RegisterDirect<MainWindow<TViewModel>, bool>(nameof(HasMultipleMainWindows), w => w.hasMultipleMainWindows);


        // Constants.
        const int InitialDialogsDelay = 1000;
        const int RestartingMainWindowsDelay = 500;
        const int SaveWindowSizeDelay = 300;
        const int UpdateContentPaddingDelay = 300;


        // Static fields.
        static readonly SettingKey<bool> DoNotCheckAppRunningLocationOnMacOSKey = new("MainWindow.DoNotCheckAppRunningLocationOnMacOS");
        static readonly SettingKey<int> ExtDepDialogShownVersionKey = new("MainWindow.ExternalDependenciesDialogShownVersion", -1);
        static bool IsAppRunningLocationOnMacOSChecked;
        static bool IsNetworkConnForActivatingProVersionNotified;
        static bool IsNotifyingAppUpdateFound;
        static bool IsReactivatingProVersion;
		static bool IsReactivatingProVersionNeeded;
        static readonly SettingKey<bool> IsUsingCompactUIConfirmedKey = new("MainWindow.IsUsingCompactUIConfirmed", false);
        static readonly SettingKey<string> LatestAppChangeListShownVersionKey = new("ApplicationChangeListDialog.LatestShownVersion", "");
        static readonly SettingKey<int> WindowHeightSettingKey = new("MainWindow.Height", 600);
        static readonly SettingKey<WindowState> WindowStateSettingKey = new("MainWindow.State", WindowState.Maximized);
        static readonly SettingKey<int> WindowWidthSettingKey = new("MainWindow.Width", 800);


        // Fields.
        bool areInitialDialogsClosed;
        TViewModel? attachedViewModel;
        Thickness contentPadding;
        ThicknessAnimator? contentPaddingAnimator;
        ContentPresenter? contentPresenter;
        bool hasMultipleMainWindows;
        bool isClosingScheduled;
        bool isFirstContentPaddingUpdate = true;
        bool isShowingInitialDialogs;
        readonly ScheduledAction notifyNetworkConnForActivatingProVersionAction;
        long openedTime;
        readonly ScheduledAction reactivateProVersionAction;
        readonly ScheduledAction restartingRootWindowsAction;
        double restoredHeight;
        double restoredWidth;
        WindowState restoredWindowState;
        readonly ScheduledAction saveWindowSizeAction;
        readonly ScheduledAction showInitDialogsAction;
        readonly ScheduledAction updateContentPaddingAction;


        /// <summary>
        /// Initialize new <see cref="MainWindow{TViewModel}"/> instance.
        /// </summary>
        protected MainWindow()
        {
            // create commands
            this.LayoutMainWindowsCommand = new Command<MultiWindowLayout>(this.LayoutMainWindows, this.GetObservable(HasMultipleMainWindowsProperty));

            // create scheduled actions
            this.notifyNetworkConnForActivatingProVersionAction = new(() =>
			{
                if (this.Application is not AppSuiteApplication asApp)
                    return;
                var productId = asApp.ProVersionProductId;
				if (productId == null 
					|| !this.IsActive
					|| this.HasDialogs
					|| IsNetworkConnForActivatingProVersionNotified
					|| NetworkManager.Default.IsNetworkConnected
					|| !asApp.ProductManager.TryGetProductState(productId, out var state))
				{
					return;
				}
                if (state != ProductState.Activated)
                {
                    this.Logger.LogTrace("No need to notify user about activating Pro-version because state of Pro-version is {state}", state);
                    return;
                }
                IsNetworkConnForActivatingProVersionNotified = true;
                if (asApp.ProductManager.IsProductActivated(productId, true))
                {
                    this.Logger.LogTrace("No need to notify user about activating Pro-version because Pro-version is already activated online");
                    return;
                }
				_ = new MessageDialog()
				{
					Icon = MessageDialogIcon.Information,
					Message = new FormattedString().Also(it =>
					{
						it.Bind(FormattedString.Arg1Property, asApp.GetObservableString($"Product.{productId}"));
						it.Bind(FormattedString.FormatProperty, asApp.GetObservableString("MainWindow.NetworkConnectionNeededForProductActivation"));
					}),
				}.ShowDialog(null);
			});
            this.reactivateProVersionAction = new(async () =>
			{
                if (this.Application is not AppSuiteApplication asApp)
                    return;
                var productId = asApp.ProVersionProductId;
				if (productId == null 
                    || !IsReactivatingProVersionNeeded
					|| this.HasDialogs 
					|| !this.IsActive 
					|| asApp.IsActivatingProVersion
					|| IsReactivatingProVersion)
				{
					return;
				}
				IsReactivatingProVersionNeeded = false;
				if (asApp.ProductManager.TryGetProductState(productId, out var state)
					&& state == ProductState.Deactivated)
				{
					await this.OnNotifyReactivatingProVersionNeededAsync();
					IsReactivatingProVersion = true;
					await asApp.ActivateProVersionAsync(this);
					IsReactivatingProVersion = false;
				}
			});
            this.restartingRootWindowsAction = new ScheduledAction(() =>
            {
                if (!this.IsOpened || this.HasDialogs || !this.Application.IsRestartingRootWindowsNeeded)
                    return;
                this.Logger.LogWarning("Restart main windows");
                this.Application.RestartRootWindowsAsync();
            });
            this.saveWindowSizeAction = new ScheduledAction(() =>
            {
                if (this.WindowState == WindowState.Normal)
                {
                    this.PersistentState.SetValue<int>(WindowWidthSettingKey, (int)(this.Width + 0.5));
                    this.PersistentState.SetValue<int>(WindowHeightSettingKey, (int)(this.Height + 0.5));
                }
            });
            this.showInitDialogsAction = new ScheduledAction(this.ShowInitialDialogs);
            this.updateContentPaddingAction = new ScheduledAction(() =>
            {
                // check state
                if (!this.IsOpened || !this.ExtendClientAreaToDecorationsHint)
                    return;
                var windowState = this.WindowState;
                if (windowState == WindowState.Minimized)
                    return;

                // check content
                if (this.contentPresenter == null)
                    return;

                // cancel current animation
                this.contentPaddingAnimator?.Cancel();

                // update content padding
                // [Workaround] cannot use padding of window because that thickness transition doesn't work on it
                var margin = windowState switch
                {
                    WindowState.FullScreen
                    or WindowState.Maximized => ExtendedClientAreaWindowConfiguration.ContentPaddingInMaximized,
                    _ => ExtendedClientAreaWindowConfiguration.ContentPadding,
                };
                if (this.isFirstContentPaddingUpdate)
                {
                    this.isFirstContentPaddingUpdate = false;
                    this.contentPresenter.Padding = margin;
                }
                else
                {
                    this.contentPaddingAnimator = new ThicknessAnimator(this.contentPresenter.Padding, margin).Also(it =>
                    {
                        it.Completed += (_, e) => this.contentPresenter.Padding = it.EndValue;
                        if (this.TryFindResource("TimeSpan/MainWindow.ContentPaddingTransition", out var res) && res is TimeSpan duration)
                            it.Duration = duration;
                        it.Interpolator = Interpolators.Deceleration;
                        it.ProgressChanged += (_, e) => this.contentPresenter.Padding = it.Value;
                        it.Start();
                    });
                }
            });

            // restore window state
            this.PersistentState.Let(it =>
            {
                this.restoredHeight = Math.Max(0, it.GetValueOrDefault(WindowHeightSettingKey));
                this.restoredWidth = Math.Max(0, it.GetValueOrDefault(WindowWidthSettingKey));
                this.restoredWindowState = it.GetValueOrDefault(WindowStateSettingKey);
                this.Height = this.restoredHeight;
                this.Width = this.restoredWidth;
                if (this.restoredWindowState == WindowState.FullScreen)
                    this.UpdateExtendClientAreaChromeHints(true); // [Workaround] Prevent making title bar be transparent
                this.WindowState = this.restoredWindowState;
            });

            // extend client area if needed
            this.UpdateExtendingClientArea();
            this.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome; // show system chrome when opened

            // observe self properties
            var isSubscribing = true;
            this.GetObservable(ContentProperty).Subscribe(content =>
            {
                if (!this.Application.IsPrivacyPolicyAgreed || !this.Application.IsUserAgreementAgreed)
                    (content as Control)?.Let(it => it.IsEnabled = false);
            });
            this.GetObservable(DataContextProperty).Subscribe(dataContext =>
            {
                if (this.attachedViewModel != null)
                {
                    this.OnDetachFromViewModel(this.attachedViewModel);
                    this.attachedViewModel = null;
                }
                this.attachedViewModel = dataContext as TViewModel;
                if (this.attachedViewModel != null)
                    this.OnAttachToViewModel(this.attachedViewModel);
            });
            this.GetObservable(ExtendClientAreaToDecorationsHintProperty).Subscribe(_ =>
            {
                if (this.IsOpened)
                    this.UpdateExtendClientAreaChromeHints(false);
            });
            this.GetObservable(HasDialogsProperty).Subscribe(hasDialogs =>
            {
                if (!isSubscribing && !hasDialogs)
                {
                    if (this.Application.IsRestartingRootWindowsNeeded)
                        this.restartingRootWindowsAction.Reschedule(RestartingMainWindowsDelay);
                    if (!this.AreInitialDialogsClosed)
                        this.showInitDialogsAction.Schedule();
                    if (this.Application is AppSuiteApplication asApp && asApp.ProVersionProductId != null)
                    {
                        if (!this.notifyNetworkConnForActivatingProVersionAction.IsScheduled 
                            && !NetworkManager.Default.IsNetworkConnected
                            && !IsNetworkConnForActivatingProVersionNotified)
                        {
                            this.Logger.LogTrace("All dialogs were closed, notify user about network connection for activating Pro-version");
                            this.notifyNetworkConnForActivatingProVersionAction.Schedule();
                        }
                        if (IsReactivatingProVersionNeeded)
                            this.reactivateProVersionAction.Schedule();
                    }
                }
            });
            this.GetObservable(HeightProperty).Subscribe(_ => 
            {
                if (isSubscribing)
                    return;
                this.saveWindowSizeAction.Reschedule(SaveWindowSizeDelay);
            });
            this.GetObservable(IsActiveProperty).Subscribe(isActive =>
            {
                if (isActive)
                {
                    if (!this.AreInitialDialogsClosed)
                        this.showInitDialogsAction.Schedule();
                    else if (this.Settings.GetValueOrDefault(SettingKeys.NotifyApplicationUpdate))
                    {
                        this.SynchronizationContext.Post(async () =>
                        {
                            if (!IsNotifyingAppUpdateFound)
                            {
                                IsNotifyingAppUpdateFound = true;
                                await this.Application.CheckForApplicationUpdateAsync(this, false);
                                IsNotifyingAppUpdateFound = false;
                            }
                        });
                    } 
                    if (this.Application is AppSuiteApplication asApp && asApp.ProVersionProductId != null)
                    {
                        if (!this.notifyNetworkConnForActivatingProVersionAction.IsScheduled
                            && !NetworkManager.Default.IsNetworkConnected
                            && !IsNetworkConnForActivatingProVersionNotified)
                        {
                            this.Logger.LogTrace("Window activated, notify user about network connection for activating Pro-version");
                            this.notifyNetworkConnForActivatingProVersionAction.Schedule(this.Configuration.GetValueOrDefault(ConfigurationKeys.TimeoutToNotifyNetworkConnectionForProductActivation));
                        }
                        if (IsReactivatingProVersionNeeded)
                            this.reactivateProVersionAction.Schedule();
                    }
                }
            });
            this.GetObservable(WidthProperty).Subscribe(_ => 
            {
                if (isSubscribing)
                    return;
                this.saveWindowSizeAction.Reschedule(SaveWindowSizeDelay);
            });
            this.GetObservable(WindowStateProperty).Subscribe(windowState =>
            {
                if (isSubscribing)
                    return;
                if (this.IsOpened)
                {
                    if (windowState != WindowState.Minimized)
                        this.PersistentState.SetValue<WindowState>(WindowStateSettingKey, windowState);
                    if (windowState == WindowState.FullScreen)
                        this.updateContentPaddingAction.Reschedule();
                    else
                        this.updateContentPaddingAction.Reschedule(UpdateContentPaddingDelay);
                }
                this.RestoreToSavedSize();
                this.InvalidateTransparencyLevelHint();
                this.UpdateExtendClientAreaChromeHints(false);
            });
            isSubscribing = false;
        }


        /// <summary>
        /// Check whether all dialogs which need to be shown after showing main window are closed or not.
        /// </summary>
        public bool AreInitialDialogsClosed { get => this.areInitialDialogsClosed; }


        /// <summary>
        /// Cancel pending window size saving.
        /// </summary>
        public void CancelSavingSize() =>
            this.saveWindowSizeAction.Cancel();
        

        // Check whether Pro-version reactivation is needed or not.
        void CheckIsReactivatingProVersionNeeded()
        {
            if (this.Application is not AppSuiteApplication asApp)
                return;
            var productId = asApp.ProVersionProductId;
            if (productId == null || !asApp.ProductManager.TryGetProductState(productId, out var state))
				return;
            switch (state)
			{
                case ProductState.Activated:
                    if (asApp.ProductManager.IsProductActivated(productId, true)
                        && this.notifyNetworkConnForActivatingProVersionAction.Cancel())
                    {
                        this.Logger.LogTrace("Cancel notifying user about network connection for activating Pro-version");
                    }
                    goto default;
				case ProductState.Deactivated:
					if (asApp.ProductManager.TryGetProductActivationFailure(productId, out var failure)
						&& failure != ProductActivationFailure.NoNetworkConnection
						&& !IsReactivatingProVersion)
					{
						this.Logger.LogWarning("Need to reactivate Pro-version because of {failure}", failure);
						IsReactivatingProVersionNeeded = true;
						this.reactivateProVersionAction.Schedule();
					}
                    if (this.notifyNetworkConnForActivatingProVersionAction.Cancel())
                        this.Logger.LogTrace("Cancel notifying user about network connection for activating Pro-version");
					break;
				default:
					IsReactivatingProVersionNeeded = false;
					this.reactivateProVersionAction.Cancel();
					break;
			}
        }


        // Check whether network connection is available for activating Pro-version or not.
        void CheckNetworkConnectionForActivatingProVersion()
        {
            if (this.Application is not AppSuiteApplication asApp)
                return;
            var productId = asApp.ProVersionProductId;
			if (productId != null)
			{
				if (NetworkManager.Default.IsNetworkConnected)
                {
					if (this.notifyNetworkConnForActivatingProVersionAction.Cancel())
                        this.Logger.LogTrace("Network connected, cancel notifying user about activating Pro-version");
                }
				else if (asApp.ProductManager.TryGetProductState(productId, out var state)
                    && state == ProductState.Activated
                    && !asApp.ProductManager.IsProductActivated(productId, true)
					&& !IsNetworkConnForActivatingProVersionNotified)
				{
                    this.Logger.LogTrace("Unable to activate Pro-version because network is disconnected, notify user later");
					this.notifyNetworkConnForActivatingProVersionAction.Reschedule(this.Configuration.GetValueOrDefault(ConfigurationKeys.TimeoutToNotifyNetworkConnectionForProductActivation));
				}
			}
        }


        /// <summary>
        /// Get application configuration.
        /// </summary>
        protected ISettings Configuration { get => this.Application.Configuration; }

        
        /// <summary>
        /// Get padding applied on content of Window automatically.
        /// </summary>
        public Thickness ContentPadding { get => this.contentPadding; }


        /// <summary>
        /// Check whether multiple main windows were opened or not.
        /// </summary>
        public bool HasMultipleMainWindows { get => this.hasMultipleMainWindows; }


        /// <summary>
        /// Check whether extending client area to title bar is allowed or not.
        /// </summary>
        public virtual bool IsExtendingClientAreaAllowed { get; } = true;


        // Layout main windows.
        void LayoutMainWindows(MultiWindowLayout layout)
        {
            // check state
            this.VerifyAccess();
            if (this.IsClosed || !this.HasMultipleMainWindows)
                return;

            // layout
            this.Application.LayoutMainWindows(this.Screens.ScreenFromWindow(this.PlatformImpl.AsNonNull()) ?? this.Screens.Primary.AsNonNull(), layout, this);
        }


        /// <summary>
        /// Command to layout main windows.
        /// </summary>
        /// <remarks>Type of parameter is <see cref="MultiWindowLayout"/>.</remarks>
        public ICommand LayoutMainWindowsCommand { get; }


        // Called when property of application changed.
        void OnApplicationPropertyChanged(object? sender, PropertyChangedEventArgs e) => this.OnApplicationPropertyChanged(e);


        /// <summary>
        /// Called when property of application changed.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected virtual void OnApplicationPropertyChanged(PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IAppSuiteApplication.IsPrivacyPolicyAgreed):
                case nameof(IAppSuiteApplication.IsUserAgreementAgreed):
                    (this.Content as Control)?.Let(content => content.IsEnabled = this.Application.IsPrivacyPolicyAgreed && this.Application.IsUserAgreementAgreed);
                    break;
                case nameof(IAppSuiteApplication.IsRestartingRootWindowsNeeded):
                    if (this.Application.IsRestartingRootWindowsNeeded)
                    {
                        if (!BaseApplicationOptionsDialog.HasOpenedDialogs)
                            this.restartingRootWindowsAction.Reschedule(RestartingMainWindowsDelay);
                    }
                    else
                        this.restartingRootWindowsAction.Cancel();
                    break;
                case nameof(IAppSuiteApplication.UpdateInfo):
                    this.SynchronizationContext.Post(async () =>
                    {
                        if (this.AreInitialDialogsClosed 
                            && this.Settings.GetValueOrDefault(SettingKeys.NotifyApplicationUpdate)
                            && !IsNotifyingAppUpdateFound)
                        {
                            IsNotifyingAppUpdateFound = true;
                            await this.Application.CheckForApplicationUpdateAsync(this, false);
                            IsNotifyingAppUpdateFound = false;
                        }
                    });
                    break;
            }
        }


        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            this.contentPresenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter")?.Also(it =>
            {
                it.GetObservable(PaddingProperty).Subscribe(padding =>
                    this.SetAndRaise<Thickness>(ContentPaddingProperty, ref this.contentPadding, padding));
            });
        }


        /// <summary>
        /// Called to attach to view-model.
        /// </summary>
        /// <param name="viewModel">View-model.</param>
        protected virtual void OnAttachToViewModel(TViewModel viewModel)
        {
            // attach
            viewModel.PropertyChanged += this.OnViewModelPropertyChanged;

            // update title
            this.Title = viewModel.Title;
        }


        /// <summary>
        /// Called when window closed.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            this.isClosingScheduled = false;
            this.DataContext = null;
            this.Application.Configuration.SettingChanged -= this.OnConfigurationChanged;
            ((INotifyCollectionChanged)this.Application.MainWindows).CollectionChanged -= this.OnMainWindowsChanged;
            this.Application.PropertyChanged -= this.OnApplicationPropertyChanged;
            this.Application.ProductManager.ProductStateChanged -= this.OnProductStateChanged;
            NetworkManager.Default.PropertyChanged -= this.OnNetworkManagerPropertyChanged;
            this.RemoveHandler(DragDrop.DragEnterEvent, this.OnDragEnter);
            this.notifyNetworkConnForActivatingProVersionAction.Cancel();
            this.restartingRootWindowsAction.Cancel();
            this.reactivateProVersionAction.Cancel();
            this.showInitDialogsAction.Cancel();
            base.OnClosed(e);
        }


        // Called when one of configuration has been changed.
        void OnConfigurationChanged(object? sender, SettingChangedEventArgs e) =>
            this.OnConfigurationChanged(e);


        /// <summary>
        /// Called when one of application configuration has been changed.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected virtual void OnConfigurationChanged(SettingChangedEventArgs e)
        { }


        /// <summary>
        /// Called to create view-model for application update dialog.
        /// </summary>
        /// <returns>View-model for application update dialog.</returns>
        protected virtual ApplicationUpdater OnCreateApplicationUpdater() => new();


        /// <summary>
        /// Called to detach from view-model.
        /// </summary>
        /// <param name="viewModel">View-model.</param>
        protected virtual void OnDetachFromViewModel(TViewModel viewModel)
        {
            // detach
            viewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
        }


        // Called when data dragged into window,
        void OnDragEnter(object? sender, DragEventArgs e) => this.ActivateAndBringToFront();


        /// <summary>
        /// Called when all dialogs which need to be shown after showing main window are closed.
        /// </summary>
        protected virtual void OnInitialDialogsClosed()
        { }


        // Called when list of main window changed.
        void OnMainWindowsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            this.SetAndRaise<bool>(HasMultipleMainWindowsProperty, ref this.hasMultipleMainWindows, this.Application.MainWindows.Count > 1);
        

        // Called when property of network manager changed.
		void OnNetworkManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(NetworkManager.IsNetworkConnected))
                this.CheckNetworkConnectionForActivatingProVersion();
		}
        

        /// <summary>
        /// Called to notify user that Pro-version is needed to be reactivated.
        /// </summary>
        /// <returns>Task of notifying user.</returns>
        protected virtual Task OnNotifyReactivatingProVersionNeededAsync()
        {
            if (this.Application is not AppSuiteApplication asApp)
                return Task.CompletedTask;
            return new MessageDialog()
            {
                Icon = MessageDialogIcon.Warning,
                Message = new FormattedString().Also(it =>
                {
                    it.Bind(FormattedString.Arg1Property, asApp.GetObservableString($"Product.{asApp.ProVersionProductId}"));
                    it.Bind(FormattedString.FormatProperty, asApp.GetObservableString("MainWindow.ReactivateProductNeeded"));
                }),
            }.ShowDialog(this);
        }


        /// <summary>
        /// Called when window opened.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnOpened(EventArgs e)
        {
            // keep time
            this.openedTime = Stopwatch.ElapsedMilliseconds;

            // restore to saved state and size
            this.restoredWindowState.Let(it =>
            {
                if (it == WindowState.FullScreen)
                    this.UpdateExtendClientAreaChromeHints(true); // [Workaround] Prevent making title bar be transparent
                if (this.WindowState != it)
                    this.WindowState = it; // Size will also be restored in OnPropertyChanged()
                else
                    this.RestoreToSavedSize();
            });

            // call base
            base.OnOpened(e);

            // add event handlers
            this.Application.Configuration.SettingChanged += this.OnConfigurationChanged;
            ((INotifyCollectionChanged)this.Application.MainWindows).CollectionChanged += this.OnMainWindowsChanged;
            this.Application.PropertyChanged += this.OnApplicationPropertyChanged;
            this.Application.ProductManager.ProductStateChanged += this.OnProductStateChanged;
            NetworkManager.Default.PropertyChanged += this.OnNetworkManagerPropertyChanged;
            this.AddHandler(DragDrop.DragEnterEvent, this.OnDragEnter);

            // check main window count
            this.SetAndRaise<bool>(HasMultipleMainWindowsProperty, ref this.hasMultipleMainWindows, this.Application.MainWindows.Count > 1);

            // update content padding
            this.updateContentPaddingAction.Execute();

            // notify application update found
            if (this.Application.MainWindows.Count == 1)
                ApplicationUpdateDialog.ResetLatestShownInfo();
            this.showInitDialogsAction.Schedule();

            // show system chrome
            this.UpdateExtendClientAreaChromeHints(false);

            // check network state
            this.CheckNetworkConnectionForActivatingProVersion();

            // check Pro-version
            this.CheckIsReactivatingProVersionNeeded();
        }


        // Called when product state changed.
		void OnProductStateChanged(IProductManager productManager, string productId)
        {
            if (this.Application is not AppSuiteApplication asApp || productId != asApp.ProVersionProductId)
                return;
            this.CheckIsReactivatingProVersionNeeded();
        }


        /// <summary>
        /// Called to select transparency level.
        /// </summary>
        /// <returns>Transparency level.</returns>
        protected override WindowTransparencyLevel OnSelectTransparentLevelHint() => this.WindowState switch
        {
            WindowState.FullScreen
            or WindowState.Maximized => WindowTransparencyLevel.None,
            _ => base.OnSelectTransparentLevelHint(),
        };


        // Called when property of view-model changed.
        void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e) => this.OnViewModelPropertyChanged(e);


        /// <summary>
        /// Called when property of view-model changed.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected virtual void OnViewModelPropertyChanged(PropertyChangedEventArgs e)
        {
            if (this.DataContext is not TViewModel viewModel)
                return;
            if (e.PropertyName == nameof(MainWindowViewModel.Title))
                this.Title = viewModel.Title;
        }


        // Restore to saved window size if available.
        void RestoreToSavedSize()
        {
            if (this.WindowState == WindowState.Normal
                && double.IsFinite(this.restoredWidth)
                && double.IsFinite(this.restoredHeight))
            {
                var screen = this.Screens.ScreenFromWindow(this.PlatformImpl.AsNonNull()) ?? this.Screens.Primary;
                if (screen == null)
                {
                    this.Logger.LogWarning("Cannot find screen for restoring size of window");
                    return;
                }
                var workingAreaWidth = (double)screen.WorkingArea.Width;
                var workingAreaHeight = (double)screen.WorkingArea.Height;
                if (!Platform.IsMacOS)
                {
                    workingAreaWidth /= screen.Scaling;
                    workingAreaHeight /= screen.Scaling;
                }
                this.restoredWidth = Math.Min(this.restoredWidth, workingAreaWidth * 0.95);
                this.restoredHeight = Math.Min(this.restoredHeight, workingAreaHeight * 0.95);
                this.Width = this.restoredWidth;
                this.Height = this.restoredHeight;
                this.restoredWidth = double.NaN;
                this.restoredHeight = double.NaN;
            }
        }


        // Show dialogs which are needed to be shown after showing main window.
        async void ShowInitialDialogs()
        {
            // check state
            if (this.AreInitialDialogsClosed)
                return;
            if (!this.IsOpened || !this.IsActive || this.isClosingScheduled || this.Application.IsShutdownStarted)
                return;
            if (this.HasDialogs || this.isShowingInitialDialogs)
                return;

            // show later
            var delay = InitialDialogsDelay - (Stopwatch.ElapsedMilliseconds - this.openedTime);
            if (delay > 0)
            {
                this.showInitDialogsAction.Reschedule((int)delay);
                return;
            }

            // check application running location on macOS
            var app = this.Application;
            if (Platform.IsMacOS && !IsAppRunningLocationOnMacOSChecked && !this.PersistentState.GetValueOrDefault(DoNotCheckAppRunningLocationOnMacOSKey))
            {
                IsAppRunningLocationOnMacOSChecked = true;
                var path = app.RootPrivateDirectoryPath;
                if (Regex.IsMatch(path, @"\.app/Contents/MacOS(/.+)?")
                    && !Regex.IsMatch(path, @"^(/Applications|/Users/[^/]+/Applications)/.+"))
                {
                    this.isShowingInitialDialogs = true;
                    var dialog = new MessageDialog()
                    {
                        Buttons = MessageDialogButtons.OKCancel,
                        CustomCancelText = app.GetObservableString("MainWindow.RunningOutsideOfApplicationFolderOnMacOS.CloseApplication"),
                        CustomOKText = app.GetObservableString("Common.ContinueToUse"),
                        DefaultResult = MessageDialogResult.Cancel,
                        DoNotAskOrShowAgain = false,
                        Icon = MessageDialogIcon.Warning,
                        Message = new FormattedString().Also(it =>
                        {
                            it.Arg1 = app.Name;
                            it.Bind(FormattedString.FormatProperty, app.GetObservableString("MainWindow.RunningOutsideOfApplicationFolderOnMacOS"));
                        }),
                    };
                    var result = await dialog.ShowDialog(this);
                    if (dialog.DoNotAskOrShowAgain.GetValueOrDefault())
                        this.PersistentState.SetValue<bool>(DoNotCheckAppRunningLocationOnMacOSKey, true);
                    if (result == MessageDialogResult.Cancel)
                    {
                        app.Shutdown(300); // [Workaround] Prevent crashing on macOS if shutting down immediately after closing dialog.
                        this.isShowingInitialDialogs = false;
                        return;
                    }
                    this.isShowingInitialDialogs = false;
                }
            }

            // use compact UI
            if (!this.PersistentState.GetValueOrDefault(IsUsingCompactUIConfirmedKey))
            {
                var screen = this.Screens.ScreenFromWindow(this.PlatformImpl.AsNonNull()) ?? this.Screens.ScreenFromVisual(this) ?? this.Screens.Primary;
                if (screen != null)
                {
                    var pixelDensity = screen.Scaling;
                    var sizeToUseCompactUI = this.Configuration.GetValueOrDefault(ConfigurationKeys.WorkingAreaSizeToSuggestUsingCompactUI);
                    var workingSize = screen.WorkingArea.Size.Let(it =>
                    {
                        if (Platform.IsMacOS)
                            return it;
                        return new PixelSize((int)(it.Width / pixelDensity + 0.5), (int)(it.Height / pixelDensity + 0.5));
                    });
                    if (workingSize.Width < sizeToUseCompactUI || workingSize.Height < sizeToUseCompactUI)
                    {
                        this.isShowingInitialDialogs = true;
                        var result = await new MessageDialog()
                        {
                            Buttons = MessageDialogButtons.YesNo,
                            DefaultResult = MessageDialogResult.Yes,
                            Icon = MessageDialogIcon.Question,
                            Message = app.GetObservableString("MainWindow.ConfirmUsingCompactUI"),
                        }.ShowDialog(this);
                        this.isShowingInitialDialogs = false;
                        this.PersistentState.SetValue<bool>(IsUsingCompactUIConfirmedKey, true);
                        if (result == MessageDialogResult.Yes
                            && !this.Settings.GetValueOrDefault(SettingKeys.UseCompactUserInterface))
                        {
                            this.Settings.SetValue<bool>(SettingKeys.UseCompactUserInterface, true);
                            this.isClosingScheduled = true;
                            return;
                        }
                    }
                }
                else
                    this.Logger.LogWarning("No screen to check using compact UI");
                this.PersistentState.SetValue<bool>(IsUsingCompactUIConfirmedKey, true);
            }

            // show user agreement
            if (!app.IsUserAgreementAgreed)
            {
                this.Logger.LogDebug("Show User Agreement dialog");
                var documentSource = app.UserAgreement;
                if (documentSource != null)
                {
                    this.isShowingInitialDialogs = true;
                    var dialog = new AgreementDialog()
                    {
                        DocumentSource = documentSource,
                        Message = new FormattedString().Also(it =>
                        {
                            it.Arg1 = app.Name;
                            it.Bind(FormattedString.FormatProperty, this.GetResourceObservable(app.IsUserAgreementAgreedBefore
                                ? "String/MainWindow.UserAgreement.Message.Updated"
                                : "String/MainWindow.UserAgreement.Message"
                            ));
                        }),
                        Title = this.GetResourceObservable("String/Common.UserAgreement"),
                    };
                    if ((await dialog.ShowDialog(this)) == AgreementDialogResult.Declined)
                    {
                        this.Logger.LogWarning("User decline the current User Agreement");
                        app.Shutdown(300); // [Workaround] Prevent crashing on macOS if shutting down immediately after closing dialog.
                        this.isShowingInitialDialogs = false;
                        return;
                    }
                    this.isShowingInitialDialogs = false;
                }
                app.AgreeUserAgreement();
            }

            // show privacy policy
            if (!app.IsPrivacyPolicyAgreed)
            {
                this.Logger.LogDebug("Show Privacy Policy dialog");
                var documentSource = app.PrivacyPolicy;
                if (documentSource != null)
                {
                    this.isShowingInitialDialogs = true;
                    var dialog = new AgreementDialog()
                    {
                        DocumentSource = documentSource,
                        Message = new FormattedString().Also(it =>
                        {
                            it.Arg1 = app.Name;
                            it.Bind(FormattedString.FormatProperty, this.GetResourceObservable(app.IsPrivacyPolicyAgreedBefore
                                ? "String/MainWindow.PrivacyPolicy.Message.Updated"
                                : "String/MainWindow.PrivacyPolicy.Message"
                            ));
                        }),
                        Title = this.GetResourceObservable("String/Common.PrivacyPolicy"),
                    };
                    if ((await dialog.ShowDialog(this)) == AgreementDialogResult.Declined)
                    {
                        this.Logger.LogWarning("User decline the current Privacy Policy");
                        app.Shutdown(300); // [Workaround] Prevent crashing on macOS if shutting down immediately after closing dialog.
                        this.isShowingInitialDialogs = false;
                        return;
                    }
                    this.isShowingInitialDialogs = false;
                }
                app.AgreePrivacyPolicy();
            }

            // show application change list
            var changeListShownVersion = this.PersistentState.GetValueOrDefault(LatestAppChangeListShownVersionKey)?.Let(it =>
            {
                if (Version.TryParse(it, out var v))
                    return v;
                return null;
            });
            var changeListVersion = app.Assembly.GetName().Version?.Let(it =>
                new Version(it.Major, it.Minor));
            if (changeListVersion != null && changeListVersion > changeListShownVersion)
            {
                this.Logger.LogDebug("Show application change list dialog");

                // show change list
                this.isShowingInitialDialogs = true;
                var changeList = app.ChangeList;
                if (changeList != null)
                {
                    this.PersistentState.SetValue<string>(LatestAppChangeListShownVersionKey, changeListVersion.ToString());
                    await new DocumentViewerDialog()
                    {
                        DocumentSource = changeList,
                    }.ShowDialog(this);
                    this.isShowingInitialDialogs = false;
                    return;
                }
                else
                {
                    this.Logger.LogDebug("No application change list to show");
                    this.isShowingInitialDialogs = false;
                }
            }

            // notify application update found
            if (this.Settings.GetValueOrDefault(SettingKeys.NotifyApplicationUpdate)
                && !IsNotifyingAppUpdateFound)
            {
                var task = app.CheckForApplicationUpdateAsync(this, false);
                if (!task.IsCompleted)
                {
                    IsNotifyingAppUpdateFound = true;
                    this.isShowingInitialDialogs = true;
                    await task;
                    IsNotifyingAppUpdateFound = false;
                    this.isShowingInitialDialogs = false;
                    return;
                }
            }

            // show external dependencies dialog
            var hasUnavailableExtDep = false;
            var hasUnavailableRequiredExtDep = false;
            foreach (var extDep in app.ExternalDependencies)
            {
                if (extDep.State != ExternalDependencyState.Available)
                {
                    hasUnavailableExtDep = true;
                    if (extDep.Priority == ExternalDependencyPriority.Required)
                        hasUnavailableRequiredExtDep = true;
                }
            }
            if (hasUnavailableRequiredExtDep || this.PersistentState.GetValueOrDefault(ExtDepDialogShownVersionKey) != app.ExternalDependenciesVersion)
            {
                if (hasUnavailableExtDep)
                {
                    this.Logger.LogDebug("Show external dependencies dialog");
                    this.PersistentState.SetValue<int>(ExtDepDialogShownVersionKey, app.ExternalDependenciesVersion);
                    this.isShowingInitialDialogs = true;
                    await new ExternalDependenciesDialog().ShowDialog(this);
                    this.isShowingInitialDialogs = false;
                    return;
                }
                else
                    this.PersistentState.SetValue<int>(ExtDepDialogShownVersionKey, app.ExternalDependenciesVersion);
            }

            // all dialogs closed
            if (!this.isClosingScheduled && !app.IsShutdownStarted)
            {
                this.Logger.LogWarning("All initial dialogs closed");
                this.SetAndRaise<bool>(AreInitialDialogsClosedProperty, ref this.areInitialDialogsClosed, true);
                this.OnInitialDialogsClosed();
            }
        }


        // Update chrome hints for extending client area.
        void UpdateExtendClientAreaChromeHints(bool willBeFullScreen)
        {
            if (this.IsClosed)
                return;
            if (this.ExtendClientAreaToDecorationsHint)
            {
                var hints = ExtendClientAreaChromeHints.PreferSystemChrome;
                if (Platform.IsMacOS && !willBeFullScreen && this.WindowState != WindowState.FullScreen)
                    hints |= ExtendClientAreaChromeHints.OSXThickTitleBar;
                this.ExtendClientAreaChromeHints = hints;
            }
        }


        // Update client area extending state.
        void UpdateExtendingClientArea()
        {
            if (!this.IsExtendingClientAreaAllowed || !ExtendedClientAreaWindowConfiguration.IsExtendedClientAreaSupported)
            {
                this.ExtendClientAreaToDecorationsHint = false;
                return;
            }
            this.ExtendClientAreaToDecorationsHint = true;
        }
    }


    /// <summary>
    /// Base class of main window pf application.
    /// </summary>
    /// <typeparam name="TApp">Type of application.</typeparam>
    /// <typeparam name="TViewModel">Type of view-model.</typeparam>
    public abstract class MainWindow<TApp, TViewModel> : MainWindow<TViewModel> where TApp : class, IAppSuiteApplication where TViewModel : MainWindowViewModel
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
