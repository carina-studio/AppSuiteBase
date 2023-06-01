using Avalonia.Controls;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="Border"/> which is suitable for item in dialog.
/// </summary>
public class CompactDialogItemBorder : Border
{
    /// <summary>
    /// Initialize new <see cref="CompactDialogItemBorder"/> instance.
    /// </summary>
    public CompactDialogItemBorder()
    {
        this.Classes.Add("Dialog_Item_Container_Small");
    }


    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(Border);
}