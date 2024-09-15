using System;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Runtime exception of script.
/// </summary>
/// <param name="message">Message.</param>
/// <param name="line">Line of related source code of script starting from 1.</param>
/// <param name="column">Column of related source code of script starting from 0.</param>
/// <param name="ex">Inner exception.</param>
public class ScriptException(string message, int line = -1, int column = -1, Exception? ex = null) : Exception(message, ex)
{
    /// <summary>
    /// Get column of related source code of script starting from 0.
    /// </summary>
    public int Column { get; } = column;


    /// <summary>
    /// Get line of related source code of script starting from 1.
    /// </summary>
    public int Line { get; } = line;
}