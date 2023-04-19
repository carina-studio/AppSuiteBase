using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace CarinaStudio.AppSuite.Scripting;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global

/// <summary>
/// Interface for script to access application functions.
/// </summary>
public interface IApplication
{
    /// <summary>
    /// Get current application culture.
    /// </summary>
    CultureInfo CultureInfo { get; }
    
    
    /// <summary>
    /// Execute external command.
    /// </summary>
    /// <param name="command">Command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code of command.</returns>
    int ExecuteCommand(string command, CancellationToken cancellationToken = default);


    /// <summary>
    /// Execute external command.
    /// </summary>
    /// <param name="command">Command.</param>
    /// <param name="action">Action to interact with process of external command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code of command.</returns>
    int ExecuteCommand(string command, Action<Process, CancellationToken> action, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Find valid path of command to execute.
    /// </summary>
    /// <param name="command">Command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path of command or Null if command cannot be found.</returns>
    string? FindCommandPath(string command, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Get formatted string defined in application resource.
    /// </summary>
    /// <param name="key">Key of string.</param>
    /// <param name="args">Arguments to format string.</param>
    /// <returns>Formatted string or Null if string not found.</returns>
    public string? GetFormattedString(string key, params object?[] args)
    {
        var format = this.GetString(key, null);
        if (format != null)
            return string.Format(format, args);
        return null;
    }


    /// <summary>
    /// Get string defined in application resource.
    /// </summary>
    /// <param name="key">Key of string.</param>
    /// <returns>String defined in resource or Null if string not found.</returns>
    public string? GetString(string key) =>
        this.GetString(key, null);
    
    
    /// <summary>
    /// Get string defined in application resource.
    /// </summary>
    /// <param name="key">Key of string.</param>
    /// <param name="defaultString">Default string.</param>
    /// <returns>String defined in resource or <paramref name="defaultString"/> if string not found.</returns>
    string? GetString(string key, string? defaultString);
    
    
    /// <summary>
    /// Get non-null string defined in application resource.
    /// </summary>
    /// <param name="key">Key of string.</param>
    /// <returns>String defined in resource or <see cref="String.Empty"/> if string not found.</returns>
    public string GetStringNonNull(string key) =>
        this.GetString(key, null) ?? "";
    
    
    /// <summary>
    /// Get non-null string defined in application resource.
    /// </summary>
    /// <param name="key">Key of string.</param>
    /// <param name="defaultString">Default string.</param>
    /// <returns>String defined in resource or <paramref name="defaultString"/> if string not found.</returns>
    public string GetStringNonNull(string key, string defaultString) =>
        this.GetString(key, null) ?? defaultString;


    /// <summary>
    /// Check whether application is running in debug mode or not.
    /// </summary>
    bool IsDebugMode { get; }


    /// <summary>
    /// Check whether current thread is main thread of application or not.
    /// </summary>
    bool IsMainThread { get; }


    /// <summary>
    /// Get <see cref="SynchronizationContext"/> of main thread of application.
    /// </summary>
    SynchronizationContext MainThreadSynchronizationContext { get; }
}