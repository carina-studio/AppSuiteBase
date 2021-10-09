using Avalonia;
using Avalonia.Controls;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using System;
using System.ComponentModel;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Base class of window of AppSuite.
    /// </summary>
    public abstract class Window : CarinaStudio.Controls.Window<IAppSuiteApplication>
    {
        /// <summary>
        /// Property of <see cref="IsSystemChromeVisibleInClientArea"/>.
        /// </summary>
        public static readonly AvaloniaProperty<bool> IsSystemChromeVisibleInClientAreaProperty = AvaloniaProperty.Register<Window, bool>(nameof(IsSystemChromeVisibleInClientArea), false);


        // Fields.
        readonly ScheduledAction updateTransparencyLevelAction;


        /// <summary>
        /// Initialize new <see cref="Window{TApp}"/> instance.
        /// </summary>
        protected Window()
        {
            new WindowContentFadingHelper(this);
            this.Application.HardwareInfo.PropertyChanged += this.OnHardwareInfoPropertyChanged;
            this.Settings.SettingChanged += this.OnSettingChanged;
            this.updateTransparencyLevelAction = new ScheduledAction(() =>
            {
                if (!this.IsClosed)
                    this.TransparencyLevelHint = this.OnSelectTransparentLevelHint();
            });
            this.updateTransparencyLevelAction.Schedule();
        }


        // Check system chrome visibility.
        void CheckSystemChromeVisibility()
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
        }


        /// <summary>
        /// Invalidate and update <see cref="TopLevel.TransparencyLevelHint"/>.
        /// </summary>
        protected void InvalidateTransparencyLevelHint() => this.updateTransparencyLevelAction.Schedule();


        /// <summary>
        /// Check whether system chrome is visible in client area or not.
        /// </summary>
        public bool IsSystemChromeVisibleInClientArea { get => this.GetValue<bool>(IsSystemChromeVisibleInClientAreaProperty); }


        /// <summary>
        /// Called when window closed.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnClosed(EventArgs e)
        {
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
            this.updateTransparencyLevelAction.ExecuteIfScheduled();
            base.OnOpened(e);
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
            if (property == ExtendClientAreaToDecorationsHintProperty
                || property == SystemDecorationsProperty
                || property == WindowStateProperty)
            {
                this.CheckSystemChromeVisibility();
            }
        }


        /// <summary>
        /// Called to select transparency level to apply on window.
        /// </summary>
        /// <returns>Transparency level.</returns>
        protected virtual WindowTransparencyLevel OnSelectTransparentLevelHint()
        {
            if (Platform.IsLinux)
                return WindowTransparencyLevel.None;
            if (this.Application.HardwareInfo.HasDedicatedGraphicsCard != true)
                return WindowTransparencyLevel.None;
            if (!this.Settings.GetValueOrDefault(SettingKeys.EnableBlurryBackground))
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
