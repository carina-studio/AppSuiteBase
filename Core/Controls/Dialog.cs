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
        protected Dialog() => new WindowContentFadingHelper(this);
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
