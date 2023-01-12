using CarinaStudio.Collections;
using CarinaStudio.Threading;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Testing.MainWindows;

class WindowLeakageTest : TestCase
{
    // Fields.
    readonly List<CarinaStudio.Controls.Window> createdMainWindows = new();


    // Constructor.
    public WindowLeakageTest(IAppSuiteApplication app) : base(app, TestCaseCategoryNames.MainWindows, "Window Leakage")
    { }


    /// <inheritdoc/>
    protected override async Task OnRunAsync(CancellationToken cancellationToken)
    {
        // create and close main windows
        var random = new Random();
        var mainWindowRefs = new List<WeakReference<CarinaStudio.Controls.Window>>();
        for (var i = 0; i < 10; ++i)
        {
            await Task.Delay(500, cancellationToken);
            var result = await this.Application.ShowMainWindowAsync(window =>
            {
                this.createdMainWindows.Add(window);
                mainWindowRefs.Add(new(window));
                this.Application.SynchronizationContext.PostDelayed(() =>
                {
                    this.createdMainWindows.Remove(window);
                    window.Close();
                }, 2000 + random.Next(2000));
            });
            Assert.IsTrue(result, "Unable to create main window for test.");
        }
        await WaitForConditionAsync(() => this.createdMainWindows.IsEmpty(), $"{this.createdMainWindows.Count} main window(s) cannot be close properly.", 30000, cancellationToken);

        // check reference count
        var retryCount = 10;
        while (!cancellationToken.IsCancellationRequested)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            for (var i = mainWindowRefs.Count - 1; i >= 0; --i)
            {
                if (!mainWindowRefs[i].TryGetTarget(out var window))
                    mainWindowRefs.RemoveAt(i);
            }
            if (mainWindowRefs.Count <= 3)
                break;
            if (retryCount <= 0)
                throw new AssertionException($"{mainWindowRefs.Count} main window instances are still remained.");
            --retryCount;
            await Task.Delay(3000, cancellationToken);
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