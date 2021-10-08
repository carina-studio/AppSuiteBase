using Avalonia;
using System;

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


        /// <summary>
        /// Initialize new <see cref="Window{TApp}"/> instance.
        /// </summary>
        protected Window() => new WindowContentFadingHelper(this);


        // Check system chrome visibility.
        void CheckSystemChromeVisibility()
        {
            this.SetValue<bool>(IsSystemChromeVisibleInClientAreaProperty, Global.Run(() =>
            {
                if (this.SystemDecorations != Avalonia.Controls.SystemDecorations.Full)
                    return false;
                if (!this.ExtendClientAreaToDecorationsHint)
                    return false;
                if (this.WindowState == Avalonia.Controls.WindowState.FullScreen)
                    return ExtendedClientAreaWindowConfiguration.IsSystemChromeVisibleInFullScreen;
                return true;
            }));
        }


        /// <summary>
        /// Check whether system chrome is visible in client area or not.
        /// </summary>
        public bool IsSystemChromeVisibleInClientArea { get => this.GetValue<bool>(IsSystemChromeVisibleInClientAreaProperty); }


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
