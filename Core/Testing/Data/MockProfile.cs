using CarinaStudio.AppSuite.Data;
using System.Text.Json;

namespace CarinaStudio.AppSuite.Testing.Data;

class MockProfile : BaseProfile<IAppSuiteApplication>
{
    // Constructor.
    public MockProfile(IAppSuiteApplication app, string id, bool isBuiltIn) : base(app, id, isBuiltIn)
    { }


    /// <inheritdoc/>
    public override bool Equals(IProfile<IAppSuiteApplication>? profile)
    {
        throw new System.NotImplementedException();
    }


    /// <inheritdoc/>
    protected override void OnLoad(JsonElement element)
    {
        throw new System.NotImplementedException();
    }


    /// <inheritdoc/>
    protected override void OnSave(Utf8JsonWriter writer, bool includeId)
    {
        throw new System.NotImplementedException();
    }
}