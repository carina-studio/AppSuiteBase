using Avalonia;
using Avalonia.Controls;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Base implementation of dialog for application options.
/// </summary>
public abstract class BaseApplicationOptionsDialog : InputDialog<IAppSuiteApplication>
{
    /// <summary>
    /// Define <see cref="HasNavigationBar"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> HasNavigationBarProperty = AvaloniaProperty.Register<BaseApplicationOptionsDialog, bool>(nameof(HasNavigationBar), false);
      
        
    // Static fields.
    static int OpenedDialogCount;


    // Fields.
    readonly TaskCompletionSource<ApplicationOptionsDialogResult> closingTaskSource = new();
    readonly int navigationBarUpdateDelay;
    readonly ScheduledAction updateNavigationBarAction;


    /// <summary>
    /// Initialize new <see cref="BaseApplicationOptionsDialog"/> instance.
    /// </summary>
    protected BaseApplicationOptionsDialog()
    {
        this.Classes.Add("Dialog");
        this.BindToResource(HeightProperty, "Double/ApplicationOptionsDialog.Height");
        this.BindToResource(MinHeightProperty, "Double/ApplicationOptionsDialog.MinHeight");
        this.BindToResource(MinWidthProperty, "Double/ApplicationOptionsDialog.MinWidth");
        this.navigationBarUpdateDelay = this.Application.Configuration.GetValueOrDefault(ConfigurationKeys.DialogNavigationBarUpdateDelay);
        this.SizeToContent = SizeToContent.Manual;
        this.BindToResource(TitleProperty, "String/ApplicationOptions");
        this.updateNavigationBarAction = new(this.OnUpdateNavigationBar);
        this.BindToResource(WidthProperty, "Double/ApplicationOptionsDialog.Width");
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
    /// Check whether at least one <see cref="BaseApplicationOptionsDialog"/> instance is opened or not.
    /// </summary>
    public static bool HasOpenedDialogs => OpenedDialogCount > 0;


    /// <summary>
    /// Invalidate navigation bar and update later.
    /// </summary>
    protected void InvalidateNavigationBar()
    {
        this.VerifyAccess();
        if (this.GetValue(HasNavigationBarProperty) && this.IsOpened)
            this.updateNavigationBarAction.Schedule(this.navigationBarUpdateDelay);
    }


    /// <inheritdoc/>
    protected override Task<object?> GenerateResultAsync(CancellationToken cancellationToken)
    {
        if (this.DataContext is not ViewModels.ApplicationOptions options)
        {
            this.closingTaskSource.SetResult(ApplicationOptionsDialogResult.None);
            return Task.FromResult((object?)ApplicationOptionsDialogResult.None);
        }
        if (options.IsCustomScreenScaleFactorAdjusted
            || (options.IsUseEmbeddedFontsForChineseSupported && options.IsUseEmbeddedFontsForChineseChanged))
        {
            this.closingTaskSource.SetResult(ApplicationOptionsDialogResult.RestartApplicationNeeded);
            return Task.FromResult((object?)ApplicationOptionsDialogResult.RestartApplicationNeeded);
        }
        if (options.IsRestartingRootWindowsNeeded)
        {
            this.closingTaskSource.SetResult(ApplicationOptionsDialogResult.RestartMainWindowsNeeded);
            return Task.FromResult((object?)ApplicationOptionsDialogResult.RestartMainWindowsNeeded);
        }
        this.closingTaskSource.SetResult(ApplicationOptionsDialogResult.None);
        return Task.FromResult((object?)ApplicationOptionsDialogResult.None);
    }


    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e)
    {
        (this.DataContext as ViewModels.ApplicationOptions)?.Dispose();
        --OpenedDialogCount;
        this.closingTaskSource.TrySetResult(ApplicationOptionsDialogResult.None);
        base.OnClosed(e);
    }


    /// <summary>
    /// Called to create view-model of dialog.
    /// </summary>
    /// <returns>View-model of dialog.</returns>
    protected abstract ViewModels.ApplicationOptions OnCreateViewModel();


    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        this.DataContext = this.OnCreateViewModel();
    }


    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        this.CanResize = true;
        this.SizeToContent = SizeToContent.Manual;
        ++OpenedDialogCount;
    }


    /// <inheritdoc/>
    protected override void OnOpening(EventArgs e)
    {
        base.OnOpening(e);
        if (this.GetValue(HasNavigationBarProperty))
            this.updateNavigationBarAction.Schedule();
    }


    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HasNavigationBarProperty)
        {
            if ((bool)change.NewValue!)
            {
                if (!this.IsOpened)
                    this.BindToResource(WidthProperty, "Double/ApplicationOptionsDialog.Width.WithNavigationBar");
                else
                    this.updateNavigationBarAction.Reschedule();
            }
            else if (!this.IsOpened)
                this.BindToResource(WidthProperty, "Double/ApplicationOptionsDialog.Width");
        }
    }
    
    
    /// <summary>
    /// Called to update navigation bar.
    /// </summary>
    protected virtual void OnUpdateNavigationBar()
    { }


    /// <summary>
    /// Wait for closing dialog.
    /// </summary>
    /// <returns>Task of waiting.</returns>
    public Task<ApplicationOptionsDialogResult> WaitForClosingDialogAsync() =>
        this.closingTaskSource.Task;
}


/// <summary>
/// Result of <see cref="BaseApplicationOptionsDialog"/>.
/// </summary>
public enum ApplicationOptionsDialogResult
{
    /// <summary>
    /// None.
    /// </summary>
    None,
    /// <summary>
    /// Need to restart main windows to take effect.
    /// </summary>
    RestartMainWindowsNeeded,
    /// <summary>
    /// Need to restart application to take effect.
    /// </summary>
    RestartApplicationNeeded,
}
