using CarinaStudio.Threading;
using CarinaStudio.Controls;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to let user input text.
/// </summary>
public class TextInputDialog : CommonDialog<string?>
{
    // Fields.
    object? checkBoxDescription;
    object? checkBoxMessage;
    string? initialText;
    bool? isCheckBoxChecked;
    int maxTextLength = -1;
    object? message;


    /// <summary>
    /// Get or set description of check box.
    /// </summary>
    public object? CheckBoxDescription
    {
        get => this.checkBoxDescription;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.checkBoxDescription = value;
        }
    }


    /// <summary>
    /// Get or set message of check box.
    /// </summary>
    public object? CheckBoxMessage
    {
        get => this.checkBoxMessage;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.checkBoxMessage = value;
        }
    }


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
    /// Get or set whether "Do not show again" has been checked or not. Set Null to hide the UI.
    /// </summary>
    public bool? IsCheckBoxChecked
    {
        get => this.isCheckBoxChecked;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.isCheckBoxChecked = value;
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
    /// <returns>Task to get result.</returns>
    protected override async Task<string?> ShowDialogCore(Avalonia.Controls.Window? owner)
    {
        var dialog = new TextInputDialogImpl()
        {
            IsCheckBoxChecked = this.isCheckBoxChecked,
            MaxTextLength = this.maxTextLength,
            Topmost = (owner?.Topmost).GetValueOrDefault(),
            WindowStartupLocation = owner != null 
                ? Avalonia.Controls.WindowStartupLocation.CenterOwner
                : Avalonia.Controls.WindowStartupLocation.CenterScreen,
        };
        using var checkBoxDescBindingToken = this.BindValueToDialog(dialog, TextInputDialogImpl.CheckBoxDescriptionProperty, this.checkBoxDescription);
        using var checkBoxMessageBindingToken = this.BindValueToDialog(dialog, TextInputDialogImpl.CheckBoxMessageProperty, this.checkBoxMessage);
        using var messageBindingToken = this.BindValueToDialog(dialog, TextInputDialogImpl.MessageProperty, this.message);
        dialog.Text = this.initialText;
        using var titleBindingToken = this.BindValueToDialog(dialog, MessageDialogImpl.TitleProperty, this.Title ?? Avalonia.Application.Current?.Name);
        var result = await (owner != null
            ? dialog.ShowDialog<string?>(owner)
            : dialog.ShowDialog<string?>());
        this.isCheckBoxChecked = dialog.IsCheckBoxChecked;
        return result;
    }
}