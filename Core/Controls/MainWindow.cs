using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Base class of main window pf application.
    /// </summary>
    /// <typeparam name="TApp">Type of application.</typeparam>
    public abstract class MainWindow<TApp> : CarinaStudio.Controls.Window<TApp> where TApp : class, IAppSuiteApplication
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
        /// Initialize new <see cref="MainWindow{TApp}"/> instance.
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


        /// <summary>
        /// Called when window opened.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnOpened(EventArgs e)
        {
            // call base
            base.OnOpened(e);

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
            if (property == ExtendClientAreaToDecorationsHintProperty)
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
    }
}
