using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.VisualTree;
using CarinaStudio.Threading;
using System;
using System.Collections;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// <see cref="Avalonia.Controls.TabControl"/> with UX improvement.
    /// </summary>
    public class TabControl : Avalonia.Controls.TabControl, IStyleable
    {
        // Fields.
        readonly ScheduledAction scrollToSelectedItemAction;
        ItemsPresenter? tabItemsPresenter;
        TabStripScrollViewer? tabStripScrollViewer;


        /// <summary>
        /// Initialize new <see cref="TabControl"/> instance.
        /// </summary>
        public TabControl()
        {
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


        /// <summary>
        /// Raised when data dragged over an item.
        /// </summary>
        public event EventHandler<DragOnTabItemEventArgs>? DragOverItem;


        /// <summary>
        /// Raised when data dropped on an item.
        /// </summary>
        public event EventHandler<DragOnTabItemEventArgs>? DropOnItem;


        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            this.tabItemsPresenter = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
            this.tabStripScrollViewer = e.NameScope.Find<TabStripScrollViewer>("PART_TabStripScrollViewer");
        }


        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            this.AddHandler(DragDrop.DragEnterEvent, this.OnDragOver);
            this.AddHandler(DragDrop.DragOverEvent, this.OnDragOver);
            this.AddHandler(DragDrop.DropEvent, this.OnDrop);
        }


        /// <inheritdoc/>
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            this.RemoveHandler(DragDrop.DragEnterEvent, this.OnDragOver);
            this.RemoveHandler(DragDrop.DragOverEvent, this.OnDragOver);
            this.RemoveHandler(DragDrop.DropEvent, this.OnDrop);
            base.OnDetachedFromLogicalTree(e);
        }


        // Called when data drag over.
        void OnDragOver(object? sender, DragEventArgs e)
        {
            // check state
            if (e.Handled)
                return;

            // not to accept data by default
            e.DragEffects = DragDropEffects.None;

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


        /// <inheritdoc/>
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == SelectedIndexProperty)
                this.scrollToSelectedItemAction.Schedule();
        }


        // Interface implementation.
        Type IStyleable.StyleKey { get; } = typeof(Avalonia.Controls.TabControl);
    }


    /// <summary>
    /// Data for drag and drop events on item of <see cref="TabControl"/>.
    /// </summary>
    public class DragOnTabItemEventArgs : EventArgs
    {
        // Constuctor.
        internal DragOnTabItemEventArgs(DragEventArgs e, int itemIndex, object item)
        {
            this.Data = e.Data;
            this.DragEffects = e.DragEffects;
            this.Item = item;
            this.ItemIndex = itemIndex;
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


        /// <summary>
        /// Get key modifiers.
        /// </summary>
        public KeyModifiers KeyModifiers { get; }
    }
}
