using CarinaStudio.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Represent a runnable script.
/// </summary>
public interface IScript : IShareableDisposable<IScript>
{
    /// <summary>
    /// Get application.
    /// </summary>
    IAppSuiteApplication Application { get; }


    /// <summary>
    /// Compile script asynchronously if needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of compilation. True if compilation succeeded, false otherwise.</returns>
    Task<bool> CompileAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// Get latest list of compilation results.
    /// </summary>
    IList<ICompilationResult> CompilationResults { get; }


    /// <summary>
    /// Check whether at least one compilation error had occurred or not.
    /// </summary>
    bool HasCompilationError { get; }


    /// <summary>
    /// Check whether at least one runtime error had occurred or not.
    /// </summary>
    bool HasRuntimeError { get; }


    /// <summary>
    /// Get language of script.
    /// </summary>
    ScriptLanguage Language { get; }


    /// <summary>
    /// Get options of script.
    /// </summary>
    ScriptOptions Options { get; }


    /// <summary>
    /// Run script asynchronously.
    /// </summary>
    /// <param name="context">Context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TContext">Type of context.</typeparam>
    /// <typeparam name="R">Type of returned value.</typeparam>
    /// <returns>Task of running script.</returns>
    Task<R> RunAsync<TContext, R>(TContext context, CancellationToken cancellationToken = default) where TContext : IContext;


    /// <summary>
    /// Get source code of script.
    /// </summary>
    string Source { get; }
}


/// <summary>
/// Language of script.
/// </summary>
public enum ScriptLanguage
{
    /// <summary>
    /// JavaScript (ECMAScript 5.1).
    /// </summary>
    JavaScript,
    /// <summary>
    /// C# script.
    /// </summary>
    CSharp,
}


/// <summary>
/// Extensions for <see cref="IScript"/>.
/// </summary>
public static class ScriptExtensions
{
    /// <summary>
    /// Check whether at least one compilation or runtime error had occurred or not.
    /// </summary>
    /// <param name="script">Script.</param>
    /// <returns>True if error has occurred before.</returns>
    public static bool HasError(this IScript script) =>
        script.HasCompilationError || script.HasRuntimeError;


    /// <summary>
    /// Run script.
    /// </summary>
    /// <param name="script">Script.</param>
    /// <param name="context">Context.</param>
    /// <param name="cancellationCheck">Function to check whether script running should be cancelled or not.</param>
    /// <typeparam name="TContext">Type of context.</typeparam>
    public static void Run<TContext>(this IScript script, TContext context, Func<bool>? cancellationCheck = null) where TContext : IContext
    {
        if (cancellationCheck?.Invoke() == true)
            return;
        using var cancellationTokenSource = new CancellationTokenSource();
        var checkingInternal = Math.Max(1000, script.Application.Configuration.GetValueOrDefault(ConfigurationKeys.ScriptCompletionCheckingInterval));
        var runningTask = script.RunAsync<TContext, object?>(context, cancellationTokenSource.Token);
        while (runningTask.IsCompleted)
        {
            if (runningTask.Wait(checkingInternal))
                break;
            if (cancellationCheck?.Invoke() == true)
            {
                cancellationTokenSource.Cancel();
                return;
            }
        }
        _ = runningTask.Result;
    }


    /// <summary>
    /// Run script.
    /// </summary>
    /// <param name="script">Script.</param>
    /// <param name="context">Context.</param>
    /// <param name="cancellationCheck">Function to check whether script running should be cancelled or not.</param>
    /// <typeparam name="TContext">Type of context.</typeparam>
    /// <typeparam name="R">Type of returned value.</typeparam>
    /// <returns>Returned value from script.</returns>
    public static R? Run<TContext, R>(this IScript script, TContext context, Func<bool>? cancellationCheck = null) where TContext : IContext
    {
        if (cancellationCheck?.Invoke() == true)
            return default;
        using var cancellationTokenSource = new CancellationTokenSource();
        var checkingInternal = Math.Max(1000, script.Application.Configuration.GetValueOrDefault(ConfigurationKeys.ScriptCompletionCheckingInterval));
        var runningTask = script.RunAsync<TContext, R>(context, cancellationTokenSource.Token);
        while (runningTask.IsCompleted)
        {
            if (runningTask.Wait(checkingInternal))
                break;
            if (cancellationCheck?.Invoke() == true)
            {
                cancellationTokenSource.Cancel();
                return default;
            }
        }
        return runningTask.Result;
    }


    /// <summary>
    /// Run script asynchronously.
    /// </summary>
    /// <param name="script">Script.</param>
    /// <param name="context">Context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TContext">Type of context.</typeparam>
    /// <returns>Task of running script.</returns>
    public static Task RunAsync<TContext>(this IScript script, TContext context, CancellationToken cancellationToken = default) where TContext : IContext =>
        script.RunAsync<TContext, object?>(context, cancellationToken);
}