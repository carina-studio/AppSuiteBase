using Avalonia.Controls;
using System;
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
    protected BaseApplicationOptionsDialog()
    {
        this.DataContext = this.OnCreateViewModel();
        this.Classes.Add("Dialog");
        this.SizeToContent = SizeToContent.Height;
        this.Bind(TitleProperty, this.GetResourceObservable("String/ApplicationOptions"));
        this.Bind(WidthProperty, this.GetResourceObservable("Double/ApplicationOptionsDialog.Width"));
    }


    /// <summary>
    /// Check whether at least one <see cref="BaseApplicationOptionsDialog"/> instance is opened or not.
    /// </summary>
    public static bool HasOpenedDialogs => OpenedDialogCount > 0;


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
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ++OpenedDialogCount;
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
