using CarinaStudio.AppSuite.ViewModels;
using System;
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
        protected override async Task<bool> ShowDialogCore(Avalonia.Controls.Window owner)
        {
            // check state
            if (this.appInfo.Application.IsPrivacyPolicyAgreed)
                return true;

            // show dialog
            return await new PrivacyPolicyDialogImpl()
            {
                DataContext = this.appInfo,
            }.ShowDialog<bool>(owner);
        }
    }
}
