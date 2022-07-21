using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Platform;
using CarinaStudio.Animation;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
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
        public static readonly AvaloniaProperty<bool> AreInitialDialogsClosedProperty = AvaloniaProperty.RegisterDirect<MainWindow<TViewModel>, bool>(nameof(AreInitialDialogsClosed), w => w.areInitialDialogsClosed);
        /// <summary>
        /// Property of <see cref="ContentPadding"/>.
        /// </summary>
        public static readonly AvaloniaProperty<Thickness> ContentPaddingProperty = AvaloniaProperty.RegisterDirect<MainWindow<TViewModel>, Thickness>(nameof(ContentPadding), w => w.contentPadding);
        /// <summary>
        /// Property of <see cref="HasMultipleMainWindows"/>.
        /// </summary>
        public static readonly AvaloniaProperty<bool> HasMultipleMainWindowsProperty = AvaloniaProperty.RegisterDirect<MainWindow<TViewModel>, bool>(nameof(HasMultipleMainWindows), w => w.hasMultipleMainWindows);


        // Constants.
        const int InitialDialogsDelay = 1000;
        const int RestartingMainWindowsDelay = 500;
        const int SaveWindowSizeDelay = 300;
        const int UpdateContentPaddingDelay = 300;


        // Static fields.
        static readonly SettingKey<int> ExtDepDialogShownVersionKey = new SettingKey<int>("MainWindow.ExternalDependenciesDialogShownVersion", -1);
        static bool IsNotifyingAppUpdateFound;
        static readonly SettingKey<int> WindowHeightSettingKey = new SettingKey<int>("MainWindow.Height", 600);
        static readonly SettingKey<WindowState> WindowStateSettingKey = new SettingKey<WindowState>("MainWindow.State", WindowState.Maximized);
        static readonly SettingKey<int> WindowWidthSettingKey = new SettingKey<int>("MainWindow.Width", 800);


        // Fields.
        bool areInitialDialogsClosed;
        Thickness contentPadding;
        ThicknessAnimator? contentPaddingAnimator;
        ContentPresenter? contentPresenter;
        bool hasMultipleMainWindows;
        bool isFirstContentPaddingUpdate = true;
        bool isShowingInitialDialogs;
        long openedTime;
        readonly ScheduledAction restartingMainWindowsAction;
        double restoredHeight;
        double restoredWidth;
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
            this.restartingMainWindowsAction = new ScheduledAction(() =>
            {
                if (!this.IsOpened || this.HasDialogs || !this.Application.IsRestartingMainWindowsNeeded)
                    return;
                this.Logger.LogWarning("Restart main windows");
                this.Application.RestartMainWindows();
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
                this.Height = this.restoredHeight;
                this.Width = this.restoredWidth;
                this.WindowState = it.GetValueOrDefault(WindowStateSettingKey);
            });

            // extend client area if needed
            this.UpdateExtendingClientArea();
            this.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.OSXThickTitleBar | ExtendClientAreaChromeHints.NoChrome; // show system chrome when opened
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
            this.Application.LayoutMainWindows(Screens.ScreenFromVisual(this), layout, this);
        }


        /// <summary>
        /// Command to layout main windows.
        /// </summary>
        /// <remarks>Type of parameter is <see cref="MultiWindowLayout"/>.</remarks>
        public ICommand LayoutMainWindowsCommand { get; }


        /// <summary>
        /// Show dialog if it is needed to notify user that application update has been found.
        /// </summary>
        /// <returns>Task of notifying user.</returns>
        protected async Task NotifyApplicationUpdateFound()
        {
            // check state
            this.VerifyAccess();
            if (!this.IsOpened || !this.IsActive)
                return;
            if (IsNotifyingAppUpdateFound)
                return;
            if (this.HasDialogs)
                return;

            // check version
            var updateInfo = this.Application.UpdateInfo;
            if (updateInfo == null)
                return;
            if (updateInfo.Version == ApplicationUpdateDialog.LatestShownVersion)
                return;

            // show dialog
            using var updater = this.OnCreateApplicationUpdater();
            var dialogResult = ApplicationUpdateDialogResult.None;
            IsNotifyingAppUpdateFound = true;
            try
            {
                dialogResult = await new ApplicationUpdateDialog(updater)
                {
                    CheckForUpdateWhenShowing = false
                }.ShowDialog(this);
            }
            finally
            {
                IsNotifyingAppUpdateFound = false;
            }
            if (this.IsClosed)
                return;

            // shutdown to update
            if (dialogResult == ApplicationUpdateDialogResult.ShutdownNeeded)
            {
                this.Logger.LogWarning("Prepare shutting down to update application");
                await this.OnPrepareShuttingDownForApplicationUpdate();
                this.Logger.LogWarning("Shut down to update application");
                this.Application.Shutdown();
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
                case nameof(IAppSuiteApplication.IsPrivacyPolicyAgreed):
                case nameof(IAppSuiteApplication.IsUserAgreementAgreed):
                    (this.Content as Control)?.Let(content => content.IsEnabled = this.Application.IsPrivacyPolicyAgreed && this.Application.IsUserAgreementAgreed);
                    break;
                case nameof(IAppSuiteApplication.IsRestartingMainWindowsNeeded):
                    if (this.Application.IsRestartingMainWindowsNeeded)
                    {
                        if (!BaseApplicationOptionsDialog.HasOpenedDialogs)
                            this.restartingMainWindowsAction.Reschedule(RestartingMainWindowsDelay);
                    }
                    else
                        this.restartingMainWindowsAction.Cancel();
                    break;
                case nameof(IAppSuiteApplication.UpdateInfo):
                    if (this.AreInitialDialogsClosed && this.Settings.GetValueOrDefault(SettingKeys.NotifyApplicationUpdate))
                        this.SynchronizationContext.Post(() => _ = this.NotifyApplicationUpdateFound());
                    break;
            }
        }


        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            this.contentPresenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter").Also(it =>
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
            this.DataContext = null;
            this.Application.Configuration.SettingChanged -= this.OnConfigurationChanged;
            ((INotifyCollectionChanged)this.Application.MainWindows).CollectionChanged -= this.OnMainWindowsChanged;
            this.Application.PropertyChanged -= this.OnApplicationPropertyChanged;
            this.RemoveHandler(DragDrop.DragEnterEvent, this.OnDragEnter);
            this.restartingMainWindowsAction.Cancel();
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
        /// Called to create view-model for application change list dialog.
        /// </summary>
        /// <returns>View-model for application change list dialog.</returns>
        protected virtual ApplicationChangeList OnCreateApplicationChangeList() => new ApplicationChangeList();


        /// <summary>
        /// Called to create view-model of application info.
        /// </summary>
        /// <returns>View-model of application info.</returns>
        protected virtual ApplicationInfo OnCreateApplicationInfo() => new ApplicationInfo();


        /// <summary>
        /// Called to create view-model for application update dialog.
        /// </summary>
        /// <returns>View-model for application update dialog.</returns>
        protected virtual ApplicationUpdater OnCreateApplicationUpdater() => new ApplicationUpdater();


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


        /// <summary>
        /// Called when window opened.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnOpened(EventArgs e)
        {
            // keep time
            this.openedTime = Stopwatch.ElapsedMilliseconds;

            // restore to saved size
            this.RestoreToSavedSize();

            // call base
            base.OnOpened(e);

            // add event handlers
            this.Application.Configuration.SettingChanged += this.OnConfigurationChanged;
            ((INotifyCollectionChanged)this.Application.MainWindows).CollectionChanged += this.OnMainWindowsChanged;
            this.Application.PropertyChanged += this.OnApplicationPropertyChanged;
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
            if (this.ExtendClientAreaToDecorationsHint)
                this.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.OSXThickTitleBar | ExtendClientAreaChromeHints.PreferSystemChrome;
        }


        /// <summary>
        /// Called to prepare before shutting down application to update.
        /// </summary>
        /// <returns>Task of preparation.</returns>
        protected virtual Task OnPrepareShuttingDownForApplicationUpdate() => Task.CompletedTask;


        /// <summary>
        /// Called when property changed.
        /// </summary>
        /// <param name="change">Data of change property.</param>
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);
            var property = change.Property;
            if (property == ContentProperty)
            {
                if (!this.Application.IsPrivacyPolicyAgreed || !this.Application.IsUserAgreementAgreed)
                    (change.NewValue.Value as Control)?.Let(content => content.IsEnabled = false);
            }
            else if (property == DataContextProperty)
            {
                (change.OldValue.Value as TViewModel)?.Let(it => this.OnDetachFromViewModel(it));
                (change.NewValue.Value as TViewModel)?.Let(it => this.OnAttachToViewModel(it));
            }
            else if (property == ExtendClientAreaToDecorationsHintProperty)
            {
                if (this.IsOpened && this.ExtendClientAreaToDecorationsHint)
                    this.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.OSXThickTitleBar | ExtendClientAreaChromeHints.PreferSystemChrome;
            }
            else if (property == HasDialogsProperty)
            {
                if (!this.HasDialogs)
                {
                    if (this.Application.IsRestartingMainWindowsNeeded)
                        this.restartingMainWindowsAction.Reschedule(RestartingMainWindowsDelay);
                    if (!this.AreInitialDialogsClosed)
                        this.showInitDialogsAction.Schedule();
                }
            }
            else if (property == HeightProperty || property == WidthProperty)
                this.saveWindowSizeAction.Reschedule(SaveWindowSizeDelay);
            else if (property == IsActiveProperty)
            {
                if (this.IsActive)
                {
                    if (!this.AreInitialDialogsClosed)
                        this.showInitDialogsAction.Schedule();
                    else if (this.Settings.GetValueOrDefault(SettingKeys.NotifyApplicationUpdate))
                        this.SynchronizationContext.Post(() => _ = this.NotifyApplicationUpdateFound());
                }
            }
            else if (property == WindowStateProperty)
            {
                var windowState = this.WindowState;
                if (windowState == WindowState.FullScreen)
                    windowState = WindowState.Maximized; // [Workaround] Prevent launching in FullScreen mode because that layout may be incorrect on macOS
                if (windowState != WindowState.Minimized)
                    this.PersistentState.SetValue<WindowState>(WindowStateSettingKey, windowState);
                if (this.IsOpened)
                {
                    if (windowState == WindowState.FullScreen)
                        this.updateContentPaddingAction.Reschedule();
                    else
                        this.updateContentPaddingAction.Reschedule(UpdateContentPaddingDelay);
                }
                this.RestoreToSavedSize();
                this.InvalidateTransparencyLevelHint();
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
                var screen = Screens.ScreenFromVisual(this);
                var workingAreaWidth = (double)screen.WorkingArea.Width;
                var workingAreaHeight = (double)screen.WorkingArea.Height;
                if (!Platform.IsMacOS)
                {
                    workingAreaWidth /= screen.PixelDensity;
                    workingAreaHeight /= screen.PixelDensity;
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
            if (!this.IsOpened || !this.IsActive)
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

            // show user agreement
            if (!this.Application.IsUserAgreementAgreed)
            {
                this.Logger.LogDebug("Show User Agreement dialog");
                using var appInfo = this.OnCreateApplicationInfo();
                this.isShowingInitialDialogs = true;
                if (!await new UserAgreementDialog(appInfo).ShowDialog(this))
                {
                    this.Logger.LogWarning("User decline the current User Agreement");
                    this.Close();
                }
                this.isShowingInitialDialogs = false;
                return;
            }

            // show privacy policy
            if (!this.Application.IsPrivacyPolicyAgreed)
            {
                this.Logger.LogDebug("Show Privacy Policy dialog");
                using var appInfo = this.OnCreateApplicationInfo();
                this.isShowingInitialDialogs = true;
                if (!await new PrivacyPolicyDialog(appInfo).ShowDialog(this))
                {
                    this.Logger.LogWarning("User decline the current Privacy Policy");
                    this.Close();
                }
                this.isShowingInitialDialogs = false;
                return;
            }

            // show application change list
            if (!ApplicationChangeListDialog.IsShownBeforeForCurrentVersion(this.Application))
            {
                this.Logger.LogDebug("Show application change list dialog");

                // check for change list
                this.isShowingInitialDialogs = true;
                using var appChangeList = this.OnCreateApplicationChangeList();
                await appChangeList.WaitForChangeListReadyAsync();
                if (this.IsClosed || !this.IsActive)
                {
                    this.Logger.LogWarning("Window is closed or inactive, show dialog later");
                    this.isShowingInitialDialogs = false;
                    return;
                }
                if (appChangeList.ChangeList.IsNotEmpty())
                {
                    await new ApplicationChangeListDialog(appChangeList).ShowDialog(this);
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
            if (this.Settings.GetValueOrDefault(SettingKeys.NotifyApplicationUpdate))
            {
                var updateInfo = this.Application.UpdateInfo;
                if (updateInfo != null && updateInfo.Version > ApplicationUpdateDialog.LatestShownVersion)
                {
                    this.Logger.LogDebug("Show application update dialog");
                    this.isShowingInitialDialogs = true;
                    await this.NotifyApplicationUpdateFound();
                    this.isShowingInitialDialogs = false;
                    return;
                }
            }

            // show external dependencies dialog
            var hasUnavailableExtDep = false;
            var hasUnavailableRequiredExtDep = false;
            foreach (var extDep in this.Application.ExternalDependencies)
            {
                if (extDep.State != ExternalDependencyState.Available)
                {
                    hasUnavailableExtDep = true;
                    if (extDep.Priority == ExternalDependencyPriority.Required)
                        hasUnavailableRequiredExtDep = true;
                }
            }
            if (hasUnavailableRequiredExtDep || this.PersistentState.GetValueOrDefault(ExtDepDialogShownVersionKey) != this.Application.ExternalDependenciesVersion)
            {
                if (hasUnavailableExtDep)
                {
                    this.Logger.LogDebug("Show external dependencies dialog");
                    this.PersistentState.SetValue<int>(ExtDepDialogShownVersionKey, this.Application.ExternalDependenciesVersion);
                    this.isShowingInitialDialogs = true;
                    await new ExternalDependenciesDialog().ShowDialog(this);
                    this.isShowingInitialDialogs = false;
                    return;
                }
                else
                    this.PersistentState.SetValue<int>(ExtDepDialogShownVersionKey, this.Application.ExternalDependenciesVersion);
            }

            // all dialogs closed
            this.Logger.LogWarning("All initial dialogs closed");
            this.SetAndRaise<bool>(AreInitialDialogsClosedProperty, ref this.areInitialDialogsClosed, true);
            this.OnInitialDialogsClosed();
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
