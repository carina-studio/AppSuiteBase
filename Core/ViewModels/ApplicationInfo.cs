using Avalonia.Controls;
using CarinaStudio.ViewModels;
using System;
using System.Reflection;

namespace CarinaStudio.AppSuite.ViewModels
{
    /// <summary>
    /// View-model of application information UI.
    /// </summary>
    public abstract class ApplicationInfo : ViewModel<IAppSuiteApplication>
    {
        /// <summary>
        /// Initialize new <see cref="ApplicationInfo"/> instance.
        /// </summary>
        protected ApplicationInfo() : base(AppSuiteApplication.Current)
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
