using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// <see cref="Avalonia.Controls.ListBox"/> with extended functions.
    /// </summary>
    public class ListBox : Avalonia.Controls.ListBox, IStyleable
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


        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(this);
            var hitElement = pointerPoint.Properties.IsLeftButtonPressed
                ? this.InputHitTest(pointerPoint.Position)
                : null;
            base.OnPointerPressed(e);
            if (hitElement != null && this.SelectedItems?.Count == 1)
            {
                var listBoxItem = hitElement.FindAncestorOfType<ListBoxItem>(true);
                if (listBoxItem != null && listBoxItem.FindAncestorOfType<ListBox>() == this)
                    this.pointerDownItem = this.SelectedItem;
            }
        }
        

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (e.InitialPressMouseButton == MouseButton.Left 
                && (this.stopwatch.ElapsedMilliseconds - this.doubleTappedTime) <= 500
                && this.pointerDownItem != null
                && this.SelectedItems?.Count == 1
                && this.SelectedItem == this.pointerDownItem)
            {
                this.DoubleClickOnItem?.Invoke(this, new ListBoxItemEventArgs(this.SelectedIndex, this.pointerDownItem));
            }
            this.pointerDownItem = null;
        }


        // Interface implementations.
		Type IStyleable.StyleKey => typeof(Avalonia.Controls.ListBox);
    }


    /// <summary>
    /// Data for events relate to item of <see cref="ListBox"/>.
    /// </summary>
    public class ListBoxItemEventArgs : EventArgs
    {
        // Constuctor.
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