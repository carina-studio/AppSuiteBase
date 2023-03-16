using Avalonia.Media;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Message dialog.
/// </summary>
public class MessageDialog : CommonDialog<MessageDialogResult>
{
	// Fields.
	MessageDialogButtons buttons = MessageDialogButtons.OK;
	object? customCancelText;
	object? customDoNotAskOrShowAgainText;
	IImage? customIcon;
	object? customNoText;
	object? customOKText;
	object? customYesText;
	MessageDialogResult? defaultResult;
	bool? doNotAskOrShowAgain;
	object? doNotAskOrShowAgainDescription;
	MessageDialogIcon icon = MessageDialogIcon.Information;
	object? message;
	object? secondaryMessage;


	/// <summary>
	/// Get or set buttons shown in dialog.
	/// </summary>
	public MessageDialogButtons Buttons
	{
		get => this.buttons;
		set
		{
			this.VerifyAccess();
			this.VerifyShowing();
			this.buttons = value;
		}
	}


	/// <summary>
	/// Get or set custom text for [Cancel] button.
	/// </summary>
	public object? CustomCancelText
	{
		get => this.customCancelText;
		set
		{
			this.VerifyAccess();
			this.VerifyShowing();
			this.customCancelText = value;
		}
	}


	/// <summary>
	/// Get or set custom text for "Do not show again" UI.
	/// </summary>
	public object? CustomDoNotAskOrShowAgainText
	{
		get => this.customDoNotAskOrShowAgainText;
		set
		{
			this.VerifyAccess();
			this.VerifyShowing();
			this.customDoNotAskOrShowAgainText = value;
		}
	}


	/// <summary>
	/// Get or set custom icon.
	/// </summary>
	public IImage? CustomIcon
	{
		get => this.customIcon;
		set
		{
			this.VerifyAccess();
			this.VerifyShowing();
			this.customIcon = value;
		}
	}


	/// <summary>
	/// Get or set custom text for [No] button.
	/// </summary>
	public object? CustomNoText
	{
		get => this.customNoText;
		set
		{
			this.VerifyAccess();
			this.VerifyShowing();
			this.customNoText = value;
		}
	}


	/// <summary>
	/// Get or set custom text for [OK] button.
	/// </summary>
	public object? CustomOKText
	{
		get => this.customOKText;
		set
		{
			this.VerifyAccess();
			this.VerifyShowing();
			this.customOKText = value;
		}
	}


	/// <summary>
	/// Get or set custom text for [Yes] button.
	/// </summary>
	public object? CustomYesText
	{
		get => this.customYesText;
		set
		{
			this.VerifyAccess();
			this.VerifyShowing();
			this.customYesText = value;
		}
	}


	/// <summary>
	/// Get or set default result of dialog.
	/// </summary>
	public MessageDialogResult? DefaultResult
	{
		get => this.defaultResult;
		set
		{
			this.VerifyAccess();
			this.VerifyShowing();
			this.defaultResult = value;
		}
	}


	/// <summary>
	/// Get or set whether "Do not ask me again" or "Do not show again" has been checked or not. Set Null to hide the UI.
	/// </summary>
	public bool? DoNotAskOrShowAgain
	{
		get => this.doNotAskOrShowAgain;
		set
		{
			this.VerifyAccess();
			this.VerifyShowing();
			this.doNotAskOrShowAgain = value;
		}
	}


	/// <summary>
	/// Get or set description of "Do not ask me again" or "Do not show again".
	/// </summary>
	public object? DoNotAskOrShowAgainDescription
	{
		get => this.doNotAskOrShowAgainDescription;
		set
		{
			this.VerifyAccess();
			this.VerifyShowing();
			this.doNotAskOrShowAgainDescription = value;
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
			this.VerifyAccess();
			this.VerifyShowing();
			this.icon = value;
		}
	}


	/// <summary>
	/// Get or set message shown in dialog.
	/// </summary>
	public object? Message
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
	/// Get or set secondary message shown in dialog.
	/// </summary>
	public object? SecondaryMessage
	{
		get => this.message;
		set
		{
			this.VerifyAccess();
			this.VerifyShowing();
			this.secondaryMessage = value;
		}
	}


	/// <summary>
	/// Show message dialog.
	/// </summary>
	/// <param name="owner">Owner window.</param>
	/// <returns>Task to get result of dialog.</returns>
	protected override async Task<MessageDialogResult> ShowDialogCore(Avalonia.Controls.Window? owner)
	{
		var dialog = new MessageDialogImpl()
		{
			Buttons = this.buttons,
			CustomIcon = this.icon == MessageDialogIcon.Custom ? this.customIcon : null,
			DefaultResult = this.defaultResult,
			DoNotAskOrShowAgain = this.doNotAskOrShowAgain,
			Icon = this.icon,
			Topmost = (owner?.Topmost).GetValueOrDefault(),
			WindowStartupLocation = owner != null 
                ? Avalonia.Controls.WindowStartupLocation.CenterOwner
                : Avalonia.Controls.WindowStartupLocation.CenterScreen,
		};
		using var customCancelTextBindingToken = this.BindValueToDialog(dialog, MessageDialogImpl.CustomCancelTextProperty, this.customCancelText);
		using var customDoNotAskOrShowAgainTextBindingToken = this.BindValueToDialog(dialog, MessageDialogImpl.CustomDoNotAskOrShowAgainTextProperty, this.customDoNotAskOrShowAgainText);
		using var customNoTextBindingToken = this.BindValueToDialog(dialog, MessageDialogImpl.CustomNoTextProperty, this.customNoText);
		using var customOKTextBindingToken = this.BindValueToDialog(dialog, MessageDialogImpl.CustomOKTextProperty, this.customOKText);
		using var customYesTextBindingToken = this.BindValueToDialog(dialog, MessageDialogImpl.CustomYesTextProperty, this.customYesText);
		using var doNotAskOrShowAgainDesBindingToken = this.BindValueToDialog(dialog, MessageDialogImpl.DoNotAskOrShowAgainDescriptionProperty, this.doNotAskOrShowAgainDescription);
		using var messageBindingToken = this.BindValueToDialog(dialog, MessageDialogImpl.MessageProperty, this.message);
		using var secondaryMessageBindingToken = this.BindValueToDialog(dialog, MessageDialogImpl.SecondaryMessageProperty, this.secondaryMessage);
		using var titleBindingToken = this.BindValueToDialog(dialog, MessageDialogImpl.TitleProperty, this.Title ?? Avalonia.Application.Current?.Name);
		var result = owner != null
			? await dialog.ShowDialog<MessageDialogResult>(owner)
			: await dialog.ShowDialog<MessageDialogResult>();
		this.doNotAskOrShowAgain = dialog.DoNotAskOrShowAgain;
		return result;
	}
}


/// <summary>
/// Combination of buttons of <see cref="MessageDialog"/>.
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
/// Icon of <see cref="MessageDialog"/>.
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
	/// Success.
	/// </summary>
	Success,
	/// <summary>
	/// Warning.
	/// </summary>
	Warning,
	/// <summary>
	/// Error.
	/// </summary>
	Error,
	/// <summary>
	/// Custom.
	/// </summary>
	Custom,
}


/// <summary>
/// Result of <see cref="MessageDialog"/>
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