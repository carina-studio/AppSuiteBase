using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="Decorator"/> which provides the snapshot of its content as the backdrop for <see cref="Backdrop"/>.
/// </summary>
public class BackdropTarget : Decorator
{
    /// <summary>
    /// Define <see cref="AutoInvalidate"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> AutoInvalidateProperty = AvaloniaProperty.Register<BackdropTarget, bool>(nameof(AutoInvalidate), true);
    /// <summary>
    /// Define <see cref="BackgroundColor"/> property.
    /// </summary>
    public static readonly StyledProperty<Color> BackgroundColorProperty = AvaloniaProperty.Register<BackdropTarget, Color>(nameof(BackgroundColor), Colors.Transparent);


    // Fields.
    RenderTargetBitmap? backdrop;
    IImmutableBrush? backgroundBrush;
    readonly HashSet<Visual> consumers = new();
    bool isBackdropValid;
    bool isPreparingBackdrop;


    /// <summary>
    /// Get or set whether the backdrop should be invalidated automatically when the content is scrolled or resized.
    /// </summary>
    public bool AutoInvalidate
    {
        get => this.GetValue(AutoInvalidateProperty);
        set => this.SetValue(AutoInvalidateProperty, value);
    }


    /// <summary>
    /// Get or set the color filled behind the backdrop, so the blur of a <see cref="Backdrop"/> has content instead of transparency where the backdrop does not cover (e.g. near the edges of the content). Set to null to fill nothing.
    /// </summary>
    public Color BackgroundColor
    {
        get => this.GetValue(BackgroundColorProperty);
        set => this.SetValue(BackgroundColorProperty, value);
    }


    // Draw the backdrop sampled by the given consumer. [srcRect] and [destRect] are both in coordinate system of [consumer].
    internal void DrawBackdrop(DrawingContext context, Visual consumer, Rect srcRect, Rect destRect)
    {
        // make sure that the backdrop is ready
        this.PrepareBackdrop();
        var backdrop = this.backdrop;
        var content = this.Child;
        if (backdrop is null || content is null)
            return;

        // fill the background color first (if set) so the backdrop has content instead of transparency where it does not cover the destination
        if (this.backgroundBrush is not null)
            context.FillRectangle(this.backgroundBrush, destRect);

        // convert source rectangle from coordinate system of consumer to pixels of the backdrop bitmap (DrawImage samples the source in bitmap pixels, not DIPs)
        var transform = consumer.TransformToVisual(content);
        if (transform is null)
            return;
        var pixelSize = backdrop.PixelSize;
        var dipSize = backdrop.Size;
        if (dipSize.Width <= 0 || dipSize.Height <= 0)
            return;
        var pixelPerDipX = pixelSize.Width / dipSize.Width;
        var pixelPerDipY = pixelSize.Height / dipSize.Height;
        var mappedSrcRect = srcRect.TransformToAABB(transform.Value);
        var pixelSrcRect = new Rect(
            mappedSrcRect.X * pixelPerDipX,
            mappedSrcRect.Y * pixelPerDipY,
            mappedSrcRect.Width * pixelPerDipX,
            mappedSrcRect.Height * pixelPerDipY);
        var clampedSrcRect = pixelSrcRect.Intersect(new(0.0, 0.0, pixelSize.Width, pixelSize.Height));
        if (clampedSrcRect.Width <= 0 || clampedSrcRect.Height <= 0)
            return;

        // adjust destination rectangle proportionally when the source rectangle is clamped
        var finalDestRect = destRect;
        if (clampedSrcRect != pixelSrcRect && pixelSrcRect.Width > 0 && pixelSrcRect.Height > 0)
        {
            finalDestRect = new(
                destRect.Left + destRect.Width * (clampedSrcRect.Left - pixelSrcRect.Left) / pixelSrcRect.Width,
                destRect.Top + destRect.Height * (clampedSrcRect.Top - pixelSrcRect.Top) / pixelSrcRect.Height,
                destRect.Width * clampedSrcRect.Width / pixelSrcRect.Width,
                destRect.Height * clampedSrcRect.Height / pixelSrcRect.Height);
        }

        // draw backdrop
        context.DrawImage(backdrop, clampedSrcRect, finalDestRect);
    }


    /// <summary>
    /// Invalidate the cached backdrop and request all registered <see cref="Backdrop"/>s to redraw.
    /// </summary>
    public void Invalidate()
    {
        // invalidate cached backdrop
        this.isBackdropValid = false;

        // request registered backdrops to redraw
        this.InvalidateConsumers();
    }


    // Request all registered consumers to redraw without dropping the cached backdrop.
    void InvalidateConsumers()
    {
        foreach (var consumer in this.consumers)
            consumer.InvalidateVisual();
    }


    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.AddHandler(ScrollViewer.ScrollChangedEvent, this.OnContentScrollChanged);
    }


    // Called when content (or a descendant of content) is scrolled, or its extent/viewport is changed.
    void OnContentScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (this.AutoInvalidate)
            this.Invalidate();
    }


    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        this.RemoveHandler(ScrollViewer.ScrollChangedEvent, this.OnContentScrollChanged);
        this.backdrop = this.backdrop.DisposeAndReturnNull();
        this.isBackdropValid = false;
        base.OnDetachedFromVisualTree(e);
    }


    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BackgroundColorProperty)
        {
            // update cached brush and request consumers to redraw (the cached backdrop itself is unchanged)
            var color = this.BackgroundColor;
            this.backgroundBrush = color.A > 0 ? new ImmutableSolidColorBrush(color) : null;
            this.InvalidateConsumers();
        }
    }


    /// <inheritdoc/>
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        this.Invalidate();
        base.OnSizeChanged(e);
    }


    // Render the content into the cached backdrop bitmap if it is invalid.
    void PrepareBackdrop()
    {
        // skip if the backdrop is already valid or is being prepared (e.g. a Backdrop is nested in content)
        if (this.isPreparingBackdrop || (this.isBackdropValid && this.backdrop is not null))
            return;

        // drop the backdrop when there is no content to capture
        var content = this.Child;
        var contentSize = content?.Bounds.Size ?? default;
        if (content is null || contentSize.Width <= 0 || contentSize.Height <= 0)
        {
            this.backdrop = this.backdrop.DisposeAndReturnNull();
            this.isBackdropValid = false;
            return;
        }

        // calculate size of the backdrop bitmap (always rendered at 96 DPI: Avalonia's RenderTargetBitmap.Render corrupts box-shadow content at any other DPI, so HiDPI/reduced-resolution snapshots are not possible)
        var pixelSize = new PixelSize(
            Math.Max(1, (int)(contentSize.Width + 0.5)),
            Math.Max(1, (int)(contentSize.Height + 0.5)));

        // (re)create the backdrop bitmap when its size is changed
        if (this.backdrop is null || this.backdrop.PixelSize != pixelSize)
        {
            this.backdrop = this.backdrop.DisposeAndReturnNull();
            this.backdrop = new RenderTargetBitmap(pixelSize, new Vector(96.0, 96.0));
        }

        // render content into the backdrop bitmap
        this.isPreparingBackdrop = true;
        try
        {
            this.backdrop.Render(content);
            this.isBackdropValid = true;
        }
        finally
        {
            this.isPreparingBackdrop = false;
        }
    }


    // Register the given backdrop as a consumer.
    internal void Register(Visual consumer) =>
        this.consumers.Add(consumer);


    // Unregister the given backdrop.
    internal void Unregister(Visual consumer) =>
        this.consumers.Remove(consumer);
}
