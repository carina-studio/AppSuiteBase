using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Base class of input dialog in AppSuite.
    /// </summary>
    public abstract class InputDialog : CarinaStudio.Controls.InputDialog<IAppSuiteApplication>
    {
        // Fields.
        Control? inputControlToAcceptEnterKey;
        bool isEnterKeyClickedOnInputControl;
        bool isEnterKeyDownOnInputControl;
        INameScope? templateNameScope;
        TutorialPresenter? tutorialPresenter;


        /// <summary>
        /// Initialize new <see cref="InputDialog"/> instance.
        /// </summary>
        protected InputDialog()
        {
#pragma warning disable CA1806
            // ReSharper disable ObjectCreationAsStatement
            new WindowContentFadingHelper(this);
            // ReSharper restore ObjectCreationAsStatement
#pragma warning restore CA1806
            this.AddHandler(KeyDownEvent, (_, e) => this.OnPreviewKeyDown(e), Avalonia.Interactivity.RoutingStrategies.Tunnel);
            this.AddHandler(KeyUpEvent, (_, e) => this.OnPreviewKeyUp(e), Avalonia.Interactivity.RoutingStrategies.Tunnel);
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


        /// <summary>
        /// Called when <see cref="Key.Enter"/> clicked on input control without handling by the control.
        /// </summary>
        /// <param name="control"><see cref="Control"/> which <see cref="Key.Enter"/> clicked on.</param>
        protected virtual void OnEnterKeyClickedOnInputControl(Control control)
        { }


        /// <inheritdoc/>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key == Key.Enter && this.isEnterKeyClickedOnInputControl)
            {
                var control = this.inputControlToAcceptEnterKey;
                this.inputControlToAcceptEnterKey = null;
                this.isEnterKeyClickedOnInputControl = false;
                if (!e.Handled && e.Source == control && control != null)
                    this.OnEnterKeyClickedOnInputControl(control);
            }
        }


        /// <summary>
        /// Called to handle key-down event before handling by child control.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.KeyModifiers == 0)
            {
                var control = (this.FocusManager?.GetFocusedElement() as Control);
                this.isEnterKeyDownOnInputControl = control switch
                {
                    NumericUpDown or TextBox => true,
                    _ => false,
                };
                if (this.isEnterKeyDownOnInputControl)
                    this.inputControlToAcceptEnterKey = control;
            }
        }


        /// <summary>
        /// Called to handle key-up event before handling by child control.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.Enter && this.isEnterKeyDownOnInputControl)
            {
                this.isEnterKeyDownOnInputControl = false;
                this.isEnterKeyClickedOnInputControl = true;
            }
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
    /// Base class of input dialog in AppSuite.
    /// </summary>
    /// <typeparam name="TApp">Type of application.</typeparam>
    public abstract class InputDialog<TApp> : InputDialog where TApp : class, IAppSuiteApplication
    {
        /// <summary>
        /// Get application instance.
        /// </summary>
        public new TApp Application => (TApp)base.Application;
    }
}
