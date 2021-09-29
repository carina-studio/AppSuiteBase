using Avalonia.Controls;
using CarinaStudio.ViewModels;
using System;
using System.Reflection;

namespace CarinaStudio.AppSuite.ViewModels
{
    /// <summary>
    /// View-model of application information UI.
    /// </summary>
    public abstract class ApplicationInfo<TApp> : ViewModel<TApp> where TApp : class, IAppSuiteApplication<TApp>
    {
        /// <summary>
        /// Initialize new <see cref="ApplicationInfo{TApp}"/> instance.
        /// </summary>
        protected ApplicationInfo() : base((TApp)(IAppSuiteApplication<TApp>)AppSuiteApplication<TApp>.Current)
        { }


        /// <summary>
        /// Get application icon.
        /// </summary>
        public abstract WindowIcon Icon { get; }


        /// <summary>
        /// Get name of application.
        /// </summary>
        public string Name { get => this.Application.Name; }


        /// <summary>
        /// Get application version.
        /// </summary>
        public Version Version { get; } = Assembly.GetEntryAssembly().AsNonNull().GetName().Version.AsNonNull();
    }
}
