using Avalonia.Media;
using CarinaStudio.AutoUpdate;
using CarinaStudio.AutoUpdate.Installers;
using CarinaStudio.AutoUpdate.Resolvers;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Logging;
using CarinaStudio.IO;
using CarinaStudio.Net;
using CarinaStudio.Threading;
using CarinaStudio.ViewModels;
using CarinaStudio.Windows.Input;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.ViewModels;

/// <summary>
/// Application updater.
/// </summary>
public class ApplicationUpdater : ViewModel<IAppSuiteApplication>
{
	// Constants.
	const string TempAutoUpdaterDirName = "AutoUpdater-Temp";


	// Static fields.
	static readonly Regex AutoUpdaterDirNameRegex = new("^AutoUpdater\\-(?<Version>[\\d\\.]+)$", RegexOptions.IgnoreCase);
	static readonly Uri AutoUpdaterPackageManifestUri = new("https://raw.githubusercontent.com/carina-studio/AutoUpdater/master/PackageManifest-Avalonia.json");
	static readonly Version AutoUpdaterVersionSupportsBaseVersion = new(1, 1, 0, 713);
	static readonly ObservableProperty<bool> HasReleasePageUriProperty = ObservableProperty.Register<ApplicationUpdater, bool>(nameof(HasReleasePageUri));
	static readonly ObservableProperty<bool> IsCheckingForUpdateProperty = ObservableProperty.Register<ApplicationUpdater, bool>(nameof(IsCheckingForUpdate));
	static readonly ObservableProperty<bool> IsLatestVersionProperty = ObservableProperty.Register<ApplicationUpdater, bool>(nameof(IsLatestVersion));
	static readonly ObservableProperty<bool> IsPreparingForUpdateProperty = ObservableProperty.Register<ApplicationUpdater, bool>(nameof(IsPreparingForUpdate));
	static readonly ObservableProperty<bool> IsShutdownNeededToContinueUpdateProperty = ObservableProperty.Register<ApplicationUpdater, bool>(nameof(IsShutdownNeededToContinueUpdate));
	static readonly ObservableProperty<bool> IsUpdatePreparationProgressAvailableProperty = ObservableProperty.Register<ApplicationUpdater, bool>(nameof(IsUpdatePreparationProgressAvailable));
	static readonly ObservableProperty<Uri?> ReleasePageUriProperty = ObservableProperty.Register<ApplicationUpdater, Uri?>(nameof(ReleasePageUri));
	static readonly ObservableProperty<Uri?> UpdatePackageUriProperty = ObservableProperty.Register<ApplicationUpdater, Uri?>(nameof(UpdatePackageUri));
	static readonly ObservableProperty<string?> UpdatePreparationMessageProperty = ObservableProperty.Register<ApplicationUpdater, string?>(nameof(UpdatePreparationMessage));
	static readonly ObservableProperty<double> UpdatePreparationProgressPercentageProperty = ObservableProperty.Register<ApplicationUpdater, double>(nameof(UpdatePreparationProgressPercentage));
	static readonly ObservableProperty<Version?> UpdateVersionProperty = ObservableProperty.Register<ApplicationUpdater, Version?>(nameof(UpdateVersion));


	// Fields.
	Updater? auUpdater;
	Version? auVersion;
	readonly MutableObservableBoolean canCheckForUpdate = new(true);
	readonly MutableObservableBoolean canStartUpdating = new();
	Uri? packageManifestUri;
	CancellationTokenSource? updatePreparationCancellationTokenSource;


	/// <summary>
	/// Initialize new <see cref="ApplicationUpdater"/> instance.
	/// </summary>
	public ApplicationUpdater() : base(IAppSuiteApplication.Current)
	{
		this.CancelUpdatingCommand = new Command(this.CancelUpdating, this.GetValueAsObservable(IsPreparingForUpdateProperty));
		this.CheckForUpdateCommand = new Command(this.CheckForUpdateAsync, this.canCheckForUpdate);
		this.StartUpdatingCommand = new Command(this.StartUpdatingAsync, this.canStartUpdating);
		this.ReportUpdateInfo();
	}
	
	
	/// <summary>
	/// Get or set whether to accept application update with non-stable version or not.
	/// </summary>
	public bool AcceptNonStableApplicationUpdate
	{
		get => this.Settings.GetValueOrDefault(SettingKeys.AcceptNonStableApplicationUpdate);
		set => this.Settings.SetValue<bool>(SettingKeys.AcceptNonStableApplicationUpdate, value);
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
	async Task CheckForUpdateAsync()
	{
		// check state
		this.VerifyAccess();
		this.VerifyDisposed();
		if (this.IsCheckingForUpdate)
			return;

		// check for update
		this.canCheckForUpdate.Update(false);
		this.canStartUpdating.Update(false);
		this.SetValue(IsCheckingForUpdateProperty, true);
		await this.Application.CheckForApplicationUpdateAsync();
		if (!this.IsDisposed)
		{
			this.SetValue(IsCheckingForUpdateProperty, false);
			this.canCheckForUpdate.Update(true);
			this.canStartUpdating.Update(this.IsAutoUpdateSupported && !this.IsPreparingForUpdate);
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
	/// Check whether <see cref="ReleasePageUri"/> is valid or not.
	/// </summary>
	public bool HasReleasePageUri => this.GetValue(HasReleasePageUriProperty);


	/// <summary>
	/// Check whether auto update is supported or not.
	/// </summary>
	public bool IsAutoUpdateSupported => true;


	/// <summary>
	/// Check whether application update checking is on-going or not.
	/// </summary>
	public bool IsCheckingForUpdate => this.GetValue(IsCheckingForUpdateProperty);


	/// <summary>
	/// Check whether application version is the latest or not.
	/// </summary>
	public bool IsLatestVersion => this.GetValue(IsLatestVersionProperty);


	/// <summary>
	/// Check whether update preparation is on-going or not.
	/// </summary>
	public bool IsPreparingForUpdate => this.GetValue(IsPreparingForUpdateProperty);


	/// <summary>
	/// Check whether shutting down is needed to continue update or not.
	/// </summary>
	public bool IsShutdownNeededToContinueUpdate => this.GetValue(IsShutdownNeededToContinueUpdateProperty);


	/// <summary>
	/// Check whether value of <see cref="UpdatePreparationProgressPercentage"/> is available or not.
	/// </summary>
	public bool IsUpdatePreparationProgressAvailable => this.GetValue(IsUpdatePreparationProgressAvailableProperty);


	/// <summary>
    /// Get or set whether to notify user when application update found or not.
    /// </summary>
    public bool NotifyApplicationUpdate
    {
        get => this.Settings.GetValueOrDefault(SettingKeys.NotifyApplicationUpdate);
        set => this.Settings.SetValue<bool>(SettingKeys.NotifyApplicationUpdate, value);
    }


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
		if (sender is Updater updater)
			_ = this.OnAuUpdaterPropertyChangedAsync(updater, e);
	}


	// Called when property of updater of AutoUpdater changed.
	async Task OnAuUpdaterPropertyChangedAsync(Updater updater, PropertyChangedEventArgs e)
	{
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
						Global.RunWithoutErrorAsync(() =>
						{
							var tempDirectory = Path.Combine(this.Application.RootPrivateDirectoryPath, TempAutoUpdaterDirName);
							this.Logger.LogWarning("Delete auto updater download directory '{tempDirectory}'", tempDirectory);
							System.IO.Directory.Delete(tempDirectory, true);
						});
						await this.OnUpdatePreparationCompletedAsync(updater.State, updater.ApplicationDirectoryPath.AsNonNull(), updater.PackageResolver?.PackageVersion);
						break;
					case UpdaterState.Succeeded:
						// rename to final auto updater directory
						var tempDirectory = Path.Combine(this.Application.RootPrivateDirectoryPath, TempAutoUpdaterDirName);
						var finalDirectory = Path.Combine(this.Application.RootPrivateDirectoryPath, $"AutoUpdater-{this.auVersion}");
						var success = await Task.Run(() =>
						{
							var retryCount = 20;
							while (true)
							{
								try
								{
									this.Logger.LogDebug("Rename auto updater directory from '{tempDirectory}' to '{finalDirectory}'", tempDirectory, finalDirectory);
									System.IO.Directory.Move(tempDirectory, finalDirectory);
									return true;
								}
								catch (Exception ex)
								{
									if (this.updatePreparationCancellationTokenSource?.IsCancellationRequested == true)
									{
										Global.RunWithoutError(() => System.IO.Directory.Delete(tempDirectory, true));
										return false;
									}
									if (retryCount > 0)
									{
										--retryCount;
										this.Logger.LogError(ex, "Unable to rename auto updater directory from '{tempDirectory}' to '{finalDirectory}', try again", tempDirectory, finalDirectory);
										Thread.Sleep(500);
									}
									else
									{
										this.Logger.LogError(ex, "Unable to rename auto updater directory from '{tempDirectory}' to '{finalDirectory}'", tempDirectory, finalDirectory);
										Global.RunWithoutError(() => System.IO.Directory.Delete(tempDirectory, true));
										return false;
									}
								}
							}
						});

						// complete
						if (this.updatePreparationCancellationTokenSource?.IsCancellationRequested == true)
						{
							Global.RunWithoutErrorAsync(() => System.IO.Directory.Delete(tempDirectory, true));
							await this.OnUpdatePreparationCompletedAsync(UpdaterState.Cancelled, "", updater.PackageResolver?.PackageVersion);
						}
						else if (success)
							await this.OnUpdatePreparationCompletedAsync(UpdaterState.Succeeded, finalDirectory, updater.PackageResolver?.PackageVersion);
						else
							await this.OnUpdatePreparationCompletedAsync(UpdaterState.Failed, "", updater.PackageResolver?.PackageVersion);
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
		if (property == ReleasePageUriProperty)
			this.SetValue(HasReleasePageUriProperty, newValue is not null);
		else if (property == UpdatePreparationProgressPercentageProperty)
			this.SetValue(IsUpdatePreparationProgressAvailableProperty, double.IsFinite(this.UpdatePreparationProgressPercentage));
	}


	/// <inheritdoc/>
    protected override void OnSettingChanged(SettingChangedEventArgs e)
    {
        base.OnSettingChanged(e);
        if (e.Key == SettingKeys.AcceptNonStableApplicationUpdate)
            this.OnPropertyChanged(nameof(AcceptNonStableApplicationUpdate));
		else if (e.Key == SettingKeys.NotifyApplicationUpdate)
			this.OnPropertyChanged(nameof(NotifyApplicationUpdate));
	}


	// Called when update preparation completed.
	async Task OnUpdatePreparationCompletedAsync(UpdaterState state, string autoUpdaterDirectory, Version? autoUpdaterVersion)
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
		this.canStartUpdating.Update(!this.IsCheckingForUpdate);
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
				this.ErrorMessageGenerated?.Invoke(this, new MessageEventArgs(this.Application.GetStringNonNull("ApplicationUpdater.FailedToPrepareForUpdate")));
				return;
		}

		// prepare auto updater
		var autoUpdaterPath = autoUpdaterDirectory.Let(it =>
		{
			if (Platform.IsWindows)
				return Path.Combine(it, "AutoUpdater.Avalonia.exe");
			if (Platform.IsMacOS)
				return Path.Combine(it, "AutoUpdater.Avalonia.app/Contents/MacOS/AutoUpdater.Avalonia");
			return Path.Combine(it, "AutoUpdater.Avalonia");
		});
		if (Platform.IsLinux)
		{
			try
			{
				await Task.Run(() => 
				{
#pragma warning disable CA1416
					System.IO.File.SetUnixFileMode(autoUpdaterPath, System.IO.File.GetUnixFileMode(autoUpdaterPath) | UnixFileMode.UserExecute | UnixFileMode.GroupExecute);
#pragma warning restore CA1416
				});
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Unable to mark '{autoUpdaterPath}' as executable", autoUpdaterPath);
			}
		}

		// start auto updater
		try
		{
			var currentProcess = Process.GetCurrentProcess();
			var mainModule = currentProcess.MainModule;
			if (mainModule is null)
			{
				this.Logger.LogError("Unable to get information of current process");
				this.ErrorMessageGenerated?.Invoke(this, new MessageEventArgs(this.Application.GetStringNonNull("ApplicationUpdater.FailedToPrepareForUpdate")));
				return;
			}
			var useDarkMode = this.Application.EffectiveThemeMode == ThemeMode.Dark;
			this.Logger.LogWarning("Start auto updater");
			using var process = Process.Start(new ProcessStartInfo().Also(it =>
			{
				// get accent color
				var accentColor = Colors.Transparent;
				(this.Application as AppSuiteApplication)?.Let(app =>
				{
					if (app.TryGetResource("SystemAccentColor", out Color? color))
						accentColor = color.Value;
				});

				// get screen scale factor
				var screenScaleFactor = this.Application.CustomScreenScaleFactor;

				// get application directory
				var appDirectory = Global.Run(() =>
				{
					var directory = Path.GetDirectoryName(mainModule.FileName);
					if (!Platform.IsMacOS || directory?.EndsWith(".app/Contents/MacOS") != true)
						return directory;
					return directory[0..^15];
				});

				// prepare arguments
				var argsBuilder = new StringBuilder($"-culture {this.Application.CultureInfo}" +
					$" {(useDarkMode ? "-dark-mode" : "")}" +
					$" -directory \"{appDirectory}\"" +
					$" -name \"{this.Application.Name}\"" +
					(autoUpdaterVersion >= AutoUpdaterVersionSupportsBaseVersion
						? $" -base-version {this.Application.Assembly.GetName().Version?.ToString() ?? "0.0.0.0"}"
						: ""
					) +
					$" -package-manifest \"{this.packageManifestUri}\"" +
					$" -wait-for-process {currentProcess.Id}");
				mainModule.FileName.Let(it =>
				{
					if (!it.EndsWith("/dotnet") && !it.EndsWith("\\dotnet.exe"))
					{
						argsBuilder.AppendFormat(" -executable \"{0}\"", it);
						argsBuilder.AppendFormat(" -executable-args \"{0}\"", AppSuiteApplication.RestoreMainWindowsArgument);
					}
				});
				if (accentColor.A > 0)
					argsBuilder.AppendFormat(" -accent-color #{0:x2}{1:x2}{2:x2}{3:x2}", accentColor.A, accentColor.R, accentColor.G, accentColor.B);
				if (!double.IsNaN(screenScaleFactor))
					argsBuilder.AppendFormat(" -screen-scale-factor {0:F2}", screenScaleFactor);
				if (this.Application.IsDebugMode)
					argsBuilder.Append(" -debug-mode");
				it.Arguments = argsBuilder.ToString();
				it.FileName = autoUpdaterPath;
			}));
		}
		catch (Exception ex)
		{
			this.Logger.LogError(ex, "Unable to start auto updater");
			this.ErrorMessageGenerated?.Invoke(this, new MessageEventArgs(this.Application.GetStringNonNull("ApplicationUpdater.FailedToStartAutoUpdater")));
			return;
		}

		// shutdown
		this.Logger.LogWarning("Need to shutdown to continue updating");
		this.SetValue(IsShutdownNeededToContinueUpdateProperty, true);
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
				this.SetValue(UpdatePreparationMessageProperty, packageSize > 0 
					? this.Application.GetFormattedString("ApplicationUpdater.DownloadingAutoUpdater", $"{downloadedSizeString} / {packageSize.ToFileSizeString()}") 
					: this.Application.GetFormattedString("ApplicationUpdater.DownloadingAutoUpdater", downloadedSizeString));
			}
			else
				this.SetValue(UpdatePreparationMessageProperty, this.Application.GetString("ApplicationUpdater.PreparingForUpdate"));
		}
		else
			this.SetValue(UpdatePreparationMessageProperty, null);
	}


	/// <summary>
	/// Get URI of update releasing page.
	/// </summary>
	public Uri? ReleasePageUri => this.GetValue(ReleasePageUriProperty);


	// Report current application update info.
	void ReportUpdateInfo()
	{
		var updateInfo = this.Application.UpdateInfo;
		if (updateInfo is null)
		{
			this.canStartUpdating.Update(false);
			this.packageManifestUri = null;
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
			this.canStartUpdating.Update(IsAutoUpdateSupported && !this.IsPreparingForUpdate && !this.IsCheckingForUpdate);
			this.packageManifestUri = updateInfo.PackageManifestUri;
		}
	}


	// Start auto updating.
	async Task StartUpdatingAsync()
	{
		// check state
		this.VerifyAccess();
		this.VerifyDisposed();
		if (!this.canStartUpdating.Value
			|| !this.IsAutoUpdateSupported
			|| this.IsPreparingForUpdate
			|| this.packageManifestUri is null)
		{
			return;
		}

		// update state
		this.canStartUpdating.Update(false);
		this.SetValue(UpdatePreparationProgressPercentageProperty, double.NaN);
		this.SetValue(IsPreparingForUpdateProperty, true);

		// update message
		this.RefreshMessages();

		// find local version of auto updater
		var localAuVersion = await Task.Run(() =>
		{
			try
			{
				var latestVersion = (Version?)null;
				foreach (var path in System.IO.Directory.EnumerateDirectories(this.Application.RootPrivateDirectoryPath))
				{
					var match = AutoUpdaterDirNameRegex.Match(Path.GetFileName(path));
					if (match.Success)
					{
						if (Version.TryParse(match.Groups["Version"].Value, out var version)
							&& (latestVersion is null || version > latestVersion))
						{
							latestVersion = version;
						}
					}
				}
				return latestVersion;
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Error occurred while deleting auto updater");
				return null;
			}
		});

		// resolve info of auto updater
		this.updatePreparationCancellationTokenSource = new CancellationTokenSource();
		var auPackageResolver = new JsonPackageResolver(this.Application, localAuVersion) { Source = new WebRequestStreamProvider(AutoUpdaterPackageManifestUri) };
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
			await this.OnUpdatePreparationCompletedAsync(UpdaterState.Cancelled, "", auPackageResolver.PackageVersion);
			return;
		}

		// delete current auto updater version
		this.auVersion = auPackageResolver.PackageVersion;
		var isAutoUpdaterInstalled = false;
		await Task.Run(() =>
		{
			try
			{
				foreach (var path in System.IO.Directory.EnumerateDirectories(this.Application.RootPrivateDirectoryPath))
				{
					var match = AutoUpdaterDirNameRegex.Match(Path.GetFileName(path));
					if (match.Success)
					{
						if (Version.TryParse(match.Groups["Version"].Value, out var version) && version == auPackageResolver.PackageVersion)
						{
							isAutoUpdaterInstalled = true;
							continue;
						}
						try
						{
							this.Logger.LogDebug("Delete auto updater '{path}'", path);
							System.IO.Directory.Delete(path, true);
						}
						catch (Exception ex)
						{
							this.Logger.LogError(ex, "Failed to delete auto updater '{path}'", path);
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
			await this.OnUpdatePreparationCompletedAsync(UpdaterState.Cancelled, "", auPackageResolver.PackageVersion);
			return;
		}

		// continue updating if auto updater is already installed
		if (isAutoUpdaterInstalled)
		{
			this.Logger.LogDebug("Auto updater {packageVersion} is already installed before", auPackageResolver.PackageVersion);
			await this.OnUpdatePreparationCompletedAsync(UpdaterState.Succeeded, Path.Combine(this.Application.RootPrivateDirectoryPath, $"AutoUpdater-{auPackageResolver.PackageVersion}"), auPackageResolver.PackageVersion);
			return;
		}

		// create directory to download auto updater
		var tempAutoUpdaterDirectory = Path.Combine(this.Application.RootPrivateDirectoryPath, TempAutoUpdaterDirName);
		await Task.Run(() =>
		{
			try
			{
				System.IO.Directory.CreateDirectory(tempAutoUpdaterDirectory);
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Fail to create '{tempAutoUpdaterDirectory}'", tempAutoUpdaterDirectory);
			}
		});
		if (this.IsDisposed)
			return;
		if (this.updatePreparationCancellationTokenSource.IsCancellationRequested)
		{
			await this.OnUpdatePreparationCompletedAsync(UpdaterState.Cancelled, "", auPackageResolver.PackageVersion);
			return;
		}

		// download auto updater
		this.auUpdater = new Updater(this.Application)
		{
			ApplicationDirectoryPath = tempAutoUpdaterDirectory,
			PackageInstaller = new ZipPackageInstaller(this.Application),
			PackageResolver = new JsonPackageResolver(this.Application, localAuVersion) { Source = new WebRequestStreamProvider(AutoUpdaterPackageManifestUri) },
		};
		this.Logger.LogWarning("Start downloading auto updater to '{tempAutoUpdaterDirectory}'", tempAutoUpdaterDirectory);
		this.auUpdater.PropertyChanged += this.OnAuUpdaterPropertyChanged;
		if (!this.auUpdater.Start())
		{
			this.Logger.LogError("Failed to start downloading auto updater");
			await this.OnUpdatePreparationCompletedAsync(UpdaterState.Failed, "", auPackageResolver.PackageVersion);
		}
	}


	/// <summary>
	/// Command to start updating application.
	/// </summary>
	public ICommand StartUpdatingCommand { get; }


	/// <summary>
	/// Get URI of update package.
	/// </summary>
	public Uri? UpdatePackageUri => this.GetValue(UpdatePackageUriProperty);


	/// <summary>
	/// Get message to describe the status of update preparation.
	/// </summary>
	public string? UpdatePreparationMessage => this.GetValue(UpdatePreparationMessageProperty);


	/// <summary>
	/// Get current progress percentage of update preparation.
	/// </summary>
	public double UpdatePreparationProgressPercentage => this.GetValue(UpdatePreparationProgressPercentageProperty);


	/// <summary>
	/// Get new version which application can be updated to.
	/// </summary>
	public Version? UpdateVersion => this.GetValue(UpdateVersionProperty);
}
