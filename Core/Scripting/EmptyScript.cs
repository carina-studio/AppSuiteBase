using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Empty script.
/// </summary>
class EmptyScript(IAppSuiteApplication app, ScriptLanguage language) : IScript
{
    // Static fields.
    static readonly IList<ICompilationResult> EmptyCompilationResults = Array.Empty<ICompilationResult>();

    
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
        script is not null
        && script.IsEmpty
        && script.Language == this.Language;
    

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is IScript script && this.Equals(script);


    /// <inheritdoc/>
    public override int GetHashCode() =>
        this.Language.GetHashCode();


    /// <inheritdoc/>
    public bool HasCompilationError => false;


    /// <inheritdoc/>
    public bool HasRuntimeError => true;


    /// <inheritdoc/>
    public bool IsEmpty => true;


    /// <inheritdoc/>
    public bool IsTemplate => false;


    /// <inheritdoc/>
    public ScriptLanguage Language { get; } = language;


    /// <inheritdoc/>
    public ScriptOptions Options => default;


    /// <inheritdoc/>
    public Task<R> RunAsync<R>(IContext context, CancellationToken cancellationToken = default) =>
        throw new ScriptException("Cannot run empty script.");
    

    /// <inheritdoc/>
    public IScript Share() =>
        new EmptyScript(this.Application, this.Language);


    /// <inheritdoc/>
    public string Source { get; } = "";
}