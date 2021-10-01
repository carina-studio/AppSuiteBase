using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using CarinaStudio.Windows.Input;
using System;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// <see cref="TextBlock"/> which supports open the link.
    /// </summary>
    public class LinkTextBlock : TextBlock, IStyleable
    {
        /// <summary>
        /// Property of <see cref="Command"/>.
        /// </summary>
        public static readonly AvaloniaProperty<ICommand?> CommandProperty = AvaloniaProperty.Register<LinkTextBlock, ICommand?>(nameof(Command));
        /// <summary>
        /// Property of <see cref="CommandParameter"/>.
        /// </summary>
        public static readonly AvaloniaProperty<object?> CommandParameterProperty = AvaloniaProperty.Register<LinkTextBlock, object?>(nameof(CommandParameter));
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
        /// Get or set command to execute when clicking the link.
        /// </summary>
        public ICommand? Command
        {
            get => this.GetValue<ICommand?>(CommandProperty);
            set => this.SetValue<ICommand?>(CommandProperty, value);
        }


        /// <summary>
        /// Get or set parameter to execute <see cref="Command"/>.
        /// </summary>
        public object? CommandParameter
        {
            get => this.GetValue<object?>(CommandParameterProperty);
            set => this.SetValue<object?>(CommandParameterProperty, value);
        }


        /// <summary>
        /// Called when pointer released.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (e.InitialPressMouseButton == MouseButton.Left && this.IsPointerOver)
            {
                var command = this.Command;
                if (command != null)
                    command.TryExecute(this.CommandParameter);
                else
                    this.Uri?.Let(it => Platform.OpenLink(it));
            }
        }


        /// <summary>
        /// Called when property changed.
        /// </summary>
        /// <typeparam name="T">Type of property.</typeparam>
        /// <param name="change">Change data.</param>
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == CommandProperty)
            {
                if (change.NewValue.Value != null)
                    this.Uri = null;
            }
            else if (change.Property == UriProperty)
            {
                if (change.NewValue.Value != null)
                {
                    this.Command = null;
                    this.CommandParameter = null;
                }
            }
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
