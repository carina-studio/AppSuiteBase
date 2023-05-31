using Avalonia.Controls;
using Avalonia.Styling;
using CarinaStudio.Controls;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="Grid"/> which is suitable for compact item in dialog.
/// </summary>
public class CompactDialogItemGrid : Grid, IStyleable
{
    /// <summary>
    /// Initialize new <see cref="CompactDialogItemGrid"/> instance.
    /// </summary>
    public CompactDialogItemGrid()
    {
        this.Classes.Add("Dialog_Item_Container_Small");
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