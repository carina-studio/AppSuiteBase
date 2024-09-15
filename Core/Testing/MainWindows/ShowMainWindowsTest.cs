using CarinaStudio.Collections;
using CarinaStudio.ViewModels;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Testing.MainWindows;

class ShowMainWindowsTest(IAppSuiteApplication app) : TestCase(app, TestCaseCategoryNames.MainWindows, "Show Main Windows")
{
    // Constants.
    const int TestMainWindowCount = 4;


    // Fields.
    readonly List<Avalonia.Controls.Window> createdMainWindows = new();
    

    /// <inheritdoc/>
    protected override async Task OnRunAsync(CancellationToken cancellationToken)
    {
        // keep initial state
        var initMainWindowCount = this.Application.MainWindows.Count;

        // show main windows
        var viewModels = new List<ViewModel>();
        for (var i = 0; i < TestMainWindowCount; ++i)
        {
            var result = await this.Application.ShowMainWindowAsync(window =>
            {
                this.createdMainWindows.Add(window);
                window.Closed += (_, _) =>
                {
                    this.createdMainWindows.Remove(window);
                };
                if (window.DataContext is ViewModel viewModel)
                    viewModels.Add(viewModel);
                else
                    throw new AssertionException("View-model of main window was not created as expected.");
            });
            Assert.That(result, "Failed to create main window.");
        }
        await WaitForConditionAsync(() => this.createdMainWindows.Count == TestMainWindowCount,
            $"{this.createdMainWindows.Count} main window(s) created, {TestMainWindowCount} expected.",
            cancellationToken);
        Assert.That(TestMainWindowCount == viewModels.Count, $"{viewModels.Count} view-model(s) created, {TestMainWindowCount} expected.");
        Assert.That(initMainWindowCount + TestMainWindowCount == this.Application.MainWindows.Count, $"Total {this.Application.MainWindows.Count} main window(s), {initMainWindowCount + TestMainWindowCount} expected.");

        // close main windows
        await Task.Delay(1000, cancellationToken);
        foreach (var window in this.createdMainWindows.ToArray())
            window.Close();
        await WaitForConditionAsync(() => this.createdMainWindows.IsEmpty(),
            $"{this.createdMainWindows.Count} main window(s) could not be closed.",
            cancellationToken);
        Assert.That(initMainWindowCount == this.Application.MainWindows.Count, $"Total {this.Application.MainWindows.Count} main window(s), {initMainWindowCount} expected.");

        // check view-models
        var isDisposedProperty = typeof(ViewModel).GetProperty("IsDisposed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).AsNonNull();
        await WaitForConditionAsync(() =>
            {
                for (var i = viewModels.Count - 1; i >= 0; --i)
                {
                    if ((bool)isDisposedProperty.GetValue(viewModels[i]).AsNonNull())
                        viewModels.RemoveAt(i);
                }
                return viewModels.IsEmpty();
            },
            $"{viewModels.Count} view-model(s) not disposed as expected.",
            cancellationToken);
    }


    /// <inheritdoc/>
    protected override Task OnTearDownAsync()
    {
        foreach (var window in this.createdMainWindows)
            window.Close();
        this.createdMainWindows.Clear();
        return Task.CompletedTask;
    }
}