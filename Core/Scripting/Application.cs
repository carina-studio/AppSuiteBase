using CarinaStudio.AppSuite.Diagnostics;
using CarinaStudio.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Implementation of <see cref="IApplication"/>.
/// </summary>
public class Application : IApplication
{
    // SynchronizationContext to prevent crashing by unhandled exception.
    class GuardedSynchronizationContext(SynchronizationContext syncContext, ILogger logger, bool isDebugMode) : SynchronizationContext
    {
        /// <inheritdoc/>
        public override SynchronizationContext CreateCopy() =>
            new GuardedSynchronizationContext(syncContext, logger, isDebugMode);

        /// <inheritdoc/>
        public override void Post(SendOrPostCallback d, object? state)
        {
            var callerStackTrace = isDebugMode ? Environment.StackTrace : null;
            syncContext.Post(s =>
            {
                try
                {
                    d(s);
                }
                catch (Exception ex)
                {
                    if (string.IsNullOrEmpty(callerStackTrace))
                        logger.LogError(ex, "Unhandled exception occurred in action posted by script");
                    else
                        logger.LogError(ex, "Unhandled exception occurred in action posted by script. Call stack:\n{stackTrace}", callerStackTrace);
                }
            }, state);
        }

        /// <inheritdoc/>
        public override void Send(SendOrPostCallback d, object? state) =>
            syncContext.Send(d, state);

        /// <inheritdoc/>
        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout) =>
            syncContext.Wait(waitHandles, waitAll, millisecondsTimeout);
    }


    // Static fields.
    [Obfuscation(Exclude = false)]
    static Regex? CommandFileNameRegex;
    [Obfuscation(Exclude = false)]
    static ILogger? StaticLogger;


    // Fields.
    [Obfuscation(Exclude = false)] 
    readonly Func<CultureInfo> cultureInfoGetter;
    [Obfuscation(Exclude = false)]
    readonly Func<bool> mainThreadChecker;
    [Obfuscation(Exclude = false)]
    readonly SynchronizationContext mainThreadSyncContext;
    [Obfuscation(Exclude = false)]
    volatile SynchronizationContext? mainThreadSyncContextWrapper;
    [Obfuscation(Exclude = false)] 
    readonly Func<string, string?, string?> stringGetter;


    /// <summary>
    /// Initialize new <see cref="Application"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    public Application([Obfuscation(Exclude = false)] IAppSuiteApplication app)
    {
        Guard.VerifyInternalCall();
        this.cultureInfoGetter = () => app.CultureInfo;
        this.IsDebugMode = app.IsDebugMode;
        // ReSharper disable once ConvertClosureToMethodGroup
        this.mainThreadChecker = () => app.CheckAccess();
        this.mainThreadSyncContext = app.SynchronizationContext;
        StaticLogger ??= app.LoggerFactory.CreateLogger("ScriptApplication");
        // ReSharper disable once ConvertClosureToMethodGroup
        this.stringGetter = (key, defaultString) => app.GetString(key, defaultString);
    }


    /// <inheritdoc/>
    public CultureInfo CultureInfo => this.cultureInfoGetter();
    
    
    /// <inheritdoc/>
    public int ExecuteCommand(string command, CancellationToken cancellationToken = default) =>
        this.ExecuteCommand(command, fallbackSearchPaths: null, cancellationToken);


    /// <inheritdoc/>
    public int ExecuteCommand(string command, string[]? fallbackSearchPaths, CancellationToken cancellationToken = default)
    {
        var startInfo = PrepareExecutingCommand(command, fallbackSearchPaths, cancellationToken);
        return Process.Start(startInfo)?.Use(process =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                process.Kill();
                throw new TaskCanceledException();
            }
            process.WaitForExitAsync(cancellationToken).Wait(cancellationToken);
            return process.ExitCode;
        }) ?? throw new InvalidOperationException($"Unable to execute command '{startInfo.FileName}'.");
    }


    /// <inheritdoc/>
    public int ExecuteCommand(string command, Action<Process, CancellationToken> action, CancellationToken cancellationToken = default) =>
        this.ExecuteCommand(command, null, action, cancellationToken);


    /// <inheritdoc/>
    public int ExecuteCommand(string command, string[]? fallbackSearchPaths, Action<Process, CancellationToken> action, CancellationToken cancellationToken = default)
    {
        var startInfo = PrepareExecutingCommand(command, fallbackSearchPaths, cancellationToken).Also(it =>
        {
            it.RedirectStandardError = true;
            it.RedirectStandardInput = true;
            it.RedirectStandardOutput = true;
        });
        return Process.Start(startInfo)?.Use(process =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                process.Kill();
                throw new TaskCanceledException();
            }
            action(process, cancellationToken);
            process.WaitForExitAsync(cancellationToken).Wait(cancellationToken);
            return process.ExitCode;
        }) ?? throw new InvalidOperationException($"Unable to execute command '{startInfo.FileName}'.");
    }
    
    
    /// <inheritdoc/>
    public string? FindCommandPath(string command, CancellationToken cancellationToken = default) =>
        IO.CommandSearchPaths.FindCommandPath(command, cancellationToken: cancellationToken);


    /// <inheritdoc/>
    public string? FindCommandPath(string command, string[]? fallbackSearchPaths, CancellationToken cancellationToken = default) =>
        IO.CommandSearchPaths.FindCommandPath(command, fallbackSearchPaths, cancellationToken);
    
    
    /// <inheritdoc/>
    public string? GetFormattedString(string key, params object?[] args)
    {
        var format = this.GetString(key, null);
        if (format != null)
            return string.Format(format, args);
        return null;
    }
    
    
    /// <inheritdoc/>
    public string? GetString(string key) =>
        this.GetString(key, null);
    

    /// <inheritdoc/>
    public string? GetString(string key, string? defaultString) =>
        this.stringGetter(key, defaultString);
    
    
    /// <inheritdoc/>
    public string GetStringNonNull(string key) =>
        this.GetString(key, null) ?? "";
    
    
    /// <inheritdoc/>
    public string GetStringNonNull(string key, string defaultString) =>
        this.GetString(key, null) ?? defaultString;
    

    /// <inheritdoc/>
    public bool IsDebugMode { get; }


    /// <inheritdoc/>
    public bool IsMainThread => this.mainThreadChecker();


    // Logger.
    internal ILogger Logger => StaticLogger.AsNonNull();


    /// <inheritdoc/>
    public SynchronizationContext MainThreadSynchronizationContext 
    { 
        get 
        {
            if (this.mainThreadSyncContextWrapper is not null)
                return this.mainThreadSyncContextWrapper;
            // ReSharper disable NonAtomicCompoundOperator
            lock (this)
                this.mainThreadSyncContextWrapper ??= new GuardedSynchronizationContext(this.mainThreadSyncContext, StaticLogger.AsNonNull(), IsDebugMode);
            // ReSharper restore NonAtomicCompoundOperator
            return this.mainThreadSyncContextWrapper;
        }
    }
    
    
    // Prepare executing command.
    static ProcessStartInfo PrepareExecutingCommand(string command, string[]? fallbackSearchPaths, CancellationToken cancellationToken)
    {
        // check state
        if (cancellationToken.IsCancellationRequested)
            throw new TaskCanceledException();
        
        // parse command
        CommandFileNameRegex ??= new("^\\s*(?<FileName>\\S+|\"[^\"]*\")");
        var match = CommandFileNameRegex.Match(command);
        if (!match.Success)
            throw new ArgumentException($"Unable to parse command: {command}");
        var fileNameGroup = match.Groups["FileName"];
        var fileName = fileNameGroup.Value.Let(it => it.Length == 0 || it[0] != '"' ? it : it[1..^1]);
        var args = command[(fileNameGroup.Index + fileNameGroup.Length)..];
        
        // find command path
        var commandPath = IO.CommandSearchPaths.FindCommandPath(fileName, fallbackSearchPaths, cancellationToken);
        if (commandPath == null)
            throw new InvalidOperationException($"Command '{fileName}' not found.");
        
        // complete
        return new ProcessStartInfo
        {
            Arguments = args,
            CreateNoWindow = true,
            FileName = commandPath,
            UseShellExecute = false,
        };
    }
}