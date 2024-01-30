using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using CarinaStudio.Controls;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="Grid"/> which is suitable for item in dialog.
/// </summary>
public class DialogItemGrid : Grid
{
    // Fields.
    readonly ColumnDefinition firstColumn;
    double minControlWidth;
    readonly ColumnDefinition secondColumn;
    
    
    /// <summary>
    /// Initialize new <see cref="DialogItemGrid"/> instance.
    /// </summary>
    public DialogItemGrid()
    {
        this.Classes.Add("Dialog_Item_Container");
        this.firstColumn = new(0, GridUnitType.Pixel);
        this.secondColumn = new(0, GridUnitType.Pixel);
        this.ColumnDefinitions.Add(this.firstColumn);
        this.ColumnDefinitions.Add(this.secondColumn);
    }


    /// <inheritdoc/>
    protected override Size MeasureOverride(Size constraint)
    {
        var containerSize = base.MeasureOverride(constraint);
        if (double.IsFinite(constraint.Width) && this.HorizontalAlignment == HorizontalAlignment.Stretch)
            containerSize = new(constraint.Width, containerSize.Height);
        if (containerSize.Width > 0)
        {
            // measure 2nd column
            var secondColumnWidth = 0.0;
            var secondColumnMaxWidth = Math.Max(0, containerSize.Width - minControlWidth);
            foreach (var child in this.Children)
            {
                if (GetColumnSpan(child) > 1 || GetColumn(child) != 1)
                    continue;
                child.Measure(new(secondColumnMaxWidth, containerSize.Height));
                secondColumnWidth = Math.Max(secondColumnWidth, child.DesiredSize.Width);
            }
            secondColumnWidth = Math.Min(secondColumnWidth, secondColumnMaxWidth);
            
            // measure 1st column
            var firstColumnWidth = 0.0;
            var firstColumnMaxWidth = containerSize.Width - secondColumnWidth;
            foreach (var child in this.Children)
            {
                if (GetColumnSpan(child) > 1 || GetColumn(child) != 0)
                    continue;
                child.Measure(new(firstColumnMaxWidth, containerSize.Height));
                firstColumnWidth = Math.Max(firstColumnWidth, child.DesiredSize.Width);
            }
            firstColumnWidth = Math.Min(firstColumnWidth, firstColumnMaxWidth);
            
            // setup column widths
            this.firstColumn.Width = new(firstColumnWidth, GridUnitType.Pixel);
            this.secondColumn.Width = new(containerSize.Width - firstColumnWidth, GridUnitType.Pixel);
        }
        return containerSize;
    }


    /// <inheritdoc/>
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        this.minControlWidth = this.FindResourceOrDefault<double>("Double/Dialog.Control.MinWidth");
    }


    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(Grid);
}