using System.Threading;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Implementation of <see cref="IApplication"/>.
/// </summary>
public class Application : IApplication
{
    // Fields.
    readonly IAppSuiteApplication app;


    /// <summary>
    /// Initialize new <see cref="Application"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    public Application(IAppSuiteApplication app) =>
        this.app = app;
    

    /// <inheritdoc/>
    public string? GetString(string key, string? defaultString) =>
        this.app.GetString(key, defaultString);
    

    /// <inheritdoc/>
    public bool IsMainThread { get => app.CheckAccess(); }


    /// <inheritdoc/>
    public SynchronizationContext MainThreadSynchronizationContext { get => this.app.SynchronizationContext; }
}