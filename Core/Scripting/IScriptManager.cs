using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Script manager.
/// </summary>
public interface IScriptManager : IApplicationObject<IAppSuiteApplication>
{
    /// <summary>
    /// Create script instance.
    /// </summary>
    /// <param name="language">Language of script.</param>
    /// <param name="source">Source code of script.</param>
    /// <param name="options">Options.</param>
    /// <returns>Script instance.</returns>
    IScript CreateScript(ScriptLanguage language, string source, ScriptOptions options);


    /// <summary>
    /// <see cref="TaskFactory"/> for I/O related tasks.
    /// </summary>
    TaskFactory IOTaskFactory { get; }


    /// <summary>
    /// Wait for completion of all I/O tasks.
    /// </summary>
    /// <returns>Task of waiting.</returns>
    Task WaitForIOTaskCompletion();
}