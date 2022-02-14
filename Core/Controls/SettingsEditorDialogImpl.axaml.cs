using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CarinaStudio.AppSuite.Controls
{
	partial class SettingsEditorDialogImpl : Dialog
	{
		// Fields.
		long settingsListBoxDoubleTappedTime;
		readonly Stopwatch stopwatch = new Stopwatch();
		readonly SortedObservableList<Tuple<SettingKey, object>> settingKeyValues = new SortedObservableList<Tuple<SettingKey, object>>((x, y) => string.Compare(x?.Item1?.Name, y?.Item1?.Name));
		readonly ListBox settingsListBox;


		// Constructor.
		public SettingsEditorDialogImpl()
		{
			this.SettingKeyValues = this.settingKeyValues.AsReadOnly();
			this.Settings = base.Settings;
			InitializeComponent();
			this.settingsListBox = this.FindControl<ListBox>(nameof(settingsListBox));
		}


		// Initialize.
		private void InitializeComponent() => AvaloniaXamlLoader.Load(this);


		// Called when closing.
		protected override void OnClosed(EventArgs e)
		{
			this.Settings.SettingChanged -= this.OnSettingChanged;
			this.stopwatch.Stop();
			base.OnClosed(e);
		}


		// Called when double click on list box.
		void OnSettingsListBoxDoubleTapped(object? sender, RoutedEventArgs e)
		{
			if (this.settingsListBox.SelectedItem != null && settingsListBox.IsPointerOver)
			{
				this.settingsListBoxDoubleTappedTime = this.stopwatch.ElapsedMilliseconds;
				e.Handled = true;
			}
		}


		// Called when pointer released on list box.
		async void OnSettingsListBoxPointerReleased(object? sender, PointerReleasedEventArgs e)
		{
			// check state
			if (this.settingsListBoxDoubleTappedTime <= 0)
				return;
			if ((this.stopwatch.ElapsedMilliseconds - this.settingsListBoxDoubleTappedTime) > 500)
			{
				this.settingsListBoxDoubleTappedTime = 0;
				return;
			}
			this.settingsListBoxDoubleTappedTime = 0;
			if (e.InitialPressMouseButton != MouseButton.Left)
				return;

			// find setting
			var selectedIndex = this.settingsListBox.SelectedIndex;
			var selectedItem = this.settingsListBox.SelectedItem;
			if (selectedItem is not Tuple<SettingKey, object> setting || !settingsListBox.IsPointerOver)
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
			if (selectedIndex >= 0)
				this.settingsListBox.SelectedIndex = selectedIndex;
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
			this.stopwatch.Start();
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


		// Reset setting values.
		void ResetValues()
		{
			var settings = this.Settings;
			foreach (var setting in this.settingKeyValues)
				settings.ResetValue(setting.Item1);
			this.Close();
		}


		// List of key of setting.
		public ISet<SettingKey> SettingKeys { get; set; } = new HashSet<SettingKey>().AsReadOnly();


		// Key-value of settings.
		IList<Tuple<SettingKey, object>> SettingKeyValues { get; }


		// Settings.
		public new ISettings Settings { get; set; }
	}
}
