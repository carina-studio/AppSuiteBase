using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Message dialog.
/// </summary>
class MessageDialogImpl : Dialog
{
	/// <summary>
	/// Define <see cref="CustomCancelText"/> property.
	/// </summary>
	public static readonly StyledProperty<string?> CustomCancelTextProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(CustomCancelText));
	/// <summary>
	/// Define <see cref="CustomDoNotAskOrShowAgainText"/> property.
	/// </summary>
	public static readonly StyledProperty<string?> CustomDoNotAskOrShowAgainTextProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(CustomDoNotAskOrShowAgainText));
	/// <summary>
	/// Define <see cref="CustomNoText"/> property.
	/// </summary>
	public static readonly StyledProperty<string?> CustomNoTextProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(CustomNoText));
	/// <summary>
	/// Define <see cref="CustomOKText"/> property.
	/// </summary>
	public static readonly StyledProperty<string?> CustomOKTextProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(CustomOKText));
	/// <summary>
	/// Define <see cref="CustomYesText"/> property.
	/// </summary>
	public static readonly StyledProperty<string?> CustomYesTextProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(CustomYesText));
	/// <summary>
	/// Define <see cref="Description"/> property.
	/// </summary>
	public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(Description));
	/// <summary>
	/// Define <see cref="DoNotAskOrShowAgainDescription"/> property.
	/// </summary>
	public static readonly StyledProperty<string?> DoNotAskOrShowAgainDescriptionProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(DoNotAskOrShowAgainDescription));
	/// <summary>
	/// Define <see cref="Message"/> property.
	/// </summary>
	public static readonly StyledProperty<string?> MessageProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(Message));
	/// <summary>
	/// Define <see cref="SecondaryMessage"/> property.
	/// </summary>
	public static readonly StyledProperty<string?> SecondaryMessageProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(SecondaryMessage));


	// Static fields.
	static readonly StyledProperty<MessageDialogResult?> Button1ResultProperty = AvaloniaProperty.Register<MessageDialogImpl, MessageDialogResult?>(nameof(Button1Result));
	static readonly StyledProperty<string?> Button1TextProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(Button1Text));
	static readonly StyledProperty<MessageDialogResult?> Button2ResultProperty = AvaloniaProperty.Register<MessageDialogImpl, MessageDialogResult?>(nameof(Button2Result));
	static readonly StyledProperty<string?> Button2TextProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(Button2Text));
	static readonly StyledProperty<MessageDialogResult?> Button3ResultProperty = AvaloniaProperty.Register<MessageDialogImpl, MessageDialogResult?>(nameof(Button3Result));
	static readonly StyledProperty<string?> Button3TextProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(Button3Text));
	static readonly StyledProperty<IImage?> IconImageProperty = AvaloniaProperty.Register<MessageDialogImpl, IImage?>(nameof(IconImage));
	static readonly StyledProperty<bool> IsButton1VisibleProperty = AvaloniaProperty.Register<MessageDialogImpl, bool>(nameof(IsButton1Visible));
	static readonly StyledProperty<bool> IsButton2VisibleProperty = AvaloniaProperty.Register<MessageDialogImpl, bool>(nameof(IsButton2Visible));
	static readonly StyledProperty<bool> IsButton3VisibleProperty = AvaloniaProperty.Register<MessageDialogImpl, bool>(nameof(IsButton3Visible));


	// Fields.
	bool? doNotAskOrShowAgain;
	readonly CheckBox doNotAskOrShowAgainCheckBox;
	readonly Panel doNotAskOrShowAgainPanel;
	MessageDialogResult? result;


	// Constructor.
	public MessageDialogImpl()
	{
		this.SelectResultCommand = new Command<MessageDialogResult?>(this.SelectResult);
		AvaloniaXamlLoader.Load(this);
		this.doNotAskOrShowAgainCheckBox = this.Get<CheckBox>(nameof(doNotAskOrShowAgainCheckBox));
		this.doNotAskOrShowAgainPanel = this.Get<Panel>(nameof(doNotAskOrShowAgainPanel));
	}


	// Get result of button 1.
	MessageDialogResult? Button1Result { get => this.GetValue<MessageDialogResult?>(Button1ResultProperty); }


	// Get text of button 1.
	string? Button1Text { get => this.GetValue<string?>(Button1TextProperty); }


	// Get result of button 2.
	MessageDialogResult? Button2Result { get => this.GetValue<MessageDialogResult?>(Button2ResultProperty); }


	// Get text of button 2.
	string? Button2Text { get => this.GetValue<string?>(Button2TextProperty); }


	// Get result of button 3.
	MessageDialogResult? Button3Result { get => this.GetValue<MessageDialogResult?>(Button3ResultProperty); }


	// Get text of button 3.
	string? Button3Text { get => this.GetValue<string?>(Button3TextProperty); }


	/// <summary>
	/// Get or set buttons.
	/// </summary>
	public MessageDialogButtons Buttons { get; set; } = MessageDialogButtons.OK;


	/// <summary>
	/// Get or set custom text for [Cancel] button.
	/// </summary>
	public string? CustomCancelText
	{
		get => this.GetValue(CustomCancelTextProperty);
		set => this.SetValue(CustomCancelTextProperty, value);
	}


	/// <summary>
	/// Get or set custom text for "Do not show again" UI.
	/// </summary>
	public string? CustomDoNotAskOrShowAgainText
	{
		get => this.GetValue(CustomDoNotAskOrShowAgainTextProperty);
		set => this.SetValue(CustomDoNotAskOrShowAgainTextProperty, value);
	}


	/// <summary>
	/// Custom icon.
	/// </summary>
	public IImage? CustomIcon { get; set; }


	/// <summary>
	/// Get or set custom text for [No] button.
	/// </summary>
	public string? CustomNoText
	{
		get => this.GetValue(CustomNoTextProperty);
		set => this.SetValue(CustomNoTextProperty, value);
	}


	/// <summary>
	/// Get or set custom text for [OK] button.
	/// </summary>
	public string? CustomOKText
	{
		get => this.GetValue(CustomOKTextProperty);
		set => this.SetValue(CustomOKTextProperty, value);
	}


	/// <summary>
	/// Get or set custom text for [Yes] button.
	/// </summary>
	public string? CustomYesText
	{
		get => this.GetValue(CustomYesTextProperty);
		set => this.SetValue(CustomYesTextProperty, value);
	}


	/// <summary>
	/// Default dialog result.
	/// </summary>
	public MessageDialogResult? DefaultResult { get; set; }


	/// <summary>
	/// Get or set description to show.
	/// </summary>
	public string? Description
	{
		get => this.GetValue(DescriptionProperty);
		set => this.SetValue(DescriptionProperty, value);
	}


	// Do not ask again or not.
	public bool? DoNotAskOrShowAgain
	{
		get => this.doNotAskOrShowAgain;
		set
		{
			if (this.doNotAskOrShowAgain == value)
				return;
			this.doNotAskOrShowAgain = value;
			if (!value.HasValue)
				this.doNotAskOrShowAgainPanel.IsVisible = false;
			else
			{
				this.doNotAskOrShowAgainCheckBox.IsChecked = value.GetValueOrDefault();
				this.doNotAskOrShowAgainPanel.IsVisible = true;
			}
		}
	}


	/// <summary>
	/// Get or set description of "Do not show again" UI.
	/// </summary>
	public string? DoNotAskOrShowAgainDescription
	{
		get => this.GetValue(DoNotAskOrShowAgainDescriptionProperty);
		set => this.SetValue(DoNotAskOrShowAgainDescriptionProperty, value);
	}


	/// <summary>
	/// Get or set icon.
	/// </summary>
	public new MessageDialogIcon Icon { get; set; } = MessageDialogIcon.Information;


	// Get IImage according to Icon.
	IImage? IconImage { get => this.GetValue<IImage?>(IconImageProperty); }


	// Check whether button 1 is visible or not.
	bool IsButton1Visible { get => this.GetValue<bool>(IsButton1VisibleProperty); }


	// Check whether button 2 is visible or not.
	bool IsButton2Visible { get => this.GetValue<bool>(IsButton2VisibleProperty); }


	// Check whether button 3 is visible or not.
	bool IsButton3Visible { get => this.GetValue<bool>(IsButton3VisibleProperty); }


	/// <summary>
	/// Get or set message to show.
	/// </summary>
	public string? Message
	{
		get => this.GetValue(MessageProperty);
		set => this.SetValue(MessageProperty, value);
	}


	// Called when application string resources updated.
	void OnAppStringsUpdated(object? sender, EventArgs e) =>
		this.UpdateButtonText();
	

	// Called when closed.
	protected override void OnClosed(EventArgs e)
	{
		this.Application.StringsUpdated -= this.OnAppStringsUpdated;
		base.OnClosed(e);
	}


	// Called when closing.
	protected override void OnClosing(WindowClosingEventArgs e)
	{
		if (this.result == null)
			e.Cancel = true;
		base.OnClosing(e);
	}


	// Called when opened.
	protected override void OnOpened(EventArgs e)
	{
		// attach to application
		this.Application.StringsUpdated += this.OnAppStringsUpdated;

		// setup icon
		var app = this.Application;
		if (this.Icon == MessageDialogIcon.Custom)
			this.SetValue(IconImageProperty, this.CustomIcon);
		else if (((Avalonia.Application)app).TryFindResource<IImage>($"Image/Icon.{this.Icon}.Colored", out var image))
			this.SetValue(IconImageProperty, image);

		// setup "do not ask again" UI
		if (this.doNotAskOrShowAgain.HasValue)
		{
			if (!string.IsNullOrEmpty(this.GetValue(CustomDoNotAskOrShowAgainTextProperty)))
				this.doNotAskOrShowAgainCheckBox.Bind(ContentProperty, this.GetObservable(CustomDoNotAskOrShowAgainTextProperty));
			else
			{
				switch (this.Buttons)
				{
					case MessageDialogButtons.OK:
					case MessageDialogButtons.OKCancel:
						this.doNotAskOrShowAgainCheckBox.Bind(ContentProperty, this.Application.GetObservableString("Common.DoNotShowAgain"));
						break;
					default:
						this.doNotAskOrShowAgainCheckBox.Bind(ContentProperty, this.Application.GetObservableString("Common.DoNotAskAgain"));
						break;
				}
			}
		}

		// setup buttons
		var defaultButton = (Button?)null;
		var defaultResult = this.DefaultResult;
		switch (this.Buttons)
		{
			case MessageDialogButtons.OK:
				this.SetValue<MessageDialogResult?>(Button1ResultProperty, MessageDialogResult.OK);
				this.SetValue<bool>(IsButton1VisibleProperty, true);
				defaultButton = defaultResult == MessageDialogResult.OK ? this.FindControl<Button>("button1") : null;
				break;
			case MessageDialogButtons.OKCancel:
				this.SetValue<MessageDialogResult?>(Button1ResultProperty, MessageDialogResult.OK);
				this.SetValue<MessageDialogResult?>(Button2ResultProperty, MessageDialogResult.Cancel);
				this.SetValue<bool>(IsButton1VisibleProperty, true);
				this.SetValue<bool>(IsButton2VisibleProperty, true);
				defaultButton = defaultResult switch
				{
					MessageDialogResult.OK => this.FindControl<Button>("button1"),
					MessageDialogResult.Cancel => this.FindControl<Button>("button2"),
					_ => null,
				};
				break;
			case MessageDialogButtons.YesNo:
				this.SetValue<MessageDialogResult?>(Button1ResultProperty, MessageDialogResult.Yes);
				this.SetValue<MessageDialogResult?>(Button2ResultProperty, MessageDialogResult.No);
				this.SetValue<bool>(IsButton1VisibleProperty, true);
				this.SetValue<bool>(IsButton2VisibleProperty, true);
				defaultButton = defaultResult switch
				{
					MessageDialogResult.Yes => this.FindControl<Button>("button1"),
					MessageDialogResult.No => this.FindControl<Button>("button2"),
					_ => null,
				};
				break;
			case MessageDialogButtons.YesNoCancel:
				this.SetValue<MessageDialogResult?>(Button1ResultProperty, MessageDialogResult.Yes);
				this.SetValue<MessageDialogResult?>(Button2ResultProperty, MessageDialogResult.No);
				this.SetValue<MessageDialogResult?>(Button3ResultProperty, MessageDialogResult.Cancel);
				this.SetValue<bool>(IsButton1VisibleProperty, true);
				this.SetValue<bool>(IsButton2VisibleProperty, true);
				this.SetValue<bool>(IsButton3VisibleProperty, true);
				defaultButton = defaultResult switch
				{
					MessageDialogResult.Yes => this.FindControl<Button>("button1"),
					MessageDialogResult.No => this.FindControl<Button>("button2"),
					MessageDialogResult.Cancel => this.FindControl<Button>("button3"),
					_ => null,
				};
				break;
			default:
				throw new ArgumentException($"Unsupported buttons type: {this.Buttons}.");
		}
		this.UpdateButtonText();
		if (defaultButton != null)
			this.SynchronizationContext.Post(defaultButton.Focus);

		// call base
		base.OnOpened(e);
	}


	/// <inheritdoc/>
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		var property = e.Property;
		if (property == CustomCancelTextProperty
			|| property == CustomNoTextProperty
			|| property == CustomOKTextProperty
			|| property == CustomYesTextProperty)
		{
			this.UpdateButtonText();
		}
	}


	/// <summary>
	/// Get or set secandary message to show.
	/// </summary>
	public string? SecondaryMessage
	{
		get => this.GetValue(SecondaryMessageProperty);
		set => this.SetValue(SecondaryMessageProperty, value);
	}


	// Select result.
	void SelectResult(MessageDialogResult? result)
	{
		if (result != null)
		{
			this.result = result;
			if (this.doNotAskOrShowAgainPanel.IsVisible)
				this.DoNotAskOrShowAgain = this.doNotAskOrShowAgainCheckBox.IsChecked;
			this.Close(result);
		}
	}


	/// <summary>
	/// Command to select result.
	/// </summary>
	public ICommand SelectResultCommand { get; }


	// Update text of buttons.
	void UpdateButtonText()
	{
		var app = this.Application;
		switch (this.Buttons)
		{
			case MessageDialogButtons.OK:
			{
				var customOKText = this.CustomOKText;
				this.SetValue(Button1TextProperty, string.IsNullOrEmpty(customOKText) ? app.GetString("Common.OK") : customOKText);
				break;
			}
			case MessageDialogButtons.OKCancel:
			{
				var customOKText = this.CustomOKText;
				var customCancelText = this.CustomCancelText;
				this.SetValue(Button1TextProperty, string.IsNullOrEmpty(customOKText) ? app.GetString("Common.OK") : customOKText);
				this.SetValue(Button2TextProperty, string.IsNullOrEmpty(customCancelText) ? app.GetString("Common.Cancel") : customCancelText);
				break;
			}
			case MessageDialogButtons.YesNo:
			{
				var customYesText = this.CustomYesText;
				var customNoText = this.CustomNoText;
				this.SetValue(Button1TextProperty, string.IsNullOrEmpty(customYesText) ? app.GetString("Common.Yes") : customYesText);
				this.SetValue(Button2TextProperty, string.IsNullOrEmpty(customNoText) ? app.GetString("Common.No") : customNoText);
				break;
			}
			case MessageDialogButtons.YesNoCancel:
			{
				var customYesText = this.CustomYesText;
				var customNoText = this.CustomNoText;
				var customCancelText = this.CustomCancelText;
				this.SetValue(Button1TextProperty, string.IsNullOrEmpty(customYesText) ? app.GetString("Common.Yes") : customYesText);
				this.SetValue(Button2TextProperty, string.IsNullOrEmpty(customNoText) ? app.GetString("Common.No") : customNoText);
				this.SetValue(Button3TextProperty, string.IsNullOrEmpty(customCancelText) ? app.GetString("Common.Cancel") : customCancelText);
				break;
			}
			default:
				throw new ArgumentException($"Unsupported buttons type: {this.Buttons}.");
		}
	}
}