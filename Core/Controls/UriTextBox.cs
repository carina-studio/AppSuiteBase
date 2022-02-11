using Avalonia;
using Avalonia.Controls;
using System;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// <see cref="TextBox"/> which treat input text as <see cref="Uri"/>.
	/// </summary>
	public class UriTextBox : ObjectTextBox<Uri>
	{
		/// <summary>
		/// Property of <see cref="Uri"/>.
		/// </summary>
		public static readonly AvaloniaProperty<Uri?> UriProperty = AvaloniaProperty.Register<UriTextBox, Uri?>(nameof(Uri), null);
		/// <summary>
		/// Property of <see cref="UriKind"/>.
		/// </summary>
		public static readonly AvaloniaProperty<UriKind> UriKindProperty = AvaloniaProperty.Register<UriTextBox, UriKind>(nameof(IsTextValid), UriKind.Absolute);


		/// <summary>
		/// Initialize new <see cref="UriTextBox"/> instance.
		/// </summary>
		public UriTextBox()
		{
			this.MaxLength = 65536;
			this.Bind(WatermarkProperty, this.GetResourceObservable("String/UriTextBox.Watermark"));
		}


		/// <inheritdoc/>
		protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
		{
			base.OnPropertyChanged(change);
			var property = change.Property;
			if (property == ObjectProperty)
				this.SetValue<Uri?>(UriProperty, change.NewValue.Value as Uri);
			else if (property == UriProperty)
				this.SetValue<Uri?>(ObjectProperty, change.NewValue.Value as Uri);
			else if (property == UriKindProperty)
				this.Validate();
		}


		/// <inheritdoc/>
		protected override bool TryConvertToObject(string text, out Uri? obj) => Uri.TryCreate(text, this.UriKind, out obj);


        /// <summary>
        /// Get or set <see cref="Uri"/>.
        /// </summary>
        public Uri? Uri
		{
			get => this.GetValue<Uri?>(UriProperty);
			set 
			{
				if (this.IsValidationScheduled)
                    this.Validate();
				this.SetValue<Uri?>(UriProperty, value);
			}
		}


		/// <summary>
		/// Get or set target <see cref="UriKind"/>.
		/// </summary>
		public UriKind UriKind
		{
			get => this.GetValue<UriKind>(UriKindProperty);
			set => this.SetValue<UriKind>(UriKindProperty, value);
		}
	}
}
