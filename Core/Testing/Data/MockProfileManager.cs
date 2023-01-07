using CarinaStudio.AppSuite.Data;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Testing.Data;

class MockProfileManager : BaseProfileManager<IAppSuiteApplication, MockProfile>, IDisposable
{
    // Constructor.
    public MockProfileManager(IAppSuiteApplication app) : base(app)
    { 
        this.ProfilesDirectory = Path.Combine(this.Application.RootPrivateDirectoryPath, "__Mock_Profiles__");
    }


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
    protected override string ProfilesDirectory { get; }
}