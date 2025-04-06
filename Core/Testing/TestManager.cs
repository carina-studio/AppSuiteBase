using System.Threading;
using CarinaStudio.Collections;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Testing;

/// <summary>
/// Manager for testing.
/// </summary>
public class TestManager : BaseApplicationObject<IAppSuiteApplication>, INotifyPropertyChanged
{
    // Static fields.
    static TestManager? DefaultInstance;


    // Fields.
    readonly ILogger logger;
    IList<TestCaseCategory>? roTestCaseCategories;
    TestCase? runningTestCase;
    SortedObservableList<TestCaseCategory>? testCaseCategories;
    readonly LinkedList<TestCase> testCasesToRun = new();
    readonly HashSet<Type> testCaseTypes = new();


    // Constructor.
    TestManager(IAppSuiteApplication app) : base(app)
    { 
        this.logger = app.LoggerFactory.CreateLogger(nameof(TestManager));
        this.AddTestCase(typeof(Common.AppLifetimeExceptionTest));
        this.AddTestCase(typeof(Common.LocalizationTest));
        this.AddTestCase(typeof(Common.ThemeModeTest));
        this.AddTestCase(typeof(MainWindows.RestartMainWindowsTest));
        this.AddTestCase(typeof(MainWindows.ShowMainWindowsTest));
        this.AddTestCase(typeof(MainWindows.WindowLeakageTest));
    }


    /// <summary>
    /// Add a test case.
    /// </summary>
    /// <param name="type">Type of test case.</param>
    public void AddTestCase([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        this.VerifyAccess();
        if (!typeof(TestCase).IsAssignableFrom(type))
            throw new ArgumentException($"Type {type.Name} is not a test case which should inherit from TestCase.");
        if (!this.testCaseTypes.Add(type))
            return;
        if (this.testCaseCategories != null && this.TryCreateTestCase(type, out var testCase))
            this.GetOrCreateTestCaseCategory(testCase.CategoryName).AddTestCase(testCase);
    }


    /// <summary>
    /// Cancel all running and waiting test cases.
    /// </summary>
    public void CancelAllTestCases()
    {
        this.VerifyAccess();
        if (this.testCasesToRun.IsNotEmpty())
        {
            this.logger.LogWarning("Cancel {count} waiting test case(s)", this.testCasesToRun.Count);
            var node = this.testCasesToRun.First;
            while (node != null)
            {
                var nextNode = node.Next;
                var testCase = node.Value;
                this.logger.LogDebug("Cancel waiting test case '{name}'", testCase.Name);
                this.testCasesToRun.Remove(node);
                testCase.Cancel();
                node = nextNode;
            }
            this.PropertyChanged?.Invoke(this, new(nameof(TestCaseWaitingCount)));
        }
        if (this.runningTestCase != null)
        {
            this.logger.LogWarning("Cancel running test case '{name}'", this.runningTestCase.Name);
            this.runningTestCase.Cancel();
        }
    }


    /// <summary>
    /// Cancel running the test case.
    /// </summary>
    /// <param name="testCase">Test case.</param>
    /// <returns>True if cancellation has been accepted.</returns>
    public bool CancelTestCase(TestCase testCase)
    {
        this.VerifyAccess();
        if (this.testCasesToRun.Remove(testCase))
        {
            this.logger.LogDebug("Cancel waiting test case '{name}'", testCase.Name);
            testCase.Cancel();
            this.PropertyChanged?.Invoke(this, new(nameof(TestCaseWaitingCount)));
            return true;
        }
        if (this.runningTestCase == testCase)
        {
            this.logger.LogWarning("Cancel running test case '{name}'", testCase.Name);
            return testCase.Cancel();
        }
        this.logger.LogWarning("Cannot cancel unknown test case '{name}'", testCase.Name);
        return false;
    }


    /// <summary>
    /// Get default instance.
    /// </summary>
    public static TestManager Default => DefaultInstance ?? throw new InvalidOperationException();


    // Get or create category of test case.
    TestCaseCategory GetOrCreateTestCaseCategory(string name)
    {
        if (this.testCaseCategories == null)
            throw new InvalidOperationException();
        var index = ((IList<TestCaseCategory>)this.testCaseCategories).BinarySearch<TestCaseCategory, string>(name, it => it.Name, string.CompareOrdinal);
        if (index >= 0)
            return this.testCaseCategories[index];
        return new TestCaseCategory(name).Also(it => this.testCaseCategories.Add(it));
    }


    // Initialize.
    internal static void Initialize(IAppSuiteApplication app)
    {
        // check state
        app.VerifyAccess();
        if (DefaultInstance != null)
            throw new InvalidOperationException();
        
        // create instance
        DefaultInstance = new(app);
    }


    /// <summary>
    /// Check whether one or more test cases are running or not.
    /// </summary>
    public bool IsRunningTestCases { get; private set; }


    /// <summary>
    /// Raised when property changed.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;


    /// <summary>
    /// Run all test cases.
    /// </summary>
    public void RunAllTestCases()
    {
        foreach (var category in this.TestCaseCategories)
            this.RunTestCases(category);
    }


    /// <summary>
    /// Get the test case which is run currently.
    /// </summary>
    public TestCase? RunningTestCase
    {
        get => this.runningTestCase;
        private set
        {
            if (this.runningTestCase != value)
            {
                this.runningTestCase = value;
                this.PropertyChanged?.Invoke(this, new(nameof(RunningTestCase)));
            }
        }
    }


    /// <summary>
    /// Schedule to run the test case.
    /// </summary>
    /// <param name="testCase">Test case.</param>
    /// <returns>True if test case has been scheduled to be run successfully.</returns>
    public bool RunTestCase(TestCase testCase)
    {
        this.VerifyAccess();
        if (testCase.State == TestCaseState.WaitingForRunning)
            return true;
        if (!testCase.WaitForRunning())
            return false;
        if (this.runningTestCase != null)
        {
            this.logger.LogDebug("Add test case '{name}' to waiting queue, waiting count: {count}", testCase.Name, this.testCasesToRun.Count + 1);
            this.testCasesToRun.AddLast(testCase);
            this.PropertyChanged?.Invoke(this, new(nameof(TestCaseWaitingCount)));
        }
        else
        {
            this.logger.LogDebug("Run test case '{name}' immediately", testCase.Name);
            _ = this.RunTestCaseAsync(testCase);
        }
        return true;
    }


    // Run the test case.
    async Task RunTestCaseAsync(TestCase testCase)
    {
        // check state
        if (this.runningTestCase != null)
            throw new InvalidOperationException();
        
        // update state
        if (!this.IsRunningTestCases)
        {
            this.IsRunningTestCases = true;
            this.PropertyChanged?.Invoke(this, new(nameof(IsRunningTestCases)));
        }
        
        // run
        this.logger.LogDebug("Start running test case '{name}', waiting count: {count}", testCase.Name, this.testCasesToRun.Count);
        this.RunningTestCase = testCase;
        await Task.Delay(100, CancellationToken.None);
        await testCase.RunAsync();
        this.RunningTestCase = null;
        await Task.Delay(100, CancellationToken.None);
        this.logger.LogDebug("Complete running test case '{name}'", testCase.Name);

        // run next test case
        if (this.testCasesToRun.IsNotEmpty())
            await Task.Delay(500, CancellationToken.None);
        if (this.testCasesToRun.IsNotEmpty())
        {
            var nextTestCase = this.testCasesToRun.First!.Value;
            this.testCasesToRun.RemoveFirst();
            this.PropertyChanged?.Invoke(this, new(nameof(TestCaseWaitingCount)));
            _ = RunTestCaseAsync(nextTestCase);
        }
        else
        {
            this.logger.LogDebug("No more test case to run");
            this.IsRunningTestCases = false;
            this.PropertyChanged?.Invoke(this, new(nameof(IsRunningTestCases)));
        }
    }


    /// <summary>
    /// Run all test cases in given category.
    /// </summary>
    /// <param name="category">Category.</param>
    public void RunTestCases(TestCaseCategory category)
    {
        foreach (var testCase in category.TestCases)
            this.RunTestCase(testCase);
    }


    /// <summary>
    /// Get list of categories of test cases.
    /// </summary>
    public IList<TestCaseCategory> TestCaseCategories
    {
        get
        {
            if (this.roTestCaseCategories != null)
                return this.roTestCaseCategories;
            this.VerifyAccess();
            this.testCaseCategories = new((lhs, rhs) => string.CompareOrdinal(lhs.Name, rhs.Name));
            foreach (var type in this.testCaseTypes)
            {
#pragma warning disable IL2072
                if (this.TryCreateTestCase(type, out var testCase))
                    this.GetOrCreateTestCaseCategory(testCase.CategoryName).AddTestCase(testCase);
#pragma warning restore IL2072
            }
            this.roTestCaseCategories = ListExtensions.AsReadOnly(this.testCaseCategories);
            return this.roTestCaseCategories;
        }
    }


    /// <summary>
    /// Get number of test cases which are waiting to be run.
    /// </summary>
    public int TestCaseWaitingCount => this.testCasesToRun.Count;


    // Try creating test case.
    bool TryCreateTestCase([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, [NotNullWhen(true)] out TestCase? testCase)
    {
        try
        {
            testCase = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [ this.Application ], this.Application.CultureInfo) as TestCase;
            if (testCase != null)
            {
                this.logger.LogDebug("Create test case '{name}' with type {type}", testCase.Name, type.Name);
                return true;
            }
            this.logger.LogError("Unable to create test case with type {type}", type.Name);
            return false;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unable to create test case with type {type}", type.Name);
            testCase = null;
            return false;
        }
    }
}