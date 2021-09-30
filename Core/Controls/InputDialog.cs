using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Base class of input dialog in AppSuite.
    /// </summary>
    public abstract class InputDialog : CarinaStudio.Controls.InputDialog<IAppSuiteApplication>
    {
        /// <summary>
        /// Initialize new <see cref="InputDialog"/> instance.
        /// </summary>
        protected InputDialog()
        {
            new WindowContentFadingHelper(this);
            this.Title = this.Application.Name;
        }
    }


    /// <summary>
    /// Base class of input dialog in AppSuite.
    /// </summary>
    /// <typeparam name="TApp">Type of application.</typeparam>
    public abstract class InputDialog<TApp> : InputDialog where TApp : class, IAppSuiteApplication
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
