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
    }


    /// <summary>
    /// Add a test case.
    /// </summary>
    /// <param name="type">Type of test case.</param>
    public void AddTestCase(Type type)
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
    /// Cancel running the test case.
    /// </summary>
    /// <param name="testCase">Test case.</param>
    /// <returns>True if cancellation has been accepted.</returns>
    public bool CancelTestCase(TestCase testCase)
    {
        this.VerifyAccess();
        if (this.testCasesToRun.Remove(testCase))
        {
            testCase.Cancel();
            return true;
        }
        return testCase.Cancel();
    }


    /// <summary>
    /// Get default instance.
    /// </summary>
    public static TestManager Default { get => DefaultInstance ?? throw new InvalidOperationException(); }


    // Get or create category of test case.
    TestCaseCategory GetOrCreateTestCaseCategory(string name)
    {
        if (this.testCaseCategories == null)
            throw new InvalidOperationException();
        var index = this.testCaseCategories.BinarySearch<TestCaseCategory, string>(name, it => it.Name, (lhs, rhs) => string.Compare(lhs, rhs));
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
            this.testCasesToRun.AddLast(testCase);
        else
            _ = this.RunTestCaseAsync(testCase);
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
        this.logger.LogDebug("Start running test case '{name}'", testCase.Name);
        this.runningTestCase = testCase;
        await testCase.RunAsync();
        this.runningTestCase = null;
        this.logger.LogDebug("Complete running test case '{name}'", testCase.Name);

        // run next test case
        if (this.testCasesToRun.IsNotEmpty())
        {
            var nextTestCase = this.testCasesToRun.First!.Value;
            this.testCasesToRun.RemoveFirst();
            _ = RunTestCaseAsync(nextTestCase);
        }
        else
        {
            this.IsRunningTestCases = false;
            this.PropertyChanged?.Invoke(this, new(nameof(IsRunningTestCases)));
        }
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
            this.testCaseCategories = new((lhs, rhs) => string.Compare(lhs.Name, rhs.Name));
            foreach (var type in this.testCaseTypes)
            {
                if (this.TryCreateTestCase(type, out var testCase))
                    this.GetOrCreateTestCaseCategory(testCase.CategoryName).AddTestCase(testCase);
            }
            this.roTestCaseCategories = ListExtensions.AsReadOnly(this.testCaseCategories);
            return this.roTestCaseCategories;
        }
    }


    // Try creating test case.
    bool TryCreateTestCase(Type type, [NotNullWhen(true)] out TestCase? testCase)
    {
        try
        {
            testCase = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new object?[] { this.Application }, this.Application.CultureInfo) as TestCase;
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