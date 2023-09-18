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
    /// Base class of dialog in AppSuite.
    /// </summary>
    public abstract class Dialog : CarinaStudio.Controls.Dialog<IAppSuiteApplication>
    {
        /// <summary>
        /// Define <see cref="HasNavigationBar"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> HasNavigationBarProperty = AvaloniaProperty.Register<Dialog, bool>(nameof(HasNavigationBar), false);
        
        
        // Fields.
        int navigationBarUpdateDelay;
        INameScope? templateNameScope;
        TutorialPresenter? tutorialPresenter;
        ScheduledAction? updateNavigationBarAction;
        
        
        /// <summary>
        /// Initialize new <see cref="Dialog"/> instance.
        /// </summary>
        protected Dialog()
        {
            _ = new WindowContentFadingHelper(this);
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


        /// <inheritdoc/>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key == Key.Escape && !e.Handled)
                this.Close();
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
