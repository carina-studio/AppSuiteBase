using Avalonia;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Base class of wizard dialog in AppSuite.
/// </summary>
public abstract class WizardDialog : InputDialog
{
    /// <summary>
    /// Define <see cref="CanGoToNextPage"/> property.
    /// </summary>
    public static readonly DirectProperty<WizardDialog, bool> CanGoToNextPageProperty = AvaloniaProperty.RegisterDirect<WizardDialog, bool>(nameof(CanGoToNextPage), d => d.canGoToNextPage.Value);
    /// <summary>
    /// Define <see cref="CanGoToPreviousPage"/> property.
    /// </summary>
    public static readonly DirectProperty<WizardDialog, bool> CanGoToPreviousPageProperty = AvaloniaProperty.RegisterDirect<WizardDialog, bool>(nameof(CanGoToPreviousPage), d => d.canGoToPreviousPage.Value);
    /// <summary>
    /// Define <see cref="CurrentPageIndex"/> property.
    /// </summary>
    public static readonly DirectProperty<WizardDialog, int> CurrentPageIndexProperty = AvaloniaProperty.RegisterDirect<WizardDialog, int>(nameof(CurrentPageIndex), d => d.currentPageIndex);
    /// <summary>
    /// Define <see cref="GoToNextPageButtonText"/> property.
    /// </summary>
    public static readonly DirectProperty<WizardDialog, string?> GoToNextPageButtonTextProperty = AvaloniaProperty.RegisterDirect<WizardDialog, string?>(nameof(GoToNextPageButtonText), d => d.goToNextPageButtonText);
    /// <summary>
    /// Define <see cref="GoToPreviousPageButtonText"/> property.
    /// </summary>
    public static readonly DirectProperty<WizardDialog, string?> GoToPreviousPageButtonTextProperty = AvaloniaProperty.RegisterDirect<WizardDialog, string?>(nameof(GoToPreviousPageButtonText), d => d.goToPreviousPageButtonText);
    /// <summary>
    /// Define <see cref="IsLastPage"/> property.
    /// </summary>
    public static readonly DirectProperty<WizardDialog, bool> IsLastPageProperty = AvaloniaProperty.RegisterDirect<WizardDialog, bool>(nameof(IsLastPage), d => d.isLastPage);
    
    
    // Fields.
    readonly MutableObservableBoolean canGoToNextPage = new(false);
    readonly MutableObservableBoolean canGoToPreviousPage = new(false);
    readonly ScheduledAction checkPageChangeAvailabilityAction;
    int currentPageIndex = -1;
    string? goToNextPageButtonText;
    string? goToPreviousPageButtonText;
    bool isLastPage;
    CancellationTokenSource? pageChangeCancellationSource;


    /// <summary>
    /// Initialize new <see cref="WizardDialog"/> instance.
    /// </summary>
    protected WizardDialog()
    {
        this.checkPageChangeAvailabilityAction = new(() =>
        {
            if (this.IsClosed)
                return;
            if (this.pageChangeCancellationSource is not null)
            {
                this.canGoToPreviousPage.Update(false);
                this.canGoToNextPage.Update(false);
            }
            else
            {
                this.OnCheckPageChangeAvailability(this.currentPageIndex, out var canGoToPreviousPage, out var canGoToNextPage, out var isLastPage);
                this.canGoToPreviousPage.Update(canGoToPreviousPage);
                this.canGoToNextPage.Update(canGoToNextPage);
                this.SetAndRaise(IsLastPageProperty, ref this.isLastPage, isLastPage);
            }
        });
        this.GoToNextPageCommand = new Command(this.GoToNextPageAsync, this.canGoToNextPage);
        this.GoToPreviousPageCommand = new Command(this.GoToPreviousPageAsync, this.canGoToPreviousPage);
    }
    
    
    /// <summary>
    /// Check whether going to next page is available or not.
    /// </summary>
    public bool CanGoToNextPage => this.canGoToNextPage.Value;
    
    
    /// <summary>
    /// Check whether going to previous page is available or not.
    /// </summary>
    public bool CanGoToPreviousPage => this.canGoToPreviousPage.Value;


    /// <summary>
    /// Get index of current page.
    /// </summary>
    public int CurrentPageIndex => this.currentPageIndex;
    
    
    // Go to next page.
    async Task GoToNextPageAsync()
    {
        // check state
        this.VerifyAccess();
        if (!this.canGoToNextPage.Value)
            return;
        
        // go to next page
        if (this.isLastPage)
            this.GenerateResultCommand.Execute(null);
        else
            await this.GoToPageAsync(this.OnSelectNextPageIndex(this.currentPageIndex));
    }
    
    
    /// <summary>
    /// Get default text for button to go to next page.
    /// </summary>
    public string? GoToNextPageButtonText => this.goToNextPageButtonText;
    
    
    /// <summary>
    /// Command to go to next page.
    /// </summary>
    public ICommand GoToNextPageCommand { get; }
    
    
    /// <summary>
    /// Go to specific page asynchronously.
    /// </summary>
    /// <param name="pageIndex">Index of page to go to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of changing page.</returns>
    protected async Task<bool> GoToPageAsync(int pageIndex, CancellationToken cancellationToken = default)
    {
        // check state
        this.VerifyAccess();
        if (cancellationToken.IsCancellationRequested)
            throw new TaskCanceledException();
        var prevPageIndex = this.currentPageIndex;
        if (prevPageIndex == pageIndex)
            return true;
        if (pageIndex < 0)
            throw new ArgumentException("Invalid index of page: " + pageIndex);
        if (this.pageChangeCancellationSource is not null)
        {
            this.Logger.LogError("Cannot change page during another page change");
            return false;
        }
        
        // go to page
        this.pageChangeCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        this.checkPageChangeAvailabilityAction.Execute();
        try
        {
            this.Logger.LogTrace("Start changing page from {prevPage} to {page}", prevPageIndex, pageIndex);
            await this.OnGoToPageAsync(prevPageIndex, pageIndex, cancellationToken);
        }
        catch (Exception ex)
        {
            if ((ex is TaskCanceledException || ex is OperationCanceledException) && this.IsClosed)
            {
                this.Logger.LogWarning("Changing page from {prevPage} to {page} has been cancelled because dialog has been closed", prevPageIndex, pageIndex);
                this.checkPageChangeAvailabilityAction.Execute();
                return false;
            }
            throw;
        }
        finally
        {
            this.pageChangeCancellationSource = this.pageChangeCancellationSource.DisposeAndReturnNull();
        }
        this.Logger.LogTrace("Complete changing page from {prevPage} to {page}", prevPageIndex, pageIndex);
        this.SetAndRaise(CurrentPageIndexProperty, ref this.currentPageIndex, pageIndex);
        this.checkPageChangeAvailabilityAction.Execute();
        this.InvalidateButtonTexts();
        return true;
    }


    // Go to previous page.
    async Task GoToPreviousPageAsync()
    {
        // check state
        this.VerifyAccess();
        if (!this.canGoToPreviousPage.Value || this.currentPageIndex < 0)
            return;
        
        // go to previous page
        await this.GoToPageAsync(this.OnSelectPreviousPageIndex(this.currentPageIndex));
    }
    
    
    /// <summary>
    /// Get default text for button to go to previous page.
    /// </summary>
    public string? GoToPreviousPageButtonText => this.goToPreviousPageButtonText;
    
    
    /// <summary>
    /// Command to go to previous page.
    /// </summary>
    public ICommand GoToPreviousPageCommand { get; }


    /// <summary>
    /// Invalidate and check availability of page change.
    /// </summary>
    protected void InvalidatePageChangeAvailability() =>
        this.checkPageChangeAvailabilityAction.Schedule();


    /// <summary>
    /// Check whether the current page is the last one in the dialog or not.
    /// </summary>
    public bool IsLastPage => this.isLastPage;
    
    
    /// <summary>
    /// Called to check availability of page change.
    /// </summary>
    /// <param name="pageIndex">Index of current page.</param>
    /// <param name="canGoToPreviousPage">True if going to previous page is available.</param>
    /// <param name="canGoToNextPage">True if going to next page is available.</param>
    /// <param name="isLastPage">True if the current page is the last one in the dialog.</param>
    protected abstract void OnCheckPageChangeAvailability(int pageIndex, out bool canGoToPreviousPage, out bool canGoToNextPage, out bool isLastPage);


    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e)
    {
        if (this.pageChangeCancellationSource is not null)
        {
            this.Logger.LogWarning("Cancel changing page because dialog has been closed");
            this.pageChangeCancellationSource.Cancel();
            this.pageChangeCancellationSource = this.pageChangeCancellationSource.DisposeAndReturnNull();
        }
        base.OnClosed(e);
    }


    /// <summary>
    /// Called to go to specific page asynchronously.
    /// </summary>
    /// <param name="previousPageIndex">Index of previous page, or -1 if this is the initial page.</param>
    /// <param name="pageIndex">Index of current page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected abstract Task OnGoToPageAsync(int previousPageIndex, int pageIndex, CancellationToken cancellationToken);


    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (this.currentPageIndex < 0)
        {
            this.Logger.LogError("No initial page, close dialog");
            this.SynchronizationContext.Post(this.Close);
        }
    }


    /// <inheritdoc/>
    protected override void OnOpening(EventArgs e)
    {
        base.OnOpening(e);
        this.checkPageChangeAvailabilityAction.Execute();
        _ = this.GoToNextPageAsync();
    }


    /// <summary>
    /// Called to select previous page to go to.
    /// </summary>
    /// <param name="currentPageIndex">Index of current page.</param>
    /// <returns>Index of previous page.</returns>
    protected abstract int OnSelectPreviousPageIndex(int currentPageIndex);
    
    
    /// <summary>
    /// Called to select next page to go to.
    /// </summary>
    /// <param name="currentPageIndex">Index of current page, or -1 if there is no current page.</param>
    /// <returns>Index of next page.</returns>
    protected abstract int OnSelectNextPageIndex(int currentPageIndex);


    /// <inheritdoc/>
    protected override void OnUpdateButtonTexts()
    {
        base.OnUpdateButtonTexts();
        this.SetAndRaise(GoToNextPageButtonTextProperty, ref this.goToNextPageButtonText, this.Application.GetString(this.isLastPage
            ? "Common.Complete"
            : "WizardDialog.GoToNextPage"));
        this.SetAndRaise(GoToPreviousPageButtonTextProperty, ref this.goToPreviousPageButtonText, this.Application.GetString("WizardDialog.GoToPreviousPage"));
    }
}


/// <summary>
/// Base class of wizard dialog in AppSuite.
/// </summary>
/// <typeparam name="TApp">Type of application.</typeparam>
public abstract class WizardDialog<TApp> : WizardDialog, IApplicationObject<TApp> where TApp : class, IAppSuiteApplication
{
    /// <inheritdoc/>
    public new TApp Application => (TApp)base.Application;
}