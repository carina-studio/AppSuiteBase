using System;
namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Default implementation of <see cref="ICompilationResult"/>.
/// </summary>
public class CompilationResult : ICompilationResult
{
    /// <summary>
    /// Initialize new <see cref="CompilationResult"/> instance.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="message">Message.</param>
    /// <param name="line">Line of start of related source code starting from 1.</param>
    /// <param name="column">Column of start of related source code starting from 0.</param>
    public CompilationResult(CompilationResultType type, string message, int line = -1, int column = -1)
    {
        if (line == 0)
            throw new ArgumentOutOfRangeException(nameof(line));
        this.Type = type;
        this.Message = message;
        this.StartLine = line;
        this.StartColumn = line >= 1 ? column : -1;
        this.EndLine = line;
        this.EndColumn = line >= 1 ? column : -1;
    }


    /// <summary>
    /// Initialize new <see cref="CompilationResult"/> instance.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="message">Message.</param>
    /// <param name="startLine">Line of start of related source code starting from 1.</param>
    /// <param name="startColumn">Column of start of related source code starting from 0.</param>
    /// <param name="endLine">Line of end of related source code starting from 1.</param>
    /// <param name="endColumn">Column of end of related source code starting from 0.</param>
    public CompilationResult(CompilationResultType type, string message, int startLine = -1, int startColumn = -1, int endLine = -1, int endColumn = -1)
    {
        if (startLine == 0)
            throw new ArgumentOutOfRangeException(nameof(startLine));
        if (endLine == 0)
            throw new ArgumentOutOfRangeException(nameof(endLine));
        this.Type = type;
        this.Message = message;
        this.StartLine = startLine;
        this.StartColumn = startLine >= 1 ? startColumn : -1;
        this.EndLine = startLine >= 1 ? endLine : -1;
        this.EndColumn = this.EndLine >= 1 ? endColumn : -1;
    }


    /// <inheritdoc/>
    public int EndColumn { get; }


    /// <inheritdoc/>
    public int EndLine { get; }


    /// <inheritdoc/>
    public string? Message { get; }


    /// <inheritdoc/>
    public int StartColumn { get; }


    /// <inheritdoc/>
    public int StartLine { get; }


    /// <inheritdoc/>
    public CompilationResultType Type { get; }
}