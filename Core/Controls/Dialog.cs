using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Base class of dialog in AppSuite.
/// </summary>
public abstract class Dialog : CarinaStudio.Controls.Dialog<IAppSuiteApplication>
{
    /// <summary>
    /// Define <see cref="HasNavigationBar"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> HasNavigationBarProperty = AvaloniaProperty.Register<Dialog, bool>(nameof(HasNavigationBar), false);
    /// <summary>
    /// Define <see cref="OKButtonText"/> property.
    /// </summary>
    public static readonly DirectProperty<Dialog, string?> OKButtonTextProperty = AvaloniaProperty.RegisterDirect<Dialog, string?>(nameof(OKButtonText), d => d.okButtonText);
    
    
    // Fields.
    int navigationBarUpdateDelay;
    string? okButtonText;
    INameScope? templateNameScope;
    TutorialPresenter? tutorialPresenter;
    readonly ScheduledAction updateButtonTextsAction;
    ScheduledAction? updateNavigationBarAction;
    
    
    /// <summary>
    /// Initialize new <see cref="Dialog"/> instance.
    /// </summary>
    protected Dialog()
    {
        _ = new WindowContentFadingHelper(this);
        this.Title = this.Application.Name;
        this.updateButtonTextsAction = new(this.OnUpdateButtonTexts);
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
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key == Key.Escape && !e.Handled)
            this.Close();
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
/// Base class of dialog in AppSuite.
/// </summary>
/// <typeparam name="TApp">Type of application.</typeparam>
public abstract class Dialog<TApp> : Dialog, IApplicationObject<TApp> where TApp : class, IAppSuiteApplication
{
    /// <summary>
    /// Get application instance.
    /// </summary>
    public new TApp Application => (TApp)base.Application;
}
