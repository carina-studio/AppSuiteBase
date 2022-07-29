using System.Threading;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Interface for type of globals for Roslyn-based script.
/// </summary>
/// <typeparam name="TContext">Type of context.</typeparam>
public interface IScriptGlobals<TContext> where TContext : IContext
{
    /// <summary>
    /// Get application.
    /// </summary>
    IApplication App { get; }


    /// <summary>
    /// Get cancellation token of running script.
    /// </summary>
    CancellationToken CancellationToken { get; }


    /// <summary>
    /// Get context.
    /// </summary>
    TContext Context { get; }
}