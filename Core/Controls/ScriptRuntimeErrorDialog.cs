using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog which shows the runtime error occured while running script.
/// </summary>
public class ScriptRuntimeErrorDialog : CommonDialog<object?>
{
    // Fields.
    Exception? error;
    object? message;


    /// <summary>
    /// Get or set the runtime error occurred.
    /// </summary>
    public Exception? Error
    {
        get => this.error;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.error = value;
        }
    }


    /// <summary>
    /// Get or set the message to be shown in dialog.
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
        if (this.error == null)
            return null;
        var dialog = new ScriptRuntimeErrorDialogImpl()
        {
            Error = error,
            Topmost = (owner?.Topmost).GetValueOrDefault(),
			WindowStartupLocation = owner != null 
                ? Avalonia.Controls.WindowStartupLocation.CenterOwner
                : Avalonia.Controls.WindowStartupLocation.CenterScreen,
        };
        using var messageBindingToken = this.BindValueToDialog(dialog, ScriptRuntimeErrorDialogImpl.MessageProperty, this.message ?? AppSuiteApplication.CurrentOrNull?.GetObservableString("ScriptRuntimeErrorDialog.DefaultMessage"));
        await (owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog<object?>());
        return null;
    }
}