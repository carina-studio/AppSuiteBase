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
using Avalonia.Styling;
using Avalonia.VisualTree;
using CarinaStudio.Collections;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// <see cref="TextBox"/> which helps user to input format of string interpolation.
    /// </summary>
    public class StringInterpolationFormatTextBox : TextBox, IStyleable
    {
        // Fields.
        readonly ObservableList<ListBoxItem> filteredPredefinedVarListBoxItems = new ObservableList<ListBoxItem>();
        readonly SortedObservableList<StringInterpolationVariable> filteredPredefinedVars = new SortedObservableList<StringInterpolationVariable>((x, y) => string.Compare(x?.Name, y?.Name));
		bool isEscapeKeyHandled;
        readonly ObservableList<StringInterpolationVariable> predefinedVars = new ObservableList<StringInterpolationVariable>();
        InputAssistancePopup? predefinedVarsPopup;
        readonly Queue<ListBoxItem> recycledListBoxItems = new Queue<ListBoxItem>();
        readonly ScheduledAction showAssistanceMenuAction;
        TextPresenter? textPresenter;


        /// <summary>
        /// Initialize new <see cref="StringInterpolationFormatTextBox"/> instance.
        /// </summary>
        public StringInterpolationFormatTextBox()
        {
            this.filteredPredefinedVars.CollectionChanged += this.OnFilteredPredefinedVarsChanged;
            this.InputVariableNameCommand = new Command<string>(this.InputVariableName);
            this.MaxLength = 1024;
            this.predefinedVars.CollectionChanged += this.OnPredefinedVarsChanged;
            this.showAssistanceMenuAction = new ScheduledAction(() =>
			{
				// close menu first
				if (this.predefinedVarsPopup?.IsOpen == true)
				{
					this.predefinedVarsPopup?.Close();
					this.showAssistanceMenuAction!.Schedule();
					return;
				}
				this.predefinedVarsPopup?.Close();
				var (start, end) = this.GetSelection();
				if (!this.IsEffectivelyVisible || start != end)
					return;

				// show predefined variable menu
				var text = this.Text ?? "";
				var textLength = text.Length;
				var popupToOpen = (Popup?)null;
				if (this.predefinedVars.IsNotEmpty())
				{
					var (varStart, varEnd) = this.GetVariableNameSelection(text);
					if (varStart >= 0)
					{
						var filterText = this.Text?.Substring(varStart, varEnd - varStart)?.ToLower() ?? "";
						this.filteredPredefinedVars.Clear();
						if (string.IsNullOrEmpty(filterText))
							this.filteredPredefinedVars.AddAll(this.predefinedVars);
						else
							this.filteredPredefinedVars.AddAll(this.predefinedVars.Where(it => it.Name.ToLower().Contains(filterText)));
						if (this.filteredPredefinedVars.IsNotEmpty())
							popupToOpen = this.SetupPredefinedVarsPopup();
					}
				}

				// open menu
				if (popupToOpen != null)
				{
					var padding = this.Padding;
					var caretRect = this.textPresenter?.Let(it =>
						it.FormattedText.HitTestTextPosition(Math.Max(0, it.CaretIndex - 1))
					) ?? new Rect();
					popupToOpen.PlacementRect = new Rect(caretRect.Left + padding.Left, caretRect.Top + padding.Top, caretRect.Width, caretRect.Height);
					popupToOpen.PlacementTarget = this;
					popupToOpen.Open();
				}
			});
        }


        // Create listbox item for given variable.
		ListBoxItem CreateListBoxItem(StringInterpolationVariable variable) =>
			this.recycledListBoxItems.Count > 0
				? this.recycledListBoxItems.Dequeue().Also(it => it.DataContext = variable)
				: new ListBoxItem().Also(it =>
				{
					it.Content = new StackPanel().Also(panel => 
					{
						panel.Children.Add(new TextBlock().Also(it =>
						{
							it.Bind(TextBlock.TextProperty, new Binding() { Path = nameof(StringInterpolationVariable.Name) });
							it.VerticalAlignment = VerticalAlignment.Center;
						}));
						panel.Children.Add(new TextBlock().Also(it =>
						{
							it.Bind(TextBlock.IsVisibleProperty, new Binding() { Path = nameof(StringInterpolationVariable.DisplayName), Converter = StringConverters.IsNotNullOrEmpty });
                            it.Bind(TextBlock.OpacityProperty, this.GetResourceObservable("Double/TextBox.Assistance.MenuItem.Description.Opacity"));
							it.Bind(TextBlock.TextProperty, new Binding() { Path = nameof(StringInterpolationVariable.DisplayName), StringFormat = " ({0})" });
							it.VerticalAlignment = VerticalAlignment.Center;
						}));
						panel.Orientation = Orientation.Horizontal ;
					});
					it.DataContext = variable;
				});


        // Get current selection range
		(int, int) GetSelection()
		{
			var start = this.SelectionStart;
			var end = this.SelectionEnd;
			if (start <= end)
				return (start, end);
			return (end, start);
		}


        // Get selection range of variable name.
        (int, int) GetVariableNameSelection() =>
            this.GetVariableNameSelection(this.Text ?? "");
		(int, int) GetVariableNameSelection(string text)
		{
			var textLength = text.Length;
			var selectionStart = Math.Min(this.SelectionStart, this.SelectionEnd) - 1;
			if (selectionStart < 0)
				return (-1, -1);
			while (selectionStart >= 0 && text[selectionStart] != '{')
			{
                var c = text[selectionStart];
				if (c == '}' || c == ':' || c == ',')
					return (-1, -1);
				--selectionStart;
			}
			if (selectionStart < 0)
				return (-1, -1);
			for (var selectionEnd = selectionStart + 1; selectionEnd < textLength; ++selectionEnd)
			{
                var c = text[selectionEnd];
				if (c == '}' || c == ':' || c == ',')
					return (selectionStart + 1, selectionEnd);
			}
			return (selectionStart + 1, textLength);
		}


        // Input given variable name.
		void InputVariableName(string name)
		{
			var (start, end) = this.GetVariableNameSelection();
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


		// Command to input variable name.
		ICommand InputVariableNameCommand { get; }


        /// <inheritdoc/>
		protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
		{
			base.OnApplyTemplate(e);
			this.textPresenter = e.NameScope.Find<TextPresenter>("PART_TextPresenter");
		}


		// Called when filtered variables changed.
		void OnFilteredPredefinedVarsChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					{
						var index = e.NewStartingIndex;
						foreach (var variable in e.NewItems.AsNonNull().Cast<StringInterpolationVariable>())
						{
							var menuItem = this.CreateListBoxItem(variable);
							this.filteredPredefinedVarListBoxItems.Insert(index++, menuItem);
						}
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					{
						var count = e.OldItems.AsNonNull().Count;
						for (var index = e.OldStartingIndex + count - 1; count > 0; --count, --index)
						{
							var menuItem = this.filteredPredefinedVarListBoxItems[index];
							this.filteredPredefinedVarListBoxItems.RemoveAt(index);
							menuItem.DataContext = null;
							this.recycledListBoxItems.Enqueue(menuItem);
						}
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					{
						foreach (var menuItem in this.filteredPredefinedVarListBoxItems)
						{
							menuItem.DataContext = null;
							this.recycledListBoxItems.Enqueue(menuItem);
						}
						this.filteredPredefinedVarListBoxItems.Clear();
						foreach (var variable in this.filteredPredefinedVars)
						{
							var menuItem = this.CreateListBoxItem(variable);
							this.filteredPredefinedVarListBoxItems.Add(menuItem);
						}
					}
					break;
				default:
					throw new NotSupportedException();
			}
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
				var (selectionStart, selectionEnd) = this.GetSelection();
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
				if (deletingChar == '{' && nextChar1 == '}' && (prevChar1 != '\\' || prevChar2 == '\\'))
				{
					this.SelectionStart = selectionStart;
                    this.SelectionEnd = selectionEnd + 1;
                    this.SelectedText = "";
                    e.Handled = true;
				}
			}
			else
			{
				switch (e.Key)
				{
					case Key.Down:
					case Key.FnDownArrow:
						if (this.predefinedVarsPopup?.IsOpen == true)
						{
							this.predefinedVarsPopup.ItemListBox?.SelectNextItem();
							isKeyForAssistentPopup = true;
							e.Handled = true;
						}
						break;
					case Key.Enter:
						if (this.predefinedVarsPopup?.IsOpen == true)
						{
							isKeyForAssistentPopup = true;
							e.Handled = true;
						}
						break;
					case Key.FnUpArrow:
					case Key.Up:
						if (this.predefinedVarsPopup?.IsOpen == true)
						{
							this.predefinedVarsPopup.ItemListBox?.SelectPreviousItem();
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
				this.predefinedVarsPopup?.Close();
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
				if (this.predefinedVarsPopup?.IsOpen == true)
				{
					(this.predefinedVarsPopup.ItemListBox?.SelectedItem as ListBoxItem)?.Let(item =>
					{
						if (item.DataContext is StringInterpolationVariable variable)
							this.InputVariableName(variable.Name);
					});
					this.predefinedVarsPopup?.Close();
				}
			}
		}


		/// <inheritdoc/>
		protected override void OnLostFocus(RoutedEventArgs e)
		{
			SynchronizationContext.Current?.PostDelayed(() =>
			{
				if (!this.IsFocused)
				{
					this.predefinedVarsPopup?.Close();
					this.showAssistanceMenuAction.Cancel();
				}
			}, 200);
			base.OnLostFocus(e);
		}


        // Called when predefined variables changed.
		void OnPredefinedVarsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
			this.showAssistanceMenuAction.Schedule();


        /// <inheritdoc/>
		protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
		{
			base.OnPropertyChanged(change);
			var property = change.Property;
			if (property == SelectionStartProperty 
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
			if (this.IsReadOnly || string.IsNullOrEmpty(s))
			{
				base.OnTextInput(e);
				return;
			}

			// assist input
			var (selectionStart, selectionEnd) = this.GetSelection();
			var text = this.Text ?? "";
			var textLength = text.Length;
			var prevChar1 = selectionStart > 0 ? text[selectionStart - 1] : '\0';
			var nextChar1 = selectionEnd < textLength ? text[selectionEnd] : '\0';
			++selectionStart;
			switch (s[0])
			{
				case '{':
					if (prevChar1 != '\\')
						e.Text = "{}";
					break;
				case '}':
					if (prevChar1 != '\\' && nextChar1 == '}')
						e.Text = "";
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
		/// Predefined list of <see cref="StringInterpolationVariable"/> for input assistance.
		/// </summary>
		public IList<StringInterpolationVariable> PredefinedVariables { get => this.predefinedVars; }


        // Setup popup of predefined variables.
		InputAssistancePopup SetupPredefinedVarsPopup()
		{
			if (this.predefinedVarsPopup != null)
				return this.predefinedVarsPopup;
			var rootPanel = this.FindDescendantOfType<Panel>().AsNonNull();
			this.predefinedVarsPopup = new InputAssistancePopup().Also(menu =>
			{
				menu.ItemListBox.Let(it =>
				{
					it.DoubleClickOnItem += (_, e) =>
					{
						menu.Close();
						if (e.Item is ListBoxItem item && item.DataContext is StringInterpolationVariable variable)
							this.InputVariableName(variable.Name);
					};
					it.Items = this.filteredPredefinedVarListBoxItems;
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
			rootPanel.Children.Insert(0, this.predefinedVarsPopup);
			return this.predefinedVarsPopup;
		}


        // Interface implementations.
		Type IStyleable.StyleKey => typeof(TextBox);
    }


    /// <summary>
	/// Predefined veriable of string interpolation for <see cref="StringInterpolationFormatTextBox"/>.
	/// </summary>
	public class StringInterpolationVariable : AvaloniaObject
	{
		/// <summary>
		/// Property of <see cref="DisplayName"/>.
		/// </summary>
		public static readonly AvaloniaProperty<string> DisplayNameProperty = AvaloniaProperty.Register<StringInterpolationVariable, string>(nameof(DisplayName), "");
		/// <summary>
		/// Property of <see cref="Name"/>.
		/// </summary>
		public static readonly AvaloniaProperty<string> NameProperty = AvaloniaProperty.Register<StringInterpolationVariable, string>(nameof(Name), "");


		/// <summary>
		/// Get or set display name of variable.
		/// </summary>
		public string DisplayName
		{
			get => this.GetValue<string>(DisplayNameProperty);
			set => this.SetValue<string>(DisplayNameProperty, value);
		}


		/// <summary>
		/// Get or set name of variable.
		/// </summary>
		public string Name
		{
			get => this.GetValue<string>(NameProperty);
			set => this.SetValue<string>(NameProperty, value);
		}
	}
}