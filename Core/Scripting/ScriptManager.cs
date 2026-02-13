using CarinaStudio.AppSuite.Diagnostics;
using CarinaStudio.Logging;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Implementation of <see cref="IScriptManager"/>.
/// </summary>
public class ScriptManager : BaseApplicationObject<IAppSuiteApplication>, IScriptManager
{
    // Static fields.
    static ScriptManager? DefaultInstance;
    static ILogger? StaticLogger;


    // Fields.
    [Obfuscation(Exclude = false)] 
    readonly MethodInfo? beginScriptContextMethod;
    [Obfuscation(Exclude = false)] 
    readonly MethodInfo? endScriptContextMethod;
    [Obfuscation(Exclude = false)]
    readonly IScriptManager? implementation;


    // Constructor.
    ScriptManager(IAppSuiteApplication app, IScriptManager? implementation) : base(app)
    {
        Guard.VerifyInternalCall();
        this.beginScriptContextMethod = implementation?.GetType().GetMethod("BeginScriptContext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        this.endScriptContextMethod = implementation?.GetType().GetMethod("EndScriptContext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        this.implementation = implementation;
        this.IOTaskFactory = implementation?.IOTaskFactory ?? new();
        implementation?.Let(it => it.PropertyChanged += (_, e) =>
            this.PropertyChanged?.Invoke(this, e));
    }


    // Begin the script context on current thread.
    [Obfuscation(Exclude = false)]
    internal bool BeginScriptContext()
    {
        if (this.beginScriptContextMethod is not null)
            return (bool)this.beginScriptContextMethod.Invoke(this.implementation, null)!;
        return false;
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
    public static ScriptManager Default
    {
        get
        {
            Guard.VerifyInternalCall();
            return DefaultInstance ?? throw new InvalidOperationException();
        }
    }
    
    
    // End the script context on current thread.
    internal bool EndScriptContext()
    {
        if (this.endScriptContextMethod is not null)
            return (bool)this.endScriptContextMethod.Invoke(this.implementation, null)!;
        return false;
    }


    // Initialize asynchronously.
    [RequiresUnreferencedCode("Create internal component.")]
    internal static async Task InitializeAsync(IAppSuiteApplication app, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] Type? implType)
    {
        // check state
        app.VerifyAccess();
        if (DefaultInstance is not null)
            throw new InvalidOperationException();
        
        // create implementation
        StaticLogger ??= app.LoggerFactory.CreateLogger(nameof(ScriptManager));
        if (implType is not null)
        {
            if (!typeof(IScriptManager).IsAssignableFrom(implType))
            {
                StaticLogger.LogError("Implementation '{implTypeName}' doesn't implement IScriptManager interface", implType.Name);
                implType = null;
            }
            else if (typeof(ScriptManager).IsAssignableFrom(implType))
            {
                StaticLogger.LogError("Cannot use '{implTypeName}' as implementation", implType.Name);
                implType = null;
            }
        }
        var implementation = Global.Run(() =>
        {
            if (implType is null)
            {
                StaticLogger.LogWarning("No implementation specified");
                return null;
            }
            return Global.RunOrDefault(() =>
            {
                var impl = Activator.CreateInstance(implType, app);
                StaticLogger.LogDebug("Implementation created");
                return impl;
            },
            ex => StaticLogger.LogError(ex, "Failed to create implementation"));
        });
        if (implementation is null)
        {
            DefaultInstance = new(app, null);
            return;
        }
        
        // initialize
        var initAsyncMethod = implType!.GetMethod("InitializeAsync");
        if (initAsyncMethod is not null)
        {
            var initAsyncResult = initAsyncMethod.Invoke(implementation, Array.Empty<object>());
            if (initAsyncResult is Task task)
                await task;
            StaticLogger.LogDebug("Implementation has been initialized");
        }
        else
            StaticLogger.LogDebug("Implementation doesn't need initialization");

        // create default instance
        DefaultInstance = new(app, (IScriptManager)implementation);
    }
    

    /// <inheritdoc/>
    public TaskFactory IOTaskFactory { get; }


    /// <inheritdoc/>
    public IScript LoadScript(JsonElement json, ScriptOptions options) =>
        this.implementation?.LoadScript(json, options) ?? throw new NotSupportedException();


    /// <inheritdoc/>
    protected override string LoggerCategoryName => nameof(ScriptManager);


    /// <inheritdoc/>
    public Task<TextReader> OpenLogReaderAsync(CancellationToken cancellationToken = default)
    {
        if (this.implementation is not null)
            return this.implementation.OpenLogReaderAsync(cancellationToken);
        return Task.FromResult<TextReader>(new StringReader(""));
    }


    /// <inheritdoc/>
    public void OpenLogWindow() =>
        this.implementation?.OpenLogWindow();
    

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;


    /// <inheritdoc/>
    public int RunningScriptCount => this.implementation?.RunningScriptCount ?? 0;


    /// <inheritdoc/>
    public void SaveScript(IScript script, Utf8JsonWriter writer)
    {
        if (this.implementation is not null)
            this.implementation?.SaveScript(script, writer);
        else
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }


    /// <inheritdoc/>
    public double ScriptRunningLoading => this.implementation?.ScriptRunningLoading ?? 0;


    /// <inheritdoc/>
    public Task WaitForIOTaskCompletion() =>
        this.implementation?.WaitForIOTaskCompletion() ?? Task.CompletedTask;
}