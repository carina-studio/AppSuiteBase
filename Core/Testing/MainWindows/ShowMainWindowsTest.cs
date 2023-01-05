using CarinaStudio.Collections;
using CarinaStudio.ViewModels;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Testing.MainWindows;

class ShowMainWindowsTest : TestCase
{
    // Constants.
    const int TestMainWindowCount = 4;


    // Fields.
    readonly List<Avalonia.Controls.Window> createdMainWindows = new();


    // Constructor.
    public ShowMainWindowsTest(IAppSuiteApplication app) : base(app, TestCaseCategoryNames.MainWindows, "Show Main Windows")
    { }


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
                window.Closed += (_, e) =>
                {
                    this.createdMainWindows.Remove(window);
                };
                if (window.DataContext is ViewModel viewModel)
                    viewModels.Add(viewModel);
                else
                    throw new AssertionException("View-model of main window was not created as expected.");
            });
            Assert.IsTrue(result, "Failed to create main window.");
        }
        var retryCount = 10;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (this.createdMainWindows.Count == TestMainWindowCount)
                break;
            if (retryCount <= 0)
                throw new AssertionException($"{this.createdMainWindows.Count} main window(s) created, {TestMainWindowCount} expected.");
            --retryCount;
            await Task.Delay(500, cancellationToken);
        }
        if (cancellationToken.IsCancellationRequested)
            return;
        Assert.AreEqual(TestMainWindowCount, viewModels.Count, "{0} view-model(s) created, {1} expected.", viewModels.Count, TestMainWindowCount);
        Assert.AreEqual(initMainWindowCount + TestMainWindowCount, this.Application.MainWindows.Count, "Total {0} main window(s), {1} expected.", this.Application.MainWindows.Count, initMainWindowCount + TestMainWindowCount);

        // close main windows
        foreach (var window in this.createdMainWindows.ToArray())
            window.Close();
        retryCount = 10;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (this.createdMainWindows.IsEmpty())
                break;
            if (retryCount <= 0)
                throw new AssertionException($"{this.createdMainWindows.Count} main window(s) could not be closed.");
            --retryCount;
            await Task.Delay(500, cancellationToken);
        }
        if (cancellationToken.IsCancellationRequested)
            return;
        Assert.AreEqual(initMainWindowCount, this.Application.MainWindows.Count, "Total {0} main window(s), {1} expected.", this.Application.MainWindows.Count, initMainWindowCount);

        // check view-models
        var isDisposedProperty = typeof(ViewModel).GetProperty("IsDisposed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).AsNonNull();
        retryCount = 10;
        while (!cancellationToken.IsCancellationRequested)
        {
            for (var i = viewModels.Count - 1; i >= 0; --i)
            {
                if ((bool)isDisposedProperty.GetValue(viewModels[i]).AsNonNull())
                    viewModels.RemoveAt(i);
            }
            if (viewModels.IsEmpty())
                break;
            if (retryCount <= 0)
                throw new AssertionException($"{viewModels.Count} view-model(s) not disposed as expected.");
            --retryCount;
            await Task.Delay(500, cancellationToken);
        }
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