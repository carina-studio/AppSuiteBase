using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="Panel"/> to contain an item in dialog.
/// </summary>
public class DialogItem : Panel
{
    /// <summary>
    /// Define <see cref="ItemSize"/> property.
    /// </summary>
    public static readonly StyledProperty<DialogItemSize> ItemSizeProperty = AvaloniaProperty.Register<DialogItem, DialogItemSize>(nameof(ItemSize), DialogItemSize.Default);
    
    
    // Fields.
    Size firstChildSize;
    double minControlWidth;
    IDisposable? minHeightBindingToken;
    Size secondChildSize;
    
    
    /// <summary>
    /// Initialize new <see cref="DialogItem"/> instance.
    /// </summary>
    public DialogItem()
    {
        // setup properties
        this.ClipToBounds = true;
        this.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        // setup min size
        this.GetObservable(ItemSizeProperty).Subscribe(itemSize =>
        {
            this.minHeightBindingToken?.Dispose();
            this.minHeightBindingToken = this.Bind(MinHeightProperty, this.GetResourceObservable(itemSize switch
            {
                DialogItemSize.Small => "Double/Dialog.Item.MinHeight.Small",
                _ => "Double/Dialog.Item.MinHeight",
            }), BindingPriority.Template);
        });
        
        // bind to resources
        this.GetResourceObservable("Double/Dialog.Control.MinWidth").Subscribe(value =>
        {
            if (value is not double width)
                return;
            this.minControlWidth = width;
            this.InvalidateMeasure();
        });
    }
    
    
    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // get state
        var childCount = this.Children.Count;
        
        // arrange 1st child
        var arrangeX = 0.0;
        if (childCount > 0)
        {
            var child = this.Children[0];
            var childSize = this.firstChildSize;
            var childMargin = child.Margin;
            child.Arrange(new(
                childMargin.Left,
                (finalSize.Height - childSize.Height) / 2 + childMargin.Top,
                childSize.Width,
                childSize.Height));
            arrangeX = childSize.Width + childMargin.Left + childMargin.Right;
        }
        
        // arrange 2nd child
        if (childCount > 1)
        {
            var child = this.Children[1];
            var childSize = this.secondChildSize;
            var childMargin = child.Margin;
            var maxChildWidth = Math.Max(0, finalSize.Width - arrangeX - childMargin.Left - childMargin.Right);
            var childX = child.HorizontalAlignment switch
            {
                HorizontalAlignment.Center => arrangeX + (maxChildWidth - childSize.Width) / 2 + childMargin.Left,
                HorizontalAlignment.Left => arrangeX,
                HorizontalAlignment.Right => finalSize.Width - childSize.Width - childMargin.Right,
                HorizontalAlignment.Stretch => arrangeX + childMargin.Left,
                _ => throw new NotSupportedException(),
            };
            var childWidth = child.HorizontalAlignment switch
            {
                HorizontalAlignment.Stretch => maxChildWidth,
                _ => childSize.Width,
            };
            child.Arrange(new(
                childX,
                (finalSize.Height - childSize.Height) / 2 + childMargin.Top,
                childWidth,
                childSize.Height));
        }
        
        // complete
        return finalSize;
    }


    /// <summary>
    /// Get or set size of item.
    /// </summary>
    public DialogItemSize ItemSize
    {
        get => this.GetValue(ItemSizeProperty);
        set => this.SetValue(ItemSizeProperty, value);
    }


    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // get state
        var childCount = this.Children.Count;
        var availableWidth = availableSize.Width;
        
        // measure 2nd child
        Thickness secondChildMargin;
        if (childCount > 1)
        {
            var child = this.Children[1];
            secondChildMargin = child.Margin;
            var maxWidth = double.IsFinite(availableWidth) 
                ? Math.Max(0, availableWidth - this.minControlWidth - secondChildMargin.Left - secondChildMargin.Right) 
                : double.PositiveInfinity;
            child.Measure(new(maxWidth, Double.PositiveInfinity));
            this.secondChildSize = child.DesiredSize;
        }
        else
        {
            this.secondChildSize = default;
            secondChildMargin = default;
        }

        // measure first child
        Thickness firstChildMargin;
        if (childCount > 0)
        {
            var child = this.Children[0];
            firstChildMargin = child.Margin;
            var maxWidth = double.IsFinite(availableWidth) 
                ? Math.Max(0, availableWidth - this.secondChildSize.Width - firstChildMargin.Left - firstChildMargin.Right) 
                : double.PositiveInfinity;
            child.Measure(new(maxWidth, Double.PositiveInfinity));
            this.firstChildSize = child.DesiredSize;
        }
        else
        {
            this.firstChildSize = default;
            firstChildMargin = default;
        }

        // complete
        var measuredWidth = this.firstChildSize.Width + firstChildMargin.Left + firstChildMargin.Right 
                            + this.secondChildSize.Width + secondChildMargin.Left + secondChildMargin.Right;
        var measuredHeight = Math.Max(this.MinHeight,
            Math.Max(this.firstChildSize.Height + firstChildMargin.Top + firstChildMargin.Bottom, 
                this.secondChildSize.Height + secondChildMargin.Top + secondChildMargin.Bottom));
        if (double.IsFinite(availableWidth))
            return new(availableWidth, measuredHeight);
        return new(measuredWidth, measuredHeight);
    }
}