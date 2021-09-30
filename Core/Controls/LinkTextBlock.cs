using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// <see cref="TextBlock"/> which supports open the link.
    /// </summary>
    public class LinkTextBlock : TextBlock, IStyleable
    {
        /// <summary>
        /// Property of <see cref="Uri"/>.
        /// </summary>
        public static readonly AvaloniaProperty<Uri?> UriProperty = AvaloniaProperty.Register<LinkTextBlock, Uri?>(nameof(Uri));


        /// <summary>
        /// Initialize new <see cref="LinkTextBlock"/> instance.
        /// </summary>
        public LinkTextBlock()
        { }


        /// <summary>
        /// Called when pointer released.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (e.InitialPressMouseButton == MouseButton.Left)
                this.Uri?.Let(it => Platform.OpenLink(it));
        }


        /// <summary>
        /// Get or set URI to open.
        /// </summary>
        public Uri? Uri
        {
            get => this.GetValue<Uri?>(UriProperty);
            set => this.SetValue<Uri?>(UriProperty, value);
        }


        // Interface implementation.
        Type IStyleable.StyleKey { get; } = typeof(LinkTextBlock);
    }
}
