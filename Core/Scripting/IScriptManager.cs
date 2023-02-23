using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Script manager.
/// </summary>
public interface IScriptManager : IApplicationObject<IAppSuiteApplication>, INotifyPropertyChanged
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
    /// Create template script instance.
    /// </summary>
    /// <param name="language">Language of script.</param>
    /// <param name="source">Source code of script.</param>
    /// <param name="options">Script options.</param>
    /// <returns>Script instance.</returns>
    IScript CreateTemplateScript(ScriptLanguage language, string source, ScriptOptions options);


    /// <summary>
    /// <see cref="TaskFactory"/> for I/O related tasks.
    /// </summary>
    TaskFactory IOTaskFactory { get; }


    /// <summary>
    /// Load script from JSON data.
    /// </summary>
    /// <param name="json">Root json element.</param>
    /// <param name="options">Script options.</param>
    /// <returns>Loaded script.</returns>
    IScript LoadScript(JsonElement json, ScriptOptions options);


    /// <summary>
    /// Open reader asynchronously to read logs output by script.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of opening reader.</returns>
    Task<TextReader> OpenLogReaderAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// Number of running script instances.
    /// </summary>
    int RunningScriptCount { get; }


    /// <summary>
    /// Save script as JSON data.
    /// </summary>
    /// <param name="script">Script to save.</param>
    /// <param name="writer">Writer to write JSON data.</param>
    void SaveScript(IScript script, Utf8JsonWriter writer);


    /// <summary>
    /// Loading of script running. The range is [0.0, 1.0].
    /// </summary>
    double ScriptRunningLoading { get; }


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
    /// Create empty script.
    /// </summary>
    /// <param name="scriptManager"><see cref="IScriptManager"/>.</param>
    /// <param name="language">Language.</param>
    /// <returns>Empty script.</returns>
    public static IScript CreateEmptyScript(this IScriptManager scriptManager, ScriptLanguage language) =>
        new EmptyScript(scriptManager.Application, language);


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

        // get JSON element
        var jsonDocument = (JsonDocument?)null;
        var json = await scriptManager.IOTaskFactory.StartNew(() =>
        {
            try
            {
                using (stream)
                {
                    jsonDocument = JsonDocument.Parse(stream);
                    var root = jsonDocument.RootElement;
                    if (root.ValueKind != JsonValueKind.Object)
                        throw new JsonException("Root element must be an object.");
                    if (!root.TryGetProperty("TypeId", out var jsonValue)
                        || jsonValue.ValueKind != JsonValueKind.String
                        || jsonValue.GetString() != "Script")
                    {
                        throw new JsonException("Invalid type identifier.");
                    }
                    return root.GetProperty("Script");
                }
            }
            catch
            {
                jsonDocument?.Dispose();
                throw;
            }
        });
        if (cancellationToken.IsCancellationRequested)
        {
            jsonDocument?.Dispose();
            throw new TaskCanceledException();
        }

        // load script
        using (jsonDocument)
            return scriptManager.LoadScript(json, options);
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
        await scriptManager.IOTaskFactory.StartNew(() =>
        {
            using (stream)
            {
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions() { Indented = true });
                writer.WriteStartObject();
                writer.WriteString("TypeId", "Script");
                writer.WritePropertyName("Script");
                scriptManager.SaveScript(script, writer);
                writer.WriteEndObject();
            }
        }, CancellationToken.None);
    }
}