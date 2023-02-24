using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Threading;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Implementation of <see cref="IApplication"/>.
/// </summary>
public class Application : IApplication
{
    // SynchronizationContext to prevent crashing by unhandled exception.
    class GuardedSynchronizationContext : SynchronizationContext
    {
        // Fields.
        readonly Application app;
        readonly SynchronizationContext syncContext;

        // Constructor.
        public GuardedSynchronizationContext(Application app)
        {
            this.app = app;
            this.syncContext = app.app.SynchronizationContext;
        }

        /// <inheritdoc/>
        public override SynchronizationContext CreateCopy() =>
            new GuardedSynchronizationContext(this.app);

        /// <inheritdoc/>
        public override void Post(SendOrPostCallback d, object? state)
        {
            var callerStackTrace = this.app.IsDebugMode ? Environment.StackTrace : null;
            this.syncContext.Post(s =>
            {
                try
                {
                    d(s);
                }
                catch (Exception ex)
                {
                    if (string.IsNullOrEmpty(callerStackTrace))
                        this.app.Logger.LogError(ex, "Unhandled exception occurred in action posted by script");
                    else
                        this.app.Logger.LogError(ex, "Unhandled exception occurred in action posted by script. Call stack:\n{stackTrace}", callerStackTrace);
                }
            }, state);
        }

        /// <inheritdoc/>
        public override void Send(SendOrPostCallback d, object? state) =>
            this.syncContext.Send(d, state);

        /// <inheritdoc/>
        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout) =>
            this.syncContext.Wait(waitHandles, waitAll, millisecondsTimeout);
    }


    // Static fields.
    static volatile ILogger? StaticLogger;


    // Fields.
    readonly IAppSuiteApplication app;
    volatile SynchronizationContext? mainThreadSyncContext;


    /// <summary>
    /// Initialize new <see cref="Application"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    public Application(IAppSuiteApplication app) =>
        this.app = app;
    

    /// <inheritdoc/>
    public CultureInfo CultureInfo => this.app.CultureInfo;
    

    /// <inheritdoc/>
    public string? GetString(string key, string? defaultString) =>
        this.app.GetString(key, defaultString);
    

    /// <inheritdoc/>
    public bool IsDebugMode { get => this.app.IsDebugMode; }
    

    /// <inheritdoc/>
    public bool IsMainThread { get => this.app.CheckAccess(); }


    // Logger.
    internal ILogger Logger
    {
        get
        {
            if (StaticLogger != null)
                return StaticLogger;
            lock (typeof(Application))
                StaticLogger ??= this.app.LoggerFactory.CreateLogger("ScriptApplication");
            return StaticLogger;
        }
    }


    /// <inheritdoc/>
    public SynchronizationContext MainThreadSynchronizationContext 
    { 
        get 
        {
            if (this.mainThreadSyncContext != null)
                return this.mainThreadSyncContext;
            lock (this)
                this.mainThreadSyncContext ??= new GuardedSynchronizationContext(this);
            return this.mainThreadSyncContext;
        }
    }
}