using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls
{
	class SettingEditorDialog : Dialog
	{
		// Static fields.
		static readonly StyledProperty<SettingKey?> SettingKeyProperty = AvaloniaProperty.Register<SettingEditorDialog, SettingKey?>(nameof(SettingKey));
		static readonly StyledProperty<ISettings?> SettingsProperty = AvaloniaProperty.Register<SettingEditorDialog, ISettings?>(nameof(Settings));


		// Fields.
		readonly EnumComboBox enumComboBox;
		readonly IntegerTextBox integerTextBox;
		readonly NumericUpDown numericUpDown;
		readonly TextBox textBox;
		readonly ToggleSwitch toggleSwitch;


		// Constructor.
		public SettingEditorDialog()
		{
			AvaloniaXamlLoader.Load(this);
			this.enumComboBox = this.Get<EnumComboBox>(nameof(enumComboBox));
			this.integerTextBox = this.Get<IntegerTextBox>(nameof(integerTextBox));
			this.numericUpDown = this.Get<NumericUpDown>(nameof(numericUpDown));
			this.textBox = this.Get<TextBox>(nameof(this.textBox));
			this.toggleSwitch = this.Get<ToggleSwitch>(nameof(toggleSwitch));
		}


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
					this.numericUpDown.Value = (decimal)settings.GetValueOrDefault(key);
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


		/// <summary>
		/// Reset setting value.
		/// </summary>
		public void ResetValue()
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
			get => this.GetValue(SettingKeyProperty);
			set => this.SetValue(SettingKeyProperty, value);
		}


		// Settings.
		public new ISettings? Settings
		{
			get => this.GetValue(SettingsProperty);
			set => this.SetValue(SettingsProperty, value);
		}


		/// <summary>
		/// Update setting value.
		/// </summary>
		public void UpdateValue()
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
					settings.SetValue(key, (double)this.numericUpDown.Value.GetValueOrDefault());
				else if (type.IsEnum)
					settings.SetValue(key, this.enumComboBox.SelectedItem.AsNonNull());
				else if (type == typeof(int))
					settings.SetValue(key, (int)this.integerTextBox.Value.GetValueOrDefault());
				else if (type == typeof(string))
					settings.SetValue(key, this.textBox.Text ?? "");
#pragma warning restore CS0618
			}
			this.Close();
		}
	}
}
