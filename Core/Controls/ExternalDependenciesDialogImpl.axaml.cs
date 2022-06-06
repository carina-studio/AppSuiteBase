using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System;
using System.ComponentModel;
using System.Linq;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Application info dialog.
/// </summary>
partial class ExternalDependenciesDialogImpl : Dialog<IAppSuiteApplication>
{
	// Converter to convert from priority to string.
	public static readonly IValueConverter PriorityConverter = new AppSuite.Converters.EnumConverter(AppSuiteApplication.CurrentOrNull, typeof(ExternalDependencyPriority));


	// Converter to convert from state to brush.
	public static readonly IValueConverter StateBrushConverter = new FuncValueConverter<ExternalDependencyState, IBrush?>(state =>
	{
		var app = AppSuiteApplication.CurrentOrNull;
		if (app == null)
			return null;
		var brush = (IBrush?)null;
		switch (state)
		{
			case ExternalDependencyState.Available:
				app.TryFindResource<IBrush>("Brush/Icon.OK", out brush);
				break;
			case ExternalDependencyState.Unavailable:
				app.TryFindResource<IBrush>("Brush/Icon.Warning", out brush);
				break;
			case ExternalDependencyState.Unknown:
				app.TryFindResource<IBrush>("SystemControlForegroundBaseMediumBrush", out brush);
				break;
			default: 
				app.TryFindResource<IBrush>("SystemControlForegroundBaseHighBrush", out brush);
				break;
		}
		return brush;
	});


	// Converter to convert from state to icon.
	public static readonly IValueConverter StateIconConverter = new FuncValueConverter<ExternalDependencyState, IImage?>(state =>
	{
		var app = AppSuiteApplication.CurrentOrNull;
		if (app == null)
			return null;
		var icon = (IImage?)null;
		switch (state)
		{
			case ExternalDependencyState.Available:
				app.TryFindResource<IImage>("Image/Icon.OK.Outline.Colored", out icon);
				break;
			case ExternalDependencyState.CheckingForAvailability:
				app.TryFindResource<IImage>("Image/Icon.Waiting.Outline", out icon);
				break;
			case ExternalDependencyState.Unavailable:
				app.TryFindResource<IImage>("Image/Icon.Warning.Outline.Colored", out icon);
				break;
			case ExternalDependencyState.Unknown:
				app.TryFindResource<IImage>("Image/Icon.Question.Outline", out icon);
				break;
			default: 
				app.TryFindResource<IImage>("Image/Icon.Information.Outline", out icon);
				break;
		}
		return icon;
	});


	// Converter to convert from state to string.
	public static readonly IValueConverter StateConverter = new AppSuite.Converters.EnumConverter(AppSuiteApplication.CurrentOrNull, typeof(ExternalDependencyState));


	// Static fields.
	static readonly AvaloniaProperty<bool> CanCloseProperty = AvaloniaProperty.Register<ExternalDependenciesDialogImpl, bool>("CanClose");


	// Fields.
	readonly ScheduledAction checkCanCloseAction;
	readonly ExternalDependency[] externalDependencies;
	readonly Panel externalDependenciesPanel;
	bool isFirstActivation = true;


	// Constructor.
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
			this.SetValue<bool>(CanCloseProperty, canClose);
		});
		this.externalDependencies = this.Application.ExternalDependencies.ToArray().Also(it =>
		{
			foreach (var externalDependency in it)
				externalDependency.PropertyChanged += this.OnExternalDependencyPropertyChanged;
		});
		this.externalDependenciesPanel = this.Get<Panel>(nameof(externalDependenciesPanel)).Also(panel =>
		{
			var template = this.DataTemplates[0];
			var count = this.externalDependencies.Length;
			for (var i = 0; i < count; ++i)
			{
				panel.Children.Add(template.Build(this.externalDependencies[i]).Also(it =>
				{
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
		});
		this.GetObservable(IsActiveProperty).Subscribe(isActive =>
		{
			if (isActive)
			{
				if (!isFirstActivation)
					this.Refresh(false);
				else
					isFirstActivation = false;
			}
		});
	}


	/// <inheritdoc/>
	protected override void OnClosed(EventArgs e)
	{
		foreach (var externalDependency in this.externalDependencies)
			externalDependency.PropertyChanged -= this.OnExternalDependencyPropertyChanged;
		base.OnClosed(e);
	}


	/// <inheritdoc/>
	protected override void OnClosing(CancelEventArgs e)
	{
		if (!this.GetValue<bool>(CanCloseProperty))
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
		this.checkCanCloseAction.Execute();
	}


	// Refresh.
	void Refresh() =>
		this.Refresh(true);
	void Refresh(bool refreshAll)
	{
		foreach (var externalDependency in this.externalDependencies)
		{
			if (refreshAll || externalDependency.State != ExternalDependencyState.Available)
				externalDependency.InvalidateAvailability();
		}
	}
}