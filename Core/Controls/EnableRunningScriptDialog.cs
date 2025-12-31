using CarinaStudio.Configuration;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to let user enable script running.
/// </summary>
public class EnableRunningScriptDialog : CommonDialog<bool>
{
    /// <inheritdoc/>
    protected override async Task<bool> ShowDialogCore(Avalonia.Controls.Window? owner)
    {
        var app = Application.CurrentOrNull;
        if (app == null)
            return false;
        if (app.Settings.GetValueOrDefault(SettingKeys.EnableRunningScript))
            return true;
        var result = await new MessageDialog()
        {
            Buttons = MessageDialogButtons.OKCancel,
            CustomOKText = app.GetObservableString("Common.Enable"),
            DefaultResult = MessageDialogResult.Cancel,
            Icon = MessageDialogIcon.Warning,
            Message = app.GetObservableString("EnableRunningScriptDialog.Message"),
        }.ShowDialog(owner);
        if (result == MessageDialogResult.OK)
        {
            app.Settings.SetValue(SettingKeys.EnableRunningScript, true);
            return true;
        }
        return false;
    }
}