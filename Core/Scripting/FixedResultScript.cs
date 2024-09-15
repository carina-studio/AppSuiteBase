using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Scripting;

class FixedResultScript(IAppSuiteApplication app, ScriptLanguage language, object? result) : IScript
{
    // Static fields.
    static readonly IList<ICompilationResult> EmptyCompilationResults = Array.Empty<ICompilationResult>();
    
    
    // Fields.
    readonly object? result = result;


    /// <inheritdoc/>
    public IAppSuiteApplication Application { get; } = app;


    /// <inheritdoc/>
    public Task<bool> CompileAsync(CancellationToken cancellationToken) =>
        Task.FromResult(true);


    /// <inheritdoc/>
    public IList<ICompilationResult> CompilationResults => EmptyCompilationResults;


    /// <inheritdoc/>
    public void Dispose()
    { }


    /// <inheritdoc/>
    public bool Equals(IScript? script) =>
        script is FixedResultScript fixedResultScript
        && (this.result?.Equals(fixedResultScript.result) ?? fixedResultScript.result is null);
    

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is IScript script && this.Equals(script);


    /// <inheritdoc/>
    public override int GetHashCode() =>
        this.Language.GetHashCode();


    /// <inheritdoc/>
    public bool HasCompilationError => false;


    /// <inheritdoc/>
    public bool HasRuntimeError => false;


    /// <inheritdoc/>
    public bool IsEmpty => false;


    /// <inheritdoc/>
    public bool IsTemplate => false;


    /// <inheritdoc/>
    public ScriptLanguage Language { get; } = language;


    /// <inheritdoc/>
    public ScriptOptions Options => default;


    /// <inheritdoc/>
    public Task<R> RunAsync<R>(IContext context, CancellationToken cancellationToken = default)
    {
        if (this.result is null)
        {
            if (typeof(R).IsClass)
#pragma warning disable CS8600
#pragma warning disable CS8604
                return Task.FromResult<R>((R)(object?)null);
#pragma warning restore CS8600
#pragma warning restore CS8604
            return Task.FromException<R>(new ScriptException($"Cannot cast result from Null to {typeof(R).Name}."));
        }
        if (this.result is R targetResult)
            return Task.FromResult(targetResult);
        return Task.FromException<R>(new ScriptException($"Cannot cast result from {this.result.GetType().Name} to {typeof(R).Name}."));
    }
    

    /// <inheritdoc/>
    public IScript Share() =>
        new FixedResultScript(this.Application, this.Language, this.result);


    /// <inheritdoc/>
    public string Source { get; } = "";
}