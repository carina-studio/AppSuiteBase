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
using Avalonia.VisualTree;
using CarinaStudio.Collections;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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
		public static readonly StyledProperty<bool> IgnoreCaseProperty = AvaloniaProperty.Register<RegexTextBox, bool>(nameof(IgnoreCase), true);
		/// <summary>
		/// Property of <see cref="IsInputAssistanceEnabled"/>.
		/// </summary>
		public static readonly StyledProperty<bool> IsInputAssistanceEnabledProperty = AvaloniaProperty.Register<RegexTextBox, bool>(nameof(IsInputAssistanceEnabled), true);


		// Fields.
		InputAssistancePopup? escapedCharactersPopup;
		readonly ObservableList<ListBoxItem> filteredPredefinedGroupListBoxItems = new();
		readonly SortedObservableList<RegexGroup> filteredPredefinedGroups = new((x, y) => string.Compare(x?.Name, y?.Name, true, CultureInfo.InvariantCulture));
		bool isBackSlashPressed;
		bool isEscapeKeyHandled;
		readonly ObservableList<RegexGroup> predefinedGroups = new();
		InputAssistancePopup? predefinedGroupsPopup;
		readonly Queue<ListBoxItem> recycledListBoxItems = new();
		readonly ScheduledAction showAssistanceMenuAction;
		TextPresenter? textPresenter;


		/// <summary>
		/// Initialize new <see cref="RegexTextBox"/> instance.
		/// </summary>
		public RegexTextBox()
		{
			this.PseudoClasses.Add(":regexTextBox");
			this.filteredPredefinedGroups.CollectionChanged += this.OnFilteredPredefinedGroupChanged;
			this.predefinedGroups.CollectionChanged += this.OnPredefinedGroupChanged;
			this.InputGroupNameCommand = new Command<string>(this.InputGroupName);
			this.InputStringCommand = new Command<string>(this.InputString);
			this.MaxLength = 1024;
			this.Bind(WatermarkProperty, this.GetResourceObservable("String/RegexTextBox.Watermark"));
			this.showAssistanceMenuAction = new ScheduledAction(() =>
			{
				// close menu first
				if (this.escapedCharactersPopup?.IsOpen == true
					|| this.predefinedGroupsPopup?.IsOpen == true)
				{
					this.escapedCharactersPopup?.Close();
					this.predefinedGroupsPopup?.Close();
					this.showAssistanceMenuAction!.Schedule();
					return;
				}
				var (start, end) = this.GetSelection();
				if (!this.IsInputAssistanceEnabled || !this.IsEffectivelyVisible || start != end)
				{
					this.isBackSlashPressed = false;
					return;
				}

				// show predefined groups menu
				var text = this.Text ?? "";
				var textLength = text.Length;
				var popupToOpen = (Popup?)null;
				if (this.predefinedGroups.IsNotEmpty())
				{
					var (groupStart, groupEnd) = this.GetGroupNameSelection(text);
					if (groupStart >= 0)
					{
						var filterText = this.Text?[groupStart..groupEnd]?.ToLower() ?? "";
						this.filteredPredefinedGroups.Clear();
						if (string.IsNullOrEmpty(filterText))
							this.filteredPredefinedGroups.AddAll(this.predefinedGroups);
						else
							this.filteredPredefinedGroups.AddAll(this.predefinedGroups.Where(it => it.Name.ToLower().Contains(filterText)));
						if (this.filteredPredefinedGroups.IsNotEmpty())
							popupToOpen = this.SetupPredefinedGroupsPopup();
					}
				}

				// show escaped characters menu
				if (this.isBackSlashPressed)
				{
					this.isBackSlashPressed = false;
					if (popupToOpen == null && start > 0 && text[start - 1] == '\\' && (start <= 1 || text[start - 2] != '\\'))
						popupToOpen = this.SetupEscapedCharactersPopup();
				}

				// open menu
				if (popupToOpen != null)
				{
					var padding = this.Padding;
					var caretRect = this.textPresenter?.Let(it =>
						it.TextLayout.HitTestTextPosition(Math.Max(0, it.CaretIndex - 1))
					) ?? new Rect();
					popupToOpen.PlacementRect = new Rect(caretRect.Left + padding.Left, caretRect.Top + padding.Top, caretRect.Width, caretRect.Height);
					popupToOpen.PlacementTarget = this;
					popupToOpen.Open();
				}
			});

			// observe self properties
			var isSubscribed = false;
			this.GetObservable(IgnoreCaseProperty).Subscribe(_ =>
			{
				if (isSubscribed)
					this.Validate();
			});
			this.GetObservable(ObjectProperty).Subscribe(o =>
			{
				if (o is Regex regex && ((regex.Options & RegexOptions.IgnoreCase) != 0) != this.IgnoreCase)
				{
					var options = regex.Options;
					if (this.IgnoreCase)
						options |= RegexOptions.IgnoreCase;
					else
						options &= ~RegexOptions.IgnoreCase;
					regex = new Regex(regex.ToString(), options);
						
				}
			});
			this.GetObservable(SelectionEndProperty).Subscribe(_ =>
			{
				if (isSubscribed)
					this.showAssistanceMenuAction.Schedule();
			});
			this.GetObservable(SelectionStartProperty).Subscribe(_ =>
			{
				if (isSubscribed)
					this.showAssistanceMenuAction.Schedule();
			});
			this.GetObservable(TextWrappingProperty).Subscribe(wrapping =>
			{
				if (wrapping != Avalonia.Media.TextWrapping.NoWrap)
					throw new NotSupportedException("Text wrapping is unsupported.");
			});
			isSubscribed = true;
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


		// Create listbox item for assistance menu.
		ListBoxItem CreateListBoxItem(char escapedChar) =>
			new ListBoxItem().Also(it =>
			{
				it.Content = new StackPanel().Also(panel => 
				{
					var opacityObservable = this.GetResourceObservable("Double/TextBox.Assistance.MenuItem.Description.Opacity");
					panel.Children.Add(new Avalonia.Controls.TextBlock().Also(it =>
					{
						it.Text = $"\\{escapedChar}";
						it.VerticalAlignment = VerticalAlignment.Center;
					}));
					panel.Children.Add(new Avalonia.Controls.TextBlock().Also(it =>
					{
						it.Bind(Avalonia.Controls.TextBlock.OpacityProperty, opacityObservable);
						it.Text = " (";
						it.VerticalAlignment = VerticalAlignment.Center;
					}));
					panel.Children.Add(new Avalonia.Controls.TextBlock().Also(it =>
					{
						it.Bind(Avalonia.Controls.TextBlock.OpacityProperty, opacityObservable);
						it.Bind(Avalonia.Controls.TextBlock.TextProperty, this.GetResourceObservable($"String/RegexTextBox.EscapedCharacter.{escapedChar}"));
						it.VerticalAlignment = VerticalAlignment.Center;
					}));
					panel.Children.Add(new Avalonia.Controls.TextBlock().Also(it =>
					{
						it.Bind(Avalonia.Controls.TextBlock.OpacityProperty, opacityObservable);
						it.Text = ")";
						it.VerticalAlignment = VerticalAlignment.Center;
					}));
					panel.Orientation = Orientation.Horizontal ;
				});
				it.DataContext = escapedChar;
			});
		ListBoxItem CreateListBoxItem(RegexGroup group) =>
			this.recycledListBoxItems.Count > 0
				? this.recycledListBoxItems.Dequeue().Also(it => it.DataContext = group)
				: new ListBoxItem().Also(it =>
				{
					it.Content = new StackPanel().Also(panel => 
					{
						panel.Children.Add(new Avalonia.Controls.TextBlock().Also(it =>
						{
							it.Bind(Avalonia.Controls.TextBlock.TextProperty, new Binding() { Path = nameof(RegexGroup.Name) });
							it.VerticalAlignment = VerticalAlignment.Center;
						}));
						panel.Children.Add(new Avalonia.Controls.TextBlock().Also(it =>
						{
							it.Bind(Avalonia.Controls.TextBlock.IsVisibleProperty, new Binding() { Path = nameof(RegexGroup.DisplayName), Converter = StringConverters.IsNotNullOrEmpty });
							it.Bind(Avalonia.Controls.TextBlock.OpacityProperty, this.GetResourceObservable("Double/TextBox.Assistance.MenuItem.Description.Opacity"));
							it.Bind(Avalonia.Controls.TextBlock.TextProperty, new Binding() { Path = nameof(RegexGroup.DisplayName), StringFormat = " ({0})" });
							it.VerticalAlignment = VerticalAlignment.Center;
						}));
						panel.Orientation = Orientation.Horizontal ;
					});
					it.DataContext = group;
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


		/// <summary>
		/// Command to input group name.
		/// </summary>
		public ICommand InputGroupNameCommand { get; }


		// Input given string.
		void InputString(string s)
		{
			this.SelectedText = s;
		}


		/// <summary>
		/// Command to input given string.
		/// </summary>
		public ICommand InputStringCommand { get; }


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
			e.NameScope.Find<SyntaxHighlightingTextBlock>("PART_SyntaxHighlightingTextBlock")?.Let(it =>
			{
				AppSuiteApplication.CurrentOrNull?.Let(app =>
					it.DefinitionSet = Highlighting.RegexSyntaxHighlighting.CreateDefinitionSet(app));
			});
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
							var listBoxItem = this.CreateListBoxItem(group);
							this.filteredPredefinedGroupListBoxItems.Insert(index++, listBoxItem);
						}
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					{
						var count = e.OldItems.AsNonNull().Count;
						for (var index = e.OldStartingIndex + count - 1; count > 0; --count, --index)
						{
							var listBoxItem = this.filteredPredefinedGroupListBoxItems[index];
							this.filteredPredefinedGroupListBoxItems.RemoveAt(index);
							listBoxItem.DataContext = null;
							this.recycledListBoxItems.Enqueue(listBoxItem);
						}
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					{
						foreach (var listBoxItem in this.filteredPredefinedGroupListBoxItems)
						{
							listBoxItem.DataContext = null;
							this.recycledListBoxItems.Enqueue(listBoxItem);
						}
						this.filteredPredefinedGroupListBoxItems.Clear();
						foreach (var group in this.filteredPredefinedGroups)
						{
							var listBoxItem = this.CreateListBoxItem(group);
							this.filteredPredefinedGroupListBoxItems.Add(listBoxItem);
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
			SynchronizationContext.Current?.PostDelayed(() =>
			{
				if (!this.IsFocused)
				{
					this.escapedCharactersPopup?.Close();
					this.predefinedGroupsPopup?.Close();
					this.showAssistanceMenuAction.Cancel();
				}
			}, 200);
			base.OnLostFocus(e);
		}


		/// <inheritdoc/>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			// delete more characters
			var isBackspace = e.Key == Key.Back;
			var isDelete = e.Key == Key.Delete;
			var isKeyForAssistentPopup = false;
			if (isBackspace || isDelete)
			{
				var selectionStart = this.SelectionStart;
				var selectionEnd = this.SelectionEnd;
				if (selectionStart > selectionEnd)
					(selectionStart, selectionEnd) = (selectionEnd, selectionStart);
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
			else
			{
				switch (e.Key)
				{
					case Key.Down:
					case Key.FnDownArrow:
						if (this.escapedCharactersPopup?.IsOpen == true)
						{
							this.escapedCharactersPopup.ItemListBox?.SelectNextItem();
							isKeyForAssistentPopup = true;
							e.Handled = true;
						}
						else if (this.predefinedGroupsPopup?.IsOpen == true)
						{
							this.predefinedGroupsPopup.ItemListBox?.SelectNextItem();
							isKeyForAssistentPopup = true;
							e.Handled = true;
						}
						break;
					case Key.Enter:
						if (this.escapedCharactersPopup?.IsOpen == true
							|| this.predefinedGroupsPopup?.IsOpen == true)
						{
							isKeyForAssistentPopup = true;
							e.Handled = true;
						}
						break;
					case Key.FnUpArrow:
					case Key.Up:
						if (this.escapedCharactersPopup?.IsOpen == true)
						{
							this.escapedCharactersPopup.ItemListBox?.SelectPreviousItem();
							isKeyForAssistentPopup = true;
							e.Handled = true;
						}
						else if (this.predefinedGroupsPopup?.IsOpen == true)
						{
							this.predefinedGroupsPopup.ItemListBox?.SelectPreviousItem();
							isKeyForAssistentPopup = true;
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
				this.escapedCharactersPopup?.Close();
				this.predefinedGroupsPopup?.Close();
				this.showAssistanceMenuAction.Cancel();
			}
			else if (!isKeyForAssistentPopup)
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
			else if (e.Key == Key.Enter)
			{
				if (this.escapedCharactersPopup?.IsOpen == true)
				{
					(this.escapedCharactersPopup.ItemListBox?.SelectedItem as ListBoxItem)?.Let(item =>
					{
						if (item.DataContext is char c)
							this.InputString(c.ToString());
					});
					this.escapedCharactersPopup?.Close();
				}
				else if (this.predefinedGroupsPopup?.IsOpen == true)
				{
					(this.predefinedGroupsPopup.ItemListBox?.SelectedItem as ListBoxItem)?.Let(item =>
					{
						if (item.DataContext is RegexGroup group)
							this.InputGroupName(group.Name);
					});
					this.predefinedGroupsPopup?.Close();
				}
			}
		}


		// Called when predefined groups changed.
		void OnPredefinedGroupChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
			this.showAssistanceMenuAction.Schedule();


		/// <inheritdoc/>
        protected override void OnTextInput(TextInputEventArgs e)
		{
			// no need to handle
			var s = e.Text;
			if (this.IsReadOnly || !this.IsInputAssistanceEnabled || string.IsNullOrEmpty(s))
			{
				base.OnTextInput(e);
				return;
			}

			// assist input
			var selectionStart = this.SelectionStart;
			var selectionEnd = this.SelectionEnd;
			if (selectionStart > selectionEnd)
				(selectionStart, selectionEnd) = (selectionEnd, selectionStart);
			var text = this.Text ?? "";
			var textLength = text.Length;
			var prevChar1 = selectionStart > 0 ? text[selectionStart - 1] : '\0';
			var prevChar2 = selectionStart > 1 ? text[selectionStart - 2] : '\0';
			var nextChar1 = selectionEnd < textLength ? text[selectionEnd] : '\0';
			var nextChar2 = selectionEnd < textLength - 1 ? text[selectionEnd + 1] : '\0';
			++selectionStart;
			switch (s[0])
			{
				case '(':
					if (prevChar1 != '\\' && nextChar1 == '\0')
						e.Text = "()";
					break;
				case ')':
					if (prevChar1 != '\\' && nextChar1 == ')' && nextChar2 == '\0')
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
		InputAssistancePopup SetupEscapedCharactersPopup()
		{
			if (this.escapedCharactersPopup != null)
				return this.escapedCharactersPopup;
			var rootPanel = this.FindDescendantOfType<Panel>().AsNonNull();
			this.escapedCharactersPopup = new InputAssistancePopup().Also(menu =>
			{
				menu.ItemListBox.Let(it =>
				{
					it.DoubleClickOnItem += (_, e) =>
					{
						menu.Close();
						if (e.Item is ListBoxItem item && item.DataContext is char c)
							this.InputString(c.ToString());
					};
					it.Items = new ListBoxItem[] {
						this.CreateListBoxItem('d'),
						this.CreateListBoxItem('s'),
						this.CreateListBoxItem('w'),
						this.CreateListBoxItem('t'),
						this.CreateListBoxItem('D'),
						this.CreateListBoxItem('S'),
						this.CreateListBoxItem('W'),
					};
					it.AddHandler(Control.PointerPressedEvent, new EventHandler<PointerPressedEventArgs>((_, e) =>
					{
						SynchronizationContext.Current?.Post(this.Focus);
					}), RoutingStrategies.Tunnel);
				});
				menu.PlacementAnchor = PopupAnchor.BottomLeft;
				menu.PlacementConstraintAdjustment = PopupPositionerConstraintAdjustment.FlipY | PopupPositionerConstraintAdjustment.ResizeY | PopupPositionerConstraintAdjustment.SlideX;
				menu.PlacementGravity = PopupGravity.BottomRight;
				menu.PlacementMode = PlacementMode.AnchorAndGravity;
			});
			rootPanel.Children.Insert(0, this.escapedCharactersPopup);
			return this.escapedCharactersPopup;
		}


		// Setup menu of predefined groups.
		InputAssistancePopup SetupPredefinedGroupsPopup()
		{
			if (this.predefinedGroupsPopup != null)
				return this.predefinedGroupsPopup;
			var rootPanel = this.FindDescendantOfType<Panel>().AsNonNull();
			this.predefinedGroupsPopup = new InputAssistancePopup().Also(menu =>
			{
				menu.ItemListBox.Let(it =>
				{
					it.DoubleClickOnItem += (_, e) =>
					{
						menu.Close();
						if (e.Item is ListBoxItem item && item.DataContext is RegexGroup group)
							this.InputGroupName(group.Name);
					};
					it.Items = this.filteredPredefinedGroupListBoxItems;
					it.AddHandler(Control.PointerPressedEvent, new EventHandler<PointerPressedEventArgs>((_, e) =>
					{
						SynchronizationContext.Current?.Post(this.Focus);
					}), RoutingStrategies.Tunnel);
				});
				menu.PlacementAnchor = PopupAnchor.BottomLeft;
				menu.PlacementConstraintAdjustment = PopupPositionerConstraintAdjustment.FlipY | PopupPositionerConstraintAdjustment.ResizeY | PopupPositionerConstraintAdjustment.SlideX;
				menu.PlacementGravity = PopupGravity.BottomRight;
				menu.PlacementMode = PlacementMode.AnchorAndGravity;
			});
			rootPanel.Children.Insert(0, this.predefinedGroupsPopup);
			return this.predefinedGroupsPopup;
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
		public static readonly StyledProperty<string> DisplayNameProperty = AvaloniaProperty.Register<RegexGroup, string>(nameof(DisplayName), "");
		/// <summary>
		/// Property of <see cref="Name"/>.
		/// </summary>
		public static readonly StyledProperty<string> NameProperty = AvaloniaProperty.Register<RegexGroup, string>(nameof(Name), "");


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
