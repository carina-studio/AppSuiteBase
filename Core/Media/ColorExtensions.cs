using Avalonia.Media;
using System;

namespace CarinaStudio.AppSuite.Media;

/// <summary>
/// Extension methods for <see cref="Color"/>.
/// </summary>
public static class ColorExtensions
{
    extension(Color color)
    {
        /// <summary>
        /// Blend the color into the target color with given ratio of target color.
        /// </summary>
        /// <param name="targetColor">Target color to blend into.</param>
        /// <param name="ratio">Ratio of target color in blended color, in range of [0.0, 1.0].</param>
        /// <returns>Blended color.</returns>
        public Color Blend(Color targetColor, double ratio)
        {
            var r = color.R + (targetColor.R - color.R) * ratio;
            var g = color.G + (targetColor.G - color.G) * ratio;
            var b = color.B + (targetColor.B - color.B) * ratio;
            return Color.FromArgb(color.A, (byte)(r + 0.5), (byte)(g + 0.5), (byte)(b + 0.5));
        }


        /// <summary>
        /// Desaturate the color toward its own luminance with given amount.
        /// </summary>
        /// <param name="amount">Amount of desaturation, in range of [0.0, 1.0].</param>
        /// <returns>Desaturated color.</returns>
        public Color Desaturate(double amount)
        {
            var luminance = color.R * 0.2126 + color.G * 0.7152 + color.B * 0.0722;
            var r = color.R + (luminance - color.R) * amount;
            var g = color.G + (luminance - color.G) * amount;
            var b = color.B + (luminance - color.B) * amount;
            return Color.FromArgb(color.A, (byte)(r + 0.5), (byte)(g + 0.5), (byte)(b + 0.5));
        }


        /// <summary>
        /// Transform RGB color values with given gamma.
        /// </summary>
        /// <param name="gamma">Gamma. Value greater than 1.0 darkens the color, value less than 1.0 lightens the color.</param>
        /// <returns>Transformed color.</returns>
        public Color GammaTransform(double gamma)
        {
            var r = (color.R / 255.0);
            var g = (color.G / 255.0);
            var b = (color.B / 255.0);
            var l = (r + g + b) / 3;
            var scale = Math.Pow(l, gamma) / l;
            return Color.FromArgb(color.A, 
                (byte)(Math.Min(255, r * scale * 255) + 0.5), 
                (byte)(Math.Min(255, g * scale * 255) + 0.5), 
                (byte)(Math.Min(255, b * scale * 255) + 0.5)
            );
        }
    }
}