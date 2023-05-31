using Avalonia.Controls;
using Avalonia.Styling;
using CarinaStudio.Controls;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="Grid"/> which is suitable for item in dialog.
/// </summary>
public class DialogItemGrid : Grid, IStyleable
{
    /// <summary>
    /// Initialize new <see cref="DialogItemGrid"/> instance.
    /// </summary>
    public DialogItemGrid()
    {
        this.Classes.Add("Dialog_Item_Container");
        this.ColumnDefinitions.Add(new()
        {
            MinWidth = AppSuiteApplication.CurrentOrNull?.FindResourceOrDefault("Double/Dialog.Control.MinWidth", 0.0) ?? 0.0,
            Width = new(1, GridUnitType.Star),
        });
        this.ColumnDefinitions.Add(new()
        {
            Width = new(1, GridUnitType.Auto),
        });
    }
    
    
    /// <inheritdoc/>
    Type IStyleable.StyleKey => typeof(Grid);
}