﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Base class of input dialog in AppSuite.
/// </summary>
public abstract class InputDialog : CarinaStudio.Controls.InputDialog<IAppSuiteApplication>
{
    /// <summary>
    /// Define <see cref="CancelButtonText"/> property.
    /// </summary>
    public static readonly DirectProperty<InputDialog, string?> CancelButtonTextProperty = AvaloniaProperty.RegisterDirect<InputDialog, string?>(nameof(CancelButtonText), d => d.cancelButtonText);
    /// <summary>
    /// Define <see cref="CanClose"/> property.
    /// </summary>
    public static readonly DirectProperty<InputDialog, bool> CanCloseProperty = AvaloniaProperty.RegisterDirect<InputDialog, bool>(nameof(CanClose), d => d.canClose);
    /// <summary>
    /// Define <see cref="HasNavigationBar"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> HasNavigationBarProperty = AvaloniaProperty.Register<InputDialog, bool>(nameof(HasNavigationBar), false);
    /// <summary>
    /// Define <see cref="HelpButtonText"/> property.
    /// </summary>
    public static readonly DirectProperty<InputDialog, string?> HelpButtonTextProperty = AvaloniaProperty.RegisterDirect<InputDialog, string?>(nameof(HelpButtonText), d => d.helpButtonText);
    /// <summary>
    /// Define <see cref="OKButtonText"/> property.
    /// </summary>
    public static readonly DirectProperty<InputDialog, string?> OKButtonTextProperty = AvaloniaProperty.RegisterDirect<InputDialog, string?>(nameof(OKButtonText), d => d.okButtonText);
    
    
    // Fields.
    string? cancelButtonText;
    bool canClose = true;
    string? helpButtonText;
    Control? inputControlToAcceptEnterKey;
    bool isEnterKeyClickedOnInputControl;
    bool isEnterKeyDownOnInputControl;
    int navigationBarUpdateDelay;
    string? okButtonText;
    INameScope? templateNameScope;
    TutorialPresenter? tutorialPresenter;
    readonly ScheduledAction updateButtonTextsAction;
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
        this.updateButtonTextsAction = new(this.OnUpdateButtonTexts);
    }


    /// <summary>
    /// Get default text for cancel button.
    /// </summary>
    public string? CancelButtonText => this.cancelButtonText;


    /// <summary>
    /// Get or set whether dialog can be closed or not.
    /// </summary>
    public bool CanClose
    {
        get => this.canClose;
        protected set => this.SetAndRaise(CanCloseProperty, ref this.canClose, value);
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
    /// Get default text for help button.
    /// </summary>
    public string? HelpButtonText => this.helpButtonText;


    /// <summary>
    /// Invalidate and update default texts of buttons.
    /// </summary>
    protected void InvalidateButtonTexts() =>
        this.updateButtonTextsAction.Schedule();
    
    
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
    
    
    /// <summary>
    /// Get default text for OK button.
    /// </summary>
    public string? OKButtonText => this.okButtonText;
    
    
    // Called when application strings has been updated.
    void OnApplicationStringsUpdated(object? sender, EventArgs? e) =>
        this.OnApplicationStringsUpdated();


    /// <summary>
    /// Called when application strings has been updated.
    /// </summary>
    protected virtual void OnApplicationStringsUpdated() =>
        this.updateButtonTextsAction.Schedule();
    
    
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
        
        // detach from application
        this.Application.StringsUpdated -= this.OnApplicationStringsUpdated;

        // [Workaround] Prevent Window leak by child controls
        this.SynchronizationContext.Post(_ => this.Content = null, null);
    }


    /// <inheritdoc/>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!this.canClose)
            e.Cancel = true;
        base.OnClosing(e);
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
            if (!e.Handled && ReferenceEquals(e.Source, control) && control is not null)
                this.OnEnterKeyClickedOnInputControl(control);
        }
    }
    
    
    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        this.Icon ??= this.Application.ApplicationIcon;
    }
    
    
    /// <inheritdoc/>
    protected override void OnOpening(EventArgs e)
    {
        // call base
        base.OnOpening(e);
        
        // attach to application
        this.Application.StringsUpdated += this.OnApplicationStringsUpdated;
        
        // setup default button texts
        this.updateButtonTextsAction.Execute();
        
        // setup navigation bar
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
    /// Called to update default texts of buttons.
    /// </summary>
    protected virtual void OnUpdateButtonTexts()
    {
        this.SetAndRaise(CancelButtonTextProperty, ref this.cancelButtonText, this.Application.GetString("Common.Cancel"));
        this.SetAndRaise(HelpButtonTextProperty, ref this.helpButtonText, this.Application.GetString("Common.Help"));
        this.SetAndRaise(OKButtonTextProperty, ref this.okButtonText, this.Application.GetString("Common.OK"));
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
public abstract class InputDialog<TApp> : InputDialog, IApplicationObject<TApp> where TApp : class, IAppSuiteApplication
{
    /// <summary>
    /// Get application instance.
    /// </summary>
    public new TApp Application => (TApp)base.Application;
}
