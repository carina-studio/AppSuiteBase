using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.Windows.Input;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

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
	bool? doNotAskAgain;
	readonly CheckBox doNotAskAgainCheckBox;
	bool? doNotShowAgain;
	readonly CheckBox doNotShowAgainCheckBox;
	readonly TextBox textBox;


	/// <summary>
	/// Initialize new <see cref="TextInputDialogImpl"/> instance.
	/// </summary>
	public TextInputDialogImpl()
	{
		AvaloniaXamlLoader.Load(this);
		this.doNotAskAgainCheckBox = this.Get<CheckBox>(nameof(doNotAskAgainCheckBox));
		this.doNotShowAgainCheckBox = this.Get<CheckBox>(nameof(doNotShowAgainCheckBox));
		this.textBox = this.Get<TextBox>(nameof(textBox));
		this.GetObservable(TextProperty).Subscribe(_ => this.InvalidateInput());
	}


	// Do not ask again or not.
	public bool? DoNotAskAgain 
	{ 
		get => this.doNotAskAgain;
		set
		{
			if (this.doNotAskAgain == value)
				return;
			this.doNotAskAgain = value;
			if (!value.HasValue)
				this.doNotAskAgainCheckBox.IsVisible = false;
			else
			{
				this.doNotAskAgainCheckBox.IsChecked = value.GetValueOrDefault();
				this.doNotAskAgainCheckBox.IsVisible = true;
			}
		}
	}


	// Do not show again or not.
	public bool? DoNotShowAgain 
	{ 
		get => this.doNotShowAgain;
		set
		{
			if (this.doNotShowAgain == value)
				return;
			this.doNotShowAgain = value;
			if (!value.HasValue)
				this.doNotShowAgainCheckBox.IsVisible = false;
			else
			{
				this.doNotShowAgainCheckBox.IsChecked = value.GetValueOrDefault();
				this.doNotShowAgainCheckBox.IsVisible = true;
			}
		}
	}


	// Generate result.
	protected override Task<object?> GenerateResultAsync(CancellationToken cancellationToken) =>
		Task.FromResult((object?)this.textBox.Text.AsNonNull());


	/// <inheritdoc/>
	protected override void OnClosing(CancelEventArgs e)
	{
		if (this.doNotAskAgainCheckBox.IsVisible)
			this.doNotAskAgain = this.doNotAskAgainCheckBox.IsChecked.GetValueOrDefault();
		if (this.doNotShowAgainCheckBox.IsVisible)
			this.doNotShowAgain = this.doNotShowAgainCheckBox.IsChecked.GetValueOrDefault();
		base.OnClosing(e);
	}


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
