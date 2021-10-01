using Avalonia.Media.Imaging;
using CarinaStudio.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.ViewModels
{
    /// <summary>
    /// View-model of application information UI.
    /// </summary>
    public abstract class ApplicationInfo : ViewModel<IAppSuiteApplication>
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


        /// <summary>
        /// Initialize new <see cref="ApplicationInfo"/> instance.
        /// </summary>
        protected ApplicationInfo() : base(AppSuiteApplication.Current)
        { }


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
        /// Get URI of project on GitHub.
        /// </summary>
        public virtual Uri? GitHubProjectUri { get; }


        /// <summary>
        /// Get application icon.
        /// </summary>
        public abstract IBitmap Icon { get; }


        /// <summary>
        /// Get creator of application icon.
        /// </summary>
        public virtual ApplicationIconCreator IconCreator { get; } = ApplicationIconCreator.Freepik;


        /// <summary>
        /// Get website of application icon.
        /// </summary>
        public virtual ApplicationIconWebSite IconWebSite { get; } = ApplicationIconWebSite.Flaticon;


        /// <summary>
        /// Get name of application.
        /// </summary>
        public string Name { get => this.Application.Name; }


        /// <summary>
        /// Get URI of Privacy Policy.
        /// </summary>
        public virtual Uri? PrivacyPolicyUri { get; }


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
