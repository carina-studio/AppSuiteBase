using CarinaStudio.Collections;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.Testing;

/// <summary>
/// Category of test case.
/// </summary>
public class TestCaseCategory
{
    // Fields.
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
    internal void AddTestCase(TestCase testCase) =>
        this.testCases.Add(testCase);


    /// <summary>
    /// Get name of category.
    /// </summary>
    public string Name { get; }


    /// <summary>
    /// Get list of test cases in the category.
    /// </summary>
    public IList<TestCase> TestCases { get; }
}