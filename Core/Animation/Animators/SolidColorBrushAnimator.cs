using Avalonia.Animation;
using Avalonia.Animation.Animators;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using System;

namespace CarinaStudio.AppSuite.Animation.Animators
{
    /// <summary>
    /// Implementation of <see cref="IAnimator"/> to animate <see cref="SolidColorBrush"/>.
    /// </summary>
    public class SolidColorBrushAnimator : Animator<IBrush?>
    {
        // Clip value into range of [0, 255].
        static byte ClipToByte(double value)
        {
            if (value < 0)
                return 0;
            if (value > 255)
                return 255;
            return (byte)(value + 0.5);
        }


        /// <inheritdoc/>
        public override IBrush? Interpolate(double progress, IBrush? oldValue, IBrush? newValue)
        {
            var oldColor = new Color();
            var newColor = new Color();
            (oldValue as ISolidColorBrush)?.Let(brush =>
            {
                oldColor = brush.Color;
                oldColor = Color.FromArgb(ClipToByte(oldColor.A * brush.Opacity), oldColor.R, oldColor.G, oldColor.B);
            });
            (newValue as ISolidColorBrush)?.Let(brush =>
            {
                newColor = brush.Color;
                newColor = Color.FromArgb(ClipToByte(newColor.A * brush.Opacity), newColor.R, newColor.G, newColor.B);
            });
            if (oldColor == Colors.Transparent)
                oldColor = Color.FromArgb(0, newColor.R, newColor.G, newColor.B);
            if (newColor == Colors.Transparent)
                newColor = Color.FromArgb(0, oldColor.R, oldColor.G, oldColor.B);
            var oldA = (double)oldColor.A;
            var oldR = (double)oldColor.R;
            var oldG = (double)oldColor.G;
            var oldB = (double)oldColor.B;
            var newA = (double)newColor.A;
            var newR = (double)newColor.R;
            var newG = (double)newColor.G;
            var newB = (double)newColor.B;
            var interpolatedColor = Color.FromArgb(
                ClipToByte(oldA + (newA - oldA) * progress),
                ClipToByte(oldR + (newR - oldR) * progress),
                ClipToByte(oldG + (newG - oldG) * progress),
                ClipToByte(oldB + (newB - oldB) * progress)
            );
            return new ImmutableSolidColorBrush(interpolatedColor);
        }
    }
}
