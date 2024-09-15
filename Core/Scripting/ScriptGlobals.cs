using Microsoft.Extensions.Logging;
using System.Threading;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Globals for Roslyn-based script.
/// </summary>
/// <typeparam name="TContext">Type of context.</typeparam>
public class ScriptGlobals<TContext> where TContext : IContext
{
    /// <summary>
    /// Initialize new <see cref="ScriptGlobals{TContext}"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <param name="context">Context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public ScriptGlobals(IAppSuiteApplication app, TContext context, CancellationToken cancellationToken)
    {
        this.App = new Application(app);
        this.CancellationToken = cancellationToken;
        this.Context = context;
    }


    /// <summary>
    /// Get application.
    /// </summary>
    public IApplication App { get; }


    /// <summary>
    /// Get cancellation token of running script.
    /// </summary>
    public CancellationToken CancellationToken { get; }


    /// <summary>
    /// Get context.
    /// </summary>
    public TContext Context { get; }   


    /// <summary>
    /// Check whether cancellation of running scriot has been requested or not.
    /// </summary>
    public bool IsCancellationRequested => this.CancellationToken.IsCancellationRequested;


    /// <summary>
    /// Write log with <see cref="LogLevel.Error"/> level.
    /// </summary>
    /// <param name="obj">Message of log.</param>
    public void LogError(object? obj) => this.Context.Logger.LogError("{msg}", obj);


    /// <summary>
    /// Write log with <see cref="LogLevel.Debug"/> level.
    /// </summary>
    /// <param name="obj">Message of log.</param>
    public void LogDebug(object? obj) => this.Context.Logger.LogDebug("{msg}", obj);


    /// <summary>
    /// Write log with <see cref="LogLevel.Information"/> level.
    /// </summary>
    /// <param name="obj">Message of log.</param>
    public void LogInfo(object? obj) => this.Context.Logger.LogInformation("{msg}", obj);


    /// <summary>
    /// Write log with <see cref="LogLevel.Trace"/> level.
    /// </summary>
    /// <param name="obj">Message of log.</param>
    public void LogTrace(object? obj) => this.Context.Logger.LogTrace("{msg}", obj);


    /// <summary>
    /// Write log with <see cref="LogLevel.Warning"/> level.
    /// </summary>
    /// <param name="obj">Message of log.</param>
    public void LogWarning(object? obj) => this.Context.Logger.LogWarning("{msg}", obj);
}