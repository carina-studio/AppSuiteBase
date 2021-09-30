using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using System;
using System.ComponentModel;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Base class of main window pf application.
    /// </summary>
    /// <typeparam name="TViewModel">Type of view-model.</typeparam>
    public abstract class MainWindow<TViewModel> : Window where TViewModel : MainWindowViewModel
    {
        // Constants.
        const int SaveWindowSizeDelay = 300;


        // Static fields.
        static readonly SettingKey<int> WindowHeightSettingKey = new SettingKey<int>("MainWindow.Height", 600);
        static readonly SettingKey<WindowState> WindowStateSettingKey = new SettingKey<WindowState>("MainWindow.State", WindowState.Maximized);
        static readonly SettingKey<int> WindowWidthSettingKey = new SettingKey<int>("MainWindow.Width", 800);


        // Fields.
        readonly ScheduledAction saveWindowSizeAction;
        readonly ScheduledAction updateContentPaddingAction;


        /// <summary>
        /// Initialize new <see cref="MainWindow{TViewModel}"/> instance.
        /// </summary>
        protected MainWindow()
        {
            // create scheduled actions
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
                this.Padding = this.WindowState != WindowState.Maximized
                    ? new Thickness(0)
                    : ExtendedClientAreaWindowConfiguration.ContentPaddingWhenMaximized;
            });

            // restore window state
            this.PersistentState.Let(it =>
            {
                this.Height = Math.Max(0, it.GetValueOrDefault(WindowHeightSettingKey));
                this.Width = Math.Max(0, it.GetValueOrDefault(WindowWidthSettingKey));
                this.WindowState = it.GetValueOrDefault(WindowStateSettingKey);
            });
        }


        // Called when property of application changed.
        void OnApplicationPropertyChanged(object? sender, PropertyChangedEventArgs e) => this.OnApplicationPropertyChanged(e);


        /// <summary>
        /// Called when property of application changed.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected virtual void OnApplicationPropertyChanged(PropertyChangedEventArgs e)
        { }


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
            base.OnClosed(e);
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


        /// <summary>
        /// Called when window opened.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnOpened(EventArgs e)
        {
            // call base
            base.OnOpened(e);

            // attach to application
            this.Application.PropertyChanged += this.OnApplicationPropertyChanged;

            // update content padding
            this.updateContentPaddingAction.Schedule();
        }


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
            else if (property == HeightProperty || property == WidthProperty)
                this.saveWindowSizeAction.Reschedule(SaveWindowSizeDelay);
            else if (property == WindowStateProperty)
            {
                if (this.WindowState != WindowState.Minimized)
                    this.PersistentState.SetValue<WindowState>(WindowStateSettingKey, this.WindowState);
                this.updateContentPaddingAction.Schedule();
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
