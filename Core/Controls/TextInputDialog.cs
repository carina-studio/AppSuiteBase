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
    bool? doNotAskAgain;
    bool? doNotShowAgain;
    string? initialText;
    int maxTextLength = -1;
    object? message;


    /// <summary>
    /// Get or set whether "Do not ask me again" has been checked or not. Set Null to hide the UI.
    /// </summary>
    public bool? DoNotAskAgain
    {
        get => this.doNotAskAgain;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.doNotAskAgain = value;
        }
    }


    /// <summary>
    /// Get or set whether "Do not show again" has been checked or not. Set Null to hide the UI.
    /// </summary>
    public bool? DoNotShowAgain
    {
        get => this.doNotShowAgain;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.doNotShowAgain = value;
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
        var dialog = new TextInputDialogImpl();
        dialog.DoNotAskAgain = this.doNotAskAgain;
        dialog.DoNotShowAgain = this.doNotShowAgain;
        dialog.MaxTextLength = this.maxTextLength;
        using var messageBindingToken = this.BindValueToDialog(dialog, TextInputDialogImpl.MessageProperty, this.message);
        dialog.Text = this.initialText;
        using var titleBindingToken = this.BindValueToDialog(dialog, MessageDialogImpl.TitleProperty, this.Title ?? Avalonia.Application.Current?.Name);
        var result = await (owner != null
            ? dialog.ShowDialog<string?>(owner)
            : dialog.ShowDialog<string?>());
        this.doNotAskAgain = dialog.DoNotAskAgain;
        this.doNotShowAgain = dialog.DoNotShowAgain;
        return result;
    }
}