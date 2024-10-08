using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Mock implementation of <see cref="IScript"/>.
/// </summary>
class MockScript : IScript
{
    // Constructor.
    public MockScript(IAppSuiteApplication app, ScriptLanguage language, string source, bool isTemplate, ScriptOptions options)
    {
        if (options.ContextType == null)
            throw new ArgumentException("No context type specified.");
        this.Application = app;
        this.CompilationResults = Array.Empty<ICompilationResult>();
        this.IsEmpty = string.IsNullOrWhiteSpace(source);
        this.IsTemplate = isTemplate;
        this.Language = language;
        this.Options = options;
        this.Source = source;
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
    public bool Equals(IScript? script) =>
        script != null
        && script.Language == this.Language
        && script.Source == this.Source;
    

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is IScript script && this.Equals(script);


    /// <inheritdoc/>
    public override int GetHashCode() =>
        this.Source.GetHashCode();


    /// <inheritdoc/>
    public bool HasCompilationError => false;


    /// <inheritdoc/>
    public bool HasRuntimeError => true;


    /// <inheritdoc/>
    public bool IsEmpty { get; }


    /// <inheritdoc/>
    public bool IsTemplate { get; }


    /// <inheritdoc/>
    public ScriptLanguage Language { get; }


    /// <inheritdoc/>
    public ScriptOptions Options { get; }


    /// <inheritdoc/>
    public Task<R> RunAsync<R>(IContext context, CancellationToken cancellationToken = default) =>
        throw new ScriptException("Cannot run mock script.");
    

    /// <inheritdoc/>
    public IScript Share() =>
        new MockScript(this.Application, this.Language, this.Source, this.IsTemplate, this.Options);


    /// <inheritdoc/>
    public string Source { get; }
}