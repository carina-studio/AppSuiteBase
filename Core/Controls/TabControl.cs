using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using CarinaStudio.Animation;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using CarinaStudio.VisualTree;
using CarinaStudio.Windows.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// <see cref="Avalonia.Controls.TabControl"/> with UX improvement.
    /// </summary>
    public class TabControl : Avalonia.Controls.TabControl
    {
        /// <summary>
        /// Property of <see cref="IsFullWindowMode"/>.
        /// </summary>
        public static readonly StyledProperty<bool> IsFullWindowModeProperty = AvaloniaProperty.Register<TabControl, bool>(nameof(IsFullWindowMode), true);
        /// <summary>
        /// Property of <see cref="TabStripSize"/>.
        /// </summary>
        public static readonly DirectProperty<TabControl, double> TabStripSizeProperty = AvaloniaProperty.RegisterDirect<TabControl, double>(nameof(TabStripSize), t => t.tabStripSize);


        // Constants.
        const int DraggingThreshold = 10;
        const int ScrollTabStripByButtonInterval = 300;


        // Fields.
        INotifyCollectionChanged? attachedItemsSource;
        readonly Dictionary<Avalonia.Controls.TabItem, List<IDisposable>> attachedTabItemObserverTokens = new();
        Window? attachedWindow;
        object? draggingOverItem;
        int draggingOverItemIndex = -1;
        object? pointerPressedItem;
        int pointerPressedItemIndex = -1;
        Point? pointerPressedPosition;
        RepeatButton? scrollTabStripLeftButton;
        readonly ScheduledAction scrollTabStripLeftByButtonAction;
        RepeatButton? scrollTabStripRightButton;
        readonly ScheduledAction scrollTabStripRightByButtonAction;
        readonly ScheduledAction scrollToSelectedItemAction;
        ItemsPresenter? tabItemsPresenter;
        TabStripScrollViewer? tabStripScrollViewer;
        double tabStripSize = -1;
        ThicknessAnimator? tabStripScrollViewerMarginAnimator;
        readonly ScheduledAction updateTabStripScrollViewerMarginAction;


        /// <summary>
        /// Initialize new <see cref="TabControl"/> instance.
        /// </summary>
        public TabControl()
        {
            this.scrollTabStripLeftByButtonAction = new ScheduledAction(() =>
            {
                if (this.scrollTabStripLeftButton != null)
                {
                    this.scrollTabStripLeftButton.Command?.TryExecute();
                    this.scrollTabStripLeftByButtonAction?.Reschedule(ScrollTabStripByButtonInterval);
                }
            });
            this.scrollTabStripRightByButtonAction = new ScheduledAction(() =>
            {
                if (this.scrollTabStripRightButton != null)
                {
                    this.scrollTabStripRightButton.Command?.TryExecute();
                    this.scrollTabStripRightByButtonAction?.Reschedule(ScrollTabStripByButtonInterval);
                }
            });
            this.scrollToSelectedItemAction = new ScheduledAction(() =>
            {
                var index = this.SelectedIndex;
                if (index < 0)
                    return;
                this.ScrollHeaderIntoViewCore(index);
            });
            this.updateTabStripScrollViewerMarginAction = new ScheduledAction(() => this.UpdateTabStripScrollViewerMargin(true));

            // observe self properties
            var isSubscribed = false;
            this.GetObservable(IsFullWindowModeProperty).Subscribe(_ =>
            {
                if (isSubscribed)
                    this.updateTabStripScrollViewerMarginAction.Schedule();
            });
            this.GetObservable(ItemsSourceProperty).Subscribe(items =>
            {
                if (this.attachedItemsSource != null)
                {
                    this.attachedItemsSource.CollectionChanged -= this.OnItemsChanged;
                    this.attachedItemsSource = null;
                    foreach (var tabItem in this.attachedTabItemObserverTokens.Keys.ToArray())
                        this.DetachFromTabItem(tabItem);
                }
                this.attachedItemsSource = (items as INotifyCollectionChanged)?.Also(it =>
                    it.CollectionChanged += this.OnItemsChanged);
                if (items is not null)
                {
                    foreach (var item in items)
                    {
                        if (item is Avalonia.Controls.TabItem tabItem)
                            this.AttachToTabItem(tabItem);
                    }
                    this.UpdateExtendedTabItemStates();
                }
            });
            this.GetObservable(SelectedIndexProperty).Subscribe(_ =>
            {
                if (isSubscribed)
                    this.scrollToSelectedItemAction.Schedule();
            });
            isSubscribed = true;
        }
        
        
        // Attach to given TabItem.
        void AttachToTabItem(Avalonia.Controls.TabItem tabItem)
        {
            if (attachedTabItemObserverTokens.TryGetValue(tabItem, out var observerTokens))
                return;
            var isAttaching = true;
            observerTokens = new()
            {
                tabItem.GetObservable(IsPointerOverProperty).Subscribe(isPointerOver =>
                {
                    if (!isAttaching)
                        this.OnTabItemPointerOverChanged(tabItem, isPointerOver);
                }),
                tabItem.GetObservable(IsSelectedProperty).Subscribe(isSelected =>
                {
                    if (!isAttaching)
                        this.OnTabItemSelectedChanged(tabItem, isSelected);
                }),
            };
            isAttaching = false;
            attachedTabItemObserverTokens[tabItem] = observerTokens;
        }
        
        
        // Detach from given TabItem.
        void DetachFromTabItem(Avalonia.Controls.TabItem tabItem)
        {
            if (!attachedTabItemObserverTokens.Remove(tabItem, out var observerTokens))
                return;
            foreach (var token in observerTokens)
                token.Dispose();
            if (tabItem is TabItem extendedTabItem)
            {
                extendedTabItem.IsFirstItem = false;
                extendedTabItem.IsLastItem = false;
                extendedTabItem.IsNextItemPointerOver = false;
                extendedTabItem.IsNextItemSelected = false;
                extendedTabItem.IsPreviousItemPointerOver = false;
                extendedTabItem.IsPreviousItemSelected = false;
            }
        }


        // Find tab item which data is dragged over.
        bool FindItemDraggedOver(DragEventArgs e, out int itemIndex, out object? item, out Visual? headerVisual)
        {
            // find index of item
            itemIndex = -1;
            item = null;
            headerVisual = null;
            var itemsPanel = this.tabItemsPresenter?.FindDescendantOfType<Panel>();
            if (itemsPanel == null)
                return false;
            var index = Global.Run(() =>
            {
                for (var i = itemsPanel.Children.Count - 1; i >= 0; --i)
                {
                    if (itemsPanel.Children[i] is Visual visual)
                    {
                        var position = e.GetPosition(visual);
                        var bounds = visual.Bounds;
                        if (position.X >= 0 && position.Y >= 0 && position.X < bounds.Width && position.Y < bounds.Height)
                            return i;
                    }
                }
                return -1;
            });
            if (index < 0)
                return false;

            // get tab item
            item = this.Items.Let(it =>
            {
                if (index < it.Count)
                    return it[index];
                return null;
            });
            if (item != null)
            {
                itemIndex = index;
                headerVisual = itemsPanel.Children[index].GetVisualChildren().FirstOrDefault()?.Let(it => 
                    it.FindDescendantOfTypeAndName<Control>("PART_ContentContainer") ?? it) ?? headerVisual;
                return true;
            }
            return false;
        }


        // Find tab item which pointer over it.
        bool FindItemPointerOver(PointerEventArgs e, out int itemIndex, out object? item)
        {
            itemIndex = -1;
            item = null;
            var index = this.tabItemsPresenter?.FindDescendantOfType<Panel>()?.Let(panel =>
            {
                for (var i = panel.Children.Count - 1; i >= 0; --i)
                {
                    if (panel.Children[i] is Visual visual)
                    {
                        var position = e.GetPosition(visual);
                        var bounds = visual.Bounds;
                        if (position.X >= 0 && position.Y >= 0 && position.X < bounds.Width && position.Y < bounds.Height)
                            return i;
                    }
                }
                return -1;
            }) ?? -1;
            if (index < 0)
                return false;

            // get tab item
            item = this.Items.Let(it =>
            {
                if (index < it.Count)
                    return it[index];
                return null;
            });
            if (item != null)
            {
                itemIndex = index;
                return true;
            }
            return false;
        }


        /// <summary>
        /// Raised when data dragged into an item.
        /// </summary>
        public event EventHandler<DragOnTabItemEventArgs>? DragEnterItem;


        /// <summary>
        /// Raised when data dragged from an item.
        /// </summary>
        public event EventHandler<TabItemEventArgs>? DragLeaveItem;


        /// <summary>
        /// Raised when data dragged over an item.
        /// </summary>
        public event EventHandler<DragOnTabItemEventArgs>? DragOverItem;


        /// <summary>
        /// Raised when data dropped on an item.
        /// </summary>
        public event EventHandler<DragOnTabItemEventArgs>? DropOnItem;


        /// <summary>
        /// Get or set whether <see cref="TabControl"/> should be layout as full-window mode or not.
        /// </summary>
        public bool IsFullWindowMode
        {
            get => this.GetValue(IsFullWindowModeProperty);
            set => this.SetValue(IsFullWindowModeProperty, value);
        }


        /// <summary>
        /// Raised when user dragged item.
        /// </summary>
        public event EventHandler<TabItemDraggedEventArgs>? ItemDragged;


        // Leave the item which is currently dragging over.
        void LeaveDraggingOverItem()
        {
            if (this.draggingOverItem != null)
            {
                this.DragLeaveItem?.Invoke(this, new TabItemEventArgs(this.draggingOverItemIndex, this.draggingOverItem));
                this.draggingOverItem = null;
                this.draggingOverItemIndex = -1;
            }
        }


        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            this.scrollTabStripLeftByButtonAction.Cancel();
            this.scrollTabStripRightByButtonAction.Cancel();
            this.scrollTabStripLeftButton = null;
            this.scrollTabStripRightButton = null;
            this.tabItemsPresenter = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
            this.tabStripScrollViewer = e.NameScope.Find<TabStripScrollViewer>("PART_TabStripScrollViewer")?.Also(it =>
            {
                it.GetObservable(BoundsProperty).Subscribe(bounds =>
                    this.SetAndRaise(TabStripSizeProperty, ref this.tabStripSize, bounds.Height));
                it.TemplateApplied += (_, e) =>
                {
                    this.scrollTabStripLeftButton = e.NameScope.Find<RepeatButton>("PART_ScrollLeftButton");
                    this.scrollTabStripRightButton = e.NameScope.Find<RepeatButton>("PART_ScrollRightButton");
                };
            });
            this.SetAndRaise(TabStripSizeProperty, ref this.tabStripSize, this.tabStripScrollViewer?.Bounds.Height ?? -1);
            this.updateTabStripScrollViewerMarginAction.Cancel();
            this.UpdateTabStripScrollViewerMargin(false);
        }


        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            this.AddHandler(DragDrop.DragEnterEvent, this.OnDragOver);
            this.AddHandler(DragDrop.DragLeaveEvent, this.OnDragLeave);
            this.AddHandler(DragDrop.DragOverEvent, this.OnDragOver);
            this.AddHandler(DragDrop.DropEvent, this.OnDrop);
            this.AddHandler(PointerMovedEvent, this.OnPreviewPointerMove, RoutingStrategies.Tunnel);
            this.AddHandler(PointerPressedEvent, this.OnPreviewPointerPressed, RoutingStrategies.Tunnel);
            this.AddHandler(PointerReleasedEvent, this.OnPreviewPointerReleased, RoutingStrategies.Tunnel);
        }


        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            this.attachedWindow = this.FindLogicalAncestorOfType<Window>()?.Also(it =>
            {
                it.PropertyChanged += this.OnWindowPropertyChanged;
            });
            this.Items.CollectionChanged += this.OnItemsChanged;
            this.UpdateTabStripScrollViewerMargin(false);
        }


        /// <inheritdoc/>
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            this.RemoveHandler(DragDrop.DragEnterEvent, this.OnDragOver);
            this.RemoveHandler(DragDrop.DragLeaveEvent, this.OnDragLeave);
            this.RemoveHandler(DragDrop.DragOverEvent, this.OnDragOver);
            this.RemoveHandler(DragDrop.DropEvent, this.OnDrop);
            this.RemoveHandler(PointerMovedEvent, this.OnPreviewPointerMove);
            this.RemoveHandler(PointerPressedEvent, this.OnPreviewPointerPressed);
            this.RemoveHandler(PointerReleasedEvent, this.OnPreviewPointerReleased);
            base.OnDetachedFromLogicalTree(e);
        }


        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            this.attachedWindow?.Let(it =>
            {
                it.PropertyChanged -= this.OnWindowPropertyChanged;
                this.attachedWindow = null;
            });
            this.Items.CollectionChanged -= this.OnItemsChanged;
            this.updateTabStripScrollViewerMarginAction.Cancel();
            base.OnDetachedFromVisualTree(e);
        }


        // Called when data drag leave.
        void OnDragLeave(object? sender, RoutedEventArgs e)
        {
            // cancel scrolling
            this.scrollTabStripLeftByButtonAction.Cancel();
            this.scrollTabStripRightByButtonAction.Cancel();

            // leave item
            this.LeaveDraggingOverItem();
        }


        // Called when data drag over.
        void OnDragOver(object? sender, DragEventArgs e)
        {
            // check state
            if (e.Handled)
            {
                this.scrollTabStripLeftByButtonAction.Cancel();
                this.scrollTabStripRightByButtonAction.Cancel();
                this.LeaveDraggingOverItem();
                return;
            }

            // not to accept data by default
            e.DragEffects = DragDropEffects.None;

            // scroll tab strip left or right
            if (this.scrollTabStripLeftButton != null)
            {
                var position = e.GetPosition(this.scrollTabStripLeftButton);
                var bounds = this.scrollTabStripLeftButton.Bounds;
                if (position.X >= 0 && position.Y >= 0 && position.X < bounds.Width && position.Y < bounds.Height)
                {
                    this.scrollTabStripLeftByButtonAction.Schedule(ScrollTabStripByButtonInterval);
                    this.scrollTabStripRightByButtonAction.Cancel();
                    this.LeaveDraggingOverItem();
                    return;
                }
            }
            if (this.scrollTabStripRightButton != null)
            {
                var position = e.GetPosition(this.scrollTabStripRightButton);
                var bounds = this.scrollTabStripRightButton.Bounds;
                if (position.X >= 0 && position.Y >= 0 && position.X < bounds.Width && position.Y < bounds.Height)
                {
                    this.scrollTabStripRightByButtonAction.Schedule(ScrollTabStripByButtonInterval);
                    this.scrollTabStripLeftByButtonAction.Cancel();
                    this.LeaveDraggingOverItem();
                    return;
                }
            }
            this.scrollTabStripLeftByButtonAction.Cancel();
            this.scrollTabStripRightByButtonAction.Cancel();

            // find tab item which data is dragged over
            if (!this.FindItemDraggedOver(e, out var itemIndex, out var item, out var headerVisual) || item == null || headerVisual == null)
            {
                this.LeaveDraggingOverItem();
                return;
            }

            // leave and enter item
            if (this.draggingOverItem != null && this.draggingOverItem != item)
                this.LeaveDraggingOverItem();
            if (this.draggingOverItem == null)
            {
                this.DragEnterItem?.Invoke(this, new DragOnTabItemEventArgs(e, itemIndex, item, headerVisual));
                this.draggingOverItem = item;
            }
            this.draggingOverItemIndex = itemIndex;

            // raise event
            var dragOnItemEventArgs = new DragOnTabItemEventArgs(e, itemIndex, item, headerVisual);
            this.DragOverItem?.Invoke(this, dragOnItemEventArgs);
            e.DragEffects = dragOnItemEventArgs.DragEffects;
            e.Handled = dragOnItemEventArgs.Handled;
        }


        // Called when data dropped.
        void OnDrop(object? sender, DragEventArgs e)
        {
            // stop scrolling tab strip
            this.scrollTabStripLeftByButtonAction.Cancel();
            this.scrollTabStripRightByButtonAction.Cancel();

            // check state
            if (e.Handled)
                return;

            // find tab item which data is dragged over
            if (!this.FindItemDraggedOver(e, out var itemIndex, out var item, out var headerVisual) || item == null || headerVisual == null)
                return;

            // raise event
            var dragOnItemEventArgs = new DragOnTabItemEventArgs(e, itemIndex, item, headerVisual);
            this.DropOnItem?.Invoke(this, dragOnItemEventArgs);
            e.DragEffects = dragOnItemEventArgs.DragEffects;
            e.Handled = dragOnItemEventArgs.Handled;
        }


        // Called when element in Items has been changed.
        void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems!)
                    {
                        if (item is Avalonia.Controls.TabItem tabItem)
                            this.AttachToTabItem(tabItem);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    // [Workaround] Prevent selecting items more than one
                    this.Items.Let(it =>
                    {
                        var selectedIndex = this.SelectedIndex;
                        for (var i = it.Count - 1 ; i >= 0 ; --i)
                            (it[i] as TabItem)?.Let(tabItem => tabItem.IsSelected = (i == selectedIndex));
                    });
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems!)
                    {
                        if (item is Avalonia.Controls.TabItem tabItem)
                            this.DetachFromTabItem(tabItem);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.OldItems!)
                    {
                        if (item is Avalonia.Controls.TabItem tabItem)
                            this.DetachFromTabItem(tabItem);
                    }
                    foreach (var item in e.NewItems!)
                    {
                        if (item is Avalonia.Controls.TabItem tabItem)
                            this.AttachToTabItem(tabItem);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var tabItem in this.attachedTabItemObserverTokens.Keys.ToArray())
                        this.DetachFromTabItem(tabItem);
                    foreach (var item in this.Items)
                    {
                        if (item is Avalonia.Controls.TabItem tabItem)
                            this.AttachToTabItem(tabItem);
                    }
                    break;
            }
            this.UpdateExtendedTabItemStates();
        }


        // Called before handling pointer-move event by its child.
        void OnPreviewPointerMove(object? sender, PointerEventArgs e)
        {
            // check state
            if (!this.pointerPressedPosition.HasValue || this.pointerPressedItem == null)
                return;

            // check moving distance
            var position = e.GetCurrentPoint(this).Position;
            var distance = this.pointerPressedPosition.GetValueOrDefault().Let(src =>
            {
                var offsetX = (src.X - position.X);
                var offsetY = (src.Y - position.Y);
                return Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
            });
            if (distance < DraggingThreshold)
                return;

            // check pointer over item
            if (!this.FindItemPointerOver(e, out _, out var item) || this.pointerPressedItem != item)
            {
                this.pointerPressedItem = null;
                this.pointerPressedItemIndex = -1;
                this.pointerPressedPosition = null;
                return;
            }

            // start dragging item
            var itemEventArgs = new TabItemDraggedEventArgs(e, this.pointerPressedItemIndex, this.pointerPressedItem);
            this.ItemDragged?.Invoke(this, itemEventArgs);
            if (itemEventArgs.Handled)
                e.Handled = true;
            this.pointerPressedItem = null;
            this.pointerPressedItemIndex = -1;
            this.pointerPressedPosition = null;
        }


        // Called before handling pointer-pressed event by its child.
        void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // check state
            var pointer = e.GetCurrentPoint(this);
            if (!pointer.Properties.IsLeftButtonPressed)
                return;

            // find item
            if (!this.FindItemPointerOver(e, out this.pointerPressedItemIndex, out this.pointerPressedItem))
                return;

            // keep pressed position
            this.pointerPressedPosition = pointer.Position;
        }


        // Called before handling pointer-released event by its child.
        void OnPreviewPointerReleased(object? sender, PointerEventArgs e)
        {
            this.pointerPressedItem = null;
            this.pointerPressedItemIndex = -1;
            this.pointerPressedPosition = null;
        }
        
        
        // Called when TabItem.IsPointerOver changed.
        void OnTabItemPointerOverChanged(Avalonia.Controls.TabItem tabItem, bool isPointerOver)
        {
            var index = this.Items.IndexOf(tabItem);
            if (index < 0)
                return;
            var items = this.Items;
            if (index > 0 && items[index - 1] is TabItem prevExtendedTabItem)
                prevExtendedTabItem.IsNextItemPointerOver = isPointerOver;
            if (index < items.Count - 1 && items[index + 1] is TabItem nextExtendedTabItem)
                nextExtendedTabItem.IsPreviousItemPointerOver = isPointerOver;
        }
        
        
        // Called when TabItem.IsSelected changed.
        void OnTabItemSelectedChanged(Avalonia.Controls.TabItem tabItem, bool isSelected)
        {
            var index = this.Items.IndexOf(tabItem);
            if (index < 0)
                return;
            var items = this.Items;
            if (index > 0 && items[index - 1] is TabItem prevExtendedTabItem)
                prevExtendedTabItem.IsNextItemSelected = isSelected;
            if (index < items.Count - 1 && items[index + 1] is TabItem nextExtendedTabItem)
                nextExtendedTabItem.IsPreviousItemSelected = isSelected;
        }


        // Called on property of window changed.
        void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Avalonia.Controls.Window.ExtendClientAreaToDecorationsHintProperty
                || e.Property == Window.IsSystemChromeVisibleInClientAreaProperty)
            {
                this.updateTabStripScrollViewerMarginAction.Schedule();
            }
        }


        /// <summary>
        /// Scroll header of given item into view of tab strip.
        /// </summary>
        /// <param name="item">Item.</param>
        public void ScrollHeaderIntoView(object item)
        {
            this.VerifyAccess();
            if (this.Items is not IList items)
                return;
            var itemIndex = items.IndexOf(item);
            if (itemIndex < 0)
                return;
            this.scrollToSelectedItemAction.Cancel();
            this.ScrollHeaderIntoViewCore(itemIndex);
        }


        /// <summary>
        /// Scroll header of given item into view of tab strip.
        /// </summary>
        /// <param name="itemIndex">Index of item.</param>
        public void ScrollHeaderIntoView(int itemIndex)
        {
            this.VerifyAccess();
            if (this.Items is not IList items)
                return;
            if (itemIndex < 0 || itemIndex >= items.Count)
                throw new ArgumentOutOfRangeException(nameof(itemIndex));
            this.scrollToSelectedItemAction.Cancel();
            this.ScrollHeaderIntoViewCore(itemIndex);
        }


        // Scroll header item into view.
        void ScrollHeaderIntoViewCore(int itemIndex)
        {
            // check state
            if (this.tabStripScrollViewer == null || this.tabItemsPresenter == null)
                return;

            // find tab item header
            var tabItemHeader = this.tabItemsPresenter.FindDescendantOfType<Panel>()?.Let(panel =>
            {
                if (panel.Children.Count > itemIndex)
                    return panel.Children[itemIndex];
                return null;
            });
            if (tabItemHeader == null)
                return;

            // scroll into viewport
            var tabItemHeaderBounds = tabItemHeader.Bounds;
            var left = tabItemHeaderBounds.Left + this.tabItemsPresenter.Bounds.Left;
            var parent = this.tabItemsPresenter.Parent;
            while (parent != this.tabStripScrollViewer && parent != null)
            {
                if (parent is Visual visual)
                    left += visual.Bounds.Left;
                parent = parent.Parent;
            }
            var leftMargin = this.scrollTabStripLeftButton?.IsVisible == true ? this.scrollTabStripLeftButton.Bounds.Right : 0;
            var rightMargin = this.scrollTabStripRightButton?.IsVisible == true ? this.scrollTabStripRightButton.Bounds.Left : 0;
            if (left < leftMargin)
                this.tabStripScrollViewer.ScrollBy(left - leftMargin);
            else if (left + tabItemHeaderBounds.Width > rightMargin)
                this.tabStripScrollViewer.ScrollBy(left + tabItemHeaderBounds.Width - rightMargin);
        }


        /// <inheritdoc/>
        protected override Type StyleKeyOverride => typeof(Avalonia.Controls.TabControl);


        /// <summary>
        /// Get width or height of tab strip.
        /// </summary>
        public double TabStripSize => this.tabStripSize;
        
        
        // Update states of extended TabItems.
        void UpdateExtendedTabItemStates()
        {
            var items = this.Items;
            var itemCount = items.Count;
            if (itemCount <= 0)
                return;
            var nextTabItem = default(Avalonia.Controls.TabItem);
            var prevTabItem = itemCount >= 2 
                ? items[^2] as Avalonia.Controls.TabItem
                : null;
            for (var i = itemCount - 1; i >= 0; --i)
            {
                var tabItem = items[i] as Avalonia.Controls.TabItem;
                try
                {
                    if (tabItem is null)
                        continue;
                    if (tabItem is TabItem extendedTabItem)
                    {
                        extendedTabItem.IsFirstItem = (i == 0);
                        extendedTabItem.IsLastItem = (i == itemCount - 1);
                        extendedTabItem.IsNextItemPointerOver = (nextTabItem?.IsPointerOver == true);
                        extendedTabItem.IsNextItemSelected = (nextTabItem?.IsSelected == true);
                        extendedTabItem.IsPreviousItemPointerOver = (prevTabItem?.IsPointerOver == true);
                        extendedTabItem.IsPreviousItemSelected = (prevTabItem?.IsSelected == true);
                    }
                }
                finally
                {
                    nextTabItem = tabItem;
                    prevTabItem = i >= 2
                        ? items[i - 2] as Avalonia.Controls.TabItem
                        : null;
                }
            }
        }


        // Update margin of tab strip.
        void UpdateTabStripScrollViewerMargin(bool animate)
        {
            // check state
            if (this.attachedWindow == null || this.tabStripScrollViewer == null)
                return;

            // calculate margin
            var margin = Global.Run(() =>
            {
                if (!this.attachedWindow.ExtendClientAreaToDecorationsHint
                    || !this.attachedWindow.IsSystemChromeVisibleInClientArea
                    || !this.IsFullWindowMode)
                {
                    return new Thickness();
                }
                var sysChromeSize = ExtendedClientAreaWindowConfiguration.SystemChromeWidth;
                var reservedSize = this.FindResourceOrDefault("Double/TabControl.TabStrip.ExtendedClientAreaReserveSpace", 0.0);
                if (ExtendedClientAreaWindowConfiguration.IsSystemChromePlacedAtRight)
                    return new Thickness(0, 0, sysChromeSize + reservedSize, 0);
                return new Thickness(sysChromeSize, 0, reservedSize, 0);
            });

            // update margin
            if (this.tabStripScrollViewerMarginAnimator != null)
            {
                this.tabStripScrollViewerMarginAnimator.Cancel();
                this.tabStripScrollViewerMarginAnimator = null;
            }
            if (animate)
            {
                var duration = this.FindResourceOrDefault("TimeSpan/Animation", TimeSpan.FromMilliseconds(500));
                this.tabStripScrollViewerMarginAnimator = new ThicknessAnimator(this.tabStripScrollViewer.Margin, margin).Also(it =>
                {
                    it.Completed += (_, _) =>
                    {
                        this.tabStripScrollViewer.Margin = it.Value;
                        this.tabStripScrollViewerMarginAnimator = null;
                    };
                    it.Duration = duration;
                    it.Interpolator = Interpolators.FastDeceleration;
                    it.ProgressChanged += (_, _) => this.tabStripScrollViewer.Margin = it.Value;
                    it.Start();
                });
            }
            else
                this.tabStripScrollViewer.Margin = margin;
        }
    }


    /// <summary>
    /// Data for drag and drop events on item of <see cref="TabControl"/>.
    /// </summary>
    public class DragOnTabItemEventArgs : TabItemEventArgs
    {
        // Constructor.
        internal DragOnTabItemEventArgs(DragEventArgs e, int itemIndex, object item, Visual headerVisual) : base(itemIndex, item)
        {
            this.Data = e.Data;
            this.DragEffects = e.DragEffects;
            this.HeaderVisual = headerVisual;
            this.KeyModifiers = e.KeyModifiers;
            this.PointerPosition = e.GetPosition(headerVisual);
        }


        /// <summary>
        /// Get dragged or dropped data.
        /// </summary>
        public IDataObject Data { get; }


        /// <summary>
        /// Get or set effect of dragging data.
        /// </summary>
        public DragDropEffects DragEffects { get; set; }


        /// <summary>
        /// Get <see cref="Visual"/> of header of item.
        /// </summary>
        public Visual HeaderVisual { get; }


        /// <summary>
        /// Get key modifiers.
        /// </summary>
        public KeyModifiers KeyModifiers { get; }


        /// <summary>
        /// Get position of pointer relates to <see cref="HeaderVisual"/>.
        /// </summary>
        public Point PointerPosition { get; }
    }


    /// <summary>
    /// Data for <see cref="TabControl.ItemDragged"/>.
    /// </summary>
    public class TabItemDraggedEventArgs : TabItemEventArgs
    {
        // Constructor.
        internal TabItemDraggedEventArgs(PointerEventArgs e, int itemIndex, object item) : base(itemIndex, item)
        {
            this.PointerEventArgs = e;
        }


        /// <summary>
        /// Get <see cref="PointerEventArgs"/> which triggered the dragging event.
        /// </summary>
        public PointerEventArgs PointerEventArgs { get; }
    }


    /// <summary>
    /// Data for events relate to item of <see cref="TabControl"/>.
    /// </summary>
    public class TabItemEventArgs : EventArgs
    {
        // Constructor.
        internal TabItemEventArgs(int itemIndex, object item)
        {
            this.Item = item;
            this.ItemIndex = itemIndex;
        }


        /// <summary>
        /// Get or set whether event has been handled/consumed or not.
        /// </summary>
        public bool Handled { get; set; }


        /// <summary>
        /// Get item which data drag or drop on.
        /// </summary>
        public object Item { get; }


        /// <summary>
        /// Get index of item which data drag or drop on.
        /// </summary>
        public int ItemIndex { get; }
    }
}
