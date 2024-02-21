using CarinaStudio.Collections;
using CarinaStudio.IO;
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
    static readonly HashSet<string> customPaths = new(PathEqualityComparer.Default);
    static ILogger? logger;


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
    /// Find valid path of command to execute.
    /// </summary>
    /// <param name="command">Command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of finding path of command.</returns>
    public static string? FindCommandPath(string command, CancellationToken cancellationToken = default)
    {
	    if (!command.IsValidFilePath())
	    {
		    Logger?.LogError("Invalid command: '{command}'", command);
		    return null;
	    }
	    try
	    {
		    var paths = GetPaths(cancellationToken);
		    var commandPathBuffer = new StringBuilder();
		    foreach (var path in paths)
		    {
			    // check state
			    if (cancellationToken.IsCancellationRequested)
				    throw new TaskCanceledException();

			    // prepare
			    commandPathBuffer.Clear();
			    commandPathBuffer.Append(path);
			    commandPathBuffer.Append(Path.DirectorySeparatorChar);

			    // check without extension
			    commandPathBuffer.Append(command);
			    var candidatePath = commandPathBuffer.ToString();
			    if (System.IO.File.Exists(candidatePath))
				    return candidatePath;

			    // check with specific extensions
			    if (Platform.IsWindows)
			    {
				    // .exe
				    commandPathBuffer.Append(".exe");
				    candidatePath = commandPathBuffer.ToString();
				    if (System.IO.File.Exists(candidatePath))
					    return candidatePath;

				    // .cmd
				    commandPathBuffer.Remove(commandPathBuffer.Length - 4, 4);
				    commandPathBuffer.Append(".cmd");
				    candidatePath = commandPathBuffer.ToString();
				    if (System.IO.File.Exists(candidatePath))
					    return candidatePath;

				    // .bat
				    commandPathBuffer.Remove(commandPathBuffer.Length - 4, 4);
				    commandPathBuffer.Append(".bat");
				    candidatePath = commandPathBuffer.ToString();
				    if (System.IO.File.Exists(candidatePath))
					    return candidatePath;
			    }
		    }
		    return null;
	    }
	    catch (Exception ex)
	    {
		    if (ex is TaskCanceledException)
			    throw;
		    Logger?.LogError(ex, "Error occurred while finding path of command '{command}'", command);
	    }
	    return null;
    }


    /// <summary>
    /// Find valid path of command to execute.
    /// </summary>
    /// <param name="command">Command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of finding path of command.</returns>
    public static Task<string?> FindCommandPathAsync(string command, CancellationToken cancellationToken = default) =>
	    Task.Run(() => FindCommandPath(command, cancellationToken), cancellationToken);


    /// <summary>
    /// Get all paths.
    /// </summary>
    /// <returns>Set of paths.</returns>
    public static ISet<string> GetPaths(CancellationToken cancellationToken = default)
    {
	    // add system paths
	    var paths = new HashSet<string>(PathEqualityComparer.Default);
	    GetSystemPaths(SystemPathsScope.All, paths, cancellationToken);

	    // add custom paths
	    lock (customPaths)
		    paths.AddAll(customPaths);

	    // complete
	    return paths;
    }


    /// <summary>
    /// Get all paths asynchronously.
    /// </summary>
    /// <returns>Set of paths.</returns>
    public static Task<ISet<string>> GetPathsAsync(CancellationToken cancellationToken = default) =>
	    Task.Run(() => GetPaths(cancellationToken), cancellationToken);


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
	
	
	// Get paths defined by system.
	static void GetSystemPaths(SystemPathsScope scopes, ISet<string> paths, CancellationToken cancellationToken = default)
	{
		if (scopes == 0)
			return;
		if (Platform.IsWindows)
		{
			if ((scopes & SystemPathsScope.User) != 0)
				Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)?.Split(Path.PathSeparator).Let(paths.AddAll);
			if ((scopes & SystemPathsScope.Global) != 0)
				Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine)?.Split(Path.PathSeparator).Let(paths.AddAll);
		}
		else if (Platform.IsLinux)
		{
			if ((scopes & SystemPathsScope.User) != 0)
				Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator).Let(paths.AddAll);
		}
		else if (Platform.IsMacOS)
		{
			if ((scopes & SystemPathsScope.Global) != 0)
			{
				try
				{
					using var reader = new StreamReader("/etc/paths");
					var path = reader.ReadLine();
					while (path is not null)
					{
						if (cancellationToken.IsCancellationRequested)
							throw new TaskCanceledException();
						if (!string.IsNullOrWhiteSpace(path))
							paths.Add(path);
						path = reader.ReadLine();
					}
				}
				// ReSharper disable EmptyGeneralCatchClause
				catch
				{ }
				// ReSharper restore EmptyGeneralCatchClause
			}
		}
		paths.RemoveAll(string.IsNullOrWhiteSpace);
	}


    /// <summary>
    /// Get paths defined by system asynchronously.
    /// </summary>
    /// <param name="scopes">Scopes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paths</returns>
    public static async Task<ISet<string>> GetSystemPathsAsync(SystemPathsScope scopes, CancellationToken cancellationToken = default)
    {
        var paths = new HashSet<string>(PathEqualityComparer.Default);
        await Task.Run(() => GetSystemPaths(scopes, paths, cancellationToken), cancellationToken);
        return paths;
    }


    // Logger.
    static ILogger? Logger
    {
        get
        {
            logger ??= AppSuiteApplication.CurrentOrNull?.LoggerFactory.CreateLogger(nameof(CommandSearchPaths));
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
        var newPaths = new HashSet<string>(paths, PathEqualityComparer.Default);
        var currentPaths = await GetSystemPathsAsync(SystemPathsScope.All, cancellationToken);
		var success = true;
		if (!currentPaths.SetEquals(newPaths))
		{
			var sortedPaths = newPaths.ToArray().Also(Array.Sort);
			success = await Task.Run(async () =>
			{
				var tempFilePaths = new List<string>();
				try
				{
					if (Platform.IsMacOS)
					{
						// generate paths file
						var tempPathsFile = Path.GetTempFileName().Also(it => tempFilePaths.Add(it));
						await using (var stream = new FileStream(tempPathsFile, FileMode.Create, FileAccess.ReadWrite))
						{
							await using var writer = new StreamWriter(stream, Encoding.UTF8);
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
						await using (var stream = new FileStream(tempScriptFile, FileMode.Create, FileAccess.ReadWrite))
						{
							await using var writer = new StreamWriter(stream, Encoding.UTF8);
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
						if (process is not null)
						{
							await process.WaitForExitAsync(CancellationToken.None);
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
						var machinePaths = new HashSet<string>(currentMachinePaths, PathEqualityComparer.Default).Also(it =>
						{
							foreach (var path in it.ToArray())
							{
								if (!paths.Contains(path))
									it.Remove(path);
							}
						});
						var userPaths = new HashSet<string>(PathEqualityComparer.Default).Also(it =>
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
							if (process is not null)
							{
								await process.WaitForExitAsync(CancellationToken.None);
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
						Global.RunWithoutError(() => System.IO.File.Delete(tempFilePath));
				}
			}, cancellationToken);
		}
        return success;
    }
}