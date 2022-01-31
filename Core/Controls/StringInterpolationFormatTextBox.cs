using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Styling;
using CarinaStudio.Collections;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// <see cref="TextBox"/> which helps user to input format of string interpolation.
    /// </summary>
    public class StringInterpolationFormatTextBox : TextBox, IStyleable
    {
        // Fields.
        readonly ObservableList<MenuItem> filteredPredefinedVarMenuItems = new ObservableList<MenuItem>();
        readonly SortedObservableList<StringInterpolationVariable> filteredPredefinedVars = new SortedObservableList<StringInterpolationVariable>((x, y) => string.Compare(x?.Name, y?.Name));
        readonly ObservableList<StringInterpolationVariable> predefinedVars = new ObservableList<StringInterpolationVariable>();
        ContextMenu? predefinedVarsMenu;
        readonly Queue<MenuItem> recycledMenuItems = new Queue<MenuItem>();
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
				this.predefinedVarsMenu?.Close();
				var (start, end) = this.GetSelection();
				if (!this.IsEffectivelyVisible || start != end)
					return;

				// show predefined variable menu
				var text = this.Text ?? "";
				var textLength = text.Length;
				var menuToOpen = (ContextMenu?)null;
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
							menuToOpen = this.SetupPredefinedVarsMenu();
					}
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


        // Create menu item for given variable.
		MenuItem CreateMenuItem(StringInterpolationVariable variable) =>
			this.recycledMenuItems.Count > 0
				? this.recycledMenuItems.Dequeue().Also(it => it.DataContext = variable)
				: new MenuItem().Also(it =>
				{
					it.Command = this.InputVariableNameCommand;
					it.Bind(MenuItem.CommandParameterProperty, new Binding() { Path = nameof(StringInterpolationVariable.Name) });
					it.DataContext = variable;
					it.Header = new StackPanel().Also(panel => 
					{
						panel.Children.Add(new TextBlock().Also(it =>
						{
							it.Bind(TextBlock.TextProperty, new Binding() { Path = nameof(StringInterpolationVariable.Name) });
							it.VerticalAlignment = VerticalAlignment.Center;
						}));
						panel.Children.Add(new TextBlock().Also(it =>
						{
							it.Bind(TextBlock.IsVisibleProperty, new Binding() { Path = nameof(StringInterpolationVariable.DisplayName), Converter = Converters.ValueToBooleanConverters.NonEmptyStringToTrue });
                            it.Bind(TextBlock.OpacityProperty, this.GetResourceObservable("Double/TextBox.Assistance.MenuItem.Description.Opacity"));
							it.Bind(TextBlock.TextProperty, new Binding() { Path = nameof(StringInterpolationVariable.DisplayName), StringFormat = " ({0})" });
							it.VerticalAlignment = VerticalAlignment.Center;
						}));
						panel.Orientation = Orientation.Horizontal ;
					});
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
							var menuItem = this.CreateMenuItem(variable);
							this.filteredPredefinedVarMenuItems.Insert(index++, menuItem);
						}
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					{
						var count = e.OldItems.AsNonNull().Count;
						for (var index = e.OldStartingIndex + count - 1; count > 0; --count, --index)
						{
							var menuItem = this.filteredPredefinedVarMenuItems[index];
							this.filteredPredefinedVarMenuItems.RemoveAt(index);
							menuItem.DataContext = null;
							this.recycledMenuItems.Enqueue(menuItem);
						}
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					{
						foreach (var menuItem in this.filteredPredefinedVarMenuItems)
						{
							menuItem.DataContext = null;
							this.recycledMenuItems.Enqueue(menuItem);
						}
						this.filteredPredefinedVarMenuItems.Clear();
						foreach (var variable in this.filteredPredefinedVars)
						{
							var menuItem = this.CreateMenuItem(variable);
							this.filteredPredefinedVarMenuItems.Add(menuItem);
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

			// call base
			base.OnKeyDown(e);

			// show/hide menu
			if (e.Key == Key.Escape)
			{
				this.predefinedVarsMenu?.Close();
				this.showAssistanceMenuAction.Cancel();
			}
			else
				this.showAssistanceMenuAction.Reschedule(50);
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
			if (string.IsNullOrEmpty(s))
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


        // Setup menu of predefined variables.
		ContextMenu SetupPredefinedVarsMenu()
		{
			if (this.predefinedVarsMenu != null)
				return this.predefinedVarsMenu;
			this.predefinedVarsMenu = new ContextMenu().Also(menu =>
			{
				menu.Items = this.filteredPredefinedVarMenuItems;
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
				menu.PlacementAnchor = PopupAnchor.BottomLeft;
				menu.PlacementConstraintAdjustment = PopupPositionerConstraintAdjustment.FlipY | PopupPositionerConstraintAdjustment.ResizeY | PopupPositionerConstraintAdjustment.SlideX;
				menu.PlacementGravity = PopupGravity.BottomRight;
				menu.PlacementMode = PlacementMode.AnchorAndGravity;
			});
			return this.predefinedVarsMenu;
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