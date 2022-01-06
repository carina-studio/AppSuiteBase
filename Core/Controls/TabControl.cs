﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.VisualTree;
using CarinaStudio.Animation;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.Collections;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// <see cref="Avalonia.Controls.TabControl"/> with UX improvement.
    /// </summary>
    public class TabControl : Avalonia.Controls.TabControl, IStyleable
    {
        /// <summary>
        /// Property of <see cref="IsFullWindowMode"/>.
        /// </summary>
        public static readonly AvaloniaProperty<bool> IsFullWindowModeProperty = AvaloniaProperty.Register<TabControl, bool>(nameof(IsFullWindowMode), true);


        // Constants.
        const int DraggingThreshold = 10;
        const int ScrollTabStripByButtonInterval = 300;


        // Fields.
        Window? attachedWindow;
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
                // get state
                var index = this.SelectedIndex;
                if (index < 0)
                    return;
                if (this.tabStripScrollViewer == null || this.tabItemsPresenter == null)
                    return;

                // find tab item header
                var tabItemHeader = this.tabItemsPresenter.FindDescendantOfType<Panel>()?.Let(panel =>
                {
                    if (panel.Children.Count > index)
                        return panel.Children[index];
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
                    left += parent.Bounds.Left;
                    parent = parent.Parent;
                }
                if (left < 0)
                    this.tabStripScrollViewer.ScrollBy(left);
                else if (left + tabItemHeaderBounds.Width > this.tabStripScrollViewer.Bounds.Width)
                    this.tabStripScrollViewer.ScrollBy(left + tabItemHeaderBounds.Width - this.tabStripScrollViewer.Bounds.Width);
            });
            this.updateTabStripScrollViewerMarginAction = new ScheduledAction(() => this.UpdateTabStripScrollViewerMargin(true));
        }


        // Find tab item which data is dragged over.
        bool FindItemDraggedOver(DragEventArgs e, out int itemIndex, out object? item)
        {
            itemIndex = -1;
            item = null;
            var index = this.tabItemsPresenter.FindDescendantOfType<Panel>()?.Let(panel =>
            {
                for (var i = panel.Children.Count - 1; i >= 0; --i)
                {
                    if (panel.Children[i] is IVisual visual)
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
            item = (this.Items as IList)?.Let(it =>
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


        // Find tab item which pointer over it.
        bool FindItemPointerOver(PointerEventArgs e, out int itemIndex, out object? item)
        {
            itemIndex = -1;
            item = null;
            var index = this.tabItemsPresenter.FindDescendantOfType<Panel>()?.Let(panel =>
            {
                for (var i = panel.Children.Count - 1; i >= 0; --i)
                {
                    if (panel.Children[i] is IVisual visual)
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
            item = (this.Items as IList)?.Let(it =>
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
            get => this.GetValue<bool>(IsFullWindowModeProperty);
            set => this.SetValue<bool>(IsFullWindowModeProperty, value);
        }


        /// <summary>
        /// Raised when user dragged item.
        /// </summary>
        public event EventHandler<TabItemDraggedEventArgs>? ItemDragged;


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
                it.TemplateApplied += (_, e) =>
                {
                    this.scrollTabStripLeftButton = e.NameScope.Find<RepeatButton>("PART_ScrollLeftButton");
                    this.scrollTabStripRightButton = e.NameScope.Find<RepeatButton>("PART_ScrollRightButton");
                };
            });
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
            this.updateTabStripScrollViewerMarginAction.Cancel();
            base.OnDetachedFromVisualTree(e);
        }


        // Called when data drag leave.
        void OnDragLeave(object? sender, RoutedEventArgs e)
        {
            this.scrollTabStripLeftByButtonAction.Cancel();
            this.scrollTabStripRightByButtonAction.Cancel();
        }


        // Called when data drag over.
        void OnDragOver(object? sender, DragEventArgs e)
        {
            // check state
            if (e.Handled)
            {
                this.scrollTabStripLeftByButtonAction.Cancel();
                this.scrollTabStripRightByButtonAction.Cancel();
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
                    return;
                }
            }
            this.scrollTabStripLeftByButtonAction.Cancel();
            this.scrollTabStripRightByButtonAction.Cancel();

            // find tab item which data is dragged over
            if (!this.FindItemDraggedOver(e, out var itemIndex, out var item) || item == null)
                return;

            // raise event
            var dragOnItemEventArgs = new DragOnTabItemEventArgs(e, itemIndex, item);
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
            if (!this.FindItemDraggedOver(e, out var itemIndex, out var item) || item == null)
                return;

            // raise event
            var dragOnItemEventArgs = new DragOnTabItemEventArgs(e, itemIndex, item);
            this.DropOnItem?.Invoke(this, dragOnItemEventArgs);
            e.DragEffects = dragOnItemEventArgs.DragEffects;
            e.Handled = dragOnItemEventArgs.Handled;
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
            if (!this.FindItemPointerOver(e, out var index, out var item) || this.pointerPressedItem != item)
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


        /// <inheritdoc/>
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == IsFullWindowModeProperty)
                this.updateTabStripScrollViewerMarginAction.Schedule();
            else if (change.Property == SelectedIndexProperty)
                this.scrollToSelectedItemAction.Schedule();
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
                var reservedSize = ExtendedClientAreaWindowConfiguration.SystemChromeWidth;
                if (this.TryFindResource("Double/TabControl.TabStrip.ExtendedClientAreaReserveSpace", out var res) && res is double doubleValue)
                    reservedSize += doubleValue;
                if (ExtendedClientAreaWindowConfiguration.IsSystemChromePlacedAtRight)
                    return new Thickness(0, 0, reservedSize, 0);
                return new Thickness(reservedSize, 0, 0, 0);
            });

            // update margin
            if (this.tabStripScrollViewerMarginAnimator != null)
            {
                this.tabStripScrollViewerMarginAnimator.Cancel();
                this.tabStripScrollViewerMarginAnimator = null;
            }
            if (animate)
            {
                var duration = (this.TryFindResource("TimeSpan/Animation.Fast", out var res) && res is TimeSpan timeSpanValue)
                    ? timeSpanValue
                    : TimeSpan.FromMilliseconds(250);
                this.tabStripScrollViewerMarginAnimator = new ThicknessAnimator(this.tabStripScrollViewer.Margin, margin).Also(it =>
                {
                    it.Completed += (_, e) => this.tabStripScrollViewerMarginAnimator = null;
                    it.Duration = duration;
                    it.Interpolator = Interpolators.Deleceleration;
                    it.ProgressChanged += (_, e) => this.tabStripScrollViewer.Margin = it.Value;
                    it.Start();
                });
            }
            else
                this.tabStripScrollViewer.Margin = margin;
        }


        // Interface implementation.
        Type IStyleable.StyleKey { get; } = typeof(Avalonia.Controls.TabControl);
    }


    /// <summary>
    /// Data for drag and drop events on item of <see cref="TabControl"/>.
    /// </summary>
    public class DragOnTabItemEventArgs : TabItemEventArgs
    {
        // Constuctor.
        internal DragOnTabItemEventArgs(DragEventArgs e, int itemIndex, object item) : base(itemIndex, item)
        {
            this.Data = e.Data;
            this.DragEffects = e.DragEffects;
            this.KeyModifiers = e.KeyModifiers;
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
        /// Get key modifiers.
        /// </summary>
        public KeyModifiers KeyModifiers { get; }
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
        // Constuctor.
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
