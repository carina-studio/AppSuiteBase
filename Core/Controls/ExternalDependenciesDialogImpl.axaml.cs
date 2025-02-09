using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CarinaStudio.Threading;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Application info dialog.
/// </summary>
class ExternalDependenciesDialogImpl : Dialog<IAppSuiteApplication>
{
	// Converter to convert from priority to string.
	public static readonly IValueConverter PriorityConverter = new AppSuite.Converters.EnumConverter(IAppSuiteApplication.CurrentOrNull, typeof(ExternalDependencyPriority));


	// Converter to convert from state to brush.
	public static readonly IValueConverter StateBrushConverter = new FuncValueConverter<ExternalDependencyState, IBrush?>(state =>
	{
		var app = IAppSuiteApplication.CurrentOrNull;
		if (app is null)
			return null;
		IBrush? brush;
		switch (state)
		{
			case ExternalDependencyState.Available:
				app.TryFindResource("Brush/Icon.OK", out brush);
				break;
			case ExternalDependencyState.Unavailable:
				app.TryFindResource("Brush/Icon.Warning", out brush);
				break;
			case ExternalDependencyState.Unknown:
				app.TryFindResource("SystemControlForegroundBaseMediumBrush", out brush);
				break;
			default: 
				app.TryFindResource("SystemControlForegroundBaseHighBrush", out brush);
				break;
		}
		return brush;
	});


	// Converter to convert from state to string.
	public static readonly IMultiValueConverter StateConverter = new FuncMultiValueConverter<object, string?>(values =>
	{
		var app = IAppSuiteApplication.CurrentOrNull;
		if (app is null)
			return null;
		var type = ExternalDependencyType.Software;
		var state = ExternalDependencyState.Unknown;
		var i = 0;
		foreach (var value in values)
		{
			if (value is UnsetValueType)
			{
				++i;
				continue;
			}
			switch (i++)
			{
				case 0:
					type = (ExternalDependencyType)value!;
					break;
				case 1:
					state = (ExternalDependencyState)value!;
					break;
			}
		}
		if (state == ExternalDependencyState.Unknown)
			return app.GetString("ExternalDependencyState.Unknown");
		return app.GetString($"ExternalDependencyState.{state}.{type}") ?? state.ToString();
	});


	// Static fields.
	static readonly StyledProperty<bool> CanCloseProperty = AvaloniaProperty.Register<ExternalDependenciesDialogImpl, bool>("CanClose");


	// Fields.
	readonly ScheduledAction checkCanCloseAction;
	readonly ExternalDependency[] externalDependencies;
	readonly Panel externalDependenciesPanel;
	bool isFirstActivation = true;


	// Constructor.
	[DynamicDependency(nameof(Download))]
	[DynamicDependency(nameof(EditPathEnvironmentVariable))]
	[DynamicDependency(nameof(Refresh))]
	[DynamicDependency(nameof(ShowDetails))]
	public ExternalDependenciesDialogImpl()
	{
		AvaloniaXamlLoader.Load(this);
		this.checkCanCloseAction = new(() =>
		{
			var canClose = true;
			foreach (var externalDependency in this.externalDependencies!)
			{
				if (externalDependency.Priority == ExternalDependencyPriority.Required
					&& externalDependency.State != ExternalDependencyState.Available)
				{
					canClose = false;
					break;
				}
			}
			this.SetValue(CanCloseProperty, canClose);
		});
		this.externalDependencies = this.Application.ExternalDependencies.ToArray().Also(it =>
		{
			foreach (var externalDependency in it)
				externalDependency.PropertyChanged += this.OnExternalDependencyPropertyChanged;
			// ReSharper disable StringCompareIsCultureSpecific.1
			Array.Sort(it, (lhs, rhs) =>
			{
				if (lhs.State == ExternalDependencyState.Unavailable)
				{
					if (rhs.State == ExternalDependencyState.Unavailable)
						return string.Compare(lhs.Name, rhs.Name);
					return -1;
				}
				if (rhs.State == ExternalDependencyState.Unavailable)
					return 1;
				return string.Compare(lhs.Name, rhs.Name);
			});
			// ReSharper restore StringCompareIsCultureSpecific.1
		});
		this.externalDependenciesPanel = this.Get<Panel>(nameof(externalDependenciesPanel)).Also(panel =>
		{
			var template = this.DataTemplates[0].AsNonNull();
			var count = this.externalDependencies.Length;
			for (var i = 0; i < count; ++i)
			{
				panel.Children.Add(template.Build(this.externalDependencies[i])!.Also(it =>
				{
					// ReSharper disable once AccessToModifiedClosure
					it.DataContext = this.externalDependencies[i];
				}));
				if (i < count - 1)
				{
					panel.Children.Add(new Separator().Also(it =>
					{
						it.Classes.Add("Dialog_Separator_Large");
					}));
				}
			}
			panel.AddHandler(PointerPressedEvent, (_, e) =>
			{
				if (e.Source is not SelectableTextBlock)
					panel.Focus();
			}, RoutingStrategies.Tunnel);
		});
	}


#pragma warning disable CA1822
	/// <summary>
	/// Download given external dependency.
	/// </summary>
	public void Download(object? parameter)
	{
		if (parameter is not ExternalDependency externalDependency)
			return;
		externalDependency.InstallationUri?.Let(it => Platform.OpenLink(it));
	}
#pragma warning restore CA1822


	/// <summary>
	/// Edit path environment variable.
	/// </summary>
	public void EditPathEnvironmentVariable() =>
		_ = new PathEnvVarEditorDialog().ShowDialog(this);
	

	/// <summary>
	/// External dependency to be focused when showing dialog.
	/// </summary>
	public ExternalDependency? FocusedExternalDependency { get; init; }


	/// <inheritdoc/>
	protected override void OnClosed(EventArgs e)
	{
		foreach (var externalDependency in this.externalDependencies)
			externalDependency.PropertyChanged -= this.OnExternalDependencyPropertyChanged;
		base.OnClosed(e);
	}


	/// <inheritdoc/>
	protected override void OnClosing(WindowClosingEventArgs e)
	{
		if (!this.GetValue(CanCloseProperty))
			e.Cancel = true;
		base.OnClosing(e);
	}


	// Called when property of external dependency changed.
	void OnExternalDependencyPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ExternalDependency.State))
			this.checkCanCloseAction.Schedule(300);
	}


	/// <inheritdoc/>
	protected override void OnOpened(EventArgs e)
	{
		base.OnOpened(e);
		this.FocusedExternalDependency?.Let(extDependency =>
		{
			this.externalDependenciesPanel.Children.FirstOrDefault(it => it.DataContext == extDependency)?.Let(extDependencyItemPanel =>
			{
				CarinaStudio.Controls.ScrollViewerExtensions.ScrollIntoView(this.Get<ScrollViewer>("scrollViewer"), extDependencyItemPanel, true);
				this.AnimateItem(extDependencyItemPanel);
			});
		});
		this.checkCanCloseAction.Execute();
	}


	/// <inheritdoc/>
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == IsActiveProperty)
		{
			if ((bool)change.NewValue!)
			{
				if (!isFirstActivation)
					this.Refresh(false);
				else
				{
					this.Refresh(true);
					isFirstActivation = false;
				}
			}
		}
	}


	/// <summary>
	/// Refresh.
	/// </summary>
	public void Refresh() =>
		this.Refresh(true);
	

	// Refresh.
	void Refresh(bool refreshAll)
	{
		foreach (var externalDependency in this.externalDependencies)
		{
			if (refreshAll || externalDependency.State != ExternalDependencyState.Available)
				externalDependency.InvalidateAvailability();
		}
	}


#pragma warning disable CA1822
	/// <summary>
	/// Show details of external dependency.
	/// </summary>
	public void ShowDetails(object? parameter)
	{
		if (parameter is not ExternalDependency externalDependency)
			return;
		externalDependency.DetailsUri?.Let(it => Platform.OpenLink(it));
	}
#pragma warning restore CA1822
}