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
            var oldOpacity = 1.0;
            var newOpacity = 1.0;
            (oldValue as ISolidColorBrush)?.Let(brush =>
            {
                oldColor = brush.Color;
                oldOpacity = brush.Opacity;
            });
            (newValue as ISolidColorBrush)?.Let(brush =>
            {
                newColor = brush.Color;
                newOpacity = brush.Opacity;
            });
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
            var interpolatedOpacity = Math.Max(0, Math.Min(1, oldOpacity + (newOpacity - oldOpacity) * progress));
            return new ImmutableSolidColorBrush(interpolatedColor, interpolatedOpacity);
        }
    }
}
