using Avalonia;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Extended <see cref="Avalonia.Controls.TabItem"/>.
/// </summary>
public class TabItem : Avalonia.Controls.TabItem
{
    /// <summary>
    /// Define <see cref="IsFirstItem"/> property.
    /// </summary>
    public static readonly DirectProperty<TabItem, bool> IsFirstItemProperty = AvaloniaProperty.RegisterDirect<TabItem, bool>(nameof(IsFirstItem), i => i.isFirstItem);
    /// <summary>
    /// Define <see cref="IsLastItem"/> property.
    /// </summary>
    public static readonly DirectProperty<TabItem, bool> IsLastItemProperty = AvaloniaProperty.RegisterDirect<TabItem, bool>(nameof(IsLastItem), i => i.isLastItem);
    /// <summary>
    /// Define <see cref="IsNextItemPointerOver"/> property.
    /// </summary>
    public static readonly DirectProperty<TabItem, bool> IsNextItemPointerOverProperty = AvaloniaProperty.RegisterDirect<TabItem, bool>(nameof(IsNextItemPointerOver), i => i.isNextItemPointerOver);
    /// <summary>
    /// Define <see cref="IsNextItemSelected"/> property.
    /// </summary>
    public static readonly DirectProperty<TabItem, bool> IsNextItemSelectedProperty = AvaloniaProperty.RegisterDirect<TabItem, bool>(nameof(IsNextItemSelected), i => i.isNextItemSeleted);
    /// <summary>
    /// Define <see cref="IsPreviousItemPointerOver"/> property.
    /// </summary>
    public static readonly DirectProperty<TabItem, bool> IsPreviousItemPointerOverProperty = AvaloniaProperty.RegisterDirect<TabItem, bool>(nameof(IsPreviousItemPointerOver), i => i.isPreviousItemPointerOverPrevItem);
    /// <summary>
    /// Define <see cref="IsPreviousItemSelected"/> property.
    /// </summary>
    public static readonly DirectProperty<TabItem, bool> IsPreviousItemSelectedProperty = AvaloniaProperty.RegisterDirect<TabItem, bool>(nameof(IsPreviousItemSelected), i => i.isPreviousItemSelected);
    
    
    // Fields.
    bool isFirstItem;
    bool isLastItem;
    bool isNextItemPointerOver;
    bool isNextItemSeleted;
    bool isPreviousItemPointerOverPrevItem;
    bool isPreviousItemSelected;
    
    
    /// <summary>
    /// Check whether the item is the first one in the <see cref="TabControl"/> or not.
    /// </summary>
    public bool IsFirstItem
    {
        get => this.isFirstItem;
        internal set => this.SetAndRaise(IsFirstItemProperty, ref this.isFirstItem, value);
    }
    
    
    /// <summary>
    /// Check whether the item is the last one in the <see cref="TabControl"/> or not.
    /// </summary>
    public bool IsLastItem
    {
        get => this.isLastItem;
        internal set => this.SetAndRaise(IsLastItemProperty, ref this.isLastItem, value);
    }
    
    
    /// <summary>
    /// Check whether the pointer is over the next item or not.
    /// </summary>
    public bool IsNextItemPointerOver
    {
        get => this.isNextItemPointerOver;
        internal set => this.SetAndRaise(IsNextItemPointerOverProperty, ref this.isNextItemPointerOver, value);
    }
    
    
    /// <summary>
    /// Check whether the next item is selected or not.
    /// </summary>
    public bool IsNextItemSelected
    {
        get => this.isNextItemSeleted;
        internal set => this.SetAndRaise(IsNextItemSelectedProperty, ref this.isNextItemSeleted, value);
    }
    
    
    /// <summary>
    /// Check whether the pointer is over the next item or not.
    /// </summary>
    public bool IsPreviousItemPointerOver
    {
        get => this.isPreviousItemPointerOverPrevItem;
        internal set => this.SetAndRaise(IsPreviousItemPointerOverProperty, ref this.isPreviousItemPointerOverPrevItem, value);
    }
    
    
    /// <summary>
    /// Check whether the previous item is selected or not.
    /// </summary>
    public bool IsPreviousItemSelected
    {
        get => this.isPreviousItemSelected;
        internal set => this.SetAndRaise(IsPreviousItemSelectedProperty, ref this.isPreviousItemSelected, value);
    }


    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(Avalonia.Controls.TabItem);
}