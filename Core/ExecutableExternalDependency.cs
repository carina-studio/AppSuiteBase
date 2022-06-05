using CarinaStudio.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Implementation of <see cref="ExternalDependency"/> based-on executable or command.
/// </summary>
public class ExecutableExternalDependency : ExternalDependency
{
    /// <summary>
    /// Initialize new <see cref="ExecutableExternalDependency"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <param name="id">Unique ID of dependency.</param>
    /// <param name="priority">Priority of dependency.</param>
    /// <param name="exeName">Name of executable or command without extension.</param>
    /// <param name="detailsUri">URI for details of external dependency.</param>
    /// <param name="installationUri">URI for downloading and installation of external dependency.</param>
    public ExecutableExternalDependency(IAppSuiteApplication app, string id, ExternalDependencyPriority priority, string exeName, Uri? detailsUri, Uri? installationUri) : base(app, id, priority)
    { 
        this.DetailsUri = detailsUri;
        this.ExecutableName = exeName;
        this.InstallationUri = installationUri;
    }


    /// <inheritdoc/>
    public override Uri? DetailsUri { get; }


    /// <summary>
    /// Get name of executable or command without extension.
    /// </summary>
    protected string ExecutableName { get; }


    /// <inheritdoc/>
    public override Uri? InstallationUri { get; }


    /// <inheritdoc/>
    protected override Task<bool> OnCheckAvailabilityAsync() => Task.Run(() =>
    {
        var exeNames = new List<string>();
        if (Platform.IsWindows)
        {
            exeNames.Add($"{this.ExecutableName}.exe");
            exeNames.Add($"{this.ExecutableName}.bat");
        }
        else
        {
            exeNames.Add(this.ExecutableName);
            exeNames.Add($"{this.ExecutableName}.sh");
        }
        var paths = Global.Run(() =>
        {
            if (Platform.IsMacOS)
            {
                try
                {
                    using var reader = new StreamReader("/etc/paths");
                    var paths = new List<string>();
                    var path = reader.ReadLine();
                    while (path != null)
                    {
                        if (!string.IsNullOrWhiteSpace(path))
                            paths.Add(path);
                        path = reader.ReadLine();
                    }
                    return paths.IsNotEmpty() ? paths.ToArray() : new string[0];
                }
                catch
                {
                    return new string[0];
                }
            }
            else
                return Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? new string[0];
        });
        foreach (var directoryPath in paths)
        {
            foreach (var exeName in exeNames)
            {
                string commandFile = Path.Combine(directoryPath, exeName);
                if (File.Exists(commandFile))
                    return true;
            }
        }
        return false;
    });
}