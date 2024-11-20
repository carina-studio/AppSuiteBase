using CarinaStudio.Collections;
using CarinaStudio.IO;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using File = System.IO.File;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Implementation of <see cref="ExternalDependency"/> based-on executable or command.
/// </summary>
/// <param name="app">Application.</param>
/// <param name="id">Unique ID of dependency.</param>
/// <param name="priority">Priority of dependency.</param>
/// <param name="defaultSearchPaths">Default paths to search the executable.</param>
/// <param name="exeName">Name of executable or command without extension.</param>
/// <param name="detailsUri">URI for details of external dependency.</param>
/// <param name="installationUri">URI for downloading and installation of external dependency.</param>
public class ExecutableExternalDependency(IAppSuiteApplication app, string id, ExternalDependencyPriority priority, IEnumerable<string> defaultSearchPaths, string exeName, Uri? detailsUri, Uri? installationUri) : ExternalDependency(app, id, ExternalDependencyType.Software, priority)
{
    // Fields.
    readonly ISet<string> defaultSearchPaths = ImmutableHashSet.Create(PathEqualityComparer.Default, defaultSearchPaths as string[] ?? defaultSearchPaths.ToArray());
    
    
    /// <summary>
    /// Initialize new <see cref="ExecutableExternalDependency"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <param name="id">Unique ID of dependency.</param>
    /// <param name="priority">Priority of dependency.</param>
    /// <param name="exeName">Name of executable or command without extension.</param>
    /// <param name="detailsUri">URI for details of external dependency.</param>
    /// <param name="installationUri">URI for downloading and installation of external dependency.</param>
    public ExecutableExternalDependency(IAppSuiteApplication app, string id, ExternalDependencyPriority priority, string exeName, Uri? detailsUri, Uri? installationUri): this(app, id, priority, [], exeName, detailsUri, installationUri)
    { }
    
    
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
        if (this.defaultSearchPaths.IsNotEmpty())
        {
            Logger.LogDebug("Start checking [{exeNames}] in {pathCount} default path(s)", exeNames, this.defaultSearchPaths.Count);
            var isExeFound = await Task.Run(() =>
            {
                foreach (var directoryPath in this.defaultSearchPaths)
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
                return false;
            }, CancellationToken.None);
            if (isExeFound)
                return true;
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