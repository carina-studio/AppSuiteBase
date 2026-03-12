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