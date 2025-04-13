using Avalonia;
using Avalonia.Controls;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Controls;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Base implementation of dialog for application options.
/// </summary>
public abstract class BaseApplicationOptionsDialog : InputDialog<IAppSuiteApplication>
{
    // Static fields.
    static int OpenedDialogCount;


    // Fields.
    readonly TaskCompletionSource<ApplicationOptionsDialogResult> closingTaskSource = new();


    /// <summary>
    /// Initialize new <see cref="BaseApplicationOptionsDialog"/> instance.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ApplicationOptions))]
    protected BaseApplicationOptionsDialog()
    {
        this.Classes.Add("Dialog");
        this.BindToResource(HeightProperty, "Double/ApplicationOptionsDialog.Height");
        this.BindToResource(MinHeightProperty, "Double/ApplicationOptionsDialog.MinHeight");
        this.BindToResource(MinWidthProperty, "Double/ApplicationOptionsDialog.MinWidth");
        this.SizeToContent = SizeToContent.Manual;
        this.BindToResource(TitleProperty, "String/ApplicationOptions");
        this.BindToResource(WidthProperty, "Double/ApplicationOptionsDialog.Width");
    }


    /// <summary>
    /// Check whether at least one <see cref="BaseApplicationOptionsDialog"/> instance is opened or not.
    /// </summary>
    public static bool HasOpenedDialogs => OpenedDialogCount > 0;


    /// <inheritdoc/>
    protected override Task<object?> GenerateResultAsync(CancellationToken cancellationToken)
    {
        if (this.DataContext is not ApplicationOptions options)
        {
            this.closingTaskSource.SetResult(ApplicationOptionsDialogResult.None);
            return Task.FromResult((object?)ApplicationOptionsDialogResult.None);
        }
        if (options.IsCustomScreenScaleFactorAdjusted
            || (options.IsDisableAngleSupported && options.IsDisableAngleChanged)
            || options.IsChineseVariantChanged)
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
        (this.DataContext as ApplicationOptions)?.Dispose();
        --OpenedDialogCount;
        this.closingTaskSource.TrySetResult(ApplicationOptionsDialogResult.None);
        base.OnClosed(e);
    }


    /// <summary>
    /// Called to create view-model of dialog.
    /// </summary>
    /// <returns>View-model of dialog.</returns>
    protected abstract ApplicationOptions OnCreateViewModel();


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
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HasNavigationBarProperty)
        {
            if (this.IsOpened)
                return;
            if ((bool)change.NewValue!)
                this.BindToResource(WidthProperty, "Double/ApplicationOptionsDialog.Width.WithNavigationBar");
            else
                this.BindToResource(WidthProperty, "Double/ApplicationOptionsDialog.Width");
        }
    }


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
