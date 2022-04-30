using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using System;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Dialog to show change list of current application.
    /// </summary>
    public class ApplicationChangeListDialog : CommonDialog<object?>
    {
        // Static fields.
        static readonly SettingKey<string> LatestShownVersionKey = new SettingKey<string>("ApplicationChangeListDialog.LatestShownVersion", "");


        // Fields.
        readonly ApplicationChangeList changeList;


        /// <summary>
        /// Initialize new <see cref="ApplicationChangeListDialog"/> instance.
        /// </summary>
        /// <param name="changeList">View-model.</param>
        public ApplicationChangeListDialog(ApplicationChangeList changeList) => this.changeList = changeList;


        /// <summary>
        /// Get latest version of application which change list has been shown to user before.
        /// </summary>
        /// <param name="app">Application.</param>
        public static Version? GetLatestShownVersion(IApplication app)
        {
            if (Version.TryParse(app.PersistentState.GetValueOrDefault(LatestShownVersionKey), out var version)
                && version != null)
            {
                return new Version(version.Major, version.Minor);
            }
            return null;
        }


        /// <summary>
        /// Check whether dialog is shown before for current version or not.
        /// </summary>
        /// <param name="app">Application.</param>
        public static bool IsShownBeforeForCurrentVersion(IApplication app) =>
            GetLatestShownVersion(app)?.Let(latestVersion => latestVersion >= app.Assembly.GetName().Version?.Let(it => new Version(it.Major, it.Minor))) ?? false;


        /// <summary>
        /// Reset all internal states which are related to showing dialog.
        /// </summary>
        /// <param name="app">Application.</param>
        public static void ResetShownState(IApplication app) =>
            app.PersistentState.ResetValue(LatestShownVersionKey);


        /// <summary>
		/// Show dialog.
		/// </summary>
		/// <param name="owner">Owner window.</param>
		/// <returns>Task to showing dialog.</returns>
        public new Task ShowDialog(Avalonia.Controls.Window owner) => base.ShowDialog(owner);


        /// <inheritdoc/>
        protected override Task<object?> ShowDialogCore(Avalonia.Controls.Window owner)
        {
            var dialog = new ApplicationChangeListDialogImpl()
            {
                DataContext = this.changeList
            };
            var showVersion = this.changeList.Version.Let(it => new Version(it.Major, it.Minor));
            this.changeList.Application.PersistentState.SetValue<string>(LatestShownVersionKey, showVersion.ToString());
            if (this.changeList.ChangeList.IsNotEmpty())
                return dialog.ShowDialog<object?>(owner);
            return Task.FromResult((object?)null); // skip showing because there is no change
        }
    }
}
