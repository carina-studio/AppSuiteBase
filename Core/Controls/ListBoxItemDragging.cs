using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using CarinaStudio.Input;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Helper class to handle item dragging in <see cref="ListBox"/>.
/// </summary>
public class ListBoxItemDragging
{
    /// <summary>
    /// Define IsItemDraggingEnabled attached property.
    /// </summary>
    public static readonly AttachedProperty<bool> IsItemDraggingEnabledProperty = AvaloniaProperty.RegisterAttached<ListBoxItemDragging, ListBox, bool>("IsItemDraggingEnabled");
    /// <summary>
    /// Define event raised when dragging has been cancelled.
    /// </summary>
    public static readonly RoutedEvent<ListBoxItemDragEventArgs> ItemDragCancelledEvent = RoutedEvent.Register<ListBoxItemDragEventArgs>("ItemDragCancelled", RoutingStrategies.Bubble, typeof(ListBoxItemDragging));
    /// <summary>
    /// Define event raised when user dragged an item.
    /// </summary>
    public static readonly RoutedEvent<ListBoxItemDragEventArgs> ItemDraggedEvent = RoutedEvent.Register<ListBoxItemDragEventArgs>("ItemDragged", RoutingStrategies.Bubble, typeof(ListBoxItemDragging));
    /// <summary>
    /// Define event raised when user dragged the item out from the <see cref="Avalonia.Controls.ListBox"/>.
    /// </summary>
    public static readonly RoutedEvent<ListBoxItemDragEventArgs> ItemDragLeavedEvent = RoutedEvent.Register<ListBoxItemDragEventArgs>("ItemDragLeaved", RoutingStrategies.Bubble, typeof(ListBoxItemDragging));
    /// <summary>
    /// Define event raised when user start dragging an item.
    /// </summary>
    public static readonly RoutedEvent<ListBoxItemDragEventArgs> ItemDragStartedEvent = RoutedEvent.Register<ListBoxItemDragEventArgs>("ItemDragStarted", RoutingStrategies.Bubble, typeof(ListBoxItemDragging));
    /// <summary>
    /// Define event raised when user dropped on an item.
    /// </summary>
    public static readonly RoutedEvent<ListBoxItemDragEventArgs> ItemDroppedEvent = RoutedEvent.Register<ListBoxItemDragEventArgs>("ItemDropped", RoutingStrategies.Bubble, typeof(ListBoxItemDragging));


    // Information of dragging item of list box.
    class ItemDraggingInfo(int startItemIndex, object? startItem)
    {
        public IDisposable? CursorValueToken;
        public bool IsCancelled;
        public int LastItemIndex = -1;
        public KeyModifiers LastKeyModifiers = KeyModifiers.None;
        public Point LastPointerPosition;
        public readonly object? StartItem = startItem;
        public readonly int StartItemIndex = startItemIndex;
    }
    
    
    // Constants.
    static readonly AttachedProperty<ItemDraggingInfo?> ItemDraggingInfoProperty = AvaloniaProperty.RegisterAttached<ListBoxItemDragging, ListBox, ItemDraggingInfo?>("ItemDraggingInfo");
    
    
    // Static constructor.
    static ListBoxItemDragging()
    {
        IsItemDraggingEnabledProperty.Changed.Subscribe(e =>
        {
            if (e.Sender is not Avalonia.Controls.ListBox listBox)
                return;
            if (e.NewValue.GetValueOrDefault())
            {
                listBox.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
                listBox.AddHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost, RoutingStrategies.Tunnel);
                listBox.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
                listBox.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
            }
            else
            {
                listBox.RemoveHandler(InputElement.KeyUpEvent, OnKeyUp);
                listBox.RemoveHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);
                listBox.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
                listBox.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
                CancelItemDragging(listBox);
            }
        });
    }
    
    
    // Constructor.
    ListBoxItemDragging()
    { }
    
    
    /// <summary>
    /// Cancel current item dragging on <see cref="Avalonia.Controls.ListBox"/>.
    /// </summary>
    /// <param name="listBox"><see cref="Avalonia.Controls.ListBox"/>.</param>
    /// <returns>True if item dragging has been cancelled successfully.</returns>
    public static bool CancelItemDragging(Avalonia.Controls.ListBox listBox)
    {
        if (listBox.GetValue(ItemDraggingInfoProperty) is not { IsCancelled: false } draggingInfo)
            return false;
        draggingInfo.IsCancelled = true;
        draggingInfo.CursorValueToken = draggingInfo.CursorValueToken.DisposeAndReturnNull();
        var prevIndex = draggingInfo.LastItemIndex;
        var prevItem = prevIndex >= 0 && prevIndex < listBox.ItemCount
            ? listBox.Items[prevIndex]
            : null;
        var e = new ListBoxItemDragEventArgs(draggingInfo.StartItemIndex, draggingInfo.StartItem, -1, null, prevIndex, prevItem, draggingInfo.LastPointerPosition, draggingInfo.LastKeyModifiers)
        {
            RoutedEvent = ItemDragCancelledEvent
        };
        listBox.RaiseEvent(e);
        if (!e.Handled)
            ItemInsertionIndicator.ClearInsertingItems(listBox);
        return true;
    }


    // Find index of item which the pointer on.
    static int FindItemIndex(Avalonia.Controls.ListBox listBox, PointerPoint point)
    {
        if (!point.Properties.IsLeftButtonPressed)
            return -1;
        if (listBox.InputHitTest<ListBoxItem>(point.Position) is not { } listBoxItem)
            return -1;
        return listBox.IndexFromContainer(listBoxItem);
    }


    /// <summary>
    /// Check whether the item dragging is on going or not.
    /// </summary>
    /// <param name="listBox"><see cref="ListBox"/>.</param>
    /// <returns>True if item dragging is on going.</returns>
    public static bool IsDraggingItem(Avalonia.Controls.ListBox listBox) =>
        listBox.GetValue(ItemDraggingInfoProperty) is { IsCancelled: false };


    /// <summary>
    /// Check whether item dragging is enabled or not.
    /// </summary>
    /// <param name="listBox"><see cref="ListBox"/>.</param>
    /// <returns>True if item dragging is enabled.</returns>
    public static bool IsItemDraggingEnabled(Avalonia.Controls.ListBox listBox) =>
        listBox.GetValue(IsItemDraggingEnabledProperty);
    
    
    // Called when key up on list box.
    static void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (sender is not Avalonia.Controls.ListBox listBox || listBox.GetValue(ItemDraggingInfoProperty) is not { IsCancelled: false })
            return;
        if (e.Key == Key.Escape)
            CancelItemDragging(listBox);
    }
    
    
    // Called when pointer capture lost from the list box.
    static void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (sender is Avalonia.Controls.ListBox listBox)
            CancelItemDragging(listBox);
    }
    
    
    // Called when pointer moved on the list box.
    static void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        // check state
        if (sender is not Avalonia.Controls.ListBox listBox || !listBox.GetValue(IsItemDraggingEnabledProperty))
            return;
        
        // handle pointer move
        var point = e.GetCurrentPoint(listBox);
        if (listBox.GetValue(ItemDraggingInfoProperty) is { } draggingInfo)
        {
            // skip if it has been cancelled
            if (draggingInfo.IsCancelled)
                return;
            
            // save pointer info
            draggingInfo.LastPointerPosition = point.Position;
            draggingInfo.LastKeyModifiers = e.KeyModifiers;
            
            // find item which the pointer over
            var index = FindItemIndex(listBox, point);
            var prevIndex = draggingInfo.LastItemIndex;
            var hasPrevItem = prevIndex >= 0 && prevIndex < listBox.ItemCount;
            var prevItem = hasPrevItem
                ? listBox.Items[prevIndex]
                : null;
            if (index < 0)
            {
                var position = point.Position;
                if (hasPrevItem)
                {
                    var bounds = listBox.Bounds;
                    if (position.X < 0 || position.X >= bounds.Width || position.Y < 0 || position.Y >= bounds.Height)
                    {
                        draggingInfo.LastItemIndex = -1;
                        var container = hasPrevItem
                            ? listBox.ContainerFromIndex(prevIndex)
                            : null;
                        var dragEventArgs = new ListBoxItemDragEventArgs(draggingInfo.StartItemIndex, draggingInfo.StartItem, -1, null, prevIndex, prevItem, point.Position, e.KeyModifiers)
                        {
                            RoutedEvent = ItemDragLeavedEvent
                        };
                        listBox.RaiseEvent(dragEventArgs);
                        if (!dragEventArgs.Handled && container is not null)
                        {
                            ItemInsertionIndicator.SetInsertingItemAfter(container, false);
                            ItemInsertionIndicator.SetInsertingItemBefore(container, false);
                        }
                    }
                }
                return;
            }
            
            // raise event
            if (prevIndex != index)
            {
                draggingInfo.LastItemIndex = index;
                var prevContainer = hasPrevItem
                    ? listBox.ContainerFromIndex(prevIndex)
                    : null;
                var container = listBox.ContainerFromIndex(index);
                var dragEventArgs = new ListBoxItemDragEventArgs(draggingInfo.StartItemIndex, draggingInfo.StartItem, index, listBox.Items[index], prevIndex, prevItem, point.Position, e.KeyModifiers)
                {
                    RoutedEvent = ItemDraggedEvent
                };
                listBox.RaiseEvent(dragEventArgs);
                if (!dragEventArgs.Handled)
                {
                    if (prevContainer is not null)
                    {
                        ItemInsertionIndicator.SetInsertingItemAfter(prevContainer, false);
                        ItemInsertionIndicator.SetInsertingItemBefore(prevContainer, false);
                    }
                    if (index < draggingInfo.StartItemIndex && container is not null)
                        ItemInsertionIndicator.SetInsertingItemBefore(container, true);
                    else if (index > draggingInfo.StartItemIndex && container is not null)
                        ItemInsertionIndicator.SetInsertingItemAfter(container, true);
                }
            }
        }
        else
        {
            // skip if left button is not pressed
            if (!point.Properties.IsLeftButtonPressed)
                return;
            
            // find item which the pointer over
            var index = FindItemIndex(listBox, point);
            if (index < 0)
            {
                draggingInfo = new ItemDraggingInfo(-1, null)
                {
                    IsCancelled = true,
                    LastPointerPosition = point.Position,
                    LastKeyModifiers = e.KeyModifiers
                };
                listBox.SetValue(ItemDraggingInfoProperty, draggingInfo);
                return;
            }

            // start dragging item
            draggingInfo = new ItemDraggingInfo(index, listBox.Items[index]);
            listBox.SetValue(ItemDraggingInfoProperty, draggingInfo);
            var dragEventArgs = new ListBoxItemDragEventArgs(index, draggingInfo.StartItem, index, draggingInfo.StartItem, -1, null, point.Position, e.KeyModifiers)
            {
                RoutedEvent = ItemDragStartedEvent
            };
            listBox.RaiseEvent(dragEventArgs);
            if (dragEventArgs.Handled)
            {
                draggingInfo.IsCancelled = true;
                return;
            }
            draggingInfo.CursorValueToken = listBox.SetValue(InputElement.CursorProperty, new Cursor(StandardCursorType.DragMove), BindingPriority.Template);
        }
    }
    
    
    // Called when pointer released from the list box.
    static void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // check state
        if (sender is not ListBox listBox || listBox.GetValue(ItemDraggingInfoProperty) is not { } draggingInfo)
            return;
        
        // reset state
        draggingInfo.CursorValueToken = draggingInfo.CursorValueToken.DisposeAndReturnNull();
        listBox.SetValue(ItemDraggingInfoProperty, null);
        
        // cancel dropping or drop on item
        if (draggingInfo.IsCancelled)
            return;
        var point = e.GetCurrentPoint(listBox);
        var index = draggingInfo.LastItemIndex;
        var item = index >= 0 && index < listBox.ItemCount 
            ? listBox.Items[index] 
            : null;
        var container = index >= 0 && index < listBox.ItemCount  
            ? listBox.ContainerFromIndex(index)
            : null;
        var dragEventArgs = index >= 0 && index < listBox.Items.Count 
            ? new ListBoxItemDragEventArgs(draggingInfo.StartItemIndex, draggingInfo.StartItem, index, item, index, item, point.Position, e.KeyModifiers)
            {
                RoutedEvent = ItemDroppedEvent
            }
            : new ListBoxItemDragEventArgs(draggingInfo.StartItemIndex, draggingInfo.StartItem, -1, null, -1, null, point.Position, e.KeyModifiers)
            {
                RoutedEvent = ItemDragCancelledEvent
            };
        if (!dragEventArgs.Handled && container is not null)
        {
            ItemInsertionIndicator.SetInsertingItemAfter(container, false);
            ItemInsertionIndicator.SetInsertingItemBefore(container, false);
        }
        listBox.RaiseEvent(dragEventArgs);
    }
    
    
    /// <summary>
    /// Enable of disable item dragging.
    /// </summary>
    /// <param name="listBox"><see cref="Avalonia.Controls.ListBox"/>.</param>
    /// <param name="isEnabled">True to enable item dragging.</param>
    public static void SetItemDraggingEnabled(Avalonia.Controls.ListBox listBox, bool isEnabled) =>
        listBox.SetValue(IsItemDraggingEnabledProperty, isEnabled);
}


/// <summary>
/// Data for dragging item of <see cref="Avalonia.Controls.ListBox"/> related events.
/// </summary>
/// <param name="startItemIndex">Index of item which the drag starts.</param>
/// <param name="startItem">Item which the drag starts.</param>
/// <param name="itemIndex">Index of item which pointer currently over.</param>
/// <param name="item">Item which pointer currently over.</param>
/// <param name="prevItemIndex">Index of item which pointer previously over.</param>
/// <param name="prevItem">Item which pointer previously over.</param>
/// <param name="pointerPosition">Position of pointer relative to <see cref="Avalonia.Controls.ListBox"/>.</param>
/// <param name="keyModifiers">Key modifiers.</param>
public class ListBoxItemDragEventArgs(int startItemIndex, object? startItem, int itemIndex, object? item, int prevItemIndex, object? prevItem, Point pointerPosition, KeyModifiers keyModifiers) : RoutedEventArgs
{
    /// <summary>
    /// Get index of item which pointer currently over.
    /// </summary>
    public object? Item { get; } = item;
    
    
    /// <summary>
    /// Get item which pointer currently over.
    /// </summary>
    public int ItemIndex { get; } = itemIndex;

    
    /// <summary>
    /// Get key modifiers.
    /// </summary>
    public KeyModifiers KeyModifiers { get; } = keyModifiers;


    /// <summary>
    /// Get position of pointer relative to <see cref="Avalonia.Controls.ListBox"/>.
    /// </summary>
    public Point PointerPosition { get; } = pointerPosition;
    
    
    /// <summary>
    /// Get item which pointer previously over.
    /// </summary>
    public object? PreviousItem { get; } = prevItem;
    
    
    /// <summary>
    /// Get index of item which pointer previously over.
    /// </summary>
    public int PreviousItemIndex { get; } = prevItemIndex;
    
    
    /// <summary>
    /// Get item which the drag starts.
    /// </summary>
    public object? StartItem { get; } = startItem;
    
    
    /// <summary>
    /// Get index of item which the drag starts.
    /// </summary>
    public int StartItemIndex { get; } = startItemIndex;
}