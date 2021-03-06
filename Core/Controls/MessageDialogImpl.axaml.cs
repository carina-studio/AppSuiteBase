using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System;
using System.ComponentModel;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Message dialog.
/// </summary>
partial class MessageDialogImpl : Dialog
{
	// Static fields.
	static readonly AvaloniaProperty<MessageDialogResult?> Button1ResultProperty = AvaloniaProperty.Register<MessageDialogImpl, MessageDialogResult?>(nameof(Button1Result));
	static readonly AvaloniaProperty<string?> Button1TextProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(Button1Text));
	static readonly AvaloniaProperty<MessageDialogResult?> Button2ResultProperty = AvaloniaProperty.Register<MessageDialogImpl, MessageDialogResult?>(nameof(Button2Result));
	static readonly AvaloniaProperty<string?> Button2TextProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(Button2Text));
	static readonly AvaloniaProperty<MessageDialogResult?> Button3ResultProperty = AvaloniaProperty.Register<MessageDialogImpl, MessageDialogResult?>(nameof(Button3Result));
	static readonly AvaloniaProperty<string?> Button3TextProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(Button3Text));
	static readonly AvaloniaProperty<IImage?> IconImageProperty = AvaloniaProperty.Register<MessageDialogImpl, IImage?>(nameof(IconImage));
	static readonly AvaloniaProperty<bool> IsButton1VisibleProperty = AvaloniaProperty.Register<MessageDialogImpl, bool>(nameof(IsButton1Visible));
	static readonly AvaloniaProperty<bool> IsButton2VisibleProperty = AvaloniaProperty.Register<MessageDialogImpl, bool>(nameof(IsButton2Visible));
	static readonly AvaloniaProperty<bool> IsButton3VisibleProperty = AvaloniaProperty.Register<MessageDialogImpl, bool>(nameof(IsButton3Visible));
	static readonly AvaloniaProperty<string?> MessageProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(Message));


	// Fields.
	bool? doNotAskOrShowAgain;
	readonly Panel doNotAskOrShowAgainPanel;
	readonly CheckBox doNotAskOrShowAgainCheckBox;
	MessageDialogResult? result;


	// Constructor.
	public MessageDialogImpl()
	{
		AvaloniaXamlLoader.Load(this);
		this.doNotAskOrShowAgainPanel = this.Get<Panel>(nameof(doNotAskOrShowAgainPanel));
		this.doNotAskOrShowAgainCheckBox = this.Get<CheckBox>(nameof(doNotAskOrShowAgainCheckBox));
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
	/// Default dialog result.
	/// </summary>
	public MessageDialogResult? DefaultResult { get; set; }


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
		get => this.GetValue<string?>(MessageProperty);
		set => this.SetValue<string?>(MessageProperty, value);
	}


	// Called when closing.
	protected override void OnClosing(CancelEventArgs e)
	{
		if (this.result == null)
			e.Cancel = true;
		base.OnClosing(e);
	}


	// Called when opened.
	protected override void OnOpened(EventArgs e)
	{
		// setup icon
		var app = this.Application;
		if (((Avalonia.Application)app).TryFindResource<IImage>($"Image/Icon.{this.Icon}.Colored", out var image))
			this.SetValue<IImage?>(IconImageProperty, image);

		// setup "do not ask again" UI
		if (this.doNotAskOrShowAgain.HasValue)
		{
			switch (this.Buttons)
			{
				case MessageDialogButtons.OK:
					this.doNotAskOrShowAgainCheckBox.Bind(CheckBox.ContentProperty, this.GetResourceObservable("String/Common.DoNotShowAgain"));
					break;
				default:
					this.doNotAskOrShowAgainCheckBox.Bind(CheckBox.ContentProperty, this.GetResourceObservable("String/Common.DoNotAskAgain"));
					break;
			}
		}

		// setup buttons
		var defaultButton = (Button?)null;
		var defaultResult = this.DefaultResult;
		switch (this.Buttons)
		{
			case MessageDialogButtons.OK:
				this.SetValue<MessageDialogResult?>(Button1ResultProperty, MessageDialogResult.OK);
				this.SetValue<string?>(Button1TextProperty, app.GetString("Common.OK"));
				this.SetValue<bool>(IsButton1VisibleProperty, true);
				defaultButton = defaultResult == MessageDialogResult.OK ? this.FindControl<Button>("button1") : null;
				break;
			case MessageDialogButtons.OKCancel:
				this.SetValue<MessageDialogResult?>(Button1ResultProperty, MessageDialogResult.OK);
				this.SetValue<MessageDialogResult?>(Button2ResultProperty, MessageDialogResult.Cancel);
				this.SetValue<string?>(Button1TextProperty, app.GetString("Common.OK"));
				this.SetValue<string?>(Button2TextProperty, app.GetString("Common.Cancel"));
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
				this.SetValue<string?>(Button1TextProperty, app.GetString("Common.Yes"));
				this.SetValue<string?>(Button2TextProperty, app.GetString("Common.No"));
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
				this.SetValue<string?>(Button1TextProperty, app.GetString("Common.Yes"));
				this.SetValue<string?>(Button2TextProperty, app.GetString("Common.No"));
				this.SetValue<string?>(Button3TextProperty, app.GetString("Common.Cancel"));
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
				throw new ArgumentException();
		}
		if (defaultButton != null)
			this.SynchronizationContext.Post(defaultButton.Focus);

		// call base
		base.OnOpened(e);
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
}