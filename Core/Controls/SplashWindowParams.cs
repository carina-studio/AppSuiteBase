using Avalonia.Media;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Parameters of splash window.
    /// </summary>
    public struct SplashWindowParams
    {
        /// <summary>
        /// Accent color of application.
        /// </summary>
        public Color AccentColor { get; set; }


        /// <summary>
        /// Opacity of background image.
        /// </summary>
        public double BackgroundImageOpacity { get; set; }


        /// <summary>
        /// URI of background image.
        /// </summary>
        public Uri? BackgroundImageUri { get; set; }


        /// <summary>
        /// URI of application icon.
        /// </summary>
        public Uri? IconUri { get; set; }
    }
}
