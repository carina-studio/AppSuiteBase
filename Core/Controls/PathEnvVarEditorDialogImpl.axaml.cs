using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage.FileIO;
using CarinaStudio.AppSuite.IO;
using CarinaStudio.Collections;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Globalization;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to path environment variable.
/// </summary>
partial class PathEnvVarEditorDialogImpl : Dialog<IAppSuiteApplication>
{
	// Static fields.
	static readonly StyledProperty<bool> IsRefreshingPathsProperty = AvaloniaProperty.Register<PathEnvVarEditorDialogImpl, bool>("IsRefreshingPaths");
	static readonly StyledProperty<bool> IsSavingPathsProperty = AvaloniaProperty.Register<PathEnvVarEditorDialogImpl, bool>("IsSavingPaths");


	// Fields.
	readonly Avalonia.Controls.ListBox pathListBox;
	readonly SortedObservableList<string> paths = new((lhs, rhs) => string.Compare(lhs, rhs, true, CultureInfo.InvariantCulture));


	// Constructor.
	public PathEnvVarEditorDialogImpl()
	{
		this.CustomPaths = ListExtensions.AsReadOnly(new SortedObservableList<string>(CommandSearchPaths.CustomPaths));
		this.EditPathCommand = new Command<string?>(this.EditPath);
		this.SystemPaths = ListExtensions.AsReadOnly(this.paths);
		this.RemovePathCommand = new Command<string?>(this.RemovePath);
		AvaloniaXamlLoader.Load(this);
		this.pathListBox = this.Get<CarinaStudio.AppSuite.Controls.ListBox>(nameof(pathListBox)).Also(it =>
		{
			it.DoubleClickOnItem += (_, e) => this.EditPath(e.Item as string);
		});
		this.RefreshPaths();
	}


	/// <summary>
	/// Add new path.
	/// </summary>
	public async void AddPath()
	{
		var path = (await this.StorageProvider.OpenFolderPickerAsync(new()
		{
			Title = this.Application.GetString("SystemPathEditorDialog.AddPath"),
		})).Let(it => 
		{
			if (it == null || it.Count == 0 || !it[0].TryGetUri(out var uri))
				return null;
			return uri.LocalPath;
		});
		if (string.IsNullOrEmpty(path) || this.IsClosed)
			return;
		var index = this.paths.IndexOf(path);
		if (index < 0)
			index = this.paths.Add(path);
		this.pathListBox.SelectedIndex = index;
		this.pathListBox.Focus();
		this.SynchronizationContext.Post(() => this.pathListBox.ScrollIntoView(index));
	}


	/// <summary>
	/// Custom path list.
	/// </summary>
	public IList<string> CustomPaths { get; }


	// Edit path.
	async void EditPath(string? path)
	{
		if (path == null 
			|| this.GetValue<bool>(IsRefreshingPathsProperty) 
			|| this.GetValue<bool>(IsSavingPathsProperty))
		{
			return;
		}
		var newPath = (await this.StorageProvider.OpenFolderPickerAsync(new()
		{
			SuggestedStartLocation = new BclStorageFolder(path),
			Title = this.Application.GetString("SystemPathEditorDialog.EditPath"),
		})).Let(it => 
		{
			if (it == null || it.Count == 0 || !it[0].TryGetUri(out var uri))
				return null;
			return uri.LocalPath;
		});
		if (string.IsNullOrEmpty(newPath)
			|| this.IsClosed 
			|| CarinaStudio.IO.PathEqualityComparer.Default.Equals(path, newPath))
		{
			return;
		}
		this.paths.Remove(path);
		var index = this.paths.IndexOf(newPath);
		if (index < 0)
			index = this.paths.Add(newPath);
		this.pathListBox.SelectedIndex = index;
		this.pathListBox.Focus();
		this.SynchronizationContext.Post(() => this.pathListBox.ScrollIntoView(index));
	}


	/// <summary>
	/// Command to edit specific path.
	/// </summary>
	public ICommand EditPathCommand { get; }


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


	// Refresh path list.
	async void RefreshPaths()
	{
		this.SetValue<bool>(IsRefreshingPathsProperty, true);
		var paths = await CommandSearchPaths.GetPathsAsync();
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


	/// <summary>
	/// Command to remove specific path.
	/// </summary>
	public ICommand RemovePathCommand { get; }


	/// <summary>
	/// Save path to system and close dialog.
	/// </summary>
	public async void SaveAndClose()
	{
		this.SetValue<bool>(IsSavingPathsProperty, true);
		var success = await CommandSearchPaths.SetSystemPathsAsync(this.paths);
		this.SetValue<bool>(IsSavingPathsProperty, false);
		if (success)
			this.Close(true);
	}


	/// <summary>
	/// System path list.
	/// </summary>
	public IList<string> SystemPaths { get; }
}