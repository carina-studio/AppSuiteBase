using Avalonia.Media;
using CarinaStudio.Configuration;
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
	FontFamily documentFontFamily = IAppSuiteApplication.CurrentOrNull?.Configuration.GetValueOrDefault(ConfigurationKeys.DefaultFontFamilyOfAgreement).Let(it => new FontFamily(it)) ?? FontFamily.Default;
    DocumentSource? documentSource;
    bool isAgreedBefore;
    object? message;


    /// <summary>
    /// Initialize new <see cref="AgreementDialog"/> instance.
    /// </summary>
    public AgreementDialog()
    { }


	/// <summary>
    /// Get or set font family for showing document.
    /// </summary>
    public FontFamily DocumentFontFamily
    {
        get => this.documentFontFamily;
        set
		{
			this.VerifyAccess();
			this.VerifyShowing();
			this.documentFontFamily = value;
		}
    }


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
			DocumentFontFamily = this.documentFontFamily,
            DocumentSource = this.documentSource,
            IsAgreedBefore = this.isAgreedBefore,
			Topmost = owner == null || owner.Topmost,
			WindowStartupLocation = owner != null 
                ? Avalonia.Controls.WindowStartupLocation.CenterOwner
                : Avalonia.Controls.WindowStartupLocation.CenterScreen,
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