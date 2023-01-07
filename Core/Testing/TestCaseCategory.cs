using CarinaStudio.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace CarinaStudio.AppSuite.Testing;

/// <summary>
/// Category of test case.
/// </summary>
public class TestCaseCategory : INotifyPropertyChanged
{
    // Fields.
    readonly HashSet<TestCase> failedTestCases = new();
    readonly HashSet<TestCase> succeededTestCases = new();
    readonly SortedObservableList<TestCase> testCases = new((lhs, rhs) =>
    {
        var result = string.Compare(lhs.Name, rhs.Name, true);
        if (result != 0)
            return result;
        return lhs.GetHashCode() - rhs.GetHashCode();
    });


    // Constructor.
    internal TestCaseCategory(string name)
    {
        this.Name = name;
        this.TestCases = ListExtensions.AsReadOnly(this.testCases);
    }


    // Add test case to the category.
    internal void AddTestCase(TestCase testCase)
    {
        this.testCases.Add(testCase);
        testCase.PropertyChanged += this.OnTestCasePropertyChanged;
    }

    
    /// <summary>
    /// Get number of failed test cases.
    /// </summary>
    public int FailedTestCaseCount { get => this.failedTestCases.Count; }


    /// <summary>
    /// Get name of category.
    /// </summary>
    public string Name { get; }


    // Called when property of test case changed.
    void OnTestCasePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TestCase.State) && sender is TestCase testCase)
        {
            switch (testCase.State)
            {
                case TestCaseState.Failed:
                    if (this.failedTestCases.Add(testCase))
                        this.PropertyChanged?.Invoke(this, new(nameof(FailedTestCaseCount)));
                    break;
                case TestCaseState.Succeeded:
                    if (this.succeededTestCases.Add(testCase))
                        this.PropertyChanged?.Invoke(this, new(nameof(SucceededTestCaseCount)));
                    break;
                default:
                    if (this.failedTestCases.Remove(testCase))
                        this.PropertyChanged?.Invoke(this, new(nameof(FailedTestCaseCount)));
                    else if (this.succeededTestCases.Remove(testCase))
                        this.PropertyChanged?.Invoke(this, new(nameof(SucceededTestCaseCount)));
                    break;
            }
        }
    }


    /// <summary>
    /// Raised when property changed.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;


    /// <summary>
    /// Get number of succeeded test cases.
    /// </summary>
    public int SucceededTestCaseCount { get => this.succeededTestCases.Count; }


    /// <summary>
    /// Get list of test cases in the category.
    /// </summary>
    public IList<TestCase> TestCases { get; }
}