using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Styling;
using CarinaStudio.AppSuite.Converters;
using System;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// <see cref="ComboBox"/> to show enumeration values.
	/// </summary>
	public class EnumComboBox : ComboBox, IStyleable
	{
		/// <summary>
		/// Property of <see cref="EnumType"/>.
		/// </summary>
		public static readonly StyledProperty<Type> EnumTypeProperty = AvaloniaProperty.Register<EnumComboBox, Type>(nameof(EnumType), validate: it => it == null || it.IsEnum);


		// Fields.
		EnumConverter? enumConverter;
		Array? enumValues;


		/// <summary>
		/// Initialize new <see cref="EnumComboBox"/> instance.
		/// </summary>
		public EnumComboBox()
		{
			this.GetObservable(EnumTypeProperty).Subscribe(type =>
			{
				this.enumConverter = null;
				this.enumValues = null;
				this.Items = null;
				if (type != null)
				{
					this.enumConverter = new EnumConverter(AppSuiteApplication.Current, type);
					this.enumValues = Enum.GetValues(type);
					this.Items = this.enumValues;
				}
				this.UpdateItemTemplate();
			});
		}


		/// <summary>
		/// Get or set type of enumeration.
		/// </summary>
		public Type EnumType
		{
			get => this.GetValue<Type>(EnumTypeProperty);
			set => this.SetValue<Type>(EnumTypeProperty, value);
		}


		// Strings updated.
		void OnApplicationStringsUpdated(object? sender, EventArgs e) =>
			this.UpdateItemTemplate();


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


		// Interface implementations.
		Type IStyleable.StyleKey => typeof(ComboBox);


		// Update item template.
		void UpdateItemTemplate()
        {
			var type = this.EnumType;
			this.ItemTemplate = (type == null)
				? null
				: new DataTemplate()
				{
					Content = new Func<IServiceProvider, object>(_ =>
					{
						var textBlock = new TextBlock().Also(it =>
						{
							it.Bind(TextBlock.TextProperty, new Binding { Converter = this.enumConverter });
							it.TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis;
						});
						return new ControlTemplateResult(textBlock, null);
					}),
					DataType = type,
				};
		}
	}
}
