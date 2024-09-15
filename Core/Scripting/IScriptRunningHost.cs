using System;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Host object which runs one or more scripts.
/// </summary>
public interface IScriptRunningHost : IApplicationObject<IAppSuiteApplication>
{
    /// <summary>
    /// Check whether one or more scripts are run by this object or not.
    /// </summary>
    bool IsRunningScripts { get; }


    /// <summary>
    /// Raised when runtime error was occurred by one of running script.
    /// </summary>
    event EventHandler<ScriptRuntimeErrorEventArgs>? ScriptRuntimeErrorOccurred;
}


/// <summary>
/// Data for event of runtime error of script.
/// </summary>
/// <param name="scriptContainer">The object which contains/owns the script.</param>
/// <param name="script">The script which causes the runtime error.</param>
/// <param name="error">The error occurred while running script.</param>
public class ScriptRuntimeErrorEventArgs(object? scriptContainer, IScript script, Exception error) : EventArgs
{
    /// <summary>
    /// Get the error occurred while running script.
    /// </summary>
    public Exception Error { get; } = error;


    /// <summary>
    /// Get the script which causes the runtime error.
    /// </summary>
    public IScript Script { get; } = script;


    /// <summary>
    /// Get the object which contains/owns the script.
    /// </summary>
    public object? ScriptContainer { get; } = scriptContainer;
}