using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Testing;

/// <summary>
/// Base class for single test case.
/// </summary>
public abstract class TestCase : BaseApplicationObject<IAppSuiteApplication>, INotifyPropertyChanged
{
    // Fields.
    CancellationTokenSource? cancellationTokenSource;
    Exception? error;
    ILogger? logger;
    TestCaseState state = TestCaseState.None;


    /// <summary>
    /// Initialize new <see cref="TestCase"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <param name="categoryName">Name of category.</param>
    /// <param name="name">Name.</param>
    protected TestCase(IAppSuiteApplication app, string? categoryName, string name) : base(app)
    {
        this.CategoryName = string.IsNullOrWhiteSpace(categoryName) ? TestCaseCategoryNames.Unclassified : categoryName;
        this.Name = name;
    }


    // Cancel the running test.
    internal bool Cancel()
    {
        // check state
        if (this.State == TestCaseState.WaitingForRunning)
        {
            this.State = TestCaseState.Cancelled;
            return true;
        }
        if (this.State != TestCaseState.Running)
            return false;
        if (this.State == TestCaseState.Cancelling)
            return true;
        
        // cancel
        this.State = TestCaseState.Cancelling;
        this.cancellationTokenSource?.Cancel();
        return true;
    }


    /// <summary>
    /// Get name of category of the test.
    /// </summary>
    public string CategoryName { get; }


    /// <summary>
    /// Get the exception thrown during the test.
    /// </summary>
    public Exception? Error
    {
        get => this.error;
        private set
        {
            if (this.error != value)
            {
                this.error = value;
                this.OnPropertyChanged(nameof(Error));
            }
        }
    }


    /// <summary>
    /// Get logger.
    /// </summary>
    protected ILogger Logger
    {
        get
        {
            this.logger ??= this.Application.LoggerFactory.CreateLogger($"TestCase({this.Name})");
            return this.logger;
        }
    }


    /// <summary>
    /// Get name of test case.
    /// </summary>
    public string Name { get; }


    /// <summary>
    /// Raise <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">Name of property.</param>
    protected void OnPropertyChanged(string propertyName) =>
        this.PropertyChanged?.Invoke(this, new(propertyName));
    

    /// <summary>
    /// Called to run test asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of running test.</returns>
    protected abstract Task OnRunAsync(CancellationToken cancellationToken);
    

    /// <summary>
    /// Called to setup the test asynchronously.
    /// </summary>
    /// <returns>Task of setting up.</returns>
    protected virtual Task OnSetupAsync() =>
        Task.CompletedTask;
    

    /// <summary>
    /// Called to tear down the test asynchronously.
    /// </summary>
    /// <returns>Task of tearing down.</returns>
    protected virtual Task OnTearDownAsync() =>
        Task.CompletedTask;


    /// <summary>
    /// Raised when property changed.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;


    // Run the test.
    internal async Task<bool> RunAsync()
    {
        // check state
        switch (this.state)
        {
            case TestCaseState.None:
            case TestCaseState.Succeeded:
            case TestCaseState.Failed:
            case TestCaseState.Cancelled:
            case TestCaseState.WaitingForRunning:
                break;
            default:
                return false;
        }

        // run
        try
        {
            // setup
            this.Error = null;
            this.cancellationTokenSource = new();
            try
            {
                this.State = TestCaseState.SettingUp;
                await this.OnSetupAsync();
            }
            catch (Exception ex)
            {
                this.Error = ex;
                return true;
            }

            // run
            try
            {
                this.State = TestCaseState.Running;
                await this.OnRunAsync(this.cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                this.Error = ex;
            }
        }
        finally
        {
            // tear down
            var isCancelling = this.State == TestCaseState.Cancelling;
            try
            {
                this.State = TestCaseState.TearingDown;
                await this.OnTearDownAsync();
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Error occurred while tearing down the test");
            }

            // complete
            if (isCancelling)
                this.State = TestCaseState.Cancelled;
            else if (this.Error == null)
                this.State = TestCaseState.Succeeded;
            else
                this.State = TestCaseState.Failed;
        }
        return true;
    }


    /// <summary>
    /// Get current state.
    /// </summary>
    public TestCaseState State
    {
        get => this.state;
        private set
        {
            if (this.state != value)
            {
                this.Logger.LogDebug("Change state from {previous} to {new}", this.state, value);
                this.state = value;
                this.OnPropertyChanged(nameof(State));
            }
        }
    }


    /// <inheritdoc/>
    public override string ToString() =>
        this.Name;
    

    // Notify that test is waiting for running.
    internal bool WaitForRunning()
    {
        // check state
        switch (this.state)
        {
            case TestCaseState.None:
            case TestCaseState.Succeeded:
            case TestCaseState.Failed:
            case TestCaseState.Cancelled:
                break;
            case TestCaseState.WaitingForRunning:
                return true;
            default:
                return false;
        }

        // update state
        this.State = TestCaseState.WaitingForRunning;
        return true;
    }
}


/// <summary>
/// State of <see cref="TestCase"/>.
/// </summary>
public enum TestCaseState
{
    /// <summary>
    /// The test case is just created.
    /// </summary>
    None,
    /// <summary>
    /// Waiting to be run.
    /// </summary>
    WaitingForRunning,
    /// <summary>
    /// Setting up the test.
    /// </summary>
    SettingUp,
    /// <summary>
    /// Running the test.
    /// </summary>
    Running,
    /// <summary>
    /// Cancelling the test.
    /// </summary>
    Cancelling,
    /// <summary>
    /// Tearing down the test.
    /// </summary>
    TearingDown,
    /// <summary>
    /// Succeeded.
    /// </summary>
    Succeeded,
    /// <summary>
    /// Failed.
    /// </summary>
    Failed,
    /// <summary>
    /// Cancelled.
    /// </summary>
    Cancelled,
}