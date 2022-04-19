using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using CarinaStudio.Collections;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

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


		// Fields.
		ContextMenu? escapedCharactersMenu;
		readonly ObservableList<MenuItem> filteredPredefinedGroupMenuItems = new ObservableList<MenuItem>();
		readonly SortedObservableList<RegexGroup> filteredPredefinedGroups = new SortedObservableList<RegexGroup>((x, y) => string.Compare(x?.Name, y?.Name));
		bool isBackSlashPressed;
		bool isEscapeKeyHandled;
		readonly ObservableList<RegexGroup> predefinedGroups = new ObservableList<RegexGroup>();
		ContextMenu? predefinedGroupsMenu;
		readonly Queue<MenuItem> recycledMenuItems = new Queue<MenuItem>();
		readonly ScheduledAction showAssistanceMenuAction;
		TextPresenter? textPresenter;


		/// <summary>
		/// Initialize new <see cref="RegexTextBox"/> instance.
		/// </summary>
		public RegexTextBox()
		{
			this.filteredPredefinedGroups.CollectionChanged += this.OnFilteredPredefinedGroupChanged;
			this.predefinedGroups.CollectionChanged += this.OnPredefinedGroupChanged;
			this.InputGroupNameCommand = new Command<string>(this.InputGroupName);
			this.InputStringCommand = new Command<string>(this.InputString);
			this.MaxLength = 1024;
			this.Bind(WatermarkProperty, this.GetResourceObservable("String/RegexTextBox.Watermark"));
			this.showAssistanceMenuAction = new ScheduledAction(() =>
			{
				// close menu first
				this.escapedCharactersMenu?.Close();
				this.predefinedGroupsMenu?.Close();
				var (start, end) = this.GetSelection();
				if (!this.IsInputAssistanceEnabled || !this.IsEffectivelyVisible || start != end)
				{
					this.isBackSlashPressed = false;
					return;
				}

				// show predefined groups menu
				var text = this.Text ?? "";
				var textLength = text.Length;
				var menuToOpen = (ContextMenu?)null;
				if (this.predefinedGroups.IsNotEmpty())
				{
					var (groupStart, groupEnd) = this.GetGroupNameSelection(text);
					if (groupStart >= 0)
					{
						var filterText = this.Text?.Substring(groupStart, groupEnd - groupStart)?.ToLower() ?? "";
						this.filteredPredefinedGroups.Clear();
						if (string.IsNullOrEmpty(filterText))
							this.filteredPredefinedGroups.AddAll(this.predefinedGroups);
						else
							this.filteredPredefinedGroups.AddAll(this.predefinedGroups.Where(it => it.Name.ToLower().Contains(filterText)));
						if (this.filteredPredefinedGroups.IsNotEmpty())
							menuToOpen = this.SetupPredefinedGroupsMenu();
					}
				}

				// show escaped characters menu
				if (this.isBackSlashPressed)
				{
					this.isBackSlashPressed = false;
					if (menuToOpen == null && start > 0 && text[start - 1] == '\\' && (start <= 1 || text[start - 2] != '\\'))
						menuToOpen = this.SetupEscapedCharactersMenu();
				}

				// open menu
				if (menuToOpen != null)
				{
					var padding = this.Padding;
					var caretRect = this.textPresenter?.Let(it =>
						it.FormattedText.HitTestTextPosition(Math.Max(0, it.CaretIndex - 1))
					) ?? new Rect();
					menuToOpen.PlacementRect = new Rect(caretRect.Left + padding.Left, caretRect.Top + padding.Top, caretRect.Width, caretRect.Height);
					menuToOpen.Open(this);
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


		// Create menu item for assistance menu.
		MenuItem CreateMenuItem(char escapedChar) =>
			new MenuItem().Also(it =>
			{
				it.Command = this.InputStringCommand;
				it.CommandParameter = escapedChar.ToString();
				it.Header = new StackPanel().Also(panel => 
				{
					var opacityObservable = this.GetResourceObservable("Double/TextBox.Assistance.MenuItem.Description.Opacity");
					panel.Children.Add(new TextBlock().Also(it =>
					{
						it.Text = $"\\{escapedChar}";
						it.VerticalAlignment = VerticalAlignment.Center;
					}));
					panel.Children.Add(new TextBlock().Also(it =>
					{
						it.Bind(TextBlock.OpacityProperty, opacityObservable);
						it.Text = " (";
						it.VerticalAlignment = VerticalAlignment.Center;
					}));
					panel.Children.Add(new TextBlock().Also(it =>
					{
						it.Bind(TextBlock.OpacityProperty, opacityObservable);
						it.Bind(TextBlock.TextProperty, this.GetResourceObservable($"String/RegexTextBox.EscapedCharacter.{escapedChar}"));
						it.VerticalAlignment = VerticalAlignment.Center;
					}));
					panel.Children.Add(new TextBlock().Also(it =>
					{
						it.Bind(TextBlock.OpacityProperty, opacityObservable);
						it.Text = ")";
						it.VerticalAlignment = VerticalAlignment.Center;
					}));
					panel.Orientation = Orientation.Horizontal ;
				});
			});
		MenuItem CreateMenuItem(RegexGroup group) =>
			this.recycledMenuItems.Count > 0
				? this.recycledMenuItems.Dequeue().Also(it => it.DataContext = group)
				: new MenuItem().Also(it =>
				{
					it.Command = this.InputGroupNameCommand;
					it.Bind(MenuItem.CommandParameterProperty, new Binding() { Path = nameof(RegexGroup.Name) });
					it.DataContext = group;
					it.Header = new StackPanel().Also(panel => 
					{
						panel.Children.Add(new TextBlock().Also(it =>
						{
							it.Bind(TextBlock.TextProperty, new Binding() { Path = nameof(RegexGroup.Name) });
							it.VerticalAlignment = VerticalAlignment.Center;
						}));
						panel.Children.Add(new TextBlock().Also(it =>
						{
							it.Bind(TextBlock.IsVisibleProperty, new Binding() { Path = nameof(RegexGroup.DisplayName), Converter = StringConverters.IsNotNullOrEmpty });
							it.Bind(TextBlock.OpacityProperty, this.GetResourceObservable("Double/TextBox.Assistance.MenuItem.Description.Opacity"));
							it.Bind(TextBlock.TextProperty, new Binding() { Path = nameof(RegexGroup.DisplayName), StringFormat = " ({0})" });
							it.VerticalAlignment = VerticalAlignment.Center;
						}));
						panel.Orientation = Orientation.Horizontal ;
					});
				});
		

		// Get selection range of group name.
		(int, int) GetGroupNameSelection() =>
			this.GetGroupNameSelection(this.Text ?? "");
		(int, int) GetGroupNameSelection(string text)
		{
			var textLength = text.Length;
			var selectionStart = Math.Min(this.SelectionStart, this.SelectionEnd) - 1;
			if (selectionStart < 0)
				return (-1, -1);
			while (selectionStart >= 0 && text[selectionStart] != '<')
			{
				if (text[selectionStart] == '>')
					return (-1, -1);
				--selectionStart;
			}
			if (selectionStart < 0)
				return (-1, -1);
			for (var selectionEnd = selectionStart + 1; selectionEnd < textLength; ++selectionEnd)
			{
				if (text[selectionEnd] == '>')
					return (selectionStart + 1, selectionEnd);
			}
			return (selectionStart + 1, textLength);
		}


		// Get current selection range
		(int, int) GetSelection()
		{
			var start = this.SelectionStart;
			var end = this.SelectionEnd;
			if (start <= end)
				return (start, end);
			return (end, start);
		}


        /// <summary>
        /// Get or set whether case in <see cref="Regex"/> can be ignored or not.
        /// </summary>
        public bool IgnoreCase
		{
			get => this.GetValue<bool>(IgnoreCaseProperty);
			set => this.SetValue<bool>(IgnoreCaseProperty, value);
		}


		// Input given group name.
		void InputGroupName(string name)
		{
			var (start, end) = this.GetGroupNameSelection();
			if (start >= 0)
			{
				this.SelectionStart = start;
				this.SelectionEnd = end;
				this.SelectedText = name;
				if (this.SelectionEnd < (this.Text?.Length ?? 0))
				{
					++this.SelectionEnd;
					++this.SelectionStart;
				}
			}
		}


		// Command to input group name.
		ICommand InputGroupNameCommand { get; }


		// Input given string.
		void InputString(string s)
		{
			this.SelectedText = s;
		}


		// Command to input given string.
		ICommand InputStringCommand { get; }


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


		// Called when filtered groups changed.
		void OnFilteredPredefinedGroupChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					{
						var index = e.NewStartingIndex;
						foreach (var group in e.NewItems.AsNonNull().Cast<RegexGroup>())
						{
							var menuItem = this.CreateMenuItem(group);
							this.filteredPredefinedGroupMenuItems.Insert(index++, menuItem);
						}
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					{
						var count = e.OldItems.AsNonNull().Count;
						for (var index = e.OldStartingIndex + count - 1; count > 0; --count, --index)
						{
							var menuItem = this.filteredPredefinedGroupMenuItems[index];
							this.filteredPredefinedGroupMenuItems.RemoveAt(index);
							menuItem.DataContext = null;
							this.recycledMenuItems.Enqueue(menuItem);
						}
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					{
						foreach (var menuItem in this.filteredPredefinedGroupMenuItems)
						{
							menuItem.DataContext = null;
							this.recycledMenuItems.Enqueue(menuItem);
						}
						this.filteredPredefinedGroupMenuItems.Clear();
						foreach (var group in this.filteredPredefinedGroups)
						{
							var menuItem = this.CreateMenuItem(group);
							this.filteredPredefinedGroupMenuItems.Add(menuItem);
						}
					}
					break;
				default:
					throw new NotSupportedException();
			}
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
			// delete more characters
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

			// call base
			base.OnKeyDown(e);

			// show/hide menu
			if (e.Key == Key.Escape)
			{
				this.escapedCharactersMenu?.Close();
				this.predefinedGroupsMenu?.Close();
				this.showAssistanceMenuAction.Cancel();
			}
			else
				this.showAssistanceMenuAction.Reschedule(50);
		}


		/// <inheritdoc/>
		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (e.Key == Key.Escape && this.isEscapeKeyHandled)
			{
				this.isEscapeKeyHandled = false;
				e.Handled = true;
			}
		}


		// Called when predefined groups changed.
		void OnPredefinedGroupChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
			this.showAssistanceMenuAction.Schedule();


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
				if (regex != null && ((regex.Options & RegexOptions.IgnoreCase) != 0) != this.IgnoreCase)
				{
					var options = regex.Options;
					if (this.IgnoreCase)
						options |= RegexOptions.IgnoreCase;
					else
						options &= ~RegexOptions.IgnoreCase;
					regex = new Regex(regex.ToString(), options);
					this.Object = regex;
				}
			}
			else if (property == SelectionStartProperty 
				|| property == SelectionEndProperty)
			{
				this.showAssistanceMenuAction.Schedule();
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
				case '\\':
					this.isBackSlashPressed = true;
					break;
			}

			// commit input
			base.OnTextInput(e);
			this.SelectionStart = selectionStart;
			this.SelectionEnd = selectionStart;

			// show assistance menu
			this.showAssistanceMenuAction.Reschedule();
		}


		/// <summary>
		/// Predefined list of <see cref="RegexGroup"/> for input assistance.
		/// </summary>
		public IList<RegexGroup> PredefinedGroups { get => this.predefinedGroups; }


		// Setup menu for escaped characters.
		ContextMenu SetupEscapedCharactersMenu()
		{
			if (this.escapedCharactersMenu != null)
				return this.escapedCharactersMenu;
			this.escapedCharactersMenu = new ContextMenu().Also(menu =>
			{
				menu.Items = new object[] {
					this.CreateMenuItem('d'),
					this.CreateMenuItem('s'),
					this.CreateMenuItem('w'),
					this.CreateMenuItem('t'),
					new Separator(),
					this.CreateMenuItem('D'),
					this.CreateMenuItem('S'),
					this.CreateMenuItem('W'),
				};
				menu.AddHandler(KeyDownEvent, (_, e) =>
				{
					switch (e.Key)
					{
						case Key.Down:
						case Key.Enter:
						case Key.Up:
							break;
						case Key.Escape:
							this.isEscapeKeyHandled = true;
							goto default;
						default:
							menu.Close();
							this.OnKeyDown(e);
							break;
					}
				}, RoutingStrategies.Tunnel);
				menu.PlacementAnchor = PopupAnchor.BottomLeft;
				menu.PlacementConstraintAdjustment = PopupPositionerConstraintAdjustment.FlipY | PopupPositionerConstraintAdjustment.ResizeY | PopupPositionerConstraintAdjustment.SlideX;
				menu.PlacementGravity = PopupGravity.BottomRight;
				menu.PlacementMode = PlacementMode.AnchorAndGravity;
			});
			return this.escapedCharactersMenu;
		}


		// Setup menu of predefined groups.
		ContextMenu SetupPredefinedGroupsMenu()
		{
			if (this.predefinedGroupsMenu != null)
				return this.predefinedGroupsMenu;
			this.predefinedGroupsMenu = new ContextMenu().Also(menu =>
			{
				menu.Items = this.filteredPredefinedGroupMenuItems;
				menu.AddHandler(KeyDownEvent, (_, e) =>
				{
					switch (e.Key)
					{
						case Key.Down:
						case Key.Enter:
						case Key.Up:
							break;
						case Key.Escape:
							this.isEscapeKeyHandled = true;
							goto default;
						default:
							menu.Close();
							this.OnKeyDown(e);
							break;
					}
				}, RoutingStrategies.Tunnel);
				menu.PlacementAnchor = PopupAnchor.BottomLeft;
				menu.PlacementConstraintAdjustment = PopupPositionerConstraintAdjustment.FlipY | PopupPositionerConstraintAdjustment.ResizeY | PopupPositionerConstraintAdjustment.SlideX;
				menu.PlacementGravity = PopupGravity.BottomRight;
				menu.PlacementMode = PlacementMode.AnchorAndGravity;
			});
			return this.predefinedGroupsMenu;
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


	/// <summary>
	/// Predefined group of regular expression for <see cref="RegexTextBox"/>.
	/// </summary>
	public class RegexGroup : AvaloniaObject
	{
		/// <summary>
		/// Property of <see cref="DisplayName"/>.
		/// </summary>
		public static readonly AvaloniaProperty<string> DisplayNameProperty = AvaloniaProperty.Register<RegexGroup, string>(nameof(DisplayName), "");
		/// <summary>
		/// Property of <see cref="Name"/>.
		/// </summary>
		public static readonly AvaloniaProperty<string> NameProperty = AvaloniaProperty.Register<RegexGroup, string>(nameof(Name), "");


		/// <summary>
		/// Get or set display name of group.
		/// </summary>
		public string DisplayName
		{
			get => this.GetValue<string>(DisplayNameProperty);
			set => this.SetValue<string>(DisplayNameProperty, value);
		}


		/// <summary>
		/// Get or set name of group.
		/// </summary>
		public string Name
		{
			get => this.GetValue<string>(NameProperty);
			set => this.SetValue<string>(NameProperty, value);
		}
	}
}
