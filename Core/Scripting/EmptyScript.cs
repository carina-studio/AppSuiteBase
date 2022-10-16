using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Empty script.
/// </summary>
class EmptyScript : IScript
{
    // Static fields.
    static readonly IList<ICompilationResult> EmptyCompilationResults = new ICompilationResult[0];


    // Constructor.
    public EmptyScript(IAppSuiteApplication app, ScriptLanguage language)
    {
        this.Application = app;
        this.Language = language;
        this.Source = "";
    }


    /// <inheritdoc/>
    public IAppSuiteApplication Application { get; }


    /// <inheritdoc/>
    public Task<bool> CompileAsync(CancellationToken cancellationToken) =>
        Task.FromResult(true);


    /// <inheritdoc/>
    public IList<ICompilationResult> CompilationResults { get => EmptyCompilationResults; }


    /// <inheritdoc/>
    public void Dispose()
    { }


    /// <inheritdoc/>
    public bool Equals(IScript? script) =>
        script != null
        && script.IsEmpty
        && script.Language == this.Language;
    

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is IScript script && this.Equals(script);


    /// <inheritdoc/>
    public override int GetHashCode() =>
        this.Language.GetHashCode();


    /// <inheritdoc/>
    public bool HasCompilationError { get => false; }
    

    /// <inheritdoc/>
    public bool HasRuntimeError { get => true; }


    /// <inheritdoc/>
    public bool IsEmpty { get => true; }


    /// <inheritdoc/>
    public bool IsTemplate { get => false; }


    /// <inheritdoc/>
    public ScriptLanguage Language { get; }


    /// <inheritdoc/>
    public ScriptOptions Options { get; }


    /// <inheritdoc/>
    public Task<R> RunAsync<R>(IContext context, CancellationToken cancellationToken = default) =>
        throw new ScriptException("Cannot run mock script.");
    

    /// <inheritdoc/>
    public IScript Share() =>
        new EmptyScript(this.Application, this.Language);


    /// <inheritdoc/>
    public string Source { get; }
}