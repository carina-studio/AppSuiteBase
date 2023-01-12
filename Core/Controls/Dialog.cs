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


        /// <inheritdoc/>
        protected override void OnClosed(EventArgs e)
        {
            // call base
            base.OnClosed(e);

            // [Workaround] Prevent Window leak by child controls
            this.SynchronizationContext.Post(_ => this.Content = null, null);
        }


        /// <inheritdoc/>
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
