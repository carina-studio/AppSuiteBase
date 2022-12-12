using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to show agreement.
/// </summary>
public class AgreementDialog : CommonDialog<AgreementDialogResult>
{
    // Fields.
    DocumentSource? documentSource;
    bool isAgreedBefore;
    object? message;


    /// <summary>
    /// Initialize new <see cref="AgreementDialog"/> instance.
    /// </summary>
    public AgreementDialog()
    { }


    /// <summary>
	/// Get or set source of agreement document.
	/// </summary>
	public DocumentSource? DocumentSource
	{
		get => this.documentSource;
		set
		{
			this.VerifyAccess();
			this.VerifyShowing();
			this.documentSource = value;
		}
	}


    /// <summary>
	/// Get or set whether agreement was agreed before or not.
	/// </summary>
	public bool IsAgreedBefore
	{
		get => this.isAgreedBefore;
		set
		{
			this.VerifyAccess();
			this.VerifyShowing();
			this.isAgreedBefore = value;
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


    /// <inheritdoc/>
    protected override async Task<AgreementDialogResult> ShowDialogCore(Avalonia.Controls.Window? owner)
    {
        if (documentSource == null)
            return AgreementDialogResult.Declined;
        var dialog = new AgreementDialogImpl()
        {
            DocumentSource = this.documentSource,
            IsAgreedBefore = this.isAgreedBefore,
        };
        using var messageBindingToken = this.BindValueToDialog(dialog, AgreementDialogImpl.MessageProperty, this.message);
		using var titleBindingToken = this.BindValueToDialog(dialog, AgreementDialogImpl.TitleProperty, this.Title ?? Avalonia.Application.Current?.Name);
		return await (owner != null
			? dialog.ShowDialog<AgreementDialogResult>(owner)
			: dialog.ShowDialog<AgreementDialogResult>());
    }
}


/// <summary>
/// Result of <see cref="AgreementDialog"/>.
/// </summary>
public enum AgreementDialogResult
{
    /// <summary>
    /// User declined the agreement.
    /// </summary>
    Declined,
    /// <summary>
    /// User agreed the agreement.
    /// </summary>
    Agreed,
}