using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// Dialog for application update.
	/// </summary>
	partial class ApplicationUpdateDialogImpl : Dialog<IAppSuiteApplication>
	{
		// Static fields.
		public static readonly SettingKey<DateTime> LatestNotifiedTimeKey = new("LatestNotifiedTime", DateTime.MinValue);
		public static readonly SettingKey<string> LatestNotifiedVersionKey = new("LatestNotifiedVersion", "");
		public static readonly AvaloniaProperty<string?> LatestVersionMessageProperty = AvaloniaProperty.Register<ApplicationUpdateDialogImpl, string?>(nameof(LatestVersionMessage));
		public static readonly AvaloniaProperty<string?> NewVersionFoundMessageProperty = AvaloniaProperty.Register<ApplicationUpdateDialogImpl, string?>(nameof(NewVersionFoundMessage));


		// Fields.
		ApplicationUpdater? attachedAppUpdater;
		bool isClosingRequested;
		readonly TaskCompletionSource<ApplicationUpdateDialogResult> closingTaskSource = new();


		/// <summary>
		/// Initialize new <see cref="ApplicationUpdateDialogImpl"/> instance.
		/// </summary>
		public ApplicationUpdateDialogImpl()
		{
			AvaloniaXamlLoader.Load(this);
		}


		/// <summary>
		/// Get or set whether performing update checking when opening dialog or not.
		/// </summary>
		public bool CheckForUpdateWhenOpening { get; set; }


		/// <summary>
		/// Download update package.
		/// </summary>
		public void DownloadUpdatePackage()
		{
			if (this.attachedAppUpdater != null && this.attachedAppUpdater.UpdatePackageUri != null)
			{
				Platform.OpenLink(this.attachedAppUpdater.UpdatePackageUri);
				this.Close();
				this.closingTaskSource.SetResult(ApplicationUpdateDialogResult.None);
			}
		}


		// Message of latest application version.
		string? LatestVersionMessage { get => this.GetValue<string?>(LatestVersionMessageProperty); }


		// Message of new application version found.
		string? NewVersionFoundMessage { get => this.GetValue<string?>(NewVersionFoundMessageProperty); }


		// Called when closed.
		protected override void OnClosed(EventArgs e)
		{
			this.DataContext = null;
			this.closingTaskSource.TrySetResult(ApplicationUpdateDialogResult.None);
			base.OnClosed(e);
		}


		// Called when closing.
		protected override void OnClosing(CancelEventArgs e)
		{
			if (this.attachedAppUpdater != null)
			{
				if (this.attachedAppUpdater.IsPreparingForUpdate)
				{
					e.Cancel = true;
					this.isClosingRequested = true;
					this.attachedAppUpdater.CancelUpdatingCommand.TryExecute();
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


		/// <inheritdoc/>
		protected override void OnDataContextChanged(EventArgs e)
		{
			base.OnDataContextChanged(e);
			if (this.attachedAppUpdater != null)
			{
				this.attachedAppUpdater.ErrorMessageGenerated -= this.OnErrorMessageGenerated;
				this.attachedAppUpdater.PropertyChanged -= this.OnUpdaterPropertyChanged;
			}
			this.attachedAppUpdater = this.DataContext as ApplicationUpdater;
			if (this.attachedAppUpdater != null)
			{
				this.attachedAppUpdater.ErrorMessageGenerated += this.OnErrorMessageGenerated;
				this.attachedAppUpdater.PropertyChanged += this.OnUpdaterPropertyChanged;
			}
		}


		// Called when opened.
		protected override void OnOpened(EventArgs e)
		{
			// setup UI
			this.UpdateLatestNotifiedInfo();

			// check for update
			if (this.CheckForUpdateWhenOpening && this.attachedAppUpdater != null)
				this.attachedAppUpdater.CheckForUpdateCommand.TryExecute();
			
			// setup default button
			this.SynchronizationContext.Post(() =>
			{
				if (this.attachedAppUpdater?.IsLatestVersion == false)
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
				this.closingTaskSource.SetResult(ApplicationUpdateDialogResult.UpdatingCancelled);
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
				{
					this.Close(ApplicationUpdateDialogResult.ShutdownNeeded);
					this.closingTaskSource.SetResult(ApplicationUpdateDialogResult.ShutdownNeeded);
				}
			}
			else if (e.PropertyName == nameof(ApplicationUpdater.UpdateVersion))
				this.UpdateLatestNotifiedInfo();
		}


		// Update latest info shown to user.
		void UpdateLatestNotifiedInfo()
        {
			if (this.attachedAppUpdater == null)
				return;
			var version = this.attachedAppUpdater.UpdateVersion;
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


		/// <summary>
		/// Wait for closing dialog.
		/// </summary>
		/// <returns>Task of waiting.</returns>
		public Task<ApplicationUpdateDialogResult> WaitForClosingAsync() =>
			this.closingTaskSource.Task;
    }
}
