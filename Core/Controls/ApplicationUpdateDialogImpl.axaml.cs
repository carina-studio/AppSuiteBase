using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// Dialog for application update.
	/// </summary>
	class ApplicationUpdateDialogImpl : Dialog<IAppSuiteApplication>
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
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ApplicationUpdater))]
		public ApplicationUpdateDialogImpl()
		{
			AvaloniaXamlLoader.Load(this);
		}


		/// <summary>
		/// Get or set whether performing update checking when opening dialog or not.
		/// </summary>
		public bool CheckForUpdateWhenOpening { get; init; }


		/// <summary>
		/// Download update package.
		/// </summary>
		public void DownloadUpdatePackage()
		{
			if (this.attachedAppUpdater is not null && this.attachedAppUpdater.UpdatePackageUri is not null)
			{
				Platform.OpenLink(this.attachedAppUpdater.UpdatePackageUri);
				this.Close();
				this.closingTaskSource.TrySetResult(ApplicationUpdateDialogResult.None);
			}
		}


		// Message of latest application version.
		string? LatestVersionMessage => this.GetValue<string?>(LatestVersionMessageProperty);


		// Message of new application version found.
		string? NewVersionFoundMessage => this.GetValue<string?>(NewVersionFoundMessageProperty);


		// Called when closed.
		protected override void OnClosed(EventArgs e)
		{
			this.DataContext = null;
			this.closingTaskSource.TrySetResult(ApplicationUpdateDialogResult.None);
			base.OnClosed(e);
		}


		// Called when closing.
		protected override void OnClosing(WindowClosingEventArgs e)
		{
			if (this.attachedAppUpdater is not null)
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
			if (this.attachedAppUpdater is not null)
			{
				this.attachedAppUpdater.ErrorMessageGenerated -= this.OnErrorMessageGenerated;
				this.attachedAppUpdater.PropertyChanged -= this.OnUpdaterPropertyChanged;
			}
			this.attachedAppUpdater = this.DataContext as ApplicationUpdater;
			if (this.attachedAppUpdater is not null)
			{
				this.attachedAppUpdater.ErrorMessageGenerated += this.OnErrorMessageGenerated;
				this.attachedAppUpdater.PropertyChanged += this.OnUpdaterPropertyChanged;
			}
		}


		/// <inheritdoc/>
		protected override void OnOpened(EventArgs e)
		{
			// check for update
			if (this.CheckForUpdateWhenOpening && this.attachedAppUpdater is not null)
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


		/// <inheritdoc/>
		protected override void OnOpening(EventArgs e)
		{
			// call base
			base.OnOpening(e);
			
			// setup UI
			this.UpdateLatestNotifiedInfo();
		}


		// Property of updater changed.
		void OnUpdaterPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is not ApplicationUpdater updater)
				return;
			switch (e.PropertyName)
			{
				case nameof(ApplicationUpdater.AcceptNonStableApplicationUpdate):
					updater.CheckForUpdateCommand.TryExecute();
					break;
				case nameof(ApplicationUpdater.IsPreparingForUpdate):
					if (!updater.IsPreparingForUpdate && this.isClosingRequested)
					{
						if (this.closingTaskSource.TrySetResult(ApplicationUpdateDialogResult.UpdatingCancelled))
							this.Close(ApplicationUpdateDialogResult.UpdatingCancelled);
					}
					break;
				case nameof(ApplicationUpdater.IsLatestVersion):
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
					break;
				case nameof(ApplicationUpdater.IsShutdownNeededToContinueUpdate):
					if (updater.IsShutdownNeededToContinueUpdate)
					{
						this.Close(ApplicationUpdateDialogResult.ShutdownNeeded);
						this.closingTaskSource.TrySetResult(ApplicationUpdateDialogResult.ShutdownNeeded);
					}
					break;
				case nameof(ApplicationUpdater.UpdateVersion):
					this.UpdateLatestNotifiedInfo();
					break;
			}
		}


		// Update latest info shown to user.
		void UpdateLatestNotifiedInfo()
        {
			if (this.attachedAppUpdater is not null)
				UpdateLatestNotifiedInfo(this.PersistentState, this.attachedAppUpdater.UpdateVersion);
		}
		public static void UpdateLatestNotifiedInfo(ISettings persistentState, Version? version)
		{
			if (version is not null)
			{
				persistentState.SetValue(LatestNotifiedTimeKey, DateTime.Now);
				persistentState.SetValue(LatestNotifiedVersionKey, version.ToString());
			}
			else
			{
				persistentState.ResetValue(LatestNotifiedTimeKey);
				persistentState.ResetValue(LatestNotifiedVersionKey);
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
