using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Base class of input dialog in AppSuite.
    /// </summary>
    public abstract class InputDialog : CarinaStudio.Controls.InputDialog<IAppSuiteApplication>
    {
        /// <summary>
        /// Define <see cref="HasNavigationBar"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> HasNavigationBarProperty = AvaloniaProperty.Register<InputDialog, bool>(nameof(HasNavigationBar), false);
        
        
        // Fields.
        Control? inputControlToAcceptEnterKey;
        bool isEnterKeyClickedOnInputControl;
        bool isEnterKeyDownOnInputControl;
        int navigationBarUpdateDelay;
        INameScope? templateNameScope;
        TutorialPresenter? tutorialPresenter;
        ScheduledAction? updateNavigationBarAction;


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
        
        
        /// <summary>
        /// Get or set whether navigation bar is available in dialog or not.
        /// </summary>
        public bool HasNavigationBar
        {
            get => this.GetValue(HasNavigationBarProperty);
            set => this.SetValue(HasNavigationBarProperty, value);
        }
        
        
        /// <summary>
        /// Invalidate navigation bar and update later.
        /// </summary>
        protected void InvalidateNavigationBar()
        {
            this.VerifyAccess();
            if (this.GetValue(HasNavigationBarProperty) && this.IsOpened)
            {
                if (this.navigationBarUpdateDelay == 0)
                    this.navigationBarUpdateDelay = this.Application.Configuration.GetValueOrDefault(ConfigurationKeys.DialogNavigationBarUpdateDelay);
                this.updateNavigationBarAction ??= new(this.OnUpdateNavigationBar);
                this.updateNavigationBarAction.Schedule(this.navigationBarUpdateDelay);
            }
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
            
            // cancel updating navigation bar
            this.updateNavigationBarAction?.Cancel();

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
        
        
        /// <inheritdoc/>
        protected override void OnOpening(EventArgs e)
        {
            base.OnOpening(e);
            if (this.GetValue(HasNavigationBarProperty))
            {
                this.updateNavigationBarAction ??= new(this.OnUpdateNavigationBar);
                this.updateNavigationBarAction.Schedule();
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
        
        
        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == HasNavigationBarProperty)
            {
                if ((bool)change.NewValue! && this.IsOpened)
                {
                    this.updateNavigationBarAction ??= new(this.OnUpdateNavigationBar);
                    this.updateNavigationBarAction.Reschedule();
                }
            }
        }
        
        
        /// <summary>
        /// Called to update navigation bar.
        /// </summary>
        protected virtual void OnUpdateNavigationBar()
        { }
        
        
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
