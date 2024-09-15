using System;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Testing;

/// <summary>
/// Empty test case.
/// </summary>
class EmptyTest(IAppSuiteApplication app) : TestCase(app, TestCaseCategoryNames.Unclassified, "Empty")
{
    /// <inheritdoc/>
    protected override Task OnRunAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


    /// <inheritdoc/>
    protected override Task OnSetupAsync()
    {
        throw new NotImplementedException();
    }


    /// <inheritdoc/>
    protected override Task OnTearDownAsync()
    {
        throw new NotImplementedException();
    }
}