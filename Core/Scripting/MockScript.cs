using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Mock implementation of <see cref="IScript"/>.
/// </summary>
class MockScript : IScript
{
    /// <summary>
    /// Initialize new <see cref="MockScript"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <param name="options">Options.</param>
    public MockScript(IAppSuiteApplication app, ScriptOptions options)
    {
        if (options.ContextType == null)
            throw new ArgumentException("No context type specified.");
        this.Application = app;
        this.CompilationResults = new ICompilationResult[0];
        this.Options = options;
    }


    /// <inheritdoc/>
    public IAppSuiteApplication Application { get; }


    /// <inheritdoc/>
    public Task<bool> CompileAsync(CancellationToken cancellationToken) =>
        Task.FromResult(true);


    /// <inheritdoc/>
    public IList<ICompilationResult> CompilationResults { get; }


    /// <inheritdoc/>
    public void Dispose()
    { }


    /// <inheritdoc/>
    public bool HasCompilationError { get => false; }
    

    /// <inheritdoc/>
    public bool HasRuntimeError { get => true; }


    /// <inheritdoc/>
    public ScriptLanguage Language { get => ScriptLanguage.JavaScript; }


    /// <inheritdoc/>
    public ScriptOptions Options { get; }


    /// <inheritdoc/>
    public Task<R> RunAsync<TContext, R>(TContext context, CancellationToken cancellationToken = default) where TContext : IContext =>
        throw new ScriptException("Cannot run mock script.");
    

    /// <inheritdoc/>
    public Task SaveAsync(Stream stream) =>
        Task.CompletedTask;
    

    /// <inheritdoc/>
    public IScript Share() =>
        new MockScript(this.Application, this.Options);


    /// <inheritdoc/>
    public string Source { get => ""; }
}