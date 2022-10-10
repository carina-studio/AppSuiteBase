using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Dialog to update application.
    /// </summary>
    public class ApplicationUpdateDialog : CommonDialog<ApplicationUpdateDialogResult>
    {
        // Fields.
        readonly ApplicationUpdater appUpdater;
        bool checkForUpdateWhenShowing;


        /// <summary>
        /// Initialize new <see cref="ApplicationUpdateDialog"/> instance.
        /// </summary>
        /// <param name="appUpdater">View-model of dialog.</param>
        public ApplicationUpdateDialog(ApplicationUpdater appUpdater) => this.appUpdater = appUpdater;


        /// <summary>
        /// Get or set whether update checking is needed when showing dialog or not.
        /// </summary>
        public bool CheckForUpdateWhenShowing
        {
            get => this.checkForUpdateWhenShowing;
            set
            {
                this.VerifyAccess();
                this.VerifyShowing();
                this.checkForUpdateWhenShowing = value;
            }
        }


        /// <summary>
        /// Get latest time of showing update info to user.
        /// </summary>
        public static DateTime? LatestShownTime
        {
            get => AppSuiteApplication.Current.PersistentState.GetValueOrDefault(ApplicationUpdateDialogImpl.LatestNotifiedTimeKey);
        }


        /// <summary>
        /// Get latest version of application update shown to user.
        /// </summary>
        public static Version? LatestShownVersion
        {
            get
            {
                var str = AppSuiteApplication.Current.PersistentState.GetValueOrDefault(ApplicationUpdateDialogImpl.LatestNotifiedVersionKey);
                if (Version.TryParse(str, out var version))
                    return version;
                return null;
            }
        }


        /// <summary>
        /// Reset <see cref="LatestShownTime"/> and <see cref="LatestShownVersion"/>.
        /// </summary>
        public static void ResetLatestShownInfo()
        {
            AppSuiteApplication.Current.PersistentState.Let(it =>
            {
                it.ResetValue(ApplicationUpdateDialogImpl.LatestNotifiedTimeKey);
                it.ResetValue(ApplicationUpdateDialogImpl.LatestNotifiedVersionKey);
            });
        }


        /// <summary>
        /// Called to show dialog and get result.
        /// </summary>
        /// <param name="owner">Owner window.</param>
        /// <returns>Task to get result.</returns>
        protected override async Task<ApplicationUpdateDialogResult> ShowDialogCore(Avalonia.Controls.Window? owner)
        {
            var dialog = new ApplicationUpdateDialogImpl()
            {
                CheckForUpdateWhenOpening = this.checkForUpdateWhenShowing,
                DataContext = this.appUpdater,
            };
            var result = await (owner != null 
                ? dialog.ShowDialog<ApplicationUpdateDialogResult?>(owner)
                : dialog.ShowDialog<ApplicationUpdateDialogResult?>());
            return result ?? ApplicationUpdateDialogResult.None;
        }
    }


    /// <summary>
    /// Result of <see cref="ApplicationUpdateDialog"/>.
    /// </summary>
    public enum ApplicationUpdateDialogResult
    {
        /// <summary>
        /// No action performed by user.
        /// </summary>
        None,
        /// <summary>
        /// Need to shutdown application to update.
        /// </summary>
        ShutdownNeeded,
        /// <summary>
        /// Update has been cancelled.
        /// </summary>
        UpdatingCancelled,
    }
}
