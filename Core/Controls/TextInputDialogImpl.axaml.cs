using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.Windows.Input;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// Dialog for single line text input.
	/// </summary>
	partial class TextInputDialogImpl : InputDialog
	{
		/// <summary>
		/// Property of <see cref="MaxTextLength"/>.
		/// </summary>
		public static readonly AvaloniaProperty<int> MaxTextLengthProperty = AvaloniaProperty.Register<TextInputDialogImpl, int>(nameof(MaxTextLength), -1);
		/// <summary>
		/// Property of <see cref="Message"/>.
		/// </summary>
		public static readonly AvaloniaProperty<string?> MessageProperty = AvaloniaProperty.Register<TextInputDialogImpl, string?>(nameof(Message));
		/// <summary>
		/// Property of <see cref="Text"/>.
		/// </summary>
		public static readonly AvaloniaProperty<string?> TextProperty = AvaloniaProperty.Register<TextInputDialogImpl, string?>(nameof(Text));


		// Fields.
		readonly TextBox textBox;


		/// <summary>
		/// Initialize new <see cref="TextInputDialogImpl"/> instance.
		/// </summary>
		public TextInputDialogImpl()
		{
			InitializeComponent();
			this.textBox = this.FindControl<TextBox>(nameof(this.textBox)).AsNonNull();
		}


		// Generate result.
		protected override Task<object?> GenerateResultAsync(CancellationToken cancellationToken) =>
			Task.FromResult((object?)this.textBox.Text.AsNonNull());


		// Initialize.
		private void InitializeComponent() => AvaloniaXamlLoader.Load(this);


		/// <inheritdoc/>
        protected override void OnEnterKeyClickedOnInputControl(IControl control)
        {
            base.OnEnterKeyClickedOnInputControl(control);
			this.GenerateResultCommand.TryExecute();
		}


		// Called when opened.
		protected override void OnOpened(EventArgs e)
		{
			base.OnOpened(e);
			this.textBox.SelectAll();
			this.textBox.Focus();
		}


		// Called when property changed.
		protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
		{
			base.OnPropertyChanged(change);
			if (change.Property == TextProperty)
				this.InvalidateInput();
		}


		// Validate input.
		protected override bool OnValidateInput()
		{
			return base.OnValidateInput() && !string.IsNullOrEmpty(this.textBox.Text);
		}


		/// <summary>
		/// Get or set maximum length of input text.
		/// </summary>
		public int MaxTextLength
		{
			get => this.GetValue<int>(MaxTextLengthProperty);
			set => this.SetValue<int>(MaxTextLengthProperty, value);
		}


		/// <summary>
		/// Get or set message to show.
		/// </summary>
		public string? Message
		{
			get => this.GetValue<string?>(MessageProperty);
			set => this.SetValue<string?>(MessageProperty, value);
		}


		/// <summary>
		/// Get or set text.
		/// </summary>
		public string? Text
		{
			get => this.GetValue<string?>(TextProperty);
			set => this.SetValue<string?>(TextProperty, value);
		}
	}
}
