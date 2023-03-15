using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.Scripting;
using CarinaStudio.Configuration;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog for single line text input.
/// </summary>
partial class ScriptRuntimeErrorDialogImpl : Dialog
{
	/// <summary>
	/// Define property of message to be shown.
	/// </summary>
	public static readonly StyledProperty<string?> MessageProperty = AvaloniaProperty.Register<ScriptRuntimeErrorDialogImpl, string?>("Message");


	// Fields.
	Exception? error;
	IDisposable? errorMessageBindingToken;
	readonly TextBlock errorMessageTextBlock;
	readonly ToggleSwitch promptWhenRuntimeErrorOccurredSwitch;


	// Constructor.
	public ScriptRuntimeErrorDialogImpl()
	{
		AvaloniaXamlLoader.Load(this);
		this.errorMessageTextBlock = this.Get<TextBlock>(nameof(errorMessageTextBlock));
		this.promptWhenRuntimeErrorOccurredSwitch = this.Get<ToggleSwitch>(nameof(promptWhenRuntimeErrorOccurredSwitch));
	}


	/// <summary>
	/// Get or set runtime error occurred.
	/// </summary>
	public Exception? Error
	{
		get => this.error;
		set
		{
			this.VerifyAccess();
			if (this.error == value)
				return;
			this.error = value;
			this.errorMessageBindingToken?.Dispose();
			if (value != null)
			{
				this.errorMessageBindingToken = this.errorMessageTextBlock.Bind(TextBlock.TextProperty, new FormattedString().Also(it =>
				{
					it.Arg1 = value.Message;
					if (value is ScriptException scriptException && scriptException.Line > 0)
					{
						it.Arg2 = scriptException.Line;
						if (scriptException.Column >= 0)
						{
							it.Arg3 = scriptException.Column;
							it.Bind(FormattedString.FormatProperty, this.Application.GetObservableString("ScriptRuntimeErrorDialog.ErrorMessage.WithLineColumn"));
						}
						else
							it.Bind(FormattedString.FormatProperty, this.Application.GetObservableString("ScriptRuntimeErrorDialog.ErrorMessage.WithLine"));
					}
					else
						it.Bind(FormattedString.FormatProperty, this.Application.GetObservableString("ScriptRuntimeErrorDialog.ErrorMessage"));
				}));
			}
		}
	}


	/// <inheritdoc/>
	protected override void OnClosed(EventArgs e)
	{
		this.Settings.SetValue<bool>(SettingKeys.PromptWhenScriptRuntimeErrorOccurred, this.promptWhenRuntimeErrorOccurredSwitch.IsChecked.GetValueOrDefault());
		this.errorMessageBindingToken?.Dispose();
		base.OnClosed(e);
	}


	/// <inheritdoc/>
	protected override void OnOpened(EventArgs e)
	{
		base.OnOpened(e);
		this.promptWhenRuntimeErrorOccurredSwitch.IsChecked = this.Settings.GetValueOrDefault(SettingKeys.PromptWhenScriptRuntimeErrorOccurred);
	}


	/// <summary>
	/// Open the window which shows logs of script.
	/// </summary>
	public void OpenScriptLogWindow()
	{
		this.Close();
		ScriptManager.Default.OpenLogWindow();
	}
}
