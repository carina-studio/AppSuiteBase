using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to let user select one or more items.
/// </summary>
public class ItemSelectionDialog : CommonDialog<IList<object?>>
{
    // Fields.
    bool canSelectMultipleItems;
    object? defaultItem;
    bool? doNotAskAgain;
    IList? items;
    object? message;
    
    
    /// <summary>
    /// Get or set whether more than one items can be selected or not.
    /// </summary>
    public bool CanSelectMultipleItems
    {
        get => this.canSelectMultipleItems;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.canSelectMultipleItems = value;
        }
    }
    
    
    /// <summary>
    /// Get or set default selected item.
    /// </summary>
    public object? DefaultItem
    {
        get => this.defaultItem;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.defaultItem = value;
        }
    }
    
    
    /// <summary>
    /// Get or set whether "Do not ask again" is checked or not.
    /// </summary>
    public bool? DoNotAskAgain
    {
        get => this.doNotAskAgain;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.doNotAskAgain = value;
        }
    }
    
    
    /// <summary>
    /// Get or set items for user to select.
    /// </summary>
    public IList? Items
    {
        get => this.items;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.items = value;
        }
    }
    
    
    /// <summary>
    /// Get or set message.
    /// </summary>
    public object? Message
    {
        get => this.message;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.message = value;
        }
    }
    
    
    /// <inheritdoc/>
    protected override async Task<IList<object?>> ShowDialogCore(Avalonia.Controls.Window? owner)
    {
        // check state
        if (this.items is null)
            throw new InvalidOperationException("No items for user to select.");
        if (this.items.Count == 0)
            return Array.Empty<object?>();
        
        // show dialog
        var dialog = new ItemSelectionDialogImpl();
        IDisposable messageBindingToken;
        IList<object?> result;
        dialog.CanSelectMultipleItems = this.canSelectMultipleItems;
        dialog.DefaultItem = this.defaultItem;
        dialog.DoNotAskAgain = this.doNotAskAgain;
        dialog.Items = this.items;
        if (this.message is not null)
            messageBindingToken = this.BindValueToDialog(dialog, ItemSelectionDialogImpl.MessageProperty, this.message);
        else if (this.canSelectMultipleItems)
            messageBindingToken = dialog.BindToResource(ItemSelectionDialogImpl.MessageProperty, "String/ItemSelectionDialog.SelectOneOrMoreItems");
        else
            messageBindingToken = dialog.BindToResource(ItemSelectionDialogImpl.MessageProperty, "String/ItemSelectionDialog.SelectOneItem");
        if (owner is not null)
        {
            dialog.Topmost = owner.Topmost;
            result = await dialog.ShowDialog<IList<object?>>(owner);
        }
        else
            result = await dialog.ShowDialog<IList<object?>>();
        
        // complete
        messageBindingToken.Dispose();
        this.doNotAskAgain = dialog.DoNotAskAgain;
        return result;
    }
}