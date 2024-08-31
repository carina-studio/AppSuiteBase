using CarinaStudio.Configuration;
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
        var app = Application.CurrentOrNull;
        if (this.error == null || app == null)
            return null;
        var result = await new MessageDialog()
        {
            Buttons = MessageDialogButtons.YesNo,
            CustomDoNotAskOrShowAgainText = app.GetObservableString("ScriptRuntimeErrorDialog.PromptWhenScriptRuntimeErrorOccurred"),
            CustomNoText = app.GetObservableString("Common.Close"),
            CustomYesText = app.GetObservableString("Common.OpenScriptLogWindow"),
            DoNotAskOrShowAgain = app.Settings.GetValueOrDefault(SettingKeys.PromptWhenScriptRuntimeErrorOccurred),
            DoNotAskOrShowAgainDescription = app.GetObservableString("ScriptRuntimeErrorDialog.PromptWhenScriptRuntimeErrorOccurred.Description"),
            Icon = MessageDialogIcon.Error,
            Message = this.message ?? app.GetObservableString("ScriptRuntimeErrorDialog.DefaultMessage"),
            SecondaryMessage = this.error.Let(ex =>
            {
                return new FormattedString().Also(it =>
                {
                    it.Arg1 = ex.Message;
					if (ex is Scripting.ScriptException scriptException && scriptException.Line > 0)
					{
						it.Arg2 = scriptException.Line;
						if (scriptException.Column >= 0)
						{
							it.Arg3 = scriptException.Column;
							it.Bind(FormattedString.FormatProperty, app.GetObservableString("ScriptRuntimeErrorDialog.ErrorMessage.WithLineColumn"));
						}
						else
							it.Bind(FormattedString.FormatProperty, app.GetObservableString("ScriptRuntimeErrorDialog.ErrorMessage.WithLine"));
					}
					else
						it.Bind(FormattedString.FormatProperty, app.GetObservableString("ScriptRuntimeErrorDialog.ErrorMessage"));
                });
            }),
        }.ShowDialog(owner);
        if (result == MessageDialogResult.Yes)
            Scripting.ScriptManager.Default.OpenLogWindow();
        return null;
    }
}