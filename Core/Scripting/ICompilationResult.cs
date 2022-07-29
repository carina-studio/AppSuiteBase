namespace CarinaStudio.AppSuite.Scripting
{
    /// <summary>
    /// Represents single result of compilation of script.
    /// </summary>
    public interface ICompilationResult
    {
        /// <summary>
        /// Get column of end of related source code starting from 0.
        /// </summary>
        int EndColumn { get; }


        /// <summary>
        /// Get line of end of related source code starting from 1.
        /// </summary>
        int EndLine { get; }


        /// <summary>
        /// Get message of result.
        /// </summary>
        string? Message { get; }


        /// <summary>
        /// Get column of start of related source code starting from 0.
        /// </summary>
        int StartColumn { get; }


        /// <summary>
        /// Get line of start of related source code starting from 1.
        /// </summary>
        int StartLine { get; }
        

        /// <summary>
        /// Get type of result.
        /// </summary>
        CompilationResultType Type { get; }
    }


    /// <summary>
    /// Type of result of script compilation.
    /// </summary>
    public enum CompilationResultType
    {
        /// <summary>
        /// Information.
        /// </summary>
        Information,
        /// <summary>
        /// Warning.
        /// </summary>
        Warning,
        /// <summary>
        /// Error.
        /// </summary>
        Error,
    }
}