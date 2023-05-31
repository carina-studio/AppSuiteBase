using Avalonia.Controls;
using Avalonia.Styling;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="Border"/> which is suitable for item in dialog.
/// </summary>
public class DialogItemBorder : Border, IStyleable
{
    /// <summary>
    /// Initialize new <see cref="DialogItemBorder"/> instance.
    /// </summary>
    public DialogItemBorder()
    {
        this.Classes.Add("Dialog_Item_Container");
    }
    
    
    /// <inheritdoc/>
    Type IStyleable.StyleKey => typeof(Border);
}