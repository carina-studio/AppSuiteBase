using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to path environment variable.
/// </summary>
partial class PathEnvVarEditorDialogImpl : Dialog<IAppSuiteApplication>
{
	// Static fields.
	static readonly AvaloniaProperty<bool> IsRefreshingPathsProperty = AvaloniaProperty.Register<PathEnvVarEditorDialogImpl, bool>("IsRefreshingPaths");
	static readonly AvaloniaProperty<bool> IsSavingPathsProperty = AvaloniaProperty.Register<PathEnvVarEditorDialogImpl, bool>("IsSavingPaths");


	// Fields.
	readonly Avalonia.Controls.ListBox pathListBox;
	readonly SortedObservableList<string> paths = new((lhs, rhs) => string.Compare(lhs, rhs, true, CultureInfo.InvariantCulture));


	// Constructor.
	public PathEnvVarEditorDialogImpl()
	{
		this.Paths = this.paths.AsReadOnly();
		AvaloniaXamlLoader.Load(this);
		this.pathListBox = this.Get<CarinaStudio.AppSuite.Controls.ListBox>(nameof(pathListBox)).Also(it =>
		{
			it.DoubleClickOnItem += (_, e) => this.EditPath(e.Item as string);
		});
		this.RefreshPaths();
	}


	// Add new path.
	async void AddPath()
	{
		var path = await new OpenFolderDialog()
		{
			Title = this.Application.GetString("SystemPathEditorDialog.AddPath"),
		}.ShowAsync(this);
		if (string.IsNullOrEmpty(path) || this.IsClosed)
			return;
		var index = this.paths.IndexOf(path);
		if (index < 0)
			index = this.paths.Add(path);
		this.pathListBox.SelectedIndex = index;
		this.pathListBox.Focus();
		this.SynchronizationContext.Post(() => this.pathListBox.ScrollIntoView(index));
	}


	// Edit path.
	async void EditPath(string? path)
	{
		if (path == null 
			|| this.GetValue<bool>(IsRefreshingPathsProperty) 
			|| this.GetValue<bool>(IsSavingPathsProperty))
		{
			return;
		}
		var newPath = await new OpenFolderDialog()
		{
			Directory = path,
			Title = this.Application.GetString("SystemPathEditorDialog.EditPath"),
		}.ShowAsync(this);
		if (string.IsNullOrEmpty(newPath) 
			|| this.IsClosed 
			|| CarinaStudio.IO.PathEqualityComparer.Default.Equals(path, newPath))
		{
			return;
		}
		this.paths.Remove(path);
		var index = this.paths.IndexOf(newPath);
		if (index < 0)
			index = this.paths.Add(path);
		this.pathListBox.SelectedIndex = index;
		this.pathListBox.Focus();
		this.SynchronizationContext.Post(() => this.pathListBox.ScrollIntoView(index));
	}


	// Get path list from system.
	Task<HashSet<string>> GetPathsAsync(bool includeGlobal = true, bool includeUser = true) => Task.Run(() =>
	{
		var pathSet = new HashSet<string>(CarinaStudio.IO.PathEqualityComparer.Default);
		if (Platform.IsMacOS)
		{
			if (includeGlobal)
			{
				try
				{
					using var reader = new StreamReader("/etc/paths");
					var paths = new List<string>();
					var path = reader.ReadLine();
					while (path != null)
					{
						if (!string.IsNullOrWhiteSpace(path))
							pathSet.Add(path);
						path = reader.ReadLine();
					}
				}
				catch
				{ }
			}
		}
		else if (Platform.IsWindows)
		{
			if (includeGlobal)
			{
				Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine)?.Split(Path.PathSeparator)?.Let(it =>
				{
					foreach (var path in it)
					{
						if (!string.IsNullOrWhiteSpace(path))
							pathSet.Add(path);
					}
				});
			}
			if (includeUser)
			{
				Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User)?.Split(Path.PathSeparator)?.Let(it =>
				{
					foreach (var path in it)
					{
						if (!string.IsNullOrWhiteSpace(path))
							pathSet.Add(path);
					}
				});
			}
		}
		else
		{
			Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator)?.Let(it =>
			{
				foreach (var path in it)
				{
					if (!string.IsNullOrWhiteSpace(path))
						pathSet.Add(path);
				}
			});
		}
		return pathSet;
	});


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


	/// <inheritdoc/>
	protected override void OnClosing(CancelEventArgs e)
	{
		if (this.GetValue<bool>(IsSavingPathsProperty))
			e.Cancel = true;
		base.OnClosing(e);
	}


	/// <inheritdoc/>
	protected override void OnOpened(EventArgs e)
	{
		base.OnOpened(e);
		this.SynchronizationContext.Post(this.pathListBox.Focus);
	}


	// Path list.
	IList<string> Paths { get; }


	// Refresh path list.
	async void RefreshPaths()
	{
		this.SetValue<bool>(IsRefreshingPathsProperty, true);
		var paths = await this.GetPathsAsync();
		this.paths.Clear();
		this.paths.AddAll(paths);
		this.SetValue<bool>(IsRefreshingPathsProperty, false);
	}


	// Remove path.
	void RemovePath(string? path)
	{
		if (path == null 
			||this.GetValue<bool>(IsRefreshingPathsProperty) 
			|| this.GetValue<bool>(IsSavingPathsProperty))
		{
			return;
		}
		this.paths.Remove(path);
		this.pathListBox.SelectedIndex = -1;
		this.pathListBox.Focus();
	}


	// Save path to system and close dialog.
	async void SaveAndClose()
	{
		this.SetValue<bool>(IsSavingPathsProperty, true);
		var currentPaths = await this.GetPathsAsync();
		var success = true;
		if (!currentPaths.SetEquals(this.paths))
		{
			var paths = new SortedObservableList<string>(CarinaStudio.IO.PathComparer.Default, this.paths);
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
							for (int i = 0, count = paths.Count; i < count; ++i)
							{
								if (i > 0)
									writer.WriteLine();
								writer.Write(paths[i]);
							}
						}

						// generate apple script file
						var tempScriptFile = Path.GetTempFileName().Also(it => tempFilePaths.Add(it));
						using (var stream = new FileStream(tempScriptFile, FileMode.Create, FileAccess.ReadWrite))
						{
							using var writer = new StreamWriter(stream, Encoding.UTF8);
							writer.Write($"do shell script \"mv -f '{tempPathsFile}' '/etc/paths'\"");
							writer.Write($" with prompt \"{this.Application.GetString("SystemPathEditorDialog.Title")}\"");
							writer.Write($" with administrator privileges");
						}

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
							this.Logger.LogError("Unable to start osascript to save paths to system");
							return false;
						}
					}
					else if (Platform.IsWindows)
					{
						// separate into machine and user paths
						var currentMachinePaths = await this.GetPathsAsync(true, false);
						var currentUserPaths = await this.GetPathsAsync(false, true);
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
								this.Logger.LogError("Unable to start cmd to save paths to system");
								return false;
							}
						}
						return true;
					}
					else
					{
						return false;
					}
				}
				catch (Exception ex)
				{
					this.Logger.LogError(ex, "Failed to save paths to system");
					return false;
				}
				finally
				{
					foreach (var tempFilePath in tempFilePaths)
						Global.RunWithoutError(() => File.Delete(tempFilePath));
				}
			});
		}
		this.SetValue<bool>(IsSavingPathsProperty, false);
		if (success)
			this.Close(true);
	}
}