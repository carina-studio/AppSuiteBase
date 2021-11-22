using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CarinaStudio.Collections;
using CarinaStudio.Threading;
using CarinaStudio.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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
        readonly ApplicationChangeList appChangeList = new ApplicationChangeList();


        /// <summary>
        /// Initialize new <see cref="ApplicationInfo"/> instance.
        /// </summary>
        public ApplicationInfo() : base(AppSuiteApplication.Current)
        {
            // get system info
            _ = Task.Run(() =>
            {
                var version = Platform.GetInstalledFrameworkVersion();
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
            this.Assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(it => it != appAssembly)
                .OrderBy(it => it.GetName().Name)
                .ToArray()
                .AsReadOnly();
        }


        /// <summary>
        /// Get change list of current version of application.
        /// </summary>
        public virtual ApplicationChangeList ApplicationChangeList { get => this.appChangeList; }


        /// <summary>
        /// Get assemblies of this application.
        /// </summary>
        public IList<Assembly> Assemblies { get; }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                this.appChangeList.Dispose();
            base.Dispose(disposing);
        }


        /// <summary>
        /// Export application logs to given file.
        /// </summary>
        /// <param name="outputFileName">Name of file to output logs to.</param>
        /// <returns>Task to export logs and get whether logs are exported successfully or not.</returns>
        public virtual Task<bool> ExportLogs(string outputFileName) => Task.Run(() =>
        {
            try
            {
                var srcFileName = Path.Combine(this.Application.RootPrivateDirectoryPath, "Log", "log.txt");
                File.Copy(srcFileName, outputFileName, true);
                return true;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, $"Failed to export logs to '{outputFileName}'");
                return false;
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
        public virtual IBitmap Icon { get; } = AvaloniaLocator.Current.GetService<IAssetLoader>().Let(loader =>
        {
            return loader.Open(new Uri($"avares://{Assembly.GetEntryAssembly().AsNonNull().GetName().Name}/AppIcon.ico")).Use(stream => new Bitmap(stream));
        });


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
        /// Get name of application.
        /// </summary>
        public string Name { get => this.Application.Name; }


        /// <summary>
        /// Description of operating system.
        /// </summary>
        public string OperatingSystemDescription { get; } = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture.ToString().ToUpperInvariant()})";


        /// <summary>
        /// Get URI of Privacy Policy.
        /// </summary>
        public virtual Uri? PrivacyPolicyUri { get; }


        /// <summary>
        /// Get type of application releasing.
        /// </summary>
        public ApplicationReleasingType ReleasingType { get => this.Application.ReleasingType; }


        /// <summary>
        /// Get URI of User Agreement.
        /// </summary>
        public virtual Uri? UserAgreementUri { get; }


        /// <summary>
        /// Get application version.
        /// </summary>
        public Version Version { get; } = Assembly.GetEntryAssembly().AsNonNull().GetName().Version.AsNonNull();
    }
}
