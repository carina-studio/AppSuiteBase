using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
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
        const int AppUpdateNotificationDelay = 1000;
        const int SaveWindowSizeDelay = 300;


        // Static fields.
        static bool IsNotifyingAppUpdateFound;
        static readonly SettingKey<int> WindowHeightSettingKey = new SettingKey<int>("MainWindow.Height", 600);
        static readonly SettingKey<WindowState> WindowStateSettingKey = new SettingKey<WindowState>("MainWindow.State", WindowState.Maximized);
        static readonly SettingKey<int> WindowWidthSettingKey = new SettingKey<int>("MainWindow.Width", 800);


        // Fields.
        readonly ScheduledAction notifyAppUpdateFoundAction;
        long openedTime;
        readonly ScheduledAction saveWindowSizeAction;
        readonly Stopwatch stopWatch = new Stopwatch().Also(it => it.Start());
        readonly ScheduledAction updateContentPaddingAction;


        /// <summary>
        /// Initialize new <see cref="MainWindow{TViewModel}"/> instance.
        /// </summary>
        protected MainWindow()
        {
            // create scheduled actions
            this.notifyAppUpdateFoundAction = new ScheduledAction(this.NotifyApplicationUpdateFound);
            this.saveWindowSizeAction = new ScheduledAction(() =>
            {
                if (this.WindowState == WindowState.Normal)
                {
                    this.PersistentState.SetValue<int>(WindowWidthSettingKey, (int)(this.Width + 0.5));
                    this.PersistentState.SetValue<int>(WindowHeightSettingKey, (int)(this.Height + 0.5));
                }
            });
            this.updateContentPaddingAction = new ScheduledAction(() =>
            {
                // check state
                if (!this.IsOpened || !this.ExtendClientAreaToDecorationsHint)
                    return;

                // update content padding
                this.Padding = this.WindowState switch
                {
                    WindowState.FullScreen
                    or WindowState.Maximized => ExtendedClientAreaWindowConfiguration.ContentPaddingWhenMaximized,
                    _ => new Thickness(0),
                };
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
        }


        /// <summary>
        /// Check whether extending client area to title bar is enabled or not.
        /// </summary>
        protected virtual bool IsExtendingClientAreaEnabled { get; } = true;


        /// <summary>
        /// Show dialog if it is needed to notify user that application update has been found.
        /// </summary>
        protected async void NotifyApplicationUpdateFound()
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

            // notify later
            var delay = AppUpdateNotificationDelay - (this.stopWatch.ElapsedMilliseconds - this.openedTime);
            if (delay > 0)
            {
                this.notifyAppUpdateFoundAction.Reschedule((int)delay);
                return;
            }

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
            if (e.PropertyName == nameof(IAppSuiteApplication.UpdateInfo))
                this.notifyAppUpdateFoundAction.Schedule();
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
            this.notifyAppUpdateFoundAction.Cancel();
            base.OnClosed(e);
        }


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
            this.openedTime = this.stopWatch.ElapsedMilliseconds;

            // call base
            base.OnOpened(e);

            // attach to application
            this.Application.PropertyChanged += this.OnApplicationPropertyChanged;

            // update content padding
            this.updateContentPaddingAction.Schedule();

            // notify application update found
            if (this.Application.MainWindows.Count == 1)
                ApplicationUpdateDialog.ResetLatestShownInfo();
            this.notifyAppUpdateFoundAction.Schedule();
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
            if (property == DataContextProperty)
            {
                (change.OldValue.Value as TViewModel)?.Let(it => this.OnDetachFromViewModel(it));
                (change.NewValue.Value as TViewModel)?.Let(it => this.OnAttachToViewModel(it));
            }
            else if (property == ExtendClientAreaToDecorationsHintProperty)
                this.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.OSXThickTitleBar | ExtendClientAreaChromeHints.PreferSystemChrome;
            else if (property == HasDialogsProperty)
            {
                if (!this.HasDialogs)
                    this.notifyAppUpdateFoundAction.Reschedule(AppUpdateNotificationDelay);
            }
            else if (property == HeightProperty || property == WidthProperty)
                this.saveWindowSizeAction.Reschedule(SaveWindowSizeDelay);
            else if (property == IsActiveProperty)
            {
                if (this.IsActive)
                    this.notifyAppUpdateFoundAction.Schedule();
            }
            else if (property == WindowStateProperty)
            {
                var windowState = this.WindowState;
                if (windowState == WindowState.FullScreen)
                    windowState = WindowState.Maximized; // [Workaround] Prevent launching in FullScreen mode because that layout may be incorrect on macOS
                if (windowState != WindowState.Minimized)
                    this.PersistentState.SetValue<WindowState>(WindowStateSettingKey, windowState);
                this.updateContentPaddingAction.Schedule();
                this.UpdateExtendingClientArea();
            }
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


        // Update client area extending state.
        void UpdateExtendingClientArea()
        {
            if (!this.IsExtendingClientAreaEnabled || !ExtendedClientAreaWindowConfiguration.IsExtendedClientAreaSupported)
            {
                this.ExtendClientAreaToDecorationsHint = false;
                return;
            }
            this.ExtendClientAreaToDecorationsHint = this.WindowState != WindowState.FullScreen;
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
