using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Application information dialog.
    /// </summary>
    public class ApplicationInfoDialog : CommonDialog<object?>
    {
        // Fields.
        readonly ApplicationInfo appInfo;
        ApplicationInfoDialogImpl? dialog;


        /// <summary>
        /// Initialize new <see cref="ApplicationInfoDialog"/> instance.
        /// </summary>
        /// <param name="appInfo">View-model.</param>
        public ApplicationInfoDialog(ApplicationInfo appInfo) => this.appInfo = appInfo;


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
		/// Show dialog.
		/// </summary>
		/// <param name="owner">Owner window.</param>
		/// <returns>Task to showing dialog.</returns>
        public new Task ShowDialog(Avalonia.Controls.Window? owner) => base.ShowDialog(owner);


        /// <summary>
        /// Called to show dialog and get result.
        /// </summary>
        /// <param name="owner">Owner window.</param>
        /// <returns>Task to get result.</returns>
        protected override async Task<object?> ShowDialogCore(Avalonia.Controls.Window? owner)
        {
            this.dialog = new ApplicationInfoDialogImpl()
            {
                DataContext = this.appInfo
            };
            try
            {
                if (owner != null)
                    await this.dialog.ShowDialog<object?>(owner);
                else
                    await this.dialog.ShowDialog<object?>();
                return null;
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
        public Task WaitForClosingDialogAsync()
        {
            this.VerifyAccess();
            if (this.dialog != null)
                return dialog.WaitForClosingAsync();
            return Task.CompletedTask;
        }
    }
}
