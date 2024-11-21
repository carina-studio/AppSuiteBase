using CarinaStudio.IO;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Implementation of <see cref="ExternalDependency"/> based-on executable or command.
/// </summary>
/// <param name="app">Application.</param>
/// <param name="id">Unique ID of dependency.</param>
/// <param name="priority">Priority of dependency.</param>
/// <param name="fallbackSearchPaths">Fall-back paths to search the executable if it cannot be found in default paths.</param>
/// <param name="exeName">Name of executable or command without extension.</param>
/// <param name="detailsUri">URI for details of external dependency.</param>
/// <param name="installationUri">URI for downloading and installation of external dependency.</param>
public class ExecutableExternalDependency(IAppSuiteApplication app, string id, ExternalDependencyPriority priority, IEnumerable<string> fallbackSearchPaths, string exeName, Uri? detailsUri, Uri? installationUri) : ExternalDependency(app, id, ExternalDependencyType.Software, priority)
{
    // Fields.
    readonly ISet<string> fallbackSearchPaths = ImmutableHashSet.Create(PathEqualityComparer.Default, fallbackSearchPaths as string[] ?? fallbackSearchPaths.ToArray());
    
    
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
        Logger.LogDebug("Start checking '{exeNames}'", this.ExecutableName);
        if (await IO.CommandSearchPaths.FindCommandPathAsync(this.ExecutableName, this.fallbackSearchPaths) is { } exePath)
        {
            Logger.LogDebug("'{exeName}' found: '{exePath}'", this.ExecutableName, exePath);
            return true;
        }
        Logger.LogWarning("'{exeNames}' not found", this.ExecutableName);
        return false;
    }
}