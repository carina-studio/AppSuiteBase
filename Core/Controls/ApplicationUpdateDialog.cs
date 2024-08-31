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
        ApplicationUpdateDialogImpl? dialog;


        /// <summary>
        /// Initialize new <see cref="ApplicationUpdateDialog"/> instance.
        /// </summary>
        /// <param name="appUpdater">View-model of dialog.</param>
        public ApplicationUpdateDialog(ApplicationUpdater appUpdater) => this.appUpdater = appUpdater;


        /// <summary>
        /// Activate the shown dialog.
        /// </summary>
        /// <returns>True if dialog has been activated successfully.</returns>
        public bool Activate()
        {
            this.VerifyAccess();
            if (this.dialog != null)
            {
                this.dialog.ActivateAndBringToFront();
                return true;
            }
            return false;
        }


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
        /// Ignore current known application update information.
        /// </summary>
        public static void IgnoreCurrentUpdateInfo()
        {
            var app = IAppSuiteApplication.CurrentOrNull;
            if (app is null)
                return;
            var updateInfo = app.UpdateInfo;
            if (updateInfo is null || updateInfo.Version <= LatestShownVersion)
                return;
            ApplicationUpdateDialogImpl.UpdateLatestNotifiedInfo(app.PersistentState, updateInfo.Version);
        }


        /// <summary>
        /// Get latest time of showing update info to user.
        /// </summary>
        public static DateTime? LatestShownTime => Application.Current.PersistentState.GetValueOrDefault(ApplicationUpdateDialogImpl.LatestNotifiedTimeKey);


        /// <summary>
        /// Get latest version of application update shown to user.
        /// </summary>
        public static Version? LatestShownVersion
        {
            get
            {
                var str = Application.Current.PersistentState.GetValueOrDefault(ApplicationUpdateDialogImpl.LatestNotifiedVersionKey);
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
            Application.Current.PersistentState.Let(it =>
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
            this.dialog = new ApplicationUpdateDialogImpl
            {
                CheckForUpdateWhenOpening = this.checkForUpdateWhenShowing,
                DataContext = this.appUpdater,
                Topmost = (owner?.Topmost).GetValueOrDefault(),
                WindowStartupLocation = owner != null 
                    ? Avalonia.Controls.WindowStartupLocation.CenterOwner
                    : Avalonia.Controls.WindowStartupLocation.CenterScreen,
            };
            try
            {
                var result = await (owner != null 
                    ? this.dialog.ShowDialog<ApplicationUpdateDialogResult?>(owner)
                    : this.dialog.ShowDialog<ApplicationUpdateDialogResult?>());
                return result ?? ApplicationUpdateDialogResult.None;
            }
            finally
            {
                this.dialog = null;
            }
        }


        /// <summary>
        /// Wait for closing dialog asynchronously.
        /// </summary>
        /// <returns>Task of waiting.</returns>
        public Task<ApplicationUpdateDialogResult> WaitForClosingDialogAsync()
        {
            this.VerifyAccess();
            if (this.dialog != null)
                return dialog.WaitForClosingAsync();
            return Task.FromResult(ApplicationUpdateDialogResult.None);
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
