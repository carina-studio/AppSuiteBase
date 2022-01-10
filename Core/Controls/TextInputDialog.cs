using CarinaStudio.Threading;
using System;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Dialog to let user input text.
    /// </summary>
    public class TextInputDialog : CommonDialog<string?>
    {
        // Fields.
        string? initialText;
        int maxTextLength = -1;
        string? message;


        /// <summary>
		/// Get or set initial text in dialog.
		/// </summary>
		public string? InitialText
        {
            get => this.initialText;
            set
            {
                this.VerifyAccess();
                this.VerifyShowing();
                this.initialText = value;
            }
        }


        /// <summary>
		/// Get or set maximum length of text to input.
		/// </summary>
		public int MaxTextLength
        {
            get => this.maxTextLength;
            set
            {
                this.VerifyAccess();
                this.VerifyShowing();
                this.maxTextLength = value;
            }
        }


        /// <summary>
		/// Get or set message shown in dialog.
		/// </summary>
		public string? Message
        {
            get => this.message;
            set
            {
                this.VerifyAccess();
                this.VerifyShowing();
                this.message = value;
            }
        }


        /// <summary>
        /// Show dialog.
        /// </summary>
        /// <param name="owner">Owner window.</param>
        /// <returns>Task to get result.</returns>
        protected override Task<string?> ShowDialogCore(Avalonia.Controls.Window owner)
        {
            var dialog = new TextInputDialogImpl();
            dialog.MaxTextLength = this.maxTextLength;
            dialog.Message = this.message;
            dialog.Text = this.initialText;
            dialog.Title = this.Title ?? Avalonia.Application.Current?.Name;
            return dialog.ShowDialog<string?>(owner);
        }
    }
}
