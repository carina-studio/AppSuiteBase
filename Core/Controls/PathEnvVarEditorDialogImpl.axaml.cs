using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using CarinaStudio.AppSuite.IO;
using CarinaStudio.Collections;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to path environment variable.
/// </summary>
class PathEnvVarEditorDialogImpl : Dialog<IAppSuiteApplication>
{
	// Static fields.
	static readonly StyledProperty<bool> IsRefreshingPathsProperty = AvaloniaProperty.Register<PathEnvVarEditorDialogImpl, bool>("IsRefreshingPaths");
	static readonly StyledProperty<bool> IsSavingPathsProperty = AvaloniaProperty.Register<PathEnvVarEditorDialogImpl, bool>("IsSavingPaths");


	// Fields.
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
	readonly Avalonia.Controls.ListBox customPathListBox;
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
		this.customPathListBox = this.Get<Avalonia.Controls.ListBox>(nameof(customPathListBox)).Also(it =>
		{
			it.SelectionChanged += (_, e) =>
			{
				if (e.AddedItems.Count > 0)
					this.pathListBox?.Let(it => it.SelectedItem = null);
			};
		});
		this.pathListBox = this.Get<ListBox>(nameof(pathListBox)).Also(it =>
		{
			it.DoubleClickOnItem += (_, e) => this.EditPath(e.Item as string);
			it.SelectionChanged += (_, e) =>
			{
				if (e.AddedItems.Count > 0)
					this.customPathListBox.SelectedItem = null;
			};
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
		})).Let(it => it.Count == 1 ? it[0].TryGetLocalPath() : null);
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
			|| this.GetValue(IsRefreshingPathsProperty) 
			|| this.GetValue(IsSavingPathsProperty))
		{
			return;
		}
		var storage = await this.StorageProvider.TryGetFolderFromPathAsync(path);
		var newPath = (await this.StorageProvider.OpenFolderPickerAsync(new()
		{
			SuggestedStartLocation = storage,
			Title = this.Application.GetString("SystemPathEditorDialog.EditPath"),
		})).Let(it => it.Count == 1 ? it[0].TryGetLocalPath() : null);
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
	protected override void OnClosing(WindowClosingEventArgs e)
	{
		if (this.GetValue(IsSavingPathsProperty))
			e.Cancel = true;
		base.OnClosing(e);
	}


	/// <inheritdoc/>
	protected override void OnOpened(EventArgs e)
	{
		base.OnOpened(e);
		this.SynchronizationContext.Post(() => this.pathListBox.Focus());
	}


	// Refresh path list.
	async void RefreshPaths()
	{
		this.SetValue(IsRefreshingPathsProperty, true);
		var paths = await CommandSearchPaths.GetPathsAsync();
		this.paths.Clear();
		this.paths.AddAll(paths);
		this.SetValue(IsRefreshingPathsProperty, false);
	}


	// Remove path.
	void RemovePath(string? path)
	{
		if (path == null 
			||this.GetValue(IsRefreshingPathsProperty) 
			|| this.GetValue(IsSavingPathsProperty))
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
		this.SetValue(IsSavingPathsProperty, true);
		var success = await CommandSearchPaths.SetSystemPathsAsync(this.paths);
		this.SetValue(IsSavingPathsProperty, false);
		if (success)
			this.Close(true);
	}


	/// <summary>
	/// System path list.
	/// </summary>
	public IList<string> SystemPaths { get; }
}