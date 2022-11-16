using CarinaStudio.Collections;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.IO;

/// <summary>
/// Paths to search command and executable.
/// </summary>
public static class CommandSearchPaths
{
    /// <summary>
    /// Scope of paths defined by system.
    /// </summary>
    [Flags]
    public enum SystemPathsScope
    {
        /// <summary>
        /// All.
        /// </summary>
        All = Global | User,
        /// <summary>
        /// Global.
        /// </summary>
        Global = 0x1,
        /// <summary>
        /// User.
        /// </summary>
        User = 0x2,
    }


    // Fields.
    static readonly HashSet<string> customPaths = new(CarinaStudio.IO.PathEqualityComparer.Default);
    static volatile ILogger? logger;


    // Static initializer.
    static CommandSearchPaths()
    {
        if (Platform.IsMacOS)
        {
            customPaths.Add("/opt/homebrew/bin");
        }
    }


    /// <summary>
    /// Add custom search path.
    /// </summary>
    /// <param name="path">Path.</param>
    public static void AddCustomPath(string path)
    {
        lock (customPaths)
            customPaths.Add(path);
    }


    /// <summary>
    /// Get custom paths.
    /// </summary>
    public static ISet<string> CustomPaths { get; } = customPaths.AsReadOnly();


    /// <summary>
    /// Get all paths asynchronously.
    /// </summary>
    /// <returns>Set of paths.</returns>
    public static async Task<ISet<string>> GetPathsAsync(CancellationToken cancellationToken = default)
    {
        // add system paths
        var paths = new HashSet<string>(CarinaStudio.IO.PathEqualityComparer.Default);
        await GetSystemPathsAsync(SystemPathsScope.All, paths, cancellationToken);

        // add custom paths
        lock (customPaths)
            paths.AddAll(customPaths);

        // complete
        return paths;
    }


    // Generate value for PATH environment.
	static string GetPathValue(IEnumerable<string> paths)
	{
		var pathBuffer = new StringBuilder();
		foreach (var path in paths)
		{
			if (pathBuffer.Length > 0)
				pathBuffer.Append(Path.PathSeparator);
			pathBuffer.Append(path);
		}
		return pathBuffer.ToString();
	}


    /// <summary>
    /// Get paths defined by system asynchronously.
    /// </summary>
    /// <param name="scopes">Scopes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paths</returns>
    public static async Task<ISet<string>> GetSystemPathsAsync(SystemPathsScope scopes, CancellationToken cancellationToken = default)
    {
        var paths = new HashSet<string>(CarinaStudio.IO.PathEqualityComparer.Default);
        await GetSystemPathsAsync(scopes, paths, cancellationToken);
        return paths;
    }


    // Get paths defined by system.
    static async Task GetSystemPathsAsync(SystemPathsScope scopes, ISet<string> paths, CancellationToken cancellationToken = default)
    {
        if (scopes == 0)
            return;
        if (Platform.IsWindows)
        {
            if ((scopes & SystemPathsScope.User) != 0)
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)?.Split(Path.PathSeparator)?.Let(it => paths.AddAll(it));
            if ((scopes & SystemPathsScope.Global) != 0)
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine)?.Split(Path.PathSeparator)?.Let(it => paths.AddAll(it));
        }
        else if (Platform.IsLinux)
        {
            if ((scopes & SystemPathsScope.User) != 0)
                Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator)?.Let(it => paths.AddAll(it));
        }
        else if (Platform.IsMacOS)
        {
            if ((scopes & SystemPathsScope.Global) != 0)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        using var reader = new StreamReader("/etc/paths");
                        var path = reader.ReadLine();
                        while (path != null)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                throw new TaskCanceledException();
                            if (!string.IsNullOrWhiteSpace(path))
                                paths.Add(path);
                            path = reader.ReadLine();
                        }
                    }
                    catch
                    { }
                }, cancellationToken);
            }
        }
    }


    // Logger.
    static ILogger? Logger
    {
        get
        {
            logger ??= AppSuiteApplication.CurrentOrNull?.LoggerFactory?.CreateLogger(nameof(CommandSearchPaths));
            return logger;
        }
    }


    /// <summary>
    /// Update paths defined by system asynchronously.
    /// </summary>
    /// <param name="paths">Paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of updating paths. Result will be True if paths updated successfully.</returns>
    public static async Task<bool> SetSystemPathsAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        var newPaths = new HashSet<string>(paths, CarinaStudio.IO.PathEqualityComparer.Default);
        var currentPaths = await GetSystemPathsAsync(SystemPathsScope.All, cancellationToken);
		var success = true;
		if (!currentPaths.SetEquals(newPaths))
		{
			var sortedPaths = newPaths.ToArray().Also(it => Array.Sort(it));
			success = await Task.Run(async () =>
			{
				var tempFilePaths = new List<string>();
				try
				{
					if (Platform.IsMacOS)
					{
						// generate paths file
						var tempPathsFile = Path.GetTempFileName().Also(it => tempFilePaths.Add(it));
						using (var stream = new FileStream(tempPathsFile, FileMode.Create, FileAccess.ReadWrite))
						{
							using var writer = new StreamWriter(stream, Encoding.UTF8);
							for (int i = 0, count = sortedPaths.Length; i < count; ++i)
							{
								if (i > 0)
									writer.WriteLine();
								writer.Write(sortedPaths[i]);
							}
						}

						// generate apple script file
                        var title = AppSuiteApplication.CurrentOrNull?.GetString("CommandSearchPaths.UpdateSystemPaths") ?? "Update paths defined by system";
						var tempScriptFile = Path.GetTempFileName().Also(it => tempFilePaths.Add(it));
						using (var stream = new FileStream(tempScriptFile, FileMode.Create, FileAccess.ReadWrite))
						{
							using var writer = new StreamWriter(stream, Encoding.UTF8);
							writer.Write($"do shell script \"mv -f '{tempPathsFile}' '/etc/paths'\"");
							writer.Write($" with prompt \"{title}\"");
							writer.Write($" with administrator privileges");
						}

                        // cancellation check
                        if (cancellationToken.IsCancellationRequested)
                            throw new TaskCanceledException();

						// run apple script
						using var process = Process.Start(new ProcessStartInfo()
						{
							Arguments = tempScriptFile,
							CreateNoWindow = true,
							FileName = "osascript",
							RedirectStandardError = true,
							RedirectStandardOutput = true,
							UseShellExecute = false,
						});
						if (process != null)
						{
							await process.WaitForExitAsync();
							return process.ExitCode == 0;
						}
						else
						{
							Logger?.LogError("Unable to start osascript to update paths to system");
							return false;
						}
					}
					else if (Platform.IsWindows)
					{
						// separate into machine and user paths
						var currentMachinePaths = await GetSystemPathsAsync(SystemPathsScope.Global, cancellationToken);
						var currentUserPaths = await GetSystemPathsAsync(SystemPathsScope.User, cancellationToken);
						var machinePaths = new HashSet<string>(currentMachinePaths, CarinaStudio.IO.PathEqualityComparer.Default).Also(it =>
						{
							foreach (var path in it.ToArray())
							{
								if (!paths.Contains(path))
									it.Remove(path);
							}
						});
						var userPaths = new HashSet<string>(CarinaStudio.IO.PathEqualityComparer.Default).Also(it =>
						{
							foreach (var path in paths)
							{
								if (!machinePaths.Contains(path))
									it.Add(path);
							}
						});

						// change user paths
						if (!currentUserPaths.SetEquals(userPaths))
							Environment.SetEnvironmentVariable("Path", GetPathValue(userPaths), EnvironmentVariableTarget.User);

						// change machine paths
						if (!currentMachinePaths.SetEquals(machinePaths))
						{
							using var process = Process.Start(new ProcessStartInfo()
							{
								Arguments = $"/c setx /M Path \"{GetPathValue(machinePaths)}\"",
								CreateNoWindow = true,
								FileName = "cmd",
								UseShellExecute = true,
								Verb = "runas",
							});
							if (process != null)
							{
								await process.WaitForExitAsync();
								if (process.ExitCode != 0)
									return false;
							}
							else
							{
								Logger?.LogError("Unable to start cmd to update paths to system");
								return false;
							}
						}
						return true;
					}
					else
					{
                        Logger?.LogError("Unable to update paths to system");
						return false;
					}
				}
				catch (Exception ex)
				{
					Logger?.LogError(ex, "Failed to update paths to system");
					return false;
				}
				finally
				{
					foreach (var tempFilePath in tempFilePaths)
						Global.RunWithoutError(() => File.Delete(tempFilePath));
				}
			}, cancellationToken);
		}
        return success;
    }
}