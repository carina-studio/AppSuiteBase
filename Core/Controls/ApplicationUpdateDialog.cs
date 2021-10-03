using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Threading;
using System;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Dialog to update application.
    /// </summary>
    /// <typeparam name="TAppUpdater">View-model of dialog.</typeparam>
    public class ApplicationUpdateDialog<TAppUpdater> : CommonDialog<ApplicationUpdateDialogResult> where TAppUpdater : ApplicationUpdater
    {
        // Fields.
        readonly TAppUpdater appUpdater;
        bool checkForUpdateWhenShowing;


        /// <summary>
        /// Initialize new <see cref="ApplicationUpdateDialog{TAppUpdater}"/> instance.
        /// </summary>
        /// <param name="appUpdater">View-model of dialog.</param>
        public ApplicationUpdateDialog(TAppUpdater appUpdater) => this.appUpdater = appUpdater;


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
        /// Called to show dialog and get result.
        /// </summary>
        /// <param name="owner">Owner window.</param>
        /// <returns>Task to get result.</returns>
        protected override async Task<ApplicationUpdateDialogResult> ShowDialogCore(Avalonia.Controls.Window owner)
        {
            var result = await new ApplicationUpdateDialogImpl()
            {
                CheckForUpdateWhenOpening = this.checkForUpdateWhenShowing,
                DataContext = this.appUpdater,
            }.ShowDialog<ApplicationUpdateDialogResult?>(owner);
            return result ?? ApplicationUpdateDialogResult.None;
        }
    }


    /// <summary>
    /// Result of <see cref="ApplicationUpdateDialog{TAppUpdater}"/>.
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
