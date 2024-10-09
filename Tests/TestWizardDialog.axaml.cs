using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.Controls;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Tests;

public class TestWizardDialog : WizardDialog
{
    readonly Panel[] pages;
    
    public TestWizardDialog()
    {
        AvaloniaXamlLoader.Load(this);
        this.pages =
        [
            this.Get<Panel>("page1"),
            this.Get<Panel>("page2"),
            this.Get<Panel>("page3"),
        ];
    }

    protected override Task<object?> GenerateResultAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<object?>(123);
    }

    protected override void OnCheckPageChangeAvailability(int pageIndex, out bool canGoToPreviousPage, out bool canGoToNextPage, out bool isLastPage)
    {
        canGoToPreviousPage = pageIndex > 0;
        canGoToNextPage = true;
        isLastPage = pageIndex >= 2;
    }

    protected override async Task OnGoToPageAsync(int previousPageIndex, int pageIndex, CancellationToken cancellationToken)
    {
        if (pageIndex == 1 && previousPageIndex < 1)
            await Task.Delay(2000, cancellationToken);
        this.pages[pageIndex].IsVisible = true;
        if (previousPageIndex >= 0)
            this.pages[previousPageIndex].IsVisible = false;
    }

    protected override int OnSelectPreviousPageIndex(int currentPageIndex) =>
        currentPageIndex - 1;

    protected override int OnSelectNextPageIndex(int currentPageIndex) =>
        currentPageIndex + 1;
}
