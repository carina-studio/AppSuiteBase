using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CarinaStudio.Collections;
using CarinaStudio.Threading;
using CarinaStudio.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.ViewModels
{
    /// <summary>
    /// View-model of application information UI.
    /// </summary>
    public class ApplicationInfo : ViewModel<IAppSuiteApplication>
    {
        /// <summary>
        /// Creator of application icon.
        /// </summary>
        public enum ApplicationIconCreator
        {
            /// <summary>
            /// Freepik.
            /// </summary>
            Freepik,
        }


        /// <summary>
        /// Web site of application icon.
        /// </summary>
        public enum ApplicationIconWebSite
        {
            /// <summary>
            /// Flaticon.
            /// </summary>
            Flaticon,
        }


        // Fields.
        IBitmap? icon;


        /// <summary>
        /// Initialize new <see cref="ApplicationInfo"/> instance.
        /// </summary>
        public ApplicationInfo() : base(AppSuiteApplication.Current)
        {
            // get system info
            _ = Task.Run(() =>
            {
                var version = Platform.GetInstalledRuntimeVersion();
                if (version != null)
                {
                    this.SynchronizationContext.Post(() =>
                    {
                        if (!this.IsDisposed)
                        {
                            this.InstalledFrameworkVersion = version;
                            this.OnPropertyChanged(nameof(InstalledFrameworkVersion));
                            this.IsFrameworkInstalled = true;
                            this.OnPropertyChanged(nameof(IsFrameworkInstalled));
                        }
                    });
                }
            });

            // get assemblies
            var appAssembly = this.Application.Assembly;
            this.Assemblies = ListExtensions.AsReadOnly(AppDomain.CurrentDomain.GetAssemblies()
                .Where(it => it != appAssembly && !it.IsDynamic && it.GetName().Name?.StartsWith("ℛ*") == false)
                .OrderBy(it => it.GetName().Name)
                .ToArray());
        }


        /// <summary>
        /// Get assemblies of this application.
        /// </summary>
        public IList<Assembly> Assemblies { get; }


        /// <summary>
        /// Get list of badge of application.
        /// </summary>
        public virtual IList<IImage> Badges { get; } = Array.Empty<IImage>();


        /// <summary>
        /// Export application logs to given file.
        /// </summary>
        /// <param name="outputFileName">Name of file to output logs to.</param>
        /// <returns>Task to export logs and get whether logs are exported successfully or not.</returns>
        public virtual Task<bool> ExportLogs(string outputFileName) => Task.Run(() =>
        {
            // create directory for log files to export
            var random = new Random();
            var rootLogDirectory = Path.Combine(this.Application.RootPrivateDirectoryPath, "Log");
            var exportLogDirectory = Path.Combine(rootLogDirectory, $"Export-{DateTime.UtcNow.Ticks}-{random.Next()}");
            try
            {
                if (this.Application.IsDebugMode)
                    this.Logger.LogDebug("Create directory '{exportLogDirectory}' for log files to export", exportLogDirectory);
                Directory.CreateDirectory(exportLogDirectory);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to create directory '{exportLogDirectory}' for log files to export", exportLogDirectory);
                return false;
            }

            // copy logs files
            try
            {
                foreach (var filePath in Directory.EnumerateFiles(rootLogDirectory))
                {
                    var fileName = Path.GetFileName(filePath);
                    if (this.Application.IsDebugMode)
                        this.Logger.LogDebug("Copy '{fileName}' to export directory", fileName);
                    File.Copy(filePath, Path.Combine(exportLogDirectory, fileName));
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to copy log files to directory '{exportLogDirectory}'", exportLogDirectory);
                Global.RunWithoutError(() => Directory.Delete(exportLogDirectory, true));
                return false;
            }

            // archive log files
            try
            {
                if (File.Exists(outputFileName))
                {
                    this.Logger.LogWarning("Delete '{outputFileName}' before archiving log file", outputFileName);
                    File.Delete(outputFileName);
                }
                ZipFile.CreateFromDirectory(exportLogDirectory, outputFileName, CompressionLevel.Optimal, false);
                return true;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to export logs to '{outputFileName}'", outputFileName);
                return false;
            }
            finally 
            {
                Global.RunWithoutError(() => Directory.Delete(exportLogDirectory, true));
            }
        });


        /// <summary>
        /// Version of .NET currently used by application.
        /// </summary>
        public Version FrameworkVersion { get; } = Environment.Version;


        /// <summary>
        /// Get URI of project on GitHub.
        /// </summary>
        public virtual Uri? GitHubProjectUri { get; }


        /// <summary>
        /// Get application icon.
        /// </summary>
        public virtual IBitmap Icon
        {
            get
            {
                if (this.icon != null)
                    return this.icon;
                this.icon = AvaloniaLocator.Current.GetService<IAssetLoader>().Let(loader =>
                {
                    if (loader != null)
                    {
                        var assembly = Assembly.GetEntryAssembly().AsNonNull();
                        var uri = new Uri($"avares://{assembly.GetName().Name}/{this.Application.Name}.ico");
                        if (loader.Exists(uri))
                            return loader.Open(uri).Use(stream => new Bitmap(stream));
                        uri = new Uri($"avares://{assembly.GetName().Name}/AppIcon.ico");
                        if (loader.Exists(uri))
                            return loader.Open(uri).Use(stream => new Bitmap(stream));
                    }
                    throw new NotImplementedException("Cannot load default icon.");
                });
                return this.icon;
            }
        }


        /// <summary>
        /// Get creator of application icon.
        /// </summary>
        public virtual ApplicationIconCreator IconCreator { get; } = ApplicationIconCreator.Freepik;


        /// <summary>
        /// Get website of application icon.
        /// </summary>
        public virtual ApplicationIconWebSite IconWebSite { get; } = ApplicationIconWebSite.Flaticon;


        /// <summary>
        /// Version of .NET installed on device.
        /// </summary>
        public Version? InstalledFrameworkVersion { get; private set; }


        /// <summary>
        /// Check whether .NET has been installed on device or not.
        /// </summary>
        public bool IsFrameworkInstalled { get; private set; }


        /// <summary>
        /// CHeck whether application is open source based or not.
        /// </summary>
        public virtual bool IsOpenSourceBased { get => true; }


        /// <summary>
        /// Get name of application.
        /// </summary>
        public string Name { get => this.Application.Name; }


        /// <summary>
        /// Description of operating system.
        /// </summary>
        public string OperatingSystemDescription { get; } = Global.Run(() =>
        {
            var osArchName = RuntimeInformation.OSArchitecture.ToString().ToUpperInvariant();
            var osVersion = Environment.OSVersion.Version;
            if (Platform.IsWindows)
            {
                return Platform.WindowsVersion switch
                {
                    WindowsVersion.Windows11 => "Windows 11",
                    WindowsVersion.Windows10 => "Windows 10",
                    WindowsVersion.Windows8 => "Windows 8",
                    WindowsVersion.Windows7 => "Windows 7",
                    _ => $"Windows",
                } + $" ({osVersion}, {osArchName})";
            }
            if (Platform.IsMacOS)
                return $"macOS {osVersion} ({osArchName})";
            if (Platform.IsLinux)
            {
                return Platform.LinuxDistribution switch
                {
                    LinuxDistribution.Debian => $"Debian (Linux kernel {osVersion}, {osArchName})",
                    LinuxDistribution.Fedora => $"Fedora (Linux kernel {osVersion}, {osArchName})",
                    LinuxDistribution.Ubuntu => $"Ubuntu (Linux kernel {osVersion}, {osArchName})",
                    _ => $"Linux kernel {osVersion} ({osArchName})",
                };
            }
            return $"{RuntimeInformation.OSDescription} ({osArchName})";
        });


        /// <summary>
        /// Get URI of PayPal for donation.
        /// </summary>
        public virtual Uri? PayPalUri { get; }


        /// <summary>
        /// Get all ID of products.
        /// </summary>
        public virtual IList<string> Products { get; } = Array.Empty<string>();


        /// <summary>
        /// Get type of application releasing.
        /// </summary>
        public ApplicationReleasingType ReleasingType { get => this.Application.ReleasingType; }


        /// <summary>
        /// Get application version.
        /// </summary>
        public Version Version { get; } = Assembly.GetEntryAssembly().AsNonNull().GetName().Version.AsNonNull();


        /// <summary>
        /// Get URI of application website.
        /// </summary>
        public virtual Uri? WebsiteUri { get; }
    }
}
