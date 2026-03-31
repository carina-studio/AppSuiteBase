using Avalonia.Media;
using System.Diagnostics.CodeAnalysis;

namespace CarinaStudio.AppSuite.Media;

/// <summary>
/// Extension methods for <see cref="Drawing"/>.
/// </summary>
public static class DrawingExtensions
{
    /// <summary>
    /// Try cloning the <see cref="Drawing"/>.
    /// </summary>
    /// <param name="drawing">The <see cref="Drawing"/> to be cloned.</param>
    /// <param name="clone">Cloned <see cref="Drawing"/>.</param>
    /// <returns>True if the <see cref="Drawing"/> has been cloned successfully.</returns>
    public static bool TryClone(this Drawing drawing, [NotNullWhen(true)] out Drawing? clone)
    {
        clone = null;
        if (drawing is GeometryDrawing geometryDrawing)
        {
            clone = new GeometryDrawing
            {
                Brush = geometryDrawing.Brush,
                Geometry = geometryDrawing.Geometry,
                Pen = geometryDrawing.Pen
            };
        }
        else if (drawing is DrawingGroup drawingGroup)
        {
            clone = new DrawingGroup().Also(it =>
            {
                foreach (var child in drawingGroup.Children)
                {
                    if (child is not null && child.TryClone(out var childClone))
                        it.Children.Add(childClone);
                }
            });
        }
        else if (drawing is ImageDrawing imageDrawing)
        {
            clone = new ImageDrawing
            {
                ImageSource = imageDrawing.ImageSource,
                Rect = imageDrawing.Rect,
            };
        }
        else
            return false;
        return true;
    }
}