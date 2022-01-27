using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.Text.RegularExpressions;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// <see cref="TextBox"/> which accept regular expression.
	/// </summary>
	public class RegexTextBox : ObjectTextBox<Regex>
	{
		/// <summary>
		/// Property of <see cref="IgnoreCase"/>.
		/// </summary>
		public static readonly AvaloniaProperty<bool> IgnoreCaseProperty = AvaloniaProperty.Register<RegexTextBox, bool>(nameof(IgnoreCase), true);
		/// <summary>
		/// Property of <see cref="IsInputAssistanceEnabled"/>.
		/// </summary>
		public static readonly AvaloniaProperty<bool> IsInputAssistanceEnabledProperty = AvaloniaProperty.Register<RegexTextBox, bool>(nameof(IsInputAssistanceEnabled), true);
		/// <summary>
		/// Property of <see cref="Regex"/>.
		/// </summary>
		public static readonly AvaloniaProperty<Regex?> RegexProperty = AvaloniaProperty.Register<RegexTextBox, Regex?>(nameof(Regex), coerce: (textBox, regex) =>
		{
			if (regex == null)
				return null;
			var ignoreCase = ((RegexTextBox)textBox).IgnoreCase;
			var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
			if (regex.Options != options)
				return new Regex(regex.ToString(), options);
			return regex;
		});


		// Fields.
		readonly ScheduledAction closeMenusIfNotFocusedAction;
		ContextMenu? escapedCharactersMenu;
		TextPresenter? textPresenter;


		/// <summary>
		/// Initialize new <see cref="RegexTextBox"/> instance.
		/// </summary>
		public RegexTextBox()
		{
			this.MaxLength = 65536;
			this.Bind(WatermarkProperty, this.GetResourceObservable("String/RegexTextBox.Watermark"));
			this.closeMenusIfNotFocusedAction = new ScheduledAction(() =>
			{
				if (!this.IsFocused)
				{
					this.escapedCharactersMenu?.Close();
				}
			});
		}


		/// <inheritdoc/>
		protected override bool CheckObjectEquality(Regex? x, Regex? y)
		{
			if (x == null)
				return y == null;
			if (y == null)
				return false;
			return x.ToString() == y.ToString() && x.Options == y.Options;
		}


        /// <summary>
        /// Get or set whether case in <see cref="Regex"/> can be ignored or not.
        /// </summary>
        public bool IgnoreCase
		{
			get => this.GetValue<bool>(IgnoreCaseProperty);
			set => this.SetValue<bool>(IgnoreCaseProperty, value);
		}


		// Input escaped character.
		void InputEscapedCharacter(char c)
		{
			this.escapedCharactersMenu?.Close();
			this.SelectedText = c.ToString();
		}


		/// <summary>
		/// Get or set whether input assistance is enabled or not.
		/// </summary>
		/// <value></value>
		public bool IsInputAssistanceEnabled
		{
			get => this.GetValue<bool>(IsInputAssistanceEnabledProperty);
			set => this.SetValue<bool>(IsInputAssistanceEnabledProperty, value);
		}


		/// <inheritdoc/>
		protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
		{
			base.OnApplyTemplate(e);
			this.textPresenter = e.NameScope.Find<TextPresenter>("PART_TextPresenter");
		}


		/// <inheritdoc/>
		protected override void OnGotFocus(GotFocusEventArgs e)
		{
			base.OnGotFocus(e);
			this.closeMenusIfNotFocusedAction.Cancel();
		}


		/// <inheritdoc/>
		protected override void OnLostFocus(RoutedEventArgs e)
		{
			//this.closeMenusIfNotFocusedAction.Reschedule(100);
			base.OnLostFocus(e);
		}


		/// <inheritdoc/>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			var isBackspace = e.Key == Key.Back;
			var isDelete = e.Key == Key.Delete;
			if (isBackspace || isDelete)
			{
				var selectionStart = this.SelectionStart;
				var selectionEnd = this.SelectionEnd;
				if (selectionStart > selectionEnd)
				{
					var t = selectionEnd;
					selectionEnd = selectionStart;
					selectionStart = t;
				}
				var text = this.Text ?? "";
				var textLength = text.Length;
				var deletingChar = Global.Run(() =>
				{
					if (selectionStart == selectionEnd)
					{
						if (isBackspace && selectionStart > 0)
							return text[selectionStart - 1];
						else if (isDelete && selectionEnd < textLength)
							return text[selectionEnd];
					}
					else if (selectionEnd == selectionStart + 1)
						return text[selectionStart];
					return '\0';
				});
				var prevChar1 = isBackspace
					? selectionStart > 1 ? text[selectionStart - 2] : '\0'
					: selectionStart > 0 ? text[selectionStart - 1] : '\0';
				var prevChar2 = isBackspace
					? selectionStart > 2 ? text[selectionStart - 3] : '\0'
					: selectionStart > 1 ? text[selectionStart - 2] : '\0';
				var nextChar1 = isBackspace
					? selectionEnd < textLength ? text[selectionEnd] : '\0'
					: selectionEnd < textLength - 1 ? text[selectionEnd + 1] : '\0';
				switch (deletingChar)
				{
					case '(':
						if (nextChar1 == ')' && (prevChar1 != '\\' || prevChar2 == '\\'))
						{
							this.SelectionStart = selectionStart;
							this.SelectionEnd = selectionEnd + 1;
							this.SelectedText = "";
							e.Handled = true;
						}
						break;
					case '[':
						if (nextChar1 == ']' && (prevChar1 != '\\' || prevChar2 == '\\'))
						{
							this.SelectionStart = selectionStart;
							this.SelectionEnd = selectionEnd + 1;
							this.SelectedText = "";
							e.Handled = true;
						}
						break;
					case '{':
						if (nextChar1 == '}' && (prevChar1 != '\\' || prevChar2 == '\\'))
						{
							this.SelectionStart = selectionStart;
							this.SelectionEnd = selectionEnd + 1;
							this.SelectedText = "";
							e.Handled = true;
						}
						break;
					case '<':
						if (nextChar1 == '>' && (prevChar1 != '\\' || prevChar2 == '\\'))
						{
							this.SelectionStart = selectionStart;
							this.SelectionEnd = selectionEnd + 1;
							this.SelectedText = "";
							e.Handled = true;
						}
						break;
				}
			}
			base.OnKeyDown(e);
		}


		/// <inheritdoc/>
		protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
		{
			base.OnPropertyChanged(change);
			var property = change.Property;
			if (property == IgnoreCaseProperty)
				this.Validate();
			else if (property == ObjectProperty)
			{
				var regex = change.NewValue.Value as Regex;
				if (!this.CheckObjectEquality(regex, this.Regex))
					this.SetValue<Regex?>(RegexProperty, regex);
			}
			else if (property == RegexProperty)
			{
				var regex = change.NewValue.Value as Regex;
				if (!this.CheckObjectEquality(regex, this.Object))
					this.SetValue<Regex?>(ObjectProperty, regex);
			}
		}


		/// <inheritdoc/>
        protected override void OnTextInput(TextInputEventArgs e)
		{
			// no need to handle
			var s = e.Text;
			if (!this.IsInputAssistanceEnabled || string.IsNullOrEmpty(s))
			{
				base.OnTextInput(e);
				return;
			}

			// close context menu
			this.escapedCharactersMenu?.Close();

			// assist input
			var selectionStart = this.SelectionStart;
			var selectionEnd = this.SelectionEnd;
			if (selectionStart > selectionEnd)
			{
				var t = selectionEnd;
				selectionEnd = selectionStart;
				selectionStart = t;
			}
			var text = this.Text ?? "";
			var textLength = text.Length;
			var prevChar1 = selectionStart > 0 ? text[selectionStart - 1] : '\0';
			var prevChar2 = selectionStart > 1 ? text[selectionStart - 2] : '\0';
			var nextChar1 = selectionEnd < textLength ? text[selectionEnd] : '\0';
			++selectionStart;
			switch (s[0])
			{
				case '(':
					if (prevChar1 != '\\')
						e.Text = "()";
					break;
				case ')':
					if (prevChar1 != '\\' && nextChar1 == ')')
						e.Text = "";
					break;
				case '[':
					if (prevChar1 != '\\')
						e.Text = "[]";
					break;
				case ']':
					if (prevChar1 != '\\' && nextChar1 == ']')
						e.Text = "";
					break;
				case '{':
					if (prevChar1 != '\\')
						e.Text = "{}";
					break;
				case '}':
					if (prevChar1 != '\\' && nextChar1 == '}')
						e.Text = "";
					break;
				case '<':
					if (prevChar1 == '?' && prevChar2 == '(')
						e.Text = "<>";
					break;
				case '>':
					if (prevChar1 != '\\' && nextChar1 == '>')
						e.Text = "";
					break;
				case '?':
					if (prevChar1 == '(' && prevChar2 != '\\')
					{
						e.Text = "?<>";
						++selectionStart;
					}
					break;
				case '\\':
					if (prevChar1 != '\\' || prevChar2 == '\\')
					{
						var menu = this.SetupEscapedCharactersMenu();
						menu.HorizontalOffset = this.textPresenter?.Let(it =>
						{
							return it.FormattedText.HitTestTextPosition(it.CaretIndex).Left;
						}) ?? 0;
						menu.Open(this);
					}
					break;
			}

			// commit input
			base.OnTextInput(e);
			this.SelectionStart = selectionStart;
			this.SelectionEnd = selectionStart;
		}


		/// <summary>
		/// Get or set <see cref="Regex"/>.
		/// </summary>
		public Regex? Regex
		{
			get => this.GetValue<Regex?>(RegexProperty);
			set => this.SetValue<Regex?>(RegexProperty, value);
		}


		// Setup menu for escaped characters.
		ContextMenu SetupEscapedCharactersMenu()
		{
			if (this.escapedCharactersMenu != null)
				return this.escapedCharactersMenu;
			this.escapedCharactersMenu = new ContextMenu().Also(menu =>
			{
				menu.Items = new object[] {
					new MenuItem().Also(it =>
					{
						it.Command = new Command<char>(this.InputEscapedCharacter);
						it.CommandParameter = 'd';
						it.Bind(MenuItem.HeaderProperty, this.GetResourceObservable("String/RegexTextBox.EscapedCharacter.d"));
					}),
					new MenuItem().Also(it =>
					{
						it.Command = new Command<char>(this.InputEscapedCharacter);
						it.CommandParameter = 's';
						it.Bind(MenuItem.HeaderProperty, this.GetResourceObservable("String/RegexTextBox.EscapedCharacter.s"));
					}),
					new MenuItem().Also(it =>
					{
						it.Command = new Command<char>(this.InputEscapedCharacter);
						it.CommandParameter = 'w';
						it.Bind(MenuItem.HeaderProperty, this.GetResourceObservable("String/RegexTextBox.EscapedCharacter.w"));
					}),
					new MenuItem().Also(it =>
					{
						it.Command = new Command<char>(this.InputEscapedCharacter);
						it.CommandParameter = 't';
						it.Bind(MenuItem.HeaderProperty, this.GetResourceObservable("String/RegexTextBox.EscapedCharacter.t"));
					}),
					new Separator(),
					new MenuItem().Also(it =>
					{
						it.Command = new Command<char>(this.InputEscapedCharacter);
						it.CommandParameter = 'D';
						it.Bind(MenuItem.HeaderProperty, this.GetResourceObservable("String/RegexTextBox.EscapedCharacter.D"));
					}),
					new MenuItem().Also(it =>
					{
						it.Command = new Command<char>(this.InputEscapedCharacter);
						it.CommandParameter = 'S';
						it.Bind(MenuItem.HeaderProperty, this.GetResourceObservable("String/RegexTextBox.EscapedCharacter.S"));
					}),
					new MenuItem().Also(it =>
					{
						it.Command = new Command<char>(this.InputEscapedCharacter);
						it.CommandParameter = 'W';
						it.Bind(MenuItem.HeaderProperty, this.GetResourceObservable("String/RegexTextBox.EscapedCharacter.W"));
					}),
				};
				menu.AddHandler(KeyDownEvent, (_, e) =>
				{
					switch (e.Key)
					{
						case Key.Down:
						case Key.Enter:
						case Key.Up:
							break;
						default:
							menu.Close();
							this.OnKeyDown(e);
							break;
					}
				}, RoutingStrategies.Tunnel);
				menu.PlacementMode = PlacementMode.Bottom;
				menu.PlacementTarget = this;
			});
			return this.escapedCharactersMenu;
		}


		/// <inheritdoc/>
        protected override bool TryConvertToObject(string text, out Regex? obj)
        {
            try
            {
				obj = new Regex(text, this.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
				return true;
            }
			catch
            {
				obj = null;
				return false;
            }
        }
    }
}
