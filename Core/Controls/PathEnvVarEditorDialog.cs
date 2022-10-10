using CarinaStudio.Controls;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to edit PATH environment variable.
/// </summary>
public class PathEnvVarEditorDialog : CommonDialog<bool>
{
    /// <summary>
    /// Initialize new <see cref="PathEnvVarEditorDialog"/> instance.
    /// </summary>
    public PathEnvVarEditorDialog()
    { }


    /// <summary>
    /// Check whether dialog is supported on current platform or not.
    /// </summary>
    public static bool IsSupported 
    { 
        get => Platform.IsMacOS || Platform.IsWindows;
    }


    /// <summary>
    /// Called to show dialog and get result.
    /// </summary>
    /// <param name="owner">Owner window.</param>
    /// <returns>Task to get result.</returns>
    protected override async Task<bool> ShowDialogCore(Avalonia.Controls.Window? owner)
    {
        if (IsSupported)
        {
            var dialog = new PathEnvVarEditorDialogImpl();
            var result = await (owner != null ? dialog.ShowDialog<bool?>(owner) : dialog.ShowDialog<bool?>());
            return result.GetValueOrDefault();
        }
        return false;
    } 
}