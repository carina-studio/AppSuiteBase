using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Platform;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Base class of main window pf application.
    /// </summary>
    /// <typeparam name="TViewModel">Type of view-model.</typeparam>
    public abstract class MainWindow<TViewModel> : Window where TViewModel : MainWindowViewModel
    {
        // Constants.
        const int InitialDialogsDelay = 1000;
        const int RestartingMainWindowsDelay = 500;
        const int SaveWindowSizeDelay = 300;
        const int UpdateContentPaddingDelay = 300;


        // Static fields.
        static bool IsNotifyingAppUpdateFound;
        static readonly SettingKey<int> WindowHeightSettingKey = new SettingKey<int>("MainWindow.Height", 600);
        static readonly SettingKey<WindowState> WindowStateSettingKey = new SettingKey<WindowState>("MainWindow.State", WindowState.Maximized);
        static readonly SettingKey<int> WindowWidthSettingKey = new SettingKey<int>("MainWindow.Width", 800);


        // Fields.
        bool isContentPaddingTransitionReady;
        bool isShowingInitialDialogs;
        long openedTime;
        readonly ScheduledAction restartingMainWindowsAction;
        readonly ScheduledAction saveWindowSizeAction;
        readonly ScheduledAction showInitDialogsAction;
        readonly ScheduledAction updateContentPaddingAction;


        /// <summary>
        /// Initialize new <see cref="MainWindow{TViewModel}"/> instance.
        /// </summary>
        protected MainWindow()
        {
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
                if (this.Content is not Control contentControl)
                    return;

                // update content padding
                // [Workaround] cannot use padding of window because that thickness transition doesn't work on it
                contentControl.Margin = windowState switch
                {
                    WindowState.FullScreen
                    or WindowState.Maximized => ExtendedClientAreaWindowConfiguration.ContentPaddingInMaximized,
                    _ => ExtendedClientAreaWindowConfiguration.ContentPadding,
                };

                // setup transition after the first padding ready
                if (!this.isContentPaddingTransitionReady)
                {
                    this.isContentPaddingTransitionReady = true;
                    if (this.TryFindResource("TimeSpan/MainWindow.ContentPaddingTransition", out var res) && res is TimeSpan duration)
                    {
                        var transitions = contentControl.Transitions;
                        if (transitions == null)
                        {
                            transitions = new Transitions();
                            contentControl.Transitions = transitions;
                        }
                        transitions.Add(new ThicknessTransition()
                        {
                            Duration = duration,
                            Easing = new ExponentialEaseOut(),
                            Property = MarginProperty,
                        });
                    }
                }
            });

            // restore window state
            this.PersistentState.Let(it =>
            {
                this.Height = Math.Max(0, it.GetValueOrDefault(WindowHeightSettingKey));
                this.Width = Math.Max(0, it.GetValueOrDefault(WindowWidthSettingKey));
                this.WindowState = it.GetValueOrDefault(WindowStateSettingKey);
            });

            // extend client area if needed
            this.UpdateExtendingClientArea();
            this.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.OSXThickTitleBar | ExtendClientAreaChromeHints.NoChrome; // show system chrome when opened
        }


        /// <summary>
        /// Check whether all dialogs which need to be shown after showing main window are closed or not.
        /// </summary>
        protected bool AreInitialDialogsClosed { get; private set; }


        /// <summary>
        /// Check whether extending client area to title bar is allowed or not.
        /// </summary>
        public virtual bool IsExtendingClientAreaAllowed { get; } = true;


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


        /// <summary>
        /// Called when all dialogs which need to be shown after showing main window are closed.
        /// </summary>
        protected virtual void OnInitialDialogsClosed()
        { }


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
                        this.restartingMainWindowsAction.Reschedule(RestartingMainWindowsDelay);
                    else
                        this.restartingMainWindowsAction.Cancel();
                    break;
                case nameof(IAppSuiteApplication.UpdateInfo):
                    if (this.AreInitialDialogsClosed && this.Settings.GetValueOrDefault(SettingKeys.NotifyApplicationUpdate))
                        this.SynchronizationContext.Post(() => _ = this.NotifyApplicationUpdateFound());
                    break;
            }
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
            this.Application.PropertyChanged -= this.OnApplicationPropertyChanged;
            this.restartingMainWindowsAction.Cancel();
            this.showInitDialogsAction.Cancel();
            base.OnClosed(e);
        }



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


        /// <summary>
        /// Called when window opened.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnOpened(EventArgs e)
        {
            // keep time
            this.openedTime = Stopwatch.ElapsedMilliseconds;

            // call base
            base.OnOpened(e);

            // attach to application
            this.Application.PropertyChanged += this.OnApplicationPropertyChanged;

            // update content padding
            this.updateContentPaddingAction.Schedule();

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
                if (windowState == WindowState.FullScreen)
                    this.updateContentPaddingAction.Reschedule();
                else
                    this.updateContentPaddingAction.Reschedule(UpdateContentPaddingDelay);
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
            if (!ApplicationChangeListDialog.ShownBeforeForCurrentVersion)
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
                await new ApplicationChangeListDialog(appChangeList).ShowDialog(this);
                this.isShowingInitialDialogs = false;
                return;
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
                }
            }

            // all dialogs closed
            this.Logger.LogWarning("All initial dialogs closed");
            this.AreInitialDialogsClosed = true;
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
