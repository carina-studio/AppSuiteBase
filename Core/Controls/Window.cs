using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Base class of window of AppSuite.
    /// </summary>
    public abstract class Window : CarinaStudio.Controls.Window<IAppSuiteApplication>
    {
        /// <summary>
        /// Initialize new <see cref="Window{TApp}"/> instance.
        /// </summary>
        protected Window() => new WindowContentFadingHelper(this);
    }


    /// <summary>
    /// Base class of window of AppSuite.
    /// </summary>
    /// <typeparam name="TApp">Type of application.</typeparam>
    public abstract class Window<TApp> : CarinaStudio.Controls.Window<TApp> where TApp : class, IAppSuiteApplication
    {
        /// <summary>
        /// Initialize new <see cref="Window{TApp}"/> instance.
        /// </summary>
        protected Window() => new WindowContentFadingHelper(this);
    }
}
