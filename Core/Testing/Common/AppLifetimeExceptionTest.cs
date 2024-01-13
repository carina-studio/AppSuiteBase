using CarinaStudio.Threading;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Testing.Common;

/// <summary>
/// Empty test case.
/// </summary>
class AppLifetimeExceptionTest : TestCase
{
    // Constants.
    const string TestExceptionMessage = "This is the test exception.";
    
    
    // Fields.
    Exception? exception;
    
    
    // Constructor.
    public AppLifetimeExceptionTest(IAppSuiteApplication app) : base(app, TestCaseCategoryNames.Common, "Application Lifetime Exception")
    { }
    
    
    // Called when exception occurred in application lifetime.
    void OnExceptionOccurredInApplicationLifetime(object? sender, IAppSuiteApplication.ExceptionEventArgs e)
    {
        if (e.Exception.Message == TestExceptionMessage)
        {
            exception = e.Exception;
            e.Handled = true;
        }
    }


    /// <inheritdoc/>
    protected override async Task OnRunAsync(CancellationToken cancellationToken)
    {
        this.Application.SynchronizationContext.PostDelayed(this.ThrowException, 200);
        await Task.Delay(3000, cancellationToken);
        Assert.That(this.exception is not null, "Event was not raised as expected.");
    }


    /// <inheritdoc/>
    protected override Task OnSetupAsync()
    {
        this.Application.ExceptionOccurredInApplicationLifetime += this.OnExceptionOccurredInApplicationLifetime;
        return Task.CompletedTask;
    }


    /// <inheritdoc/>
    protected override Task OnTearDownAsync()
    {
        this.Application.ExceptionOccurredInApplicationLifetime -= this.OnExceptionOccurredInApplicationLifetime;
        return Task.CompletedTask;
    }
    
    
    // Throw the exception.
    void ThrowException() => 
        throw new Exception(TestExceptionMessage);
}