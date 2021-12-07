using CarinaStudio.AppSuite.ViewModels;
using System;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Dialog to show User Agreement and let user decide to agree or decline.
    /// </summary>
    public class UserAgreementDialog : CommonDialog<bool>
    {
        // Fields.
        readonly ApplicationInfo appInfo;


        /// <summary>
        /// Initialize new <see cref="UserAgreementDialog"/> instance.
        /// </summary>
        /// <param name="appInfo">View-model.</param>
        public UserAgreementDialog(ApplicationInfo appInfo) => this.appInfo = appInfo;


        /// <inheritdoc/>
        protected override async Task<bool> ShowDialogCore(Avalonia.Controls.Window owner)
        {
            // check state
            if (this.appInfo.Application.IsUserAgreementAgreed)
                return true;

            // show dialog
            return await new UserAgreementDialogImpl()
            {
                DataContext = this.appInfo,
            }.ShowDialog<bool>(owner);
        }
    }
}
