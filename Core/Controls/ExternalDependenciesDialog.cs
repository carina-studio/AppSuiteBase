using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// External dependencies dialog.
/// </summary>
public class ExternalDependenciesDialog : CommonDialog<object?>
{
    // Fields.
    ExternalDependency? focusedExternalDependency;


    /// <summary>
    /// Get or set the external dependency to be focused when showing dialog.
    /// </summary>
    public ExternalDependency? FocusedExternalDependency
    {
        get => this.focusedExternalDependency;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.focusedExternalDependency = value;
        }
    }


    /// <summary>
    /// Show dialog.
    /// </summary>
    /// <param name="owner">Owner window.</param>
    /// <returns>Task to showing dialog.</returns>
    public new Task ShowDialog(Avalonia.Controls.Window? owner) => base.ShowDialog(owner);


    /// <inheritdoc/>
    protected override Task<object?> ShowDialogCore(Avalonia.Controls.Window? owner)
    {
        var dialog = new ExternalDependenciesDialogImpl()
        {
            FocusedExternalDependency = this.focusedExternalDependency,
            WindowStartupLocation = owner != null 
                ? Avalonia.Controls.WindowStartupLocation.CenterOwner
                : Avalonia.Controls.WindowStartupLocation.CenterScreen,
        };
        return owner != null
            ? dialog.ShowDialog<object?>(owner)
            : dialog.ShowDialog<object?>();
    }
}
