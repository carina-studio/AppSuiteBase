using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;

namespace CarinaStudio.AppSuite.Media;

/// <summary>
/// Extension methods for <see cref="IImage"/>.
/// </summary>
public static class ImageExtensions
{
    // Constants.
    const float NativeMenuItemIconDpi = 96;
    static readonly int NativeMenuItemIconPadding = Platform.IsMacOS ? 5 : 0;
    static readonly int NativeMenuItemIconSize = 32;
    
    
    /// <summary>
    /// Convert the <see cref="IImage"/> to <see cref="Bitmap"/> which is suitable for icon of native menu item.
    /// </summary>
    /// <param name="image"><see cref="IImage"/>.</param>
    /// <returns><see cref="Bitmap"/> for icon of native menu item.</returns>
    public static Bitmap ToNativeMenuItemIcon(this IImage image)
    {
        var iconSize = new PixelSize(NativeMenuItemIconSize, NativeMenuItemIconSize);
        var iconDpi = new Vector(NativeMenuItemIconDpi, NativeMenuItemIconDpi);
        if (IAppSuiteApplication.CurrentOrNull is { } app
            && app.EffectiveThemeMode != app.SystemThemeMode
            && image is DrawingImage drawingImage
            && drawingImage.Drawing is { } drawing
            && drawing.TryClone(out var clonedDrawing))
        {
            var iconBrush = app.FindResourceOrDefault<IBrush?>("Brush/Icon.Inverse");
            void ApplyIconBrush(Drawing? drawing)
            {
                if (drawing is GeometryDrawing geometryDrawing)
                    geometryDrawing.Brush = iconBrush;
                else if (drawing is DrawingGroup drawingGroup)
                {
                    foreach (var child in drawingGroup.Children)
                        ApplyIconBrush(child);
                }
            }
            ApplyIconBrush(clonedDrawing);
            image = new DrawingImage(clonedDrawing);
        }
        return new RenderTargetBitmap(iconSize, iconDpi).Also(bitmap =>
        {
            using var dc = bitmap.CreateDrawingContext();
            var srcSize = image.Size;
            var destWidth = NativeMenuItemIconSize - NativeMenuItemIconPadding - NativeMenuItemIconPadding;
            var destHeight = destWidth;
            if (srcSize.Width < srcSize.Height) 
                destWidth = (int)Math.Round(destWidth * srcSize.Width / srcSize.Height);
            else if (srcSize.Height < srcSize.Width)
                destHeight = (int)Math.Round(destHeight * srcSize.Height / srcSize.Width);
            dc.PushTransform(Matrix.CreateTranslation((NativeMenuItemIconSize - destWidth) >> 1, (NativeMenuItemIconSize - destHeight) >> 1));
            image.Draw(dc, new(srcSize), new(0, 0, destWidth, destHeight));
        });
    }
}