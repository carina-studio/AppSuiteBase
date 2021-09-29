using Avalonia.Controls;
using System;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// Message dialog.
	/// </summary>
	public class MessageDialog
	{
		// Fields.
		MessageDialogButtons buttons = MessageDialogButtons.OK;
		MessageDialogIcon icon = MessageDialogIcon.Information;
		bool isDialogShowing;
		string? message;
		string? title;


		/// <summary>
		/// Get or set buttons shown in dialog.
		/// </summary>
		public MessageDialogButtons Buttons
		{
			get => this.buttons;
			set
			{
				this.VerifyShowing();
				this.buttons = value;
			}
		}


		/// <summary>
		/// Get or set icon shown in dialog.
		/// </summary>
		public MessageDialogIcon Icon
		{
			get => this.icon;
			set
			{
				this.VerifyShowing();
				this.icon = value;
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
				this.VerifyShowing();
				this.message = value;
			}
		}


		/// <summary>
		/// Show message dialog.
		/// </summary>
		/// <param name="owner">Owner window.</param>
		/// <returns>Task to get result of dialog.</returns>
		public async Task<MessageDialogResult> ShowDialog(Window owner)
		{
			// check state
			this.VerifyShowing();
			owner.VerifyAccess();

			// update state
			this.isDialogShowing = true;

			// show dialog
			try
			{
				var dialog = new MessageDialogImpl();
				dialog.Buttons = this.buttons;
				dialog.Icon = this.icon;
				dialog.Message = this.message;
				dialog.Title = this.title ?? Avalonia.Application.Current.Name;
				return await dialog.ShowDialog<MessageDialogResult>(owner);
			}
			finally
			{
				this.isDialogShowing = false;
			}
		}


		/// <summary>
		/// Get or set title of dialog.
		/// </summary>
		public string? Title
		{
			get => this.title;
			set
			{
				this.VerifyShowing();
				this.title = value;
			}
		}


		// Throw exception if dialog is showing.
		void VerifyShowing()
		{
			if (this.isDialogShowing)
				throw new InvalidOperationException("Cannot perform operation when dialog is showing.");
		}
	}


	/// <summary>
	/// Combination of buttons of <see cref="MessageDialog{TApp}"/>.
	/// </summary>
	public enum MessageDialogButtons
	{
		/// <summary>
		/// OK.
		/// </summary>
		OK,
		/// <summary>
		/// OK and Cancel.
		/// </summary>
		OKCancel,
		/// <summary>
		/// Yes and No.
		/// </summary>
		YesNo,
		/// <summary>
		/// Yes, No and Cancel.
		/// </summary>
		YesNoCancel,
	}


	/// <summary>
	/// Icon of <see cref="MessageDialog{TApp}"/>.
	/// </summary>
	public enum MessageDialogIcon
	{
		/// <summary>
		/// Information.
		/// </summary>
		Information,
		/// <summary>
		/// Question.
		/// </summary>
		Question,
		/// <summary>
		/// Warning.
		/// </summary>
		Warning,
		/// <summary>
		/// Error.
		/// </summary>
		Error,
	}


	/// <summary>
	/// Result of <see cref="MessageDialog{TApp}"/>
	/// </summary>
	public enum MessageDialogResult
	{
		/// <summary>
		/// OK.
		/// </summary>
		OK,
		/// <summary>
		/// Cancel.
		/// </summary>
		Cancel,
		/// <summary>
		/// Yes.
		/// </summary>
		Yes,
		/// <summary>
		/// No.
		/// </summary>
		No,
	}
}
