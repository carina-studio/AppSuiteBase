using CarinaStudio.AutoUpdate;
using CarinaStudio.AutoUpdate.Installers;
using CarinaStudio.AutoUpdate.Resolvers;
using CarinaStudio.IO;
using CarinaStudio.Net;
using CarinaStudio.Threading;
using CarinaStudio.ViewModels;
using CarinaStudio.Windows.Input;
using Microsoft.Extensions.Logging;
using Mono.Unix;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.ViewModels
{
	/// <summary>
	/// Application updater.
	/// </summary>
	public class ApplicationUpdater : ViewModel<IAppSuiteApplication>
	{
		// Static fields.
		static readonly Regex AutoUpdaterDirNameRegex = new Regex("^AutoUpdater\\-(?<Version>[\\d\\.]+)$", RegexOptions.IgnoreCase);
		static readonly Uri AutoUpdaterPackageManifestUri = new Uri("https://raw.githubusercontent.com/carina-studio/AutoUpdater/master/PackageManifest-Avalonia.json");
		static readonly ObservableProperty<bool> IsCheckingForUpdateProperty = ObservableProperty.Register<ApplicationUpdater, bool>(nameof(IsCheckingForUpdate));
		static readonly ObservableProperty<bool> IsLatestVersionProperty = ObservableProperty.Register<ApplicationUpdater, bool>(nameof(IsLatestVersion));
		static readonly ObservableProperty<bool> IsPreparingForUpdateProperty = ObservableProperty.Register<ApplicationUpdater, bool>(nameof(IsPreparingForUpdate));
		static readonly ObservableProperty<bool> IsUpdatePreparationProgressAvailableProperty = ObservableProperty.Register<ApplicationUpdater, bool>(nameof(IsUpdatePreparationProgressAvailable));
		static readonly ObservableProperty<Uri?> ReleasePageUriProperty = ObservableProperty.Register<ApplicationUpdater, Uri?>(nameof(ReleasePageUri));
		static readonly ObservableProperty<Uri?> UpdatePackageUriProperty = ObservableProperty.Register<ApplicationUpdater, Uri?>(nameof(UpdatePackageUri));
		static readonly ObservableProperty<string?> UpdatePreparationMessageProperty = ObservableProperty.Register<ApplicationUpdater, string?>(nameof(UpdatePreparationMessage));
		static readonly ObservableProperty<double> UpdatePreparationProgressPercentageProperty = ObservableProperty.Register<ApplicationUpdater, double>(nameof(UpdatePreparationProgressPercentage));
		static readonly ObservableProperty<Version?> UpdateVersionProperty = ObservableProperty.Register<ApplicationUpdater, Version?>(nameof(UpdateVersion));


		// Fields.
		Updater? auUpdater;
		readonly MutableObservableBoolean canCheckForUpdate = new MutableObservableBoolean(true);
		readonly MutableObservableBoolean canStartUpdating = new MutableObservableBoolean();
		CancellationTokenSource? updatePreparationCancellationTokenSource;


		/// <summary>
		/// Initialize new <see cref="ApplicationUpdater"/> instance.
		/// </summary>
		public ApplicationUpdater() : base(AppSuiteApplication.Current)
		{
			this.CancelUpdatingCommand = new Command(this.CancelUpdating, this.GetValueAsObservable(IsPreparingForUpdateProperty));
			this.CheckForUpdateCommand = new Command(this.CheckForUpdate, this.canCheckForUpdate);
			this.StartUpdatingCommand = new Command(this.StartUpdating, this.canStartUpdating);
			this.ReportUpdateInfo();
		}


		// Cancel auto updating.
		void CancelUpdating()
		{
			// check state
			this.VerifyAccess();
			this.VerifyDisposed();
			if (!this.IsPreparingForUpdate)
				return;

			this.Logger.LogWarning("Cancel auto updating");

			// cancel
			this.updatePreparationCancellationTokenSource?.Cancel();
			this.auUpdater?.Cancel();
		}


		/// <summary>
		/// Command to cancel auto updating.
		/// </summary>
		public ICommand CancelUpdatingCommand { get; }


		// Check for update.
		async void CheckForUpdate()
		{
			// check state
			this.VerifyAccess();
			this.VerifyDisposed();
			if (this.IsCheckingForUpdate)
				return;

			// check for update
			this.canCheckForUpdate.Update(false);
			this.SetValue(IsCheckingForUpdateProperty, true);
			await this.Application.CheckUpdateInfoAsync();
			if (!this.IsDisposed)
			{
				this.SetValue(IsCheckingForUpdateProperty, false);
				this.canCheckForUpdate.Update(true);
			}
		}


		/// <summary>
		/// Command to check for application update.
		/// </summary>
		public ICommand CheckForUpdateCommand { get; }


		/// <summary>
		/// Dispose the instance.
		/// </summary>
		/// <param name="disposing">True to release managed resources.</param>
		protected override void Dispose(bool disposing)
		{
			// cancel updating
			this.auUpdater?.Dispose();

			// call base
			base.Dispose(disposing);
		}


		/// <summary>
		/// Raised when error message generated.
		/// </summary>
		public event EventHandler<MessageEventArgs>? ErrorMessageGenerated;


		/// <summary>
		/// Check whether auto update is supported or not.
		/// </summary>
		public bool IsAutoUpdateSupported { get; } = Platform.IsWindows || Platform.IsLinux;


		/// <summary>
		/// Check whether application update checking is on-going or not.
		/// </summary>
		public bool IsCheckingForUpdate { get => this.GetValue(IsCheckingForUpdateProperty); }


		/// <summary>
		/// Check whether application version is the latest or not.
		/// </summary>
		public bool IsLatestVersion { get => this.GetValue(IsLatestVersionProperty); }


		/// <summary>
		/// Check whether update preparation is on-going or not.
		/// </summary>
		public bool IsPreparingForUpdate { get => this.GetValue(IsPreparingForUpdateProperty); }


		/// <summary>
		/// Check whether value of <see cref="UpdatePreparationProgressPercentage"/> is available or not.
		/// </summary>
		public bool IsUpdatePreparationProgressAvailable { get => this.GetValue(IsUpdatePreparationProgressAvailableProperty); }


		/// <summary>
		/// Called when property of application changed.
		/// </summary>
		/// <param name="e">Event data</param>
		protected override void OnApplicationPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnApplicationPropertyChanged(e);
			if (e.PropertyName == nameof(IAppSuiteApplication.UpdateInfo))
				this.ReportUpdateInfo();
		}


		// Called when property of updater of AutoUpdater changed.
		void OnAuUpdaterPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is not Updater updater)
				return;
			switch (e.PropertyName)
			{
				case nameof(Updater.DownloadedPackageSize):
				case nameof(Updater.PackageSize):
					this.RefreshMessages();
					break;
				case nameof(Updater.Progress):
					if (this.IsPreparingForUpdate)
						this.SetValue(UpdatePreparationProgressPercentageProperty, updater.Progress * 100);
					break;
				case nameof(Updater.State):
					this.RefreshMessages();
					switch (updater.State)
					{
						case UpdaterState.Cancelled:
						case UpdaterState.Failed:
						case UpdaterState.Succeeded:
							this.OnUpdatePreparationCompleted(updater.State, updater.ApplicationDirectoryPath.AsNonNull());
							break;
					}
					break;
			}
		}


		/// <summary>
		/// Called when property changed.
		/// </summary>
		/// <param name="property">Changed property.</param>
		/// <param name="oldValue">Old value.</param>
		/// <param name="newValue">New value.</param>
		protected override void OnPropertyChanged(ObservableProperty property, object? oldValue, object? newValue)
		{
			base.OnPropertyChanged(property, oldValue, newValue);
			if (property == UpdatePreparationProgressPercentageProperty)
				this.SetValue(IsUpdatePreparationProgressAvailableProperty, double.IsFinite(this.UpdatePreparationProgressPercentage));
		}


		// Called when update preparation completed.
		async void OnUpdatePreparationCompleted(UpdaterState state, string autoUpdaterDirectory)
		{
			// release updater
			this.auUpdater?.Let(it =>
			{
				it.PropertyChanged -= this.OnAuUpdaterPropertyChanged;
				it.Dispose();
				this.auUpdater = null;
			});

			// update state
			this.SetValue(IsPreparingForUpdateProperty, false);
			this.canStartUpdating.Update(true);
			this.RefreshMessages();

			// check state
			switch (state)
			{
				case UpdaterState.Succeeded:
					break;
				case UpdaterState.Cancelled:
					this.Logger.LogWarning("Update preparation was cancelled");
					return;
				default:
					this.Logger.LogError("Update preparation was failed");
					this.ErrorMessageGenerated?.Invoke(this, new MessageEventArgs(this.Application.GetStringNonNull("AppUpdater.FailedToPrepareForUpdate")));
					return;
			}

			// prepare auto updater
			var autoUpdaterPath = autoUpdaterDirectory.Let(it =>
			{
				if (Platform.IsWindows)
					return Path.Combine(it, "AutoUpdater.Avalonia.exe");
				return Path.Combine(it, "AutoUpdater.Avalonia");
			});
			if (!Platform.IsWindows)
			{
				try
				{
					await Task.Run(() => new UnixFileInfo(autoUpdaterPath).FileAccessPermissions |= (FileAccessPermissions.UserExecute | FileAccessPermissions.GroupExecute));
				}
				catch (Exception ex)
				{
					this.Logger.LogError(ex, $"Unable to mark '{autoUpdaterPath}' as executable");
				}
			}

			// start auto updater
			try
			{
				var currentProcess = Process.GetCurrentProcess();
				var mainModule = currentProcess.MainModule;
				if (mainModule == null)
				{
					this.Logger.LogError("Unable to get information of current process");
					this.ErrorMessageGenerated?.Invoke(this, new MessageEventArgs(this.Application.GetStringNonNull("AppUpdater.FailedToPrepareForUpdate")));
					return;
				}
				var useDarkMode = this.Application.EffectiveThemeMode == ThemeMode.Dark;
				this.Logger.LogWarning("Start auto updater");
				using var process = Process.Start(new ProcessStartInfo()
				{
					Arguments = $"-culture {this.Application.CultureInfo}" + 
						$" {(useDarkMode ? "-dark-mode" : "")}" +
						$" -directory \"{Path.GetDirectoryName(mainModule.FileName)}\"" +
						$" -executable \"{mainModule.FileName}\"" +
						$" -executable-args \"-restore-state\"" +
						$" -name \"{this.Application.Name}\"" +
						$" -package-manifest {this.Application.PackageManifestUri}" +
						$" -wait-for-process {currentProcess.Id}",
					FileName = autoUpdaterPath,
				});
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Unable to start auto updater");
				this.ErrorMessageGenerated?.Invoke(this, new MessageEventArgs(this.Application.GetStringNonNull("AppUpdater.FailedToStartAutoUpdater")));
				return;
			}

			// shutdown
			this.Logger.LogWarning("Shutdown for updating");
			this.Application.Shutdown();
		}


		// Refresh messages according to current state.
		void RefreshMessages()
		{
			if (this.IsDisposed)
				return;
			if (this.IsPreparingForUpdate)
			{
				if (this.auUpdater?.State == UpdaterState.DownloadingPackage)
				{
					var downloadedSizeString = this.auUpdater.DownloadedPackageSize.ToFileSizeString();
					var packageSize = this.auUpdater.PackageSize.GetValueOrDefault();
					if (packageSize > 0)
						this.SetValue(UpdatePreparationMessageProperty, this.Application.GetFormattedString("AppUpdater.DownloadingAutoUpdater", $"{downloadedSizeString} / {packageSize.ToFileSizeString()}"));
					else
						this.SetValue(UpdatePreparationMessageProperty, this.Application.GetFormattedString("AppUpdater.DownloadingAutoUpdater", downloadedSizeString));
				}
				else
					this.SetValue(UpdatePreparationMessageProperty, this.Application.GetString("AppUpdater.PreparingForUpdate"));
			}
			else
				this.SetValue(UpdatePreparationMessageProperty, null);
		}


		/// <summary>
		/// Get URI of update releasing page.
		/// </summary>
		public Uri? ReleasePageUri { get => this.GetValue(ReleasePageUriProperty); }


		// Report current application update info.
		void ReportUpdateInfo()
		{
			var updateInfo = this.Application.UpdateInfo;
			if (updateInfo == null)
			{
				this.canStartUpdating.Update(false);
				this.SetValue(IsLatestVersionProperty, true);
				this.SetValue(ReleasePageUriProperty, null);
				this.SetValue(UpdatePackageUriProperty, null);
				this.SetValue(UpdateVersionProperty, null);
			}
			else
			{
				this.SetValue(IsLatestVersionProperty, false);
				this.SetValue(ReleasePageUriProperty, updateInfo.ReleasePageUri);
				this.SetValue(UpdatePackageUriProperty, updateInfo.PackageUri);
				this.SetValue(UpdateVersionProperty, updateInfo.Version);
				this.canStartUpdating.Update(IsAutoUpdateSupported && !this.IsPreparingForUpdate);
			}
		}


		// Start auto updating.
		async void StartUpdating()
		{
			// check state
			this.VerifyAccess();
			this.VerifyDisposed();
			if (!this.canStartUpdating.Value
				|| !this.IsAutoUpdateSupported
				|| this.IsPreparingForUpdate)
			{
				return;
			}

			// update state
			this.canStartUpdating.Update(false);
			this.SetValue(UpdatePreparationProgressPercentageProperty, double.NaN);
			this.SetValue(IsPreparingForUpdateProperty, true);

			// update message
			this.RefreshMessages();

			// resolve info of auto updater
			this.updatePreparationCancellationTokenSource = new CancellationTokenSource();
			var auPackageResolver = new JsonPackageResolver() { Source = new WebRequestStreamProvider(AutoUpdaterPackageManifestUri) };
			try
			{
				await auPackageResolver.StartAndWaitAsync(this.updatePreparationCancellationTokenSource.Token);
			}
			catch (TaskCanceledException)
			{ }
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Failed to resolve package info of auto updater");
			}
			if (this.IsDisposed)
				return;
			if (this.updatePreparationCancellationTokenSource.IsCancellationRequested)
			{
				this.OnUpdatePreparationCompleted(UpdaterState.Cancelled, "");
				return;
			}

			// delete current auto updater version
			await Task.Run(() =>
			{
				try
				{
					foreach (var path in Directory.EnumerateDirectories(this.Application.RootPrivateDirectoryPath))
					{
						if (AutoUpdaterDirNameRegex.IsMatch(Path.GetFileName(path)))
						{
							try
							{
								this.Logger.LogDebug($"Delete auto updater '{path}'");
								Directory.Delete(path, true);
							}
							catch (Exception ex)
							{
								this.Logger.LogError(ex, $"Failed to delete auto updater '{path}'");
							}
						}
					}
				}
				catch (Exception ex)
				{
					this.Logger.LogError(ex, "Error occurred while deleting auto updater");
				}
			});
			if (this.IsDisposed)
				return;
			if (this.updatePreparationCancellationTokenSource.IsCancellationRequested)
			{
				this.OnUpdatePreparationCompleted(UpdaterState.Cancelled, "");
				return;
			}

			// create directory for auto updater
			var autoUpdaterDirectory = Path.Combine(this.Application.RootPrivateDirectoryPath, $"AutoUpdater-{auPackageResolver.PackageVersion}");
			await Task.Run(() =>
			{
				try
				{
					Directory.CreateDirectory(autoUpdaterDirectory);
				}
				catch (Exception ex)
				{
					this.Logger.LogError(ex, $"Fail to create '{autoUpdaterDirectory}'");
				}
			});
			if (this.IsDisposed)
				return;
			if (this.updatePreparationCancellationTokenSource.IsCancellationRequested)
			{
				this.OnUpdatePreparationCompleted(UpdaterState.Cancelled, "");
				return;
			}

			// download auto updater
			this.auUpdater = new Updater()
			{
				ApplicationDirectoryPath = autoUpdaterDirectory,
				PackageInstaller = new ZipPackageInstaller(),
				PackageResolver = new JsonPackageResolver() { Source = new WebRequestStreamProvider(AutoUpdaterPackageManifestUri) },
			};
			this.Logger.LogWarning($"Start downloading auto updater to '{autoUpdaterDirectory}'");
			this.auUpdater.PropertyChanged += this.OnAuUpdaterPropertyChanged;
			if (!this.auUpdater.Start())
			{
				this.Logger.LogError("Failed to start downloading auto updater");
				this.OnUpdatePreparationCompleted(UpdaterState.Failed, "");
			}
		}


		/// <summary>
		/// Command to start updating application.
		/// </summary>
		public ICommand StartUpdatingCommand { get; }


		/// <summary>
		/// Get URI of update package.
		/// </summary>
		public Uri? UpdatePackageUri { get => this.GetValue(UpdatePackageUriProperty); }


		/// <summary>
		/// Get message to describe the status of update preparation.
		/// </summary>
		public string? UpdatePreparationMessage { get => this.GetValue(UpdatePreparationMessageProperty); }


		/// <summary>
		/// Get current progress percentage of update preparation.
		/// </summary>
		public double UpdatePreparationProgressPercentage { get => this.GetValue(UpdatePreparationProgressPercentageProperty); }


		/// <summary>
		/// Get new version which application can be updated to.
		/// </summary>
		public Version? UpdateVersion { get => this.GetValue(UpdateVersionProperty); }
	}
}
