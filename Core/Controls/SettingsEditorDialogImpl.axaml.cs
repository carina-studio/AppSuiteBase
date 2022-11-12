using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CarinaStudio.AppSuite.Controls
{
	partial class SettingsEditorDialogImpl : Dialog
	{
		// Fields.
		readonly SortedObservableList<Tuple<SettingKey, object>> settingKeyValues = new((x, y) => string.Compare(x?.Item1?.Name, y?.Item1?.Name, true, CultureInfo.InvariantCulture));
		readonly ListBox settingsListBox;


		// Constructor.
		public SettingsEditorDialogImpl()
		{
			this.SettingKeyValues = ListExtensions.AsReadOnly(this.settingKeyValues);
			this.Settings = base.Settings;
			AvaloniaXamlLoader.Load(this);
			this.settingsListBox = this.Get<ListBox>(nameof(settingsListBox));
		}


		// Called when closing.
		protected override void OnClosed(EventArgs e)
		{
			this.Settings.SettingChanged -= this.OnSettingChanged;
			base.OnClosed(e);
		}


		// Called when double click on list box.
		async void OnSettingsListBoxDoubleClickOnItem(object? sender, ListBoxItemEventArgs e)
		{
			// find setting
			if (e.Item is not Tuple<SettingKey, object> setting)
				return;
			e.Handled = true;

			// edit setting
			await new SettingEditorDialog()
			{
				SettingKey = setting.Item1,
				Settings = this.Settings,
			}.ShowDialog(this);

			// select item again
			this.settingsListBox.Focus();
			if (e.ItemIndex >= 0)
				this.settingsListBox.SelectedIndex = e.ItemIndex;
		}


		// Called when opened.
		protected override void OnOpened(EventArgs e)
		{
			base.OnOpened(e);
			var settings = this.Settings;
			foreach (var key in this.SettingKeys)
			{
#pragma warning disable CS0618
				this.settingKeyValues.Add(new Tuple<SettingKey, object>(key, settings.GetValueOrDefault(key)));
#pragma warning restore CS0618
			}
			if (this.settingKeyValues.IsEmpty())
			{
				this.SynchronizationContext.Post(this.Close);
				return;
			}
			settings.SettingChanged += this.OnSettingChanged;
			this.SynchronizationContext.Post(this.settingsListBox.Focus);
		}


		// Called when setting changed.
		void OnSettingChanged(object? sender, SettingChangedEventArgs e)
		{
			for (var i = this.settingKeyValues.Count - 1 ; i >= 0 ; --i)
			{
				var setting = this.settingKeyValues[i];
				if (setting.Item1 == e.Key)
				{
					this.settingKeyValues.RemoveAt(i);
					this.settingKeyValues.Add(new Tuple<SettingKey, object>(e.Key, e.Value));
					break;
				}
			}
		}


		/// <summary>
		/// Reset setting values.
		/// </summary>
		public void ResetValues()
		{
			var settings = this.Settings;
			foreach (var setting in this.settingKeyValues)
				settings.ResetValue(setting.Item1);
			this.Close();
		}


		// List of key of setting.
		public ISet<SettingKey> SettingKeys { get; set; } = SetExtensions.AsReadOnly(new HashSet<SettingKey>());


		/// <summary>
		/// Key-value of settings.
		/// </summary>
		public IList<Tuple<SettingKey, object>> SettingKeyValues { get; }


		// Settings.
		public new ISettings Settings { get; set; }
	}
}
