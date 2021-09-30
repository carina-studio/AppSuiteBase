using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CarinaStudio.Controls;
using System;
using System.ComponentModel;

namespace CarinaStudio.AppSuite.Controls
{
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
		static readonly AvaloniaProperty<Drawing?> IconDrawingProperty = AvaloniaProperty.Register<MessageDialogImpl, Drawing?>(nameof(IconDrawing));
		static readonly AvaloniaProperty<bool> IsButton1VisibleProperty = AvaloniaProperty.Register<MessageDialogImpl, bool>(nameof(IsButton1Visible));
		static readonly AvaloniaProperty<bool> IsButton2VisibleProperty = AvaloniaProperty.Register<MessageDialogImpl, bool>(nameof(IsButton2Visible));
		static readonly AvaloniaProperty<bool> IsButton3VisibleProperty = AvaloniaProperty.Register<MessageDialogImpl, bool>(nameof(IsButton3Visible));
		static readonly AvaloniaProperty<string?> MessageProperty = AvaloniaProperty.Register<MessageDialogImpl, string?>(nameof(Message));


		// Fields.
		MessageDialogResult? result;


		// Constructor.
		public MessageDialogImpl()
		{
			InitializeComponent();
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
		/// Get or set icon.
		/// </summary>
		public new MessageDialogIcon Icon { get; set; } = MessageDialogIcon.Information;


		// Get Drawing according to Icon.
		Drawing? IconDrawing { get => this.GetValue<Drawing?>(IconDrawingProperty); }


		// Initialize Avalonia components.
		private void InitializeComponent() => AvaloniaXamlLoader.Load(this);


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
			if (((Avalonia.Application)app).TryFindResource<Drawing>($"Drawing/Icon.{this.Icon}.Colored", out var drawing))
				this.SetValue<Drawing?>(IconDrawingProperty, drawing);

			// setup buttons
			switch (this.Buttons)
			{
				case MessageDialogButtons.OK:
					this.SetValue<MessageDialogResult?>(Button1ResultProperty, MessageDialogResult.OK);
					this.SetValue<string?>(Button1TextProperty, app.GetString("Common.OK"));
					this.SetValue<bool>(IsButton1VisibleProperty, true);
					break;
				case MessageDialogButtons.OKCancel:
					this.SetValue<MessageDialogResult?>(Button1ResultProperty, MessageDialogResult.OK);
					this.SetValue<MessageDialogResult?>(Button2ResultProperty, MessageDialogResult.Cancel);
					this.SetValue<string?>(Button1TextProperty, app.GetString("Common.OK"));
					this.SetValue<string?>(Button2TextProperty, app.GetString("Common.Cancel"));
					this.SetValue<bool>(IsButton1VisibleProperty, true);
					this.SetValue<bool>(IsButton2VisibleProperty, true);
					break;
				case MessageDialogButtons.YesNo:
					this.SetValue<MessageDialogResult?>(Button1ResultProperty, MessageDialogResult.Yes);
					this.SetValue<MessageDialogResult?>(Button2ResultProperty, MessageDialogResult.No);
					this.SetValue<string?>(Button1TextProperty, app.GetString("Common.Yes"));
					this.SetValue<string?>(Button2TextProperty, app.GetString("Common.No"));
					this.SetValue<bool>(IsButton1VisibleProperty, true);
					this.SetValue<bool>(IsButton2VisibleProperty, true);
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
					break;
				default:
					throw new ArgumentException();
			}

			// call base
			base.OnOpened(e);
		}


		// Select result.
		void SelectResult(MessageDialogResult? result)
		{
			if (result != null)
			{
				this.result = result;
				this.Close(result);
			}
		}
	}
}
