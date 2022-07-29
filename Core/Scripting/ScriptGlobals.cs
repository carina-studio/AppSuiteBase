using System.Threading;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Default implementation of <see cref="IScriptGlobals{TContext}"/>
/// </summary>
/// <typeparam name="TContext">Type of context.</typeparam>
public class ScriptGlobals<TContext> : IScriptGlobals<TContext> where TContext : IContext
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


    /// <inheritdoc/>
    public IApplication App { get; }


    /// <inheritdoc/>
    public CancellationToken CancellationToken { get; }


    /// <inheritdoc/>
    public TContext Context { get; }   
}