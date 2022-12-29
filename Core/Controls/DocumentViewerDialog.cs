using Avalonia.Media;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to show document.
/// </summary>
public class DocumentViewerDialog : CommonDialog<object?>
{
    // Fields.
	FontFamily documentFontFamily = AppSuiteApplication.CurrentOrNull?.Configuration?.GetValueOrDefault(ConfigurationKeys.DefaultFontFamilyOfDocument)?.Let(it =>
	{
		return new FontFamily(it);
	}) ?? FontFamily.Default;
    DocumentSource? documentSource;
    object? message;


    /// <summary>
    /// Initialize new <see cref="DocumentViewerDialog"/> instance.
    /// </summary>
    public DocumentViewerDialog()
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
	/// Get or set source of document.
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
	/// Show dialog.
	/// </summary>
	/// <param name="owner">Owner window.</param>
	/// <returns>Task to showing dialog.</returns>
	public new Task ShowDialog(Avalonia.Controls.Window? owner) => base.ShowDialog(owner);


    /// <inheritdoc/>
    protected override async Task<object?> ShowDialogCore(Avalonia.Controls.Window? owner)
    {
        if (documentSource == null)
            return null;
        var dialog = new DocumentViewerDialogImpl()
        {
			DocumentFontFamily = this.documentFontFamily,
            DocumentSource = this.documentSource,
			WindowStartupLocation = owner != null 
                ? Avalonia.Controls.WindowStartupLocation.CenterOwner
                : Avalonia.Controls.WindowStartupLocation.CenterScreen,
        };
        using var messageBindingToken = this.BindValueToDialog(dialog, DocumentViewerDialogImpl.MessageProperty, this.message);
		using var titleBindingToken = this.BindValueToDialog(dialog, DocumentViewerDialogImpl.TitleProperty, this.Title ?? Avalonia.Application.Current?.Name);
		return await (owner != null
			? dialog.ShowDialog<object?>(owner)
			: dialog.ShowDialog<object?>());
    }
}