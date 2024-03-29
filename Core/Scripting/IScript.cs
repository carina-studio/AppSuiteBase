using CarinaStudio.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Represent a runnable script.
/// </summary>
public interface IScript : IShareableDisposable<IScript>, IEquatable<IScript>
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
    /// Check whether the script is empty or not.
    /// </summary>
    bool IsEmpty { get; }


    /// <summary>
    /// Check whether the script is a template script or not.
    /// </summary>
    bool IsTemplate { get; }


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
    /// <typeparam name="R">Type of returned value.</typeparam>
    /// <returns>Task of running script.</returns>
    Task<R> RunAsync<R>(IContext context, CancellationToken cancellationToken = default);


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
    /// <summary>
    /// Python 3.4.
    /// </summary>
    Python,
}


/// <summary>
/// Extensions for <see cref="IScript"/>.
/// </summary>
public static class ScriptExtensions
{
    // Constants.
    const int CachedCancellationTokenSourcesCapacity = 16;
    
    
    // Fields.
    static readonly ConcurrentStack<CancellationTokenSource> cachedCancellationTokenSources = new();
    static Stopwatch? stopwatch;
    
    
    /// <summary>
    /// Check whether at least one compilation or runtime error had occurred or not.
    /// </summary>
    /// <param name="script">Script.</param>
    /// <returns>True if error has occurred before.</returns>
    public static bool HasError(this IScript script) =>
        script.HasCompilationError || script.HasRuntimeError;
    

    /// <summary>
    /// Check whether given <see cref="IScript"/> is neither Null nor empty.
    /// </summary>
    /// <param name="script"><see cref="IScript"/>.</param>
    /// <returns>True if given <see cref="IScript"/> is neither Null nor empty.</returns>
    public static bool IsNotEmpty([NotNullWhen(true)] this IScript? script) =>
        script != null && !script.IsEmpty;
    

    /// <summary>
    /// Check whether given <see cref="IScript"/> is either Null or empty.
    /// </summary>
    /// <param name="script"><see cref="IScript"/>.</param>
    /// <returns>True if given <see cref="IScript"/> is either Null or empty.</returns>
    public static bool IsNullOrEmpty([NotNullWhen(false)] this IScript? script) =>
        script == null || script.IsEmpty;


    /// <summary>
    /// Run script.
    /// </summary>
    /// <param name="script">Script.</param>
    /// <param name="context">Context.</param>
    /// <param name="cancellationCheck">Function to check whether script running should be cancelled or not.</param>
    public static void Run(this IScript script, IContext context, Func<bool>? cancellationCheck = null)
    {
        // check state
        if (cancellationCheck?.Invoke() == true)
            return;
        
        // create cancellation token
        if (!cachedCancellationTokenSources.TryPop(out var cancellationTokenSource))
            cancellationTokenSource = new();
        
        // start running
        var checkingInternal = Math.Max(1000, script.Application.Configuration.GetValueOrDefault(ConfigurationKeys.ScriptCompletionCheckingInterval));
        var runningTask = script.RunAsync<object?>(context, cancellationTokenSource.Token);
        
        // wait for completion
        stopwatch ??= new Stopwatch().Also(it => it.Start());
        var startTime = stopwatch.ElapsedMilliseconds;
        do
        {
            if (runningTask.IsCompleted)
                break;
            Thread.Yield();
        } while ((stopwatch.ElapsedMilliseconds - startTime) < 100);
        while (!runningTask.IsCompleted)
        {
            if (runningTask.Wait(checkingInternal))
                break;
            if (cancellationCheck?.Invoke() == true)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                return;
            }
        }
        
        // complete running
        try
        {
            _ = runningTask.Result;
        }
        catch (Exception ex)
        {
            if (ex is AggregateException && ex.InnerException != null)
                ex = ex.InnerException;
            if (ex is ScriptException)
                throw;
            throw new ScriptException(ex.Message, -1, -1, ex);
        }
        finally
        {
            if (!cancellationTokenSource.IsCancellationRequested && cachedCancellationTokenSources.Count < CachedCancellationTokenSourcesCapacity)
                cachedCancellationTokenSources.Push(cancellationTokenSource);
            else
                cancellationTokenSource.Dispose();
        }
    }


    /// <summary>
    /// Run script.
    /// </summary>
    /// <param name="script">Script.</param>
    /// <param name="context">Context.</param>
    /// <param name="cancellationCheck">Function to check whether script running should be cancelled or not.</param>
    /// <typeparam name="R">Type of returned value.</typeparam>
    /// <returns>Returned value from script.</returns>
    public static R? Run<R>(this IScript script, IContext context, Func<bool>? cancellationCheck = null)
    {
        // check state
        if (cancellationCheck?.Invoke() == true)
            return default;
        
        // create cancellation token
        if (!cachedCancellationTokenSources.TryPop(out var cancellationTokenSource))
            cancellationTokenSource = new();
        
        // start running
        var checkingInternal = Math.Max(1000, script.Application.Configuration.GetValueOrDefault(ConfigurationKeys.ScriptCompletionCheckingInterval));
        var runningTask = script.RunAsync<R>(context, cancellationTokenSource.Token);
        
        // wait for completion
        stopwatch ??= new Stopwatch().Also(it => it.Start());
        var startTime = stopwatch.ElapsedMilliseconds;
        do
        {
            if (runningTask.IsCompleted)
                break;
            Thread.Yield();
        } while ((stopwatch.ElapsedMilliseconds - startTime) < 100);
        while (!runningTask.IsCompleted)
        {
            if (cancellationCheck?.Invoke() == true)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                return default;
            }
            if (runningTask.Wait(checkingInternal))
                break;
        }
        
        // complete running
        try
        {
            return runningTask.Result;
        }
        catch (Exception ex)
        {
            if (ex is AggregateException && ex.InnerException != null)
                ex = ex.InnerException;
            if (ex is ScriptException)
                throw;
            throw new ScriptException(ex.Message, -1, -1, ex);
        }
        finally
        {
            if (!cancellationTokenSource.IsCancellationRequested && cachedCancellationTokenSources.Count < CachedCancellationTokenSourcesCapacity)
                cachedCancellationTokenSources.Push(cancellationTokenSource);
            else
                cancellationTokenSource.Dispose();
        }
    }


    /// <summary>
    /// Run script asynchronously.
    /// </summary>
    /// <param name="script">Script.</param>
    /// <param name="context">Context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of running script.</returns>
    public static Task RunAsync(this IScript script, IContext context, CancellationToken cancellationToken = default) =>
        script.RunAsync<object?>(context, cancellationToken);
}