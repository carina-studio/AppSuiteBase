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
class TextInputDialogImpl : InputDialog
{
	/// <summary>
	/// Property of <see cref="CheckBoxDescription"/>.
	/// </summary>
	public static readonly StyledProperty<string?> CheckBoxDescriptionProperty = AvaloniaProperty.Register<TextInputDialogImpl, string?>(nameof(CheckBoxDescription));
	/// <summary>
	/// Property of <see cref="CheckBoxMessage"/>.
	/// </summary>
	public static readonly StyledProperty<string?> CheckBoxMessageProperty = AvaloniaProperty.Register<TextInputDialogImpl, string?>(nameof(CheckBoxMessage));
	/// <summary>
	/// Property of <see cref="MaxTextLength"/>.
	/// </summary>
	public static readonly StyledProperty<int> MaxTextLengthProperty = AvaloniaProperty.Register<TextInputDialogImpl, int>(nameof(MaxTextLength), -1);
	/// <summary>
	/// Property of <see cref="Message"/>.
	/// </summary>
	public static readonly StyledProperty<string?> MessageProperty = AvaloniaProperty.Register<TextInputDialogImpl, string?>(nameof(Message));
	/// <summary>
	/// Property of <see cref="Text"/>.
	/// </summary>
	public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<TextInputDialogImpl, string?>(nameof(Text));


	// Fields.
	readonly CheckBox checkBox;
	readonly Panel checkBoxPanel;
	bool? isCheckBoxChecked;
	readonly TextBox textBox;


	/// <summary>
	/// Initialize new <see cref="TextInputDialogImpl"/> instance.
	/// </summary>
	public TextInputDialogImpl()
	{
		AvaloniaXamlLoader.Load(this);
		this.checkBox = this.Get<CheckBox>(nameof(checkBox));
		this.checkBoxPanel = this.Get<Panel>(nameof(checkBoxPanel));
		this.textBox = this.Get<TextBox>(nameof(textBox));
		this.GetObservable(TextProperty).Subscribe(_ => this.InvalidateInput());
	}


	/// <summary>
	/// Get or set description of check box.
	/// </summary>
	public string? CheckBoxDescription
	{
		get => this.GetValue(CheckBoxDescriptionProperty);
		set => this.SetValue(CheckBoxDescriptionProperty, value);
	}


	/// <summary>
	/// Get or set message of check box.
	/// </summary>
	public string? CheckBoxMessage
	{
		get => this.GetValue(CheckBoxMessageProperty);
		set => this.SetValue(CheckBoxMessageProperty, value);
	}


	// Do not show again or not.
	public bool? IsCheckBoxChecked 
	{ 
		get => this.isCheckBoxChecked;
		set
		{
			if (this.isCheckBoxChecked == value)
				return;
			this.isCheckBoxChecked = value;
			if (!value.HasValue)
				this.checkBoxPanel.IsVisible = false;
			else
			{
				this.checkBoxPanel.IsVisible = true;
				this.checkBox.IsChecked = value.GetValueOrDefault();
			}
		}
	}


	// Generate result.
	protected override Task<object?> GenerateResultAsync(CancellationToken cancellationToken) =>
		Task.FromResult((object?)this.textBox.Text.AsNonNull());


	/// <inheritdoc/>
	protected override void OnClosing(WindowClosingEventArgs e)
	{
		if (this.checkBoxPanel.IsVisible)
			this.isCheckBoxChecked = this.checkBox.IsChecked.GetValueOrDefault();
		base.OnClosing(e);
	}


	/// <inheritdoc/>
	protected override void OnEnterKeyClickedOnInputControl(Control control)
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
	protected override bool OnValidateInput() =>
		base.OnValidateInput() && !string.IsNullOrEmpty(this.textBox.Text);


	/// <summary>
	/// Get or set maximum length of input text.
	/// </summary>
	public int MaxTextLength
	{
		get => this.GetValue(MaxTextLengthProperty);
		set => this.SetValue(MaxTextLengthProperty, value);
	}


	/// <summary>
	/// Get or set message to show.
	/// </summary>
	public string? Message
	{
		get => this.GetValue(MessageProperty);
		set => this.SetValue(MessageProperty, value);
	}


	/// <summary>
	/// Get or set text.
	/// </summary>
	public string? Text
	{
		get => this.GetValue(TextProperty);
		set => this.SetValue(TextProperty, value);
	}
}
