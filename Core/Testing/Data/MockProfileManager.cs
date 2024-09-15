using CarinaStudio.AppSuite.Data;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Testing.Data;

class MockProfileManager(IAppSuiteApplication app) : BaseProfileManager<IAppSuiteApplication, MockProfile>(app), IDisposable
{
    /// <inheritdoc/>
    public void Dispose()
    {
        //
    }


    /// <inheritdoc/>
    protected override Task<MockProfile> OnLoadProfileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }


    /// <inheritdoc/>
    protected override string ProfilesDirectory { get; } = Path.Combine(app.RootPrivateDirectoryPath, "__Mock_Profiles__");
}