﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.LogicalTree;
using CarinaStudio.AppSuite.Converters;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="ComboBox"/> to show enumeration values.
/// </summary>
public class EnumComboBox : ComboBox
{
	/// <summary>
	/// Property of <see cref="Converter"/>.
	/// </summary>
	public static readonly StyledProperty<IValueConverter?> ConverterProperty = AvaloniaProperty.Register<EnumComboBox, IValueConverter?>(nameof(Converter));
	/// <summary>
	/// Property of <see cref="EnumType"/>.
	/// </summary>
	public static readonly StyledProperty<Type?> EnumTypeProperty = AvaloniaProperty.Register<EnumComboBox, Type?>(nameof(EnumType), validate: it => it is null || it.IsEnum);


	// Fields.
	IValueConverter? enumConverter;
	readonly ScheduledAction updateItemTemplateAction;


	/// <summary>
	/// Initialize new <see cref="EnumComboBox"/> instance.
	/// </summary>
	public EnumComboBox()
	{
		this.updateItemTemplateAction = new(this.UpdateItemTemplate);
		this.GetObservable(ConverterProperty).Subscribe(_ => this.UpdateConverter());
		this.GetObservable(EnumTypeProperty).Subscribe(type =>
		{
			this.ItemsSource = type is not null ? Enum.GetValues(type) : null;
			this.UpdateConverter();
			this.updateItemTemplateAction.Schedule();
		});
	}


	/// <summary>
	/// Get or set custom converter to convert from enumeration value to displayable string.
	/// </summary>
	public IValueConverter? Converter
	{
		get => this.GetValue(ConverterProperty);
		set => this.SetValue(ConverterProperty, value);
	}


	/// <summary>
	/// Get or set type of enumeration.
	/// </summary>
	public Type? EnumType
	{
		get => this.GetValue(EnumTypeProperty);
		set => this.SetValue(EnumTypeProperty, value);
	}


	// Strings updated.
	void OnApplicationStringsUpdated(object? sender, EventArgs e) =>
		this.updateItemTemplateAction.Schedule();


	/// <inheritdoc/>
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
		Application.CurrentOrNull?.Let(it => it.StringsUpdated += this.OnApplicationStringsUpdated);
    }


	/// <inheritdoc/>
	protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
		Application.CurrentOrNull?.Let(it => it.StringsUpdated -= this.OnApplicationStringsUpdated);
		base.OnDetachedFromLogicalTree(e);
    }


	/// <inheritdoc/>
	protected override Type StyleKeyOverride => typeof(ComboBox);
	
	
	// Update converter.
	void UpdateConverter()
	{
		var enumType = this.GetValue(EnumTypeProperty);
		if (enumType is null)
		{
			if (this.enumConverter != null)
			{
				this.enumConverter = null;
				this.updateItemTemplateAction.Schedule();
			}
			return;
		}
		var converter = this.GetValue(ConverterProperty) ?? new EnumConverter(AppSuiteApplication.CurrentOrNull, enumType);
		if (this.enumConverter != converter)
		{
			this.enumConverter = converter;
			this.updateItemTemplateAction.Schedule();
		}
	}


	// Update item template.
	void UpdateItemTemplate()
    {
		var type = this.EnumType;
		this.ItemTemplate = type is null
			? null
			: new FuncDataTemplate(type, (_, _) =>
			{
				return new TextBlock().Also(it =>
				{
#pragma warning disable IL2026
					it.Bind(TextBlock.TextProperty, new Binding { Converter = this.enumConverter });
#pragma warning restore IL2026
					it.TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis;
				});
			}, true);
	}
}
