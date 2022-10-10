using CarinaStudio.Controls;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// External dependencies dialog.
/// </summary>
public class ExternalDependenciesDialog : CommonDialog<object?>
{
    /// <summary>
    /// Show dialog.
    /// </summary>
    /// <param name="owner">Owner window.</param>
    /// <returns>Task to showing dialog.</returns>
    public new Task ShowDialog(Avalonia.Controls.Window? owner) => base.ShowDialog(owner);


    /// <inheritdoc/>
    protected override Task<object?> ShowDialogCore(Avalonia.Controls.Window? owner)
    {
        var dialog = new ExternalDependenciesDialogImpl();
        return owner != null
            ? dialog.ShowDialog<object?>(owner)
            : dialog.ShowDialog<object?>();
    }
}
