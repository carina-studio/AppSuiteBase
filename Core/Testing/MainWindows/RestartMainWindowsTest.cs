using CarinaStudio.Collections;
using CarinaStudio.ViewModels;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Testing.MainWindows;

class RestartMainWindowsTest(IAppSuiteApplication app) : TestCase(app, TestCaseCategoryNames.MainWindows, "Restart Main Windows")
{
    // Fields.
    int mainWindowCount;
    readonly List<ViewModel> viewModels = new();
    

    /// <inheritdoc/>
    protected override async Task OnRunAsync(CancellationToken cancellationToken)
    {
        // restart main windows
        var result = await this.Application.RestartRootWindowsAsync();
        Assert.That(result, "Unable to restart main window(s)");

        // wait for restarting main windows
        await WaitForConditionAsync(() => this.Application.MainWindows.Count == this.mainWindowCount,
            $"{this.Application.MainWindows.Count} main window(s) restarted, {this.mainWindowCount} expected.",
            cancellationToken);
        Assert.That(this.mainWindowCount == this.Application.MainWindows.Count, $"{this.Application.MainWindows.Count} main window(s) restarted, {this.mainWindowCount} expected.");

        // check view-models
        await WaitForConditionAsync(() =>
            {
                foreach (var mainWindow in this.Application.MainWindows)
                {
                    if (mainWindow.DataContext is ViewModel viewModel)
                        this.viewModels.Remove(viewModel);
                }
                return this.viewModels.IsEmpty();
            }, 
            $"{this.viewModels.Count} view-model(s) not restored.",
            cancellationToken);
    }


    /// <inheritdoc/>
    protected override Task OnSetupAsync()
    {
        var mainWindows = this.Application.MainWindows;
        this.mainWindowCount = mainWindows.Count;
        Assert.That(mainWindowCount > 0, "No main window to test");
        foreach (var mainWindow in mainWindows)
        {
            if (mainWindow.DataContext is ViewModel viewModel)
                this.viewModels.Add(viewModel);
        }
        Assert.That(this.viewModels.IsNotEmpty(), "No view-model of main window to test");
        return Task.CompletedTask;
    }


    /// <inheritdoc/>
    protected override Task OnTearDownAsync()
    {
        this.viewModels.Clear();
        return Task.CompletedTask;
    }
}