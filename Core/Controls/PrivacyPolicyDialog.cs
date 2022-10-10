using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Controls;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Dialog to show Privacy Policy and let user decide to agree or decline.
    /// </summary>
    public class PrivacyPolicyDialog : CommonDialog<bool>
    {
        // Fields.
        readonly ApplicationInfo appInfo;


        /// <summary>
        /// Initialize new <see cref="PrivacyPolicyDialog"/> instance.
        /// </summary>
        /// <param name="appInfo">View-model.</param>
        public PrivacyPolicyDialog(ApplicationInfo appInfo) => this.appInfo = appInfo;


        /// <inheritdoc/>
        protected override Task<bool> ShowDialogCore(Avalonia.Controls.Window? owner)
        {
            // check state
            if (this.appInfo.Application.IsPrivacyPolicyAgreed)
                return Task.FromResult(true);

            // show dialog
            var dialog = new PrivacyPolicyDialogImpl()
            {
                DataContext = this.appInfo,
            };
            return owner != null
                ? dialog.ShowDialog<bool>(owner)
                : dialog.ShowDialog<bool>();
        }
    }
}
