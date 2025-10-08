using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using CarinaStudio.Animation;
using CarinaStudio.AppSuite.Net;
using CarinaStudio.AppSuite.Product;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Base class of main window pf application.
/// </summary>
public abstract class MainWindow : Window
{
    /// <summary>
    /// Property of <see cref="AreInitialDialogsClosed"/>.
    /// </summary>
    public static readonly DirectProperty<MainWindow, bool> AreInitialDialogsClosedProperty = AvaloniaProperty.RegisterDirect<MainWindow, bool>(nameof(AreInitialDialogsClosed), w => w.areInitialDialogsClosed);
    /// <summary>
    /// Property of <see cref="ContentPadding"/>.
    /// </summary>
    public static readonly DirectProperty<MainWindow, Thickness> ContentPaddingProperty = AvaloniaProperty.RegisterDirect<MainWindow, Thickness>(nameof(ContentPadding), w => w.contentPadding);
    /// <summary>
    /// Property of <see cref="HasMultipleMainWindows"/>.
    /// </summary>
    public static readonly DirectProperty<MainWindow, bool> HasMultipleMainWindowsProperty = AvaloniaProperty.RegisterDirect<MainWindow, bool>(nameof(HasMultipleMainWindows), w => w.hasMultipleMainWindows);
    
    
    // Constants.
    const int InitialDialogsDelay = 1000;
    const int RestartingMainWindowsDelay = 500;
    const int RestoringWindowStateOnLinuxDuration = 500;
    const int SaveWindowSizeDelay = 300;
    const int UpdateContentPaddingDelay = 300;
    
    
    // Static fields.
    internal static readonly SettingKey<bool> DoNotCheckAppRunningLocationOnMacOSKey = new("MainWindow.DoNotCheckAppRunningLocationOnMacOS");
    internal static readonly SettingKey<int> ExtDepDialogShownVersionKey = new("MainWindow.ExternalDependenciesDialogShownVersion", -1);
    internal static bool IsAppRunningLocationOnMacOSChecked;
    internal static bool IsNetworkConnForActivatingProVersionNotified;
    internal static bool IsNotifyingAppUpdateFound;
    internal static bool IsReactivatingProVersion;
    internal static bool IsReactivatingProVersionNeeded;
    internal static readonly SettingKey<string> LatestAppChangeListShownVersionKey = new("ApplicationChangeListDialog.LatestShownVersion", "");
    internal static readonly SettingKey<int> WindowHeightSettingKey = new("MainWindow.Height", 600);
    internal static readonly SettingKey<WindowState> WindowStateSettingKey = new("MainWindow.State", WindowState.Maximized);
    internal static readonly SettingKey<int> WindowWidthSettingKey = new("MainWindow.Width", 800);
    
    
    // Fields.
    Notification? appUpdateNotification;
    bool areInitialDialogsClosed;
    Thickness contentPadding;
    ThicknessRenderingAnimator? contentPaddingAnimator;
    ContentPresenter? contentPresenter;
    bool hasMultipleMainWindows;
    bool isClosingScheduled;
    bool isFirstContentPaddingUpdate = true;
    bool isShowingInitialDialogs;
    readonly ScheduledAction notifyChineseVariantChangedAction;
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
    
    
    // Constructor.
    /// <summary>
    /// Initialize new <see cref="MainWindow{TViewModel}"/> instance.
    /// </summary>
    internal MainWindow()
    {
        // create commands
        this.LayoutMainWindowsCommand = new Command<MultiWindowLayout>(this.LayoutMainWindows, this.GetObservable(HasMultipleMainWindowsProperty));

        // create scheduled actions
        this.notifyChineseVariantChangedAction = new(() => _ = this.NotifyChineseVariantChanged());
        this.notifyNetworkConnForActivatingProVersionAction = new(this.NotifyNetworkConnectionForActivatingProVersion);
        this.reactivateProVersionAction = new(this.ReactivateProVersion);
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
        this.updateContentPaddingAction = new ScheduledAction(this.UpdateClientPadding);

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
        });

        // extend client area if needed
        this.UpdateExtendingClientArea();
        this.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome; // show system chrome when opened
        
        // setup debug overlays
        this.UpdateDebugOverlays();
    }
    
    
    /// <summary>
    /// Check whether all dialogs which need to be shown after showing main window are closed or not.
    /// </summary>
    public bool AreInitialDialogsClosed => this.areInitialDialogsClosed;


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
        if (string.IsNullOrEmpty(productId))
            return;
        if (!asApp.ProductManager.TryGetProductState(productId, out var state))
        {
            if (this.Configuration.GetValueOrDefault(SimulationConfigurationKeys.FailToActivateProVersion))
                state = ProductState.Deactivated;
            else
                return;
        }
        switch (state)
		{
            case ProductState.Activated:
                if (this.Configuration.GetValueOrDefault(SimulationConfigurationKeys.FailToActivateProVersion))
                    goto case ProductState.Deactivated;
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
                if (this.Configuration.GetValueOrDefault(SimulationConfigurationKeys.FailToActivateProVersion) 
                    && !IsReactivatingProVersion)
                {
                    this.Logger.LogWarning("Need to reactivate Pro-version because of simulation");
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
		if (productId is not null)
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
    protected ISettings Configuration => this.Application.Configuration;


    /// <summary>
    /// Get padding applied on content of Window automatically.
    /// </summary>
    public Thickness ContentPadding => this.contentPadding;


    /// <summary>
    /// Check whether multiple main windows were opened or not.
    /// </summary>
    public bool HasMultipleMainWindows => this.hasMultipleMainWindows;


    /// <summary>
    /// Check whether extending client area to title bar is allowed or not.
    /// </summary>
    public virtual bool IsExtendingClientAreaAllowed => true;


    // Layout main windows.
    void LayoutMainWindows(MultiWindowLayout layout)
    {
        // check state
        this.VerifyAccess();
        if (this.IsClosed || !this.HasMultipleMainWindows)
            return;

        // layout
        this.Application.LayoutMainWindows(this.Screens.ScreenFromWindow(this) ?? this.Screens.Primary.AsNonNull(), layout, this);
    }


    /// <summary>
    /// Command to layout main windows.
    /// </summary>
    /// <remarks>Type of parameter is <see cref="MultiWindowLayout"/>.</remarks>
    public ICommand LayoutMainWindowsCommand { get; }


    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (!this.IsOpened && this.Application.IsDebugMode)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var size = base.MeasureOverride(availableSize);
            stopwatch.Stop();
            this.Logger.LogTrace("[Performance] Took {duration} ms to perform layout measurement before opening", stopwatch.ElapsedMilliseconds);
            return size;
        }
        return base.MeasureOverride(availableSize);
    }


    // Notify user that application update has been found.
    async Task NotifyApplicationUpdateFoundAsync(bool isInitialCheck)
    {
        // check state
        if (!this.Settings.GetValueOrDefault(SettingKeys.NotifyApplicationUpdate))
            return;
        if (IsNotifyingAppUpdateFound || !this.IsOpened)
            return;
        var updateInfo = this.Application.UpdateInfo;
        if (updateInfo is null || updateInfo.Version <= ApplicationUpdateDialog.LatestShownVersion)
            return;
        
        // show notification
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (this is INotificationPresenter notificationPresenter)
        {
            if (this.appUpdateNotification?.IsVisible == true)
                return;
            this.appUpdateNotification = new Notification().Also(notification =>
            {
                notification.Actions =
                [
                    new NotificationAction().Also(it =>
                    {
                        it.Command = new Command(() =>
                        {
                            notification.Dismiss();
                            _ = this.Application.CheckForApplicationUpdateAsync(this, true);
                        });
                        it.Bind(NotificationAction.NameProperty, this.Application.GetObservableString("Common.KnowMoreAbout.WithDialog"));
                    })
                ];
                notification.Dismissed += (_, _) =>
                {
                    if (this.appUpdateNotification == notification)
                        this.appUpdateNotification = null;
                };
                notification.BindToResource(Notification.IconProperty, this, "Image/Icon.Update.Colored");
                notification.Bind(Notification.MessageProperty, new FormattedString().Also(it =>
                {
                    it.Arg1 = this.Application.Name;
                    it.Bind(FormattedString.FormatProperty, this.Application.GetObservableString("MainWindow.ApplicationUpdateFound"));
                }));
                notification.Timeout = null;
            });
            notificationPresenter.AddNotification(this.appUpdateNotification);
            ApplicationUpdateDialog.IgnoreCurrentUpdateInfo();
            return;
        }
        
        // show dialog
        if (isInitialCheck || this.areInitialDialogsClosed)
        {
            IsNotifyingAppUpdateFound = true;
            await this.Application.CheckForApplicationUpdateAsync(this, false);
            IsNotifyingAppUpdateFound = false;
        }
    }
    
    
    // Notify user that the variant of Chinese has been changed.
    async Task NotifyChineseVariantChanged()
    {
        // check state
        if (this.Application is not AppSuiteApplication asApp)
            return;
        if (this.Application.LaunchChineseVariant == this.Application.ChineseVariant)
            return;
        if (this.HasDialogs || !this.IsOpened || !this.IsActive || this.isClosingScheduled || this.Application.IsShutdownStarted)
            return;
        
        // show dialog
        _ = await new MessageDialog
        {
            Icon = MessageDialogIcon.Information,
            Message = asApp.GetObservableString("MainWindow.ChineseVariantChanged"),
        }.ShowDialog(this);
        
        // restart
        asApp.Restart();
    }
    
    
    // Notify user that network connection is needed for activating Pro version.
    void NotifyNetworkConnectionForActivatingProVersion()
    {
        if (this.Application is not AppSuiteApplication asApp)
            return;
        var productId = asApp.ProVersionProductId;
        // ReSharper disable once SuspiciousTypeConversion.Global
        var notificationPresenter = this as INotificationPresenter;
        if (productId is null
            || !this.IsActive
            || (notificationPresenter is null && this.HasDialogs)
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
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (notificationPresenter is not null)
        {
            notificationPresenter.AddNotification(new Notification().Also(it =>
            {
                it.BindToResource(Notification.IconProperty, this, "Image/Icon.Warning.Colored");
                it.Bind(Notification.MessageProperty, new FormattedString().Also(it =>
                {
                    it.Bind(FormattedString.Arg1Property, asApp.GetObservableString($"Product.{productId}"));
                    it.Bind(FormattedString.FormatProperty, asApp.GetObservableString("MainWindow.NetworkConnectionNeededForProductActivation"));
                }));
                it.Timeout = null;
            }));
        }
        else
        {
            _ = new MessageDialog
            {
                Icon = MessageDialogIcon.Information,
                Message = new FormattedString().Also(it =>
                {
                    it.Bind(FormattedString.Arg1Property, asApp.GetObservableString($"Product.{productId}"));
                    it.Bind(FormattedString.FormatProperty, asApp.GetObservableString("MainWindow.NetworkConnectionNeededForProductActivation"));
                }),
            }.ShowDialog(null);
        }
    }


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
            case nameof(IAppSuiteApplication.ChineseVariant):
                if (this.Application.LaunchChineseVariant != this.Application.ChineseVariant)
                    this.notifyChineseVariantChangedAction.Schedule();
                break;
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
                this.SynchronizationContext.Post(() => 
                    _ = this.NotifyApplicationUpdateFoundAsync(false));
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
                this.SetAndRaise(ContentPaddingProperty, ref this.contentPadding, padding));
        });
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
    void OnConfigurationChanged(object? sender, SettingChangedEventArgs e)
    {
        if (this.CheckAccess())
            this.OnConfigurationChanged(e);
        else
            Dispatcher.UIThread.Post(() => this.OnConfigurationChanged(e));
    }


    /// <summary>
    /// Called when one of application configuration has been changed.
    /// </summary>
    /// <param name="e">Event data.</param>
    protected virtual void OnConfigurationChanged(SettingChangedEventArgs e)
    {
        var key = e.Key;
        if (key == ConfigurationKeys.ShowFpsDebugOverlay
            || key == ConfigurationKeys.ShowLayoutTimeGraphDebugOverlay
            || key == ConfigurationKeys.ShowRenderTimeGraphDebugOverlay)
        {
            this.UpdateDebugOverlays();
        }
    }


    /// <summary>
    /// Called to create view-model for application update dialog.
    /// </summary>
    /// <returns>View-model for application update dialog.</returns>
    protected virtual ApplicationUpdater OnCreateApplicationUpdater() => new();
    
    
    // Called when data dragged into window,
    void OnDragEnter(object? sender, DragEventArgs e) => this.ActivateAndBringToFront();
    
    
    /// <inheritdoc/>
    protected override void OnFirstMeasurementCompleted(Size measuredSize)
    {
        // call base
        base.OnFirstMeasurementCompleted(measuredSize);
        
        // update content padding
        this.updateContentPaddingAction.Execute();
    }


    /// <summary>
    /// Called when all dialogs which need to be shown after showing main window are closed.
    /// </summary>
    protected virtual void OnInitialDialogsClosed()
    { }


    // Called when list of main window changed.
    void OnMainWindowsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        this.SetAndRaise(HasMultipleMainWindowsProperty, ref this.hasMultipleMainWindows, this.Application.MainWindows.Count > 1);
    

    // Called when property of network manager changed.
	void OnNetworkManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(NetworkManager.IsNetworkConnected))
            this.CheckNetworkConnectionForActivatingProVersion();
	}
    

    /// <summary>
    /// Called to notify user that Pro-version is needed to be reactivated.
    /// </summary>
    /// <returns>Task of notifying user. The result will be true if user want to reactivate Pro-version.</returns>
    protected virtual async Task<bool> OnNotifyReactivatingProVersionNeededAsync()
    {
        // check state
        if (this.Application is not AppSuiteApplication asApp)
            return false;
        var productId = asApp.ProVersionProductId;
        if (string.IsNullOrEmpty(productId))
            return false;
        
        this.Logger.LogWarning("Notify user that Pro-version is needed to be reactivated");
        
        // show notification or message dialog
        var activate = false;
        var deactivate = false;
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (this is INotificationPresenter notificationPresenter)
        {
            var taskCompletionSource = new TaskCompletionSource();
            notificationPresenter.AddNotification(new Notification().Also(notification =>
            {
                notification.Actions =
                [
                    new NotificationAction().Also(it =>
                    {
                        it.Command = new Command(() =>
                        {
                            if (!taskCompletionSource.Task.IsCompleted)
                            {
                                activate = true;
                                taskCompletionSource.TrySetResult();
                            }
                            notification.Dismiss();
                        });
                        it.Bind(NotificationAction.NameProperty, asApp.GetObservableString("Common.Reactivate.WithDialog"));
                    }),
                    new NotificationAction().Also(it =>
                    {
                        it.Command = new Command(() =>
                        {
                            if (!taskCompletionSource.Task.IsCompleted)
                            {
                                deactivate = true;
                                taskCompletionSource.TrySetResult();
                            }
                            notification.Dismiss();
                        });
                        it.Bind(NotificationAction.NameProperty, asApp.GetObservableString("ApplicationInfoDialog.DeactivateProduct"));
                    })
                ];
                notification.Dismissed += (_, _) => taskCompletionSource.TrySetResult();
                notification.BindToResource(Notification.IconProperty, this, "Image/Icon.Warning.Colored");
                notification.Bind(Notification.MessageProperty, new FormattedString().Also(it =>
                {
                    it.Bind(FormattedString.Arg1Property, asApp.GetObservableString($"Product.{productId}"));
                    it.Bind(FormattedString.FormatProperty, asApp.GetObservableString("MainWindow.ReactivateProductNeeded"));
                }));
                notification.Timeout = null;
            }));
            await taskCompletionSource.Task;
        }
        else
        {
            activate = await new MessageDialog
            {
                Buttons = MessageDialogButtons.YesNo,
                CustomNoText = asApp.GetObservableString("ApplicationInfoDialog.DeactivateProduct"),
                CustomYesText = asApp.GetObservableString("Common.Reactivate"),
                DefaultResult = MessageDialogResult.Yes,
                Icon = MessageDialogIcon.Warning,
                Message = new FormattedString().Also(it =>
                {
                    it.Bind(FormattedString.Arg1Property, asApp.GetObservableString($"Product.{productId}"));
                    it.Bind(FormattedString.FormatProperty, asApp.GetObservableString("MainWindow.ReactivateProductNeeded"));
                }),
            }.ShowDialog(this) == MessageDialogResult.Yes;
            deactivate = !activate;
        }
        
        // deactivate Pro-version
        if (deactivate)
            _ = asApp.ProductManager.DeactivateAndRemoveDeviceAsync(productId, this);
        
        // complete
        return activate;
    }


    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        // keep time
        this.openedTime = Stopwatch.ElapsedMilliseconds;

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
        this.SetAndRaise(HasMultipleMainWindowsProperty, ref this.hasMultipleMainWindows, this.Application.MainWindows.Count > 1);
        
        // notify change of chinese variant
        if (this.Application.LaunchChineseVariant != this.Application.ChineseVariant)
            this.notifyChineseVariantChangedAction.Schedule();

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
        
        // restart window state
        this.RestoreToSavedWindowState();
    }


    /// <inheritdoc/>
    protected override void OnOpening(EventArgs e)
    {
        // call base
        base.OnOpening(e);
        
        // restore to saved state and size
        if (!this.RestoreToSavedWindowState())
            this.RestoreToSavedSize();
    }


    // Called when product state changed.
	void OnProductStateChanged(IProductManager productManager, string productId)
    {
        if (this.Application is not AppSuiteApplication asApp || productId != asApp.ProVersionProductId)
            return;
        this.CheckIsReactivatingProVersionNeeded();
    }


    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var property = change.Property;
        if (property == ContentProperty)
        {
            if (!this.Application.IsPrivacyPolicyAgreed || !this.Application.IsUserAgreementAgreed)
                (change.NewValue as Control)?.Let(it => it.IsEnabled = false);
        }
        else if (property == ExtendClientAreaToDecorationsHintProperty)
        {
            if (this.IsOpened)
                this.UpdateExtendClientAreaChromeHints(false);
        }
        else if (property == HasDialogsProperty)
        {
            if (!(bool)change.NewValue!)
            {
                if (this.Application.LaunchChineseVariant != this.Application.ChineseVariant)
                    this.notifyChineseVariantChangedAction.Schedule();
                if (this.Application.IsRestartingRootWindowsNeeded)
                    this.restartingRootWindowsAction.Reschedule(RestartingMainWindowsDelay);
                if (!this.AreInitialDialogsClosed)
                    this.showInitDialogsAction.Schedule();
                if (this.Application is AppSuiteApplication asApp && asApp.ProVersionProductId is not null)
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
        }
        else if (property == HeightProperty || property == WidthProperty)
            this.saveWindowSizeAction.Reschedule(SaveWindowSizeDelay);
        else if (property == IsActiveProperty)
        {
            if ((bool)change.NewValue!)
            {
                if (this.Application.LaunchChineseVariant != this.Application.ChineseVariant)
                    this.notifyChineseVariantChangedAction.Schedule();
                if (!this.AreInitialDialogsClosed)
                    this.showInitDialogsAction.Schedule();
                else
                {
                    this.SynchronizationContext.Post(() =>
                        _ = this.NotifyApplicationUpdateFoundAsync(false));
                }
                if (this.Application is AppSuiteApplication asApp && asApp.ProVersionProductId is not null)
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
        }
        else if (property == WindowStateProperty)
        {
            if (this.IsOpened)
            {
                var windowState = (WindowState)change.NewValue!;
                if (windowState != WindowState.Minimized)
                {
                    // [Workaround] Need to restore window state (Maximized) twice on Linux (Fedora)
                    if (Platform.IsLinux
                        && windowState == WindowState.Normal
                        && this.restoredWindowState != WindowState.Normal
                        && (Stopwatch.ElapsedMilliseconds - this.openedTime) < RestoringWindowStateOnLinuxDuration)
                    {
                        this.RestoreToSavedSize();
                        this.SynchronizationContext.PostDelayed(() => this.RestoreToSavedWindowState(), 100);
                        return;
                    }

                    // save window state
                    this.PersistentState.SetValue<WindowState>(WindowStateSettingKey, windowState);
                }
                if (windowState == WindowState.FullScreen || Platform.IsMacOS)
                    this.updateContentPaddingAction.Reschedule();
                else
                    this.updateContentPaddingAction.Reschedule(UpdateContentPaddingDelay);
            }
            this.RestoreToSavedSize();
            this.InvalidateTransparencyLevelHint();
            this.UpdateExtendClientAreaChromeHints(false);
        }
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
    
    
    // Re-activate Pro version.
    async void ReactivateProVersion()
    {
        if (this.Application is not AppSuiteApplication asApp)
            return;
        var productId = asApp.ProVersionProductId;
        if (productId is null 
            || !IsReactivatingProVersionNeeded
            || this.HasDialogs 
            || !this.IsActive 
            || asApp.IsActivatingProVersion
            || IsReactivatingProVersion)
        {
            return;
        }
        IsReactivatingProVersionNeeded = false;
        if (asApp.ProductManager.TryGetProductState(productId, out var state))
        {
            if (state == ProductState.Activated && asApp.Configuration.GetValueOrDefault(SimulationConfigurationKeys.FailToActivateProVersion))
                state = ProductState.Deactivated;
        }
        else if (asApp.Configuration.GetValueOrDefault(SimulationConfigurationKeys.FailToActivateProVersion))
            state = ProductState.Deactivated;
        else
            return;
        if (state == ProductState.Deactivated && await this.OnNotifyReactivatingProVersionNeededAsync())
        {
            IsReactivatingProVersion = true;
            await asApp.ActivateProVersionAsync(this);
            IsReactivatingProVersion = false;
        }
    }


    // Restore to saved window size if available.
    void RestoreToSavedSize()
    {
        if (this.WindowState == WindowState.Normal
            && double.IsFinite(this.restoredWidth)
            && double.IsFinite(this.restoredHeight))
        {
            var screen = this.Screens.ScreenFromWindow(this) ?? this.Screens.Primary;
            if (screen is null)
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


    // Restore to saved window state if available.
    bool RestoreToSavedWindowState()
    {
        var savedWindowState = this.restoredWindowState;
        if (savedWindowState == WindowState.FullScreen)
        {
            if (!this.IsOpened)
                savedWindowState = WindowState.Normal;
            this.UpdateExtendClientAreaChromeHints(true); // [Workaround] Prevent making title bar be transparent
        }
        if (this.WindowState != savedWindowState)
        {
            this.WindowState = savedWindowState; // Size will also be restored in OnPropertyChanged()
            return true;
        }
        return false;
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

        // show user agreement
        if (!app.IsUserAgreementAgreed)
        {
            this.Logger.LogDebug("Show User Agreement dialog");
            var documentSource = app.UserAgreement;
            if (documentSource is not null)
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
            if (documentSource is not null)
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
        var changeListShownVersion = this.PersistentState.GetValueOrDefault(LatestAppChangeListShownVersionKey).Let(it =>
        {
            if (Version.TryParse(it, out var v))
                return v;
            return null;
        });
        var changeListVersion = app.Assembly.GetName().Version?.Let(it =>
            new Version(it.Major, it.Minor));
        if (changeListVersion is not null && changeListVersion > changeListShownVersion)
        {
            this.Logger.LogDebug("Show application change list dialog");

            // show change list
            this.isShowingInitialDialogs = true;
            var changeList = app.ChangeList;
            if (changeList is not null)
            {
                this.PersistentState.SetValue<string>(LatestAppChangeListShownVersionKey, changeListVersion.ToString());
                await new DocumentViewerWindow
                {
                    DocumentSource = changeList,
                    Topmost = this.Topmost,
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
        var task = this.NotifyApplicationUpdateFoundAsync(true);
        if (!task.IsCompleted)
        {
            this.isShowingInitialDialogs = true;
            await task;
            IsNotifyingAppUpdateFound = false;
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
            this.PersistentState.SetValue<int>(ExtDepDialogShownVersionKey, app.ExternalDependenciesVersion);
        }

        // all dialogs closed
        if (!this.isClosingScheduled && !app.IsShutdownStarted)
        {
            this.Logger.LogWarning("All initial dialogs closed");
            this.SetAndRaise(AreInitialDialogsClosedProperty, ref this.areInitialDialogsClosed, true);
            this.OnInitialDialogsClosed();
        }
    }
    
    
    // Update padding of client.
    void UpdateClientPadding()
    {
        // check state
        if (this.IsClosed || !this.ExtendClientAreaToDecorationsHint)
            return;
        var windowState = this.WindowState;
        if (windowState == WindowState.Minimized)
            return;

        // check content
        if (this.contentPresenter is null)
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
            this.contentPaddingAnimator = new ThicknessRenderingAnimator(this, this.contentPresenter.Padding, margin).Also(it =>
            {
                it.Completed += (_, _) => this.contentPresenter.Padding = it.EndValue;
                it.Duration = this.FindResourceOrDefault("TimeSpan/MainWindow.ContentPaddingTransition", TimeSpan.FromMilliseconds(500));
                it.Interpolator = Interpolators.FastDeceleration;
                it.ProgressChanged += (_, _) => this.contentPresenter.Padding = it.Value;
                it.Start();
            });
        }
    }
    
    
    // Update rendered debug overlays.
    void UpdateDebugOverlays()
    {
        var overlays = RendererDebugOverlays.None;
        var config = this.Configuration;
        if (config.GetValueOrDefault(ConfigurationKeys.ShowFpsDebugOverlay))
            overlays |= RendererDebugOverlays.Fps;
        if (config.GetValueOrDefault(ConfigurationKeys.ShowLayoutTimeGraphDebugOverlay))
            overlays |= RendererDebugOverlays.LayoutTimeGraph;
        if (config.GetValueOrDefault(ConfigurationKeys.ShowRenderTimeGraphDebugOverlay))
            overlays |= RendererDebugOverlays.RenderTimeGraph;
        this.RendererDiagnostics.DebugOverlays = overlays;
    }


    // Update chrome hints for extending client area.
    void UpdateExtendClientAreaChromeHints(bool willBeFullScreen)
    {
        if (this.IsClosed)
            return;
        if (this.ExtendClientAreaToDecorationsHint)
        {
            var hints = ExtendClientAreaChromeHints.PreferSystemChrome;
            if (Platform.IsMacOS 
                && !willBeFullScreen 
                && this.WindowState != WindowState.FullScreen)
            {
                hints |= ExtendClientAreaChromeHints.OSXThickTitleBar;
            }
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
/// <typeparam name="TViewModel">Type of view-model.</typeparam>
public abstract class MainWindow<TViewModel> : MainWindow, IMainWindow where TViewModel : MainWindowViewModel
{
    // Fields.
    TViewModel? attachedViewModel;


    /// <summary>
    /// Initialize new <see cref="MainWindow{TViewModel}"/> instance.
    /// </summary>
    protected MainWindow()
    {
        // observe self properties
        this.GetObservable(DataContextProperty).Subscribe(dataContext =>
        {
            if (this.attachedViewModel is not null)
            {
                this.OnDetachFromViewModel(this.attachedViewModel);
                this.attachedViewModel = null;
            }
            this.attachedViewModel = dataContext as TViewModel;
            if (this.attachedViewModel is not null)
                this.OnAttachToViewModel(this.attachedViewModel);
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
    /// Called to detach from view-model.
    /// </summary>
    /// <param name="viewModel">View-model.</param>
    protected virtual void OnDetachFromViewModel(TViewModel viewModel)
    {
        // detach
        viewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
    }
    
    
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
    public new TApp Application => (TApp)base.Application;
}