using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls
{
	partial class SettingEditorDialog : Dialog
	{
		// Static fields.
		static readonly AvaloniaProperty<SettingKey?> SettingKeyProperty = AvaloniaProperty.Register<SettingEditorDialog, SettingKey?>(nameof(SettingKey));
		static readonly AvaloniaProperty<ISettings?> SettingsProperty = AvaloniaProperty.Register<SettingEditorDialog, ISettings?>(nameof(Settings));


		// Fields.
		readonly EnumComboBox enumComboBox;
		readonly IntegerTextBox integerTextBox;
		readonly NumericUpDown numericUpDown;
		readonly TextBox textBox;
		readonly ToggleSwitch toggleSwitch;


		// Constructor.
		public SettingEditorDialog()
		{
			InitializeComponent();
			this.enumComboBox = this.FindControl<EnumComboBox>(nameof(enumComboBox)).AsNonNull();
			this.integerTextBox = this.FindControl<IntegerTextBox>(nameof(integerTextBox)).AsNonNull();
			this.numericUpDown = this.FindControl<NumericUpDown>(nameof(numericUpDown)).AsNonNull();
			this.textBox = this.FindControl<TextBox>(nameof(this.textBox)).AsNonNull();
			this.toggleSwitch = this.FindControl<ToggleSwitch>(nameof(toggleSwitch)).AsNonNull();
		}


		// Initialize.
		private void InitializeComponent() => AvaloniaXamlLoader.Load(this);


		// Called when opened.
		protected override void OnOpened(EventArgs e)
		{
			base.OnOpened(e);
			var settings = this.Settings;
			var key = this.SettingKey;
			if (settings is not null && key is not null)
			{
#pragma warning disable CS0618
				var type = key.ValueType;
				if (type == typeof(bool))
				{
					this.toggleSwitch.IsChecked = (bool)settings.GetValueOrDefault(key);
					this.toggleSwitch.IsVisible = true;
				}
				else if (type == typeof(double))
				{
					this.numericUpDown.Value = (double)settings.GetValueOrDefault(key);
					this.numericUpDown.IsVisible = true;
				}
				else if (type.IsEnum)
				{
					this.enumComboBox.EnumType = type;
					this.enumComboBox.SelectedItem = settings.GetValueOrDefault(key);
					this.enumComboBox.IsVisible = true;
				}
				else if (type == typeof(int))
				{
					this.integerTextBox.Value = (int)settings.GetValueOrDefault(key);
					this.integerTextBox.IsVisible = true;
				}
				else if (type == typeof(string))
				{
					this.textBox.Text = settings.GetValueOrDefault(key) as string;
					this.textBox.IsVisible = true;
				}
				else
					this.SynchronizationContext.Post(this.Close);
#pragma warning restore CS0618
			}
			else
				this.SynchronizationContext.Post(this.Close);
		}


		// Reset setting value.
		void ResetValue()
		{
			var settings = this.Settings;
			var key = this.SettingKey;
			if (settings is not null && key is not null)
				settings.ResetValue(key);
			this.Close();
		}


		// Key of setting.
		public SettingKey? SettingKey
		{
			get => this.GetValue<SettingKey?>(SettingKeyProperty);
			set => this.SetValue<SettingKey?>(SettingKeyProperty, value);
		}


		// Settings.
		public new ISettings? Settings
		{
			get => this.GetValue<ISettings?>(SettingsProperty);
			set => this.SetValue<ISettings?>(SettingsProperty, value);
		}


		// Update setting value.
		void UpdateValue()
		{
			var settings = this.Settings;
			var key = this.SettingKey;
			if (settings is not null && key is not null)
			{
#pragma warning disable CS0618
				var type = key.ValueType;
				if (type == typeof(bool))
					settings.SetValue(key, this.toggleSwitch.IsChecked.GetValueOrDefault());
				if (type == typeof(double))
					settings.SetValue(key, this.numericUpDown.Value);
				else if (type.IsEnum)
					settings.SetValue(key, this.enumComboBox.SelectedItem.AsNonNull());
				else if (type == typeof(int))
					settings.SetValue(key, this.integerTextBox.Value.GetValueOrDefault());
				else if (type == typeof(string))
					settings.SetValue(key, this.textBox.Text ?? "");
#pragma warning restore CS0618
			}
			this.Close();
		}
	}
}
