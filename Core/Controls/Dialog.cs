using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Base class of dialog in AppSuite.
    /// </summary>
    public abstract class Dialog : CarinaStudio.Controls.Dialog<IAppSuiteApplication>
    {
        // Fields.
        INameScope? templateNameScope;
        TutorialPresenter? tutorialPresenter;
        
        
        /// <summary>
        /// Initialize new <see cref="Dialog"/> instance.
        /// </summary>
        protected Dialog()
        {
            _ = new WindowContentFadingHelper(this);
            this.Title = this.Application.Name;
        }


        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            this.templateNameScope = e.NameScope;
            this.tutorialPresenter = null;
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


        /// <summary>
        /// Get <see cref="TutorialPresenter"/> of this dialog.
        /// </summary>
        protected TutorialPresenter? TutorialPresenter
        {
            get
            {
                this.tutorialPresenter ??= this.templateNameScope?.Find<TutorialPresenter>("PART_TutorialPresenter");
                return this.tutorialPresenter;
            }
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
        public new TApp Application => (TApp)base.Application;
    }
}
