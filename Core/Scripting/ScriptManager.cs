using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Implementation of <see cref="IScriptManager"/>.
/// </summary>
public class ScriptManager : BaseApplicationObject<IAppSuiteApplication>, IScriptManager
{
    // Statis fields.
    static ScriptManager? DefaultInstance;
    static ILogger? Logger;


    // Fields.
    readonly IScriptManager? implementation;


    // Constructor.
    ScriptManager(IAppSuiteApplication app, IScriptManager? implementation) : base(app)
    {
        this.implementation = implementation;
        this.IOTaskFactory = implementation?.IOTaskFactory ?? new();
        implementation?.Let(it => it.PropertyChanged += (_, e) =>
            this.PropertyChanged?.Invoke(this, e));
    }


    /// <inheritdoc/>
    public IScript CreateScript(ScriptLanguage language, string source, ScriptOptions options) =>
        this.implementation?.CreateScript(language, source, options) ?? new MockScript(this.Application, language, source, false, options);
    

    /// <inheritdoc/>
    public ILogger CreateScriptLogger(string name)
    {
        if (this.implementation != null)
            return this.implementation.CreateScriptLogger(name);
        return this.Application.LoggerFactory.CreateLogger(name);
    }
    

    /// <inheritdoc/>
    public IScript CreateTemplateScript(ScriptLanguage language, string source, ScriptOptions options) =>
        this.implementation?.CreateTemplateScript(language, source, options) ?? new MockScript(this.Application, language, source, true, options);
    

    /// <summary>
    /// Get default instance.
    /// </summary>
    public static ScriptManager Default { get => DefaultInstance ?? throw new InvalidOperationException(); }
    

    // Initialize asynchronously.
    internal static async Task InitializeAsync(IAppSuiteApplication app, Type? implType)
    {
        // check state
        app.VerifyAccess();
        if (DefaultInstance != null)
            throw new InvalidOperationException();
        
        // create logger
        Logger = app.LoggerFactory.CreateLogger(nameof(ScriptManager));
        
        // create implementation
        if (implType != null)
        {
            if (!typeof(IScriptManager).IsAssignableFrom(implType))
            {
                Logger.LogError("Implementation '{implTypeName}' doesn't implement IScriptManager interface", implType.Name);
                implType = null;
            }
            else if (typeof(ScriptManager).IsAssignableFrom(implType))
            {
                Logger.LogError("Cannot use '{implTypeName}' as implementation", implType.Name);
                implType = null;
            }
        }
        var implementation = Global.Run(() =>
        {
            if (implType == null)
            {
                Logger.LogWarning("No implementation specified");
                return null;
            }
            return Global.RunOrDefault(() =>
            {
                var impl = Activator.CreateInstance(implType, app);
                Logger.LogDebug("Implementation created");
                return impl;
            },
            ex => Logger.LogError(ex, "Failed to create implementation"));
        });
        if (implementation == null)
        {
            DefaultInstance = new(app, null);
            return;
        }
        
        // initialize
        var initAsyncMethod = implType!.GetMethod("InitializeAsync");
        if (initAsyncMethod != null)
        {
            var initAsyncResult = initAsyncMethod.Invoke(implementation, Array.Empty<object>());
            if (initAsyncResult is Task task)
                await task;
            Logger.LogDebug("Implementation has been initialized");
        }
        else
            Logger.LogDebug("Implementation doesn't need initialization");

        // create default instance
        DefaultInstance = new(app, (IScriptManager)implementation);
    }
    

    /// <inheritdoc/>
    public TaskFactory IOTaskFactory { get; }


    /// <inheritdoc/>
    public IScript LoadScript(JsonElement json, ScriptOptions options) =>
        this.implementation?.LoadScript(json, options) ?? throw new NotSupportedException();


    /// <inheritdoc/>
    public Task<TextReader> OpenLogReaderAsync(CancellationToken cancellationToken = default)
    {
        if (this.implementation != null)
            return this.implementation.OpenLogReaderAsync(cancellationToken);
        return Task.FromResult<TextReader>(new StringReader(""));
    }
    

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;


    /// <inheritdoc/>
    public int RunningScriptCount { get => this.implementation?.RunningScriptCount ?? 0; }
    

    /// <inheritdoc/>
    public void SaveScript(IScript script, Utf8JsonWriter writer)
    {
        if (this.implementation != null)
            this.implementation?.SaveScript(script, writer);
        else
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }


    /// <inheritdoc/>
    public double ScriptRunningLoading { get => this.implementation?.ScriptRunningLoading ?? 0; }


    /// <inheritdoc/>
    public Task WaitForIOTaskCompletion() =>
        this.implementation?.WaitForIOTaskCompletion() ?? Task.CompletedTask;
}