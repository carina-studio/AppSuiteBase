using Avalonia.Input;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Base class of dialog in AppSuite.
    /// </summary>
    public abstract class Dialog : CarinaStudio.Controls.Dialog<IAppSuiteApplication>
    {
        /// <summary>
        /// Initialize new <see cref="Dialog"/> instance.
        /// </summary>
        protected Dialog()
        {
            _ = new WindowContentFadingHelper(this);
            this.Title = this.Application.Name;
        }


        /// <summary>
        /// Called when key up.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key == Key.Escape && !e.Handled)
                this.Close();
        }
    }


    /// <summary>
    /// Base class of dialog in AppSuite.
    /// </summary>
    /// <typeparam name="TApp">Type of application.</typeparam>
    public abstract class Dialog<TApp> : Dialog where TApp : class, IAppSuiteApplication
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
