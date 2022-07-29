using System;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Runtime exception of script.
/// </summary>
public class ScriptException : Exception
{
    /// <summary>
    /// Initialize new <see cref="ScriptException"/> instance.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="line">Line of related source code of script starting from 1.</param>
    /// <param name="column">Column of related source code of script starting from 0.</param>
    /// <param name="ex">Inner exception.</param>
    public ScriptException(string message, int line = -1, int column = -1, Exception? ex = null) : base(message, ex)
    {
        this.Column = column;
        this.Line = line;
    }


    /// <summary>
    /// Get column of related source code of script starting from 0.
    /// </summary>
    public int Column { get; }


    /// <summary>
    /// Get line of related source code of script starting from 1.
    /// </summary>
    public int Line { get; }
}