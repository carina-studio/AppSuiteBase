using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Implementation of <see cref="ExternalDependency"/> based-on executable or command.
/// </summary>
/// <param name="app">Application.</param>
/// <param name="id">Unique ID of dependency.</param>
/// <param name="priority">Priority of dependency.</param>
/// <param name="exeName">Name of executable or command without extension.</param>
/// <param name="detailsUri">URI for details of external dependency.</param>
/// <param name="installationUri">URI for downloading and installation of external dependency.</param>
public class ExecutableExternalDependency(IAppSuiteApplication app, string id, ExternalDependencyPriority priority, string exeName, Uri? detailsUri, Uri? installationUri) : ExternalDependency(app, id, ExternalDependencyType.Software, priority)
{
    /// <inheritdoc/>
    public override Uri? DetailsUri { get; } = detailsUri;


    /// <summary>
    /// Get name of executable or command without extension.
    /// </summary>
    protected string ExecutableName { get; } = exeName;


    /// <inheritdoc/>
    public override Uri? InstallationUri { get; } = installationUri;


    /// <inheritdoc/>
    protected override async Task<bool> OnCheckAvailabilityAsync()
    {
        var exeNames = new List<string>();
        if (Platform.IsWindows)
        {
            exeNames.Add($"{this.ExecutableName}.exe");
            exeNames.Add($"{this.ExecutableName}.bat");
            exeNames.Add($"{this.ExecutableName}.cmd");
        }
        else
        {
            exeNames.Add(this.ExecutableName);
            exeNames.Add($"{this.ExecutableName}.sh");
        }
        var paths = await IO.CommandSearchPaths.GetPathsAsync();
        Logger.LogDebug("Start checking [{exeNames}] in {pathCount} path(s)", exeNames, paths.Count);
        return await Task.Run(() =>
        {
            foreach (var directoryPath in paths)
            {
                foreach (var exeName in exeNames)
                {
                    string commandFile = Path.Combine(directoryPath, exeName);
                    if (File.Exists(commandFile))
                    {
                        Logger.LogDebug("'{exeName}' found in '{dir}'", exeName, directoryPath);
                        return true;
                    }
                }
            }
            Logger.LogWarning("[{exeNames}] not found in all paths", exeNames);
            return false;
        }, CancellationToken.None);
    }
}