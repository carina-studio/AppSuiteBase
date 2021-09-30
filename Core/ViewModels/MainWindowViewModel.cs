using CarinaStudio.Threading;
using CarinaStudio.ViewModels;
using System;

namespace CarinaStudio.AppSuite.ViewModels
{
    /// <summary>
    /// Base class of view-model for <see cref="Controls.MainWindow{TViewModel}"/>.
    /// </summary>
    public abstract class MainWindowViewModel : ViewModel<IAppSuiteApplication>
    {
        /// <summary>
        /// Property of <see cref="Title"/>.
        /// </summary>
        public static readonly ObservableProperty<string?> TitleProperty = ObservableProperty.Register<MainWindowViewModel, string?>(nameof(Title));


        // Fields.
        readonly ScheduledAction updateTitleAction;


        /// <summary>
        /// Initialize new <see cref="MainWindowViewModel"/> instance.
        /// </summary>
        protected MainWindowViewModel() : base(AppSuiteApplication.Current)
        {
            this.updateTitleAction = new ScheduledAction(() =>
            {
                if (this.IsDisposed)
                    return;
                this.SetValue(TitleProperty, this.OnUpdateTitle());
            });
            this.updateTitleAction.Schedule();
        }


        /// <summary>
        /// Dispose instance.
        /// </summary>
        /// <param name="disposing">True to release managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            this.updateTitleAction.Cancel();
            base.Dispose(disposing);
        }


        /// <summary>
        /// Invalidate and update title.
        /// </summary>
        protected void InvalidateTitle()
        {
            this.VerifyAccess();
            if (this.IsDisposed)
                return;
            this.updateTitleAction.Schedule();
        }


        /// <summary>
        /// Called to update title.
        /// </summary>
        /// <returns>Title.</returns>
        protected abstract string? OnUpdateTitle();


        /// <summary>
        /// Get title of main window.
        /// </summary>
        public string? Title { get => this.GetValue(TitleProperty); }
    }


    /// <summary>
    /// Base class of view-model for <see cref="Controls.MainWindow{TViewModel}"/>.
    /// </summary>
    /// <typeparam name="TApp">Type of application.</typeparam>
    public abstract class MainWindowViewModel<TApp> : MainWindowViewModel where TApp : class, IAppSuiteApplication
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
