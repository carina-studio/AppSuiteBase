using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.ComponentModel;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// Dialog for application update.
	/// </summary>
	partial class ApplicationUpdateDialogImpl : Dialog<IAppSuiteApplication>
	{
		// Static fields.
		public static readonly SettingKey<DateTime> LatestNotifiedTimeKey = new SettingKey<DateTime>("LatestNotifiedTime", DateTime.MinValue);
		public static readonly SettingKey<string> LatestNotifiedVersionKey = new SettingKey<string>("LatestNotifiedVersion", "");
		public static readonly AvaloniaProperty<string?> LatestVersionMessageProperty = AvaloniaProperty.Register<ApplicationUpdateDialogImpl, string?>(nameof(LatestVersionMessage));
		public static readonly AvaloniaProperty<string?> NewVersionFoundMessageProperty = AvaloniaProperty.Register<ApplicationUpdateDialogImpl, string?>(nameof(NewVersionFoundMessage));


		// Fields.
		bool isClosingRequested;


		/// <summary>
		/// Initialize new <see cref="ApplicationUpdateDialogImpl"/> instance.
		/// </summary>
		public ApplicationUpdateDialogImpl()
		{
			InitializeComponent();
		}


		/// <summary>
		/// Get or set whether performing update checking when opening dialog or not.
		/// </summary>
		public bool CheckForUpdateWhenOpening { get; set; }


		// Download update package.
		void DownloadUpdatePackage()
		{
			if (this.DataContext is ApplicationUpdater updater && updater.UpdatePackageUri != null)
			{
				Platform.OpenLink(updater.UpdatePackageUri);
				this.Close();
			}
		}


		// Initialize.
		private void InitializeComponent() => AvaloniaXamlLoader.Load(this);


		// Message of latest application version.
		string? LatestVersionMessage { get => this.GetValue<string?>(LatestVersionMessageProperty); }


		// Message of new application version found.
		string? NewVersionFoundMessage { get => this.GetValue<string?>(NewVersionFoundMessageProperty); }


		// String resources updated.
		void OnApplicationStringsUpdated(object? sender, EventArgs e)
		{
			this.UpdateMessages();
		}


		// Called when closed.
		protected override void OnClosed(EventArgs e)
		{
			this.DataContext = null;
			this.Application.StringsUpdated -= this.OnApplicationStringsUpdated;
			base.OnClosed(e);
		}


		// Called when closing.
		protected override void OnClosing(CancelEventArgs e)
		{
			if (this.DataContext is ApplicationUpdater updater)
			{
				if (updater.IsPreparingForUpdate)
				{
					e.Cancel = true;
					this.isClosingRequested = true;
					updater.CancelUpdatingCommand.TryExecute();
				}
			}
			base.OnClosing(e);
		}


		// Called when error message generated.
		void OnErrorMessageGenerated(object? sender, MessageEventArgs e)
		{
			_ = new MessageDialog()
			{
				Icon = MessageDialogIcon.Error,
				Message = e.Message,
			}.ShowDialog(this);
		}


		// Called when opened.
		protected override void OnOpened(EventArgs e)
		{
			// add handlers
			this.Application.StringsUpdated += this.OnApplicationStringsUpdated;

			// setup UI
			this.UpdateLatestNotifiedInfo();
			this.UpdateMessages();

			// check for update
			if (this.CheckForUpdateWhenOpening && this.DataContext is ApplicationUpdater updater)
				updater.CheckForUpdateCommand.TryExecute();
			
			// setup default button
			this.SynchronizationContext.Post(() =>
			{
				if ((this.DataContext as ApplicationUpdater)?.IsLatestVersion == false)
				{
					this.FindControl<Button>("startUpdatingButton")?.Let(it =>
					{
						if (it.IsEffectivelyVisible)
							it.Focus();
					});
					this.FindControl<Button>("downloadUpdatePackageButton")?.Let(it =>
					{
						if (it.IsEffectivelyVisible)
							it.Focus();
					});
				}
			});
			
			// call base
			base.OnOpened(e);
		}


		// Property changed.
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);
			if(change.Property == DataContextProperty)
            {
				(change.OldValue.Value as ApplicationUpdater)?.Let(updater =>
				{
					updater.ErrorMessageGenerated -= this.OnErrorMessageGenerated;
					updater.PropertyChanged -= this.OnUpdaterPropertyChanged;
				});
				(change.NewValue.Value as ApplicationUpdater)?.Let(updater =>
				{
					updater.ErrorMessageGenerated += this.OnErrorMessageGenerated;
					updater.PropertyChanged += this.OnUpdaterPropertyChanged;
				});
			}
        }


		// Property of updater changed.
		void OnUpdaterPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is not ApplicationUpdater updater)
				return;
			if (e.PropertyName == nameof(ApplicationUpdater.IsPreparingForUpdate)
				&& !updater.IsPreparingForUpdate
				&& this.isClosingRequested)
			{
				this.Close(ApplicationUpdateDialogResult.UpdatingCancelled);
			}
			else if (e.PropertyName == nameof(ApplicationUpdater.IsLatestVersion))
			{
				this.SynchronizationContext.Post(() =>
				{
					if (!updater.IsLatestVersion)
					{
						this.FindControl<Button>("startUpdatingButton")?.Let(it =>
						{
							if (it.IsEffectivelyVisible)
								it.Focus();
						});
						this.FindControl<Button>("downloadUpdatePackageButton")?.Let(it =>
						{
							if (it.IsEffectivelyVisible)
								it.Focus();
						});
					}
				});
			}
			else if (e.PropertyName == nameof(ApplicationUpdater.IsShutdownNeededToContinueUpdate))
			{
				if (updater.IsShutdownNeededToContinueUpdate)
					this.Close(ApplicationUpdateDialogResult.ShutdownNeeded);
			}
			else if (e.PropertyName == nameof(ApplicationUpdater.UpdateVersion))
			{
				this.UpdateLatestNotifiedInfo();
				this.UpdateMessages();
			}
		}


		// Update latest info shown to user.
		void UpdateLatestNotifiedInfo()
        {
			if (this.DataContext is not ApplicationUpdater updater)
				return;
			var version = updater.UpdateVersion;
			if (version != null)
			{
				this.PersistentState.SetValue<DateTime>(LatestNotifiedTimeKey, DateTime.Now);
				this.PersistentState.SetValue<string>(LatestNotifiedVersionKey, version.ToString());
			}
			else
			{
				this.PersistentState.ResetValue(LatestNotifiedTimeKey);
				this.PersistentState.ResetValue(LatestNotifiedVersionKey);
			}
		}


		// Update messages.
		void UpdateMessages()
        {
			this.SetValue<string?>(LatestVersionMessageProperty, this.Application.GetFormattedString("ApplicationUpdateDialog.LatestVersion", this.Application.Name));
			if (this.DataContext is ApplicationUpdater updater)
				this.SetValue<string?>(NewVersionFoundMessageProperty, this.Application.GetFormattedString("ApplicationUpdateDialog.NewVersionFound", this.Application.Name, updater.UpdateVersion));
			else
				this.SetValue<string?>(NewVersionFoundMessageProperty, null);
        }
    }
}
