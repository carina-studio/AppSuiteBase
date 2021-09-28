using CarinaStudio.Threading;
using CarinaStudio.ViewModels;
using System;

namespace CarinaStudio.AppSuite.ViewModels
{
    /// <summary>
    /// Base class of view-model for <see cref="Controls.MainWindow{TApp, TViewModel}"/>.
    /// </summary>
    /// <typeparam name="TApp">Type of application.</typeparam>
    public abstract class MainWindowViewModel<TApp> : ViewModel<TApp> where TApp : class, IAppSuiteApplication<TApp>
    {
        /// <summary>
        /// Property of <see cref="Title"/>.
        /// </summary>
        public static readonly ObservableProperty<string?> TitleProperty = ObservableProperty.Register<MainWindowViewModel<TApp>, string?>(nameof(Title));


        // Fields.
        readonly ScheduledAction updateTitleAction;


        /// <summary>
        /// Initialize new <see cref="MainWindowViewModel{TApp}"/> instance.
        /// </summary>
        protected MainWindowViewModel() : base((TApp)(IAppSuiteApplication<TApp>)AppSuiteApplication<TApp>.Current)
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
}
