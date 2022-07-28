using System.Threading;
using System.IO;
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
    /// <param name="options">Script options.</param>
    /// <returns>Script instance.</returns>
    IScript CreateScript(ScriptLanguage language, string source, ScriptOptions options);


    /// <summary>
    /// <see cref="TaskFactory"/> for I/O related tasks.
    /// </summary>
    TaskFactory IOTaskFactory { get; }


    /// <summary>
    /// Load script asynchronously.
    /// </summary>
    /// <param name="stream">Stream to load script.</param>
    /// <param name="options">Script options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of loading script.</returns>
    Task<IScript> LoadScriptAsync(Stream stream, ScriptOptions options, CancellationToken cancellationToken = default);


    /// <summary>
    /// Save script asynchronously.
    /// </summary>
    /// <param name="script">Script to save.</param>
    /// <param name="stream">Stream to save script.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of saving script.</returns>
    Task SaveScriptAsync(IScript script, Stream stream, CancellationToken cancellationToken = default);


    /// <summary>
    /// Wait for completion of all I/O tasks.
    /// </summary>
    /// <returns>Task of waiting.</returns>
    Task WaitForIOTaskCompletion();
}


/// <summary>
/// Extensions for <see cref="IScriptManager"/>.
/// </summary>
public static class ScriptManagerExtensions
{
    /// <summary>
    /// Load script asynchronously.
    /// </summary>
    /// <param name="scriptManager"><see cref="IScriptManager"/>.</param>
    /// <param name="fileName">Name of file to load script.</param>
    /// <param name="options">Script options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of loading script.</returns>
    public static async Task<IScript> LoadScriptAsync(this IScriptManager scriptManager, string fileName, ScriptOptions options, CancellationToken cancellationToken = default)
    {
        // open file
        if (cancellationToken.IsCancellationRequested)
            throw new TaskCanceledException();
        var stream = await scriptManager.IOTaskFactory.StartNew(() =>
        {
            for (var i = 0; i < 5; ++i)
            {
                if (CarinaStudio.IO.File.TryOpenRead(fileName, 1000, out var stream))
                    return stream;
                if (cancellationToken.IsCancellationRequested)
                    throw new TaskCanceledException();
            }
            return null;
        });
        if (stream == null)
            throw new IOException($"Unable to open file '{fileName}' to load script.");
        if (cancellationToken.IsCancellationRequested)
        {
            Global.RunWithoutErrorAsync(stream.Close);
            throw new TaskCanceledException();
        }

        // load script
        using (stream)
            return await scriptManager.LoadScriptAsync(stream, options, cancellationToken);
    }


    /// <summary>
    /// Save script asynchronously.
    /// </summary>
    /// <param name="scriptManager"><see cref="IScriptManager"/>.</param>
    /// <param name="script">Script to save.</param>
    /// <param name="fileName">Name of file to save script.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of saving script.</returns>
    public static async Task SaveScriptAsync(this IScriptManager scriptManager, IScript script, string fileName, CancellationToken cancellationToken = default)
    {
        // open file
        if (cancellationToken.IsCancellationRequested)
            throw new TaskCanceledException();
        var stream = await ScriptManager.Default.IOTaskFactory.StartNew(() =>
        {
            for (var i = 0; i < 5; ++i)
            {
                if (CarinaStudio.IO.File.TryOpenReadWrite(fileName, 1000, out var stream))
                    return stream;
                if (cancellationToken.IsCancellationRequested)
                    throw new TaskCanceledException();
            }
            return null;
        });
        if (stream == null)
            throw new IOException($"Unable to open file '{fileName}' to save script.");
        if (cancellationToken.IsCancellationRequested)
        {
            Global.RunWithoutErrorAsync(stream.Close);
            throw new TaskCanceledException();
        }

        // save script
        using (stream)
            await scriptManager.SaveScriptAsync(script, stream, cancellationToken);
    }
}