using CarinaStudio.AppSuite.ViewModels;
using System;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Application information dialog.
    /// </summary>
    /// <typeparam name="TAppInfo">Type of view-model.</typeparam>
    public class ApplicationInfoDialog<TAppInfo> : CommonDialog<object?> where TAppInfo : ApplicationInfo
    {
        // Fields.
        readonly TAppInfo appInfo;


        /// <summary>
        /// Initialize new <see cref="ApplicationInfoDialog{TAppInfo}"/> instance.
        /// </summary>
        /// <param name="appInfo">View-model.</param>
        public ApplicationInfoDialog(TAppInfo appInfo) => this.appInfo = appInfo;


        /// <summary>
		/// Show dialog.
		/// </summary>
		/// <param name="owner">Owner window.</param>
		/// <returns>Task to showing dialog.</returns>
        public new Task ShowDialog(Avalonia.Controls.Window owner) => base.ShowDialog(owner);


        /// <summary>
        /// Called to show dialog and get result.
        /// </summary>
        /// <param name="owner">Owner window.</param>
        /// <returns>Task to get result.</returns>
        protected override Task<object?> ShowDialogCore(Avalonia.Controls.Window owner)
        {
            var dialog = new ApplicationInfoDialogImpl()
            {
                DataContext = this.appInfo
            };
            return dialog.ShowDialog<object?>(owner);
        }
    }
}
