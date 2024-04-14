using System;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace CarinaStudio.AppSuite.Media;

/// <summary>
/// Image for window background.
/// </summary>
public class WindowBackgroundImage : AvaloniaObject, IImage
{
    /**
     * Fixed height of image.
     */
    public const double Height = 128;

    /**
     * Fixed width of image.
     */
    public const double Width = 128;


    /// <summary>
    /// Fixed bounds of image with <see cref="Avalonia.Layout.Orientation.Horizontal"/> orientation.
    /// </summary>
    public static readonly Rect HorizontalBounds = new(0, 0, Width, 1);

    /// <summary>
    /// Define <see cref="DarkColor"/> property.
    /// </summary>
    public static readonly StyledProperty<Color> DarkColorProperty =
        AvaloniaProperty.Register<WindowBackgroundImage, Color>(nameof(DarkColor), Colors.Black);

    /// <summary>
    /// Define <see cref="LightColor"/> property.
    /// </summary>
    public static readonly StyledProperty<Color> LightColorProperty =
        AvaloniaProperty.Register<WindowBackgroundImage, Color>(nameof(LightColor), Colors.White);

    /// <summary>
    /// Define <see cref="Orientation"/> property.
    /// </summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<WindowBackgroundImage, Orientation>(nameof(Orientation), Orientation.Horizontal);

    /// <summary>
    /// Fixed bounds of image with <see cref="Avalonia.Layout.Orientation.Vertical"/> orientation.
    /// </summary>
    public static readonly Rect VerticalBounds = new(0, 0, 1, Height);


    // Drawing operation.
    class DrawingOperation(Color darkColor, Color lightColor, Orientation orientation, Rect destRect)
        : ICustomDrawOperation
    {
        // Fields.
        SKPaint? paint;
        SKShader? shader;

        /// <inheritdoc/>
        public bool Equals(ICustomDrawOperation? other) =>
            ReferenceEquals(this, other);

        /// <inheritdoc/>
        public void Dispose()
        {
            this.paint = this.paint.DisposeAndReturnNull();
            this.shader = this.shader.DisposeAndReturnNull();
        }

        /// <inheritdoc/>
        public bool HitTest(Point p) => false;

        /// <inheritdoc/>
        public void Render(ImmediateDrawingContext context)
        {
            if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is not ISkiaSharpApiLeaseFeature skiaFeature)
                return;
            using var lease = skiaFeature.Lease();
            var canvas = lease.SkCanvas;
            this.shader ??= SKShader.CreateLinearGradient(
                orientation switch
                {
                    Orientation.Vertical => new(0, (float)destRect.Top),
                    _ => new SKPoint((float)destRect.Left, 0),
                },
                orientation switch
                {
                    Orientation.Vertical => new(0, (float)destRect.Bottom),
                    _ => new SKPoint((float)destRect.Right, 0),
                },
                [
                    new SKColorF(lightColor.R / 255f, lightColor.G / 255f, lightColor.B / 255f, lightColor.A / 255f),
                    new SKColorF(darkColor.R / 255f, darkColor.G / 255f, darkColor.B / 255f, darkColor.A / 255f)
                ],
                SKColorSpace.CreateSrgb(),
                [0f, 1],
                SKShaderTileMode.Clamp
            );
            this.paint ??= new SKPaint().Also(it =>
            {
                it.IsDither = true;
                it.Shader = this.shader;
            });
            canvas.DrawRect(
                new((float)destRect.Left, (float)destRect.Top, (float)destRect.Right, (float)destRect.Bottom),
                this.paint);
        }

        /// <inheritdoc/>
        public Rect Bounds => destRect;
    }


    /// <inheritdoc/>
    public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
    {
        context.Custom(new DrawingOperation(
            this.GetValue(DarkColorProperty),
            this.GetValue(LightColorProperty),
            this.GetValue(OrientationProperty),
            destRect));
    }


    /// <summary>
    /// Get or set dark color.
    /// </summary>
    public Color DarkColor
    {
        get => this.GetValue(DarkColorProperty);
        set => this.SetValue(DarkColorProperty, value);
    }


    /// <summary>
    /// Get or set light color.
    /// </summary>
    public Color LightColor
    {
        get => this.GetValue(LightColorProperty);
        set => this.SetValue(LightColorProperty, value);
    }


    /// <summary>
    /// Get or set orientation of color interpolation.
    /// </summary>
    public Orientation Orientation
    {
        get => this.GetValue(OrientationProperty);
        set => this.SetValue(OrientationProperty, value);
    }


    /// <inheritdoc/>
    public Size Size => GetValue(OrientationProperty) switch
    {
        Orientation.Horizontal => new(Width, 1),
        Orientation.Vertical => new(1, Height),
        _ => throw new NotSupportedException(),
    };
}