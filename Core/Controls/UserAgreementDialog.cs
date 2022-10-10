using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Controls;
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
        protected override Task<bool> ShowDialogCore(Avalonia.Controls.Window? owner)
        {
            // check state
            if (this.appInfo.Application.IsUserAgreementAgreed)
                return Task.FromResult(true);

            // show dialog
            var dialog = new UserAgreementDialogImpl()
            {
                DataContext = this.appInfo,
            };
            return owner != null
                ? dialog.ShowDialog<bool>(owner)
                : dialog.ShowDialog<bool>();
        }
    }
}
