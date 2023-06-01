using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// <see cref="Avalonia.Controls.ListBox"/> with extended functions.
    /// </summary>
    public class ListBox : Avalonia.Controls.ListBox
    {
        // Fields.
        long doubleTappedTime;
        object? pointerDownItem;
        readonly Stopwatch stopwatch = new();


        /// <summary>
        /// Initialize new <see cref="ListBox"/> instance.
        /// </summary>
        public ListBox()
        {
            this.DoubleTapped += this.OnDoubleTapped;
            this.AddHandler(PointerPressedEvent, this.OnPreviewPointerPressed, RoutingStrategies.Tunnel);
            this.AddHandler(PointerReleasedEvent, this.OnPreviewPointerReleased, RoutingStrategies.Tunnel);
        }


        /// <summary>
        /// Raised when double clicked on item in the control.
        /// </summary>
        public event EventHandler<ListBoxItemEventArgs>? DoubleClickOnItem;


        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            this.stopwatch.Start();
        }


        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            this.stopwatch.Stop();
            base.OnDetachedFromVisualTree(e);
        }


        // Called when double tapped.
        void OnDoubleTapped(object? sender, RoutedEventArgs e) =>
            this.doubleTappedTime = this.stopwatch.ElapsedMilliseconds;


        // Pointer pressed.
        void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(this);
            var hitElement = pointerPoint.Properties.IsLeftButtonPressed
                ? this.InputHitTest(pointerPoint.Position)
                : null;
            Dispatcher.UIThread.Post(() =>
            {
                if (hitElement != null && this.SelectedItems?.Count == 1)
                {
                    var listBoxItem = hitElement is Visual visual
                        ? visual.FindAncestorOfType<ListBoxItem>(true)
                        : null;
                    if (listBoxItem != null && listBoxItem.FindAncestorOfType<ListBox>() == this)
                        this.pointerDownItem = this.SelectedItem;
                }
            });
        }
        

        // Pointer released.
        void OnPreviewPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Left 
                && (this.stopwatch.ElapsedMilliseconds - this.doubleTappedTime) <= 500
                && this.pointerDownItem != null
                && this.SelectedItems?.Count == 1
                && this.SelectedItem == this.pointerDownItem)
            {
                var doubleClickedItem = this.pointerDownItem;
                Dispatcher.UIThread.Post(() =>
                {
                    if (this.SelectedItem == doubleClickedItem
                        && this.SelectedItems?.Count == 1)
                    {
                        this.DoubleClickOnItem?.Invoke(this, new ListBoxItemEventArgs(this.SelectedIndex, doubleClickedItem));
                    }
                }, DispatcherPriority.Normal);
            }
            this.pointerDownItem = null;
        }


        /// <inheritdoc/>
        protected override Type StyleKeyOverride => typeof(Avalonia.Controls.ListBox);
    }


    /// <summary>
    /// Data for events relate to item of <see cref="ListBox"/>.
    /// </summary>
    public class ListBoxItemEventArgs : EventArgs
    {
        // Constructor.
        internal ListBoxItemEventArgs(int itemIndex, object item)
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