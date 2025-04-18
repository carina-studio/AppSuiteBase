using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.VisualTree;
using CarinaStudio.AppSuite.Controls.Highlighting;
using CarinaStudio.Collections;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="TextBox"/> which helps user to input format of string interpolation.
/// </summary>
public class StringInterpolationFormatTextBox : SyntaxHighlightingObjectTextBox<string>
{
	/// <summary>
	/// Property of <see cref="HasOpenedAssistanceMenus"/>.
	/// </summary>
	public static readonly DirectProperty<StringInterpolationFormatTextBox, bool> HasOpenedAssistanceMenusProperty = AvaloniaProperty.RegisterDirect<StringInterpolationFormatTextBox, bool>(nameof(HasOpenedAssistanceMenus), tb => tb.hasOpenedAssistanceMenus);
	/// <summary>
	/// Property of <see cref="Object"/>.
	/// </summary>
	public static new readonly DirectProperty<StringInterpolationFormatTextBox, string?> ObjectProperty = AvaloniaProperty.RegisterDirect<StringInterpolationFormatTextBox, string?>(nameof(Object), t => t.Object, (t, o) => t.Object = o);
	
	
	// Static fields.
	static MethodInfo? HandleTextInputMethod;


    // Fields.
    readonly ObservableList<ListBoxItem> filteredPredefinedVarListBoxItems = new();
    readonly SortedObservableList<StringInterpolationVariable> filteredPredefinedVars = new((x, y) => string.Compare(x.Name, y.Name, true, CultureInfo.InvariantCulture));
    bool hasOpenedAssistanceMenus;
	bool isEscapeKeyHandled;
    readonly ObservableList<StringInterpolationVariable> predefinedVars = new();
    InputAssistancePopup? predefinedVarsPopup;
    readonly Queue<ListBoxItem> recycledListBoxItems = new();
    readonly ScheduledAction showAssistanceMenuAction;
    TextPresenter? textPresenter;


    /// <summary>
    /// Initialize new <see cref="StringInterpolationFormatTextBox"/> instance.
    /// </summary>
    public StringInterpolationFormatTextBox()
    {
		SyntaxHighlighting.VerifyInitialization();
		this.PseudoClasses.Add(":stringInterpolationFormatTextBox");
        this.filteredPredefinedVars.CollectionChanged += this.OnFilteredPredefinedVarsChanged;
        this.InputVariableNameCommand = new Command<string>(this.InputVariableName);
        this.MaxLength = 1024;
        this.predefinedVars.CollectionChanged += this.OnPredefinedVarsChanged;
        this.showAssistanceMenuAction = new(this.ShowAssistanceMenu);

		// attach to self
		this.AddHandler(KeyDownEvent, this.OnPreviewKeyDown, RoutingStrategies.Tunnel);
		this.AddHandler(KeyUpEvent, this.OnPreviewKeyUp, RoutingStrategies.Tunnel);
    }
    
    
    /// <summary>
    /// Raised when one of assistance menus has been opened.
    /// </summary>
    public event EventHandler? AssistanceMenuOpened;
    
    
    /// <summary>
    /// Close all assistance menus.
    /// </summary>
    public void CloseAssistanceMenus()
    {
	    this.predefinedVarsPopup?.Close();
	    this.showAssistanceMenuAction.Cancel();
    }


    // Create listbox item for given variable.
    [DynamicDependency(nameof(StringInterpolationVariable.DisplayName), typeof(StringInterpolationVariable))]
    [DynamicDependency(nameof(StringInterpolationVariable.Name), typeof(StringInterpolationVariable))]
	ListBoxItem CreateListBoxItem(StringInterpolationVariable variable) =>
		this.recycledListBoxItems.Count > 0
			? this.recycledListBoxItems.Dequeue().Also(it => it.DataContext = variable)
			: new ListBoxItem().Also(it =>
			{
				it.Content = new Grid().Also(panel => 
				{
					panel.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto).Also(columnDefinition =>
					{
						columnDefinition.SharedSizeGroup = "Name";
					}));
					panel.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto).Also(columnDefinition =>
					{
						columnDefinition.SharedSizeGroup = "Separator";
					}));
					panel.ColumnDefinitions.Add(new(0, GridUnitType.Auto));
					panel.Children.Add(new Avalonia.Controls.TextBlock().Also(it =>
					{
						it.Bind(Avalonia.Controls.TextBlock.FontFamilyProperty, this.GetObservable(FontFamilyProperty));
#pragma warning disable IL2026
						it.Bind(Avalonia.Controls.TextBlock.TextProperty, new Binding { Path = nameof(StringInterpolationVariable.Name) });
#pragma warning restore IL2026
						it.VerticalAlignment = VerticalAlignment.Center;
					}));
					var displayNameTextBlock = new Avalonia.Controls.TextBlock().Also(it =>
					{
#pragma warning disable IL2026
						it.Bind(IsVisibleProperty, new Binding { Path = nameof(StringInterpolationVariable.DisplayName), Converter = StringConverters.IsNotNullOrEmpty });
                        it.Bind(OpacityProperty, this.GetResourceObservable("Double/TextBox.Assistance.MenuItem.Description.Opacity"));
						it.Bind(Avalonia.Controls.TextBlock.TextProperty, new Binding { Path = nameof(StringInterpolationVariable.DisplayName) });
#pragma warning restore IL2026
						it.VerticalAlignment = VerticalAlignment.Center;
						Grid.SetColumn(it, 2);
					});
					panel.Children.Add(new Separator().Also(it => 
					{
						it.Classes.Add("Dialog_Separator");
						it.Bind(IsVisibleProperty, displayNameTextBlock.GetObservable(IsVisibleProperty));
						Grid.SetColumn(it, 1);
					}));
					panel.Children.Add(displayNameTextBlock);
				});
				it.DataContext = variable;
			});
	
	
	/// <summary>
	/// Get bounds of caret related to the control.
	/// </summary>
	/// <returns>Bounds of caret related to the control.</returns>
	public Rect? GetCaretBounds()
	{
		if (this.textPresenter is null)
			return null;
		var padding = this.Padding;
		var caretRect = this.textPresenter.TextLayout.HitTestTextPosition(Math.Max(0, this.textPresenter.CaretIndex - 1));
		return new Rect(caretRect.Left + padding.Left, caretRect.Top + padding.Top, caretRect.Width, caretRect.Height);
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
	/// Check whether at least one assistance menu has been opened or not.
	/// </summary>
	public bool HasOpenedAssistanceMenus => this.hasOpenedAssistanceMenus;


    // Input given variable name.
	void InputVariableName(string name)
	{
		var text = this.Text;
		var varNameRange = StringInterpolationFormatSyntaxHighlighting.FindVariableNameRange(text, Math.Min(this.SelectionStart, this.SelectionEnd));
		if (varNameRange.IsClosed)
		{
			var endingChar = text![varNameRange.End!.Value - 1];
			this.SelectionStart = varNameRange.Start!.Value;
			this.SelectionEnd = varNameRange.End!.Value;
			this.SelectedText = $"{{{name}{endingChar}";
			if (this.SelectionEnd < (this.Text?.Length ?? 0))
			{
				++this.SelectionEnd;
				++this.SelectionStart;
			}
		}
	}


	/// <summary>
	/// Command to input variable name.
	/// </summary>
	public ICommand InputVariableNameCommand { get; }


	/// <inheritdox/>
	public override string? Object
	{
		get => this.Text;
		set => this.Text = value;
	}


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


	/// <inheritdoc/>
	protected override void OnLostFocus(RoutedEventArgs e)
	{
		SynchronizationContext.Current?.PostDelayed(() =>
		{
			if (!this.IsFocused)
				this.CloseAssistanceMenus();
		}, 200);
		base.OnLostFocus(e);
	}


    // Called when predefined variables changed.
	void OnPredefinedVarsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
		this.showAssistanceMenuAction.Schedule();
	
	// Called before handling key down event by children.
	void OnPreviewKeyDown(object? sender, KeyEventArgs e)
	{
		// handle key
		var isKeyForAssistantPopup = false;
		switch (e.Key)
		{
			case Key.Down:
			case Key.FnDownArrow:
				if (this.predefinedVarsPopup?.IsOpen == true)
				{
					this.predefinedVarsPopup.ItemListBox.SelectNextItem();
					isKeyForAssistantPopup = true;
					e.Handled = true;
				}
				break;
			case Key.Enter:
				if (this.predefinedVarsPopup?.IsOpen == true)
				{
					isKeyForAssistantPopup = true;
					e.Handled = true;
				}
				break;
			case Key.FnUpArrow:
			case Key.Up:
				if (this.predefinedVarsPopup?.IsOpen == true)
				{
					this.predefinedVarsPopup.ItemListBox.SelectPreviousItem();
					isKeyForAssistantPopup = true;
					e.Handled = true;
				}
				break;
		}
		
		// show/hide menu
		if (e.Key == Key.Escape)
			this.CloseAssistanceMenus();
		else if (!isKeyForAssistantPopup)
		{
			switch (e.Key)
			{
				case Key.LeftAlt:
				case Key.LeftCtrl:
				case Key.LeftShift:
				case Key.LWin:
				case Key.RightAlt:
				case Key.RightCtrl:
				case Key.RightShift:
				case Key.RWin:
					break;
				default:
					this.showAssistanceMenuAction.Reschedule(50);
					break;
			}
		}
	}


	// Called before handling key up event by children.
	void OnPreviewKeyUp(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter)
		{
			if (this.predefinedVarsPopup?.IsOpen == true)
			{
				(this.predefinedVarsPopup.ItemListBox.SelectedItem as ListBoxItem)?.Let(item =>
				{
					if (item.DataContext is StringInterpolationVariable variable)
						this.InputVariableName(variable.Name);
				});
				this.CloseAssistanceMenus();
			}
		}
	}


	/// <inheritdoc/>
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		var property = change.Property;
		if (property == SelectionEndProperty || property == SelectionStartProperty)
			this.showAssistanceMenuAction.Schedule();
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
				{
					HandleTextInputMethod ??= typeof(TextBox).GetMethod("HandleTextInput", BindingFlags.Instance | BindingFlags.NonPublic, [ typeof(string) ]);
					if (HandleTextInputMethod is not null)
					{
						HandleTextInputMethod.Invoke(this, [ "{}" ]);
						e.Handled = true;
					}
				}
				break;
			case '}':
				if (prevChar1 != '\\' && nextChar1 == '}')
					e.Handled = true;
				break;
		}

		// commit input
		var handled = e.Handled;
		base.OnTextInput(e);
		if (handled)
		{
			this.SelectionStart = selectionStart;
			this.SelectionEnd = selectionStart;
		}

		// show assistance menu
		this.showAssistanceMenuAction.Reschedule();
	}


    /// <summary>
	/// Predefined list of <see cref="StringInterpolationVariable"/> for input assistance.
	/// </summary>
	public IList<StringInterpolationVariable> PredefinedVariables => this.predefinedVars;


	/// <inheritdoc/>
	protected override void RaiseObjectChanged(string? oldValue, string? newValue) =>
		this.RaisePropertyChanged(ObjectProperty, oldValue, newValue);


	// Setup popup of predefined variables.
	InputAssistancePopup SetupPredefinedVarsPopup()
	{
		if (this.predefinedVarsPopup != null)
			return this.predefinedVarsPopup;
		var rootPanel = this.FindDescendantOfType<Panel>().AsNonNull();
		this.predefinedVarsPopup = new InputAssistancePopup().Also(menu =>
		{
			menu.Closed += (_, _) => this.UpdateHasOpenedAssistanceMenus();
			menu.ItemListBox.Let(it =>
			{
				Grid.SetIsSharedSizeScope(it, true);
				it.DoubleClickOnItem += (_, e) =>
				{
					menu.Close();
					if (e.Item is ListBoxItem item && item.DataContext is StringInterpolationVariable variable)
						this.InputVariableName(variable.Name);
				};
				it.ItemsPanel = this.FindResourceOrDefault<ItemsPanelTemplate>("ItemsPanelTemplate/StackPanel"); // [Workaround] Prevent crashing caused by VirtualizationStackPanel
				it.ItemsSource = this.filteredPredefinedVarListBoxItems;
				it.AddHandler(PointerPressedEvent, (_, _) =>
				{
					SynchronizationContext.Current?.Post(() => this.Focus());
				}, RoutingStrategies.Tunnel);
			});
			menu.Opened += (_, _) =>
			{
				this.UpdateHasOpenedAssistanceMenus();
				this.AssistanceMenuOpened?.Invoke(this, EventArgs.Empty);
			};
			menu.PlacementTarget = this;
		});
		rootPanel.Children.Insert(0, this.predefinedVarsPopup);
		return this.predefinedVarsPopup;
	}
	
	
	// Show assistance menu.
	void ShowAssistanceMenu()
	{
		// close menu first
		if (this.hasOpenedAssistanceMenus)
		{
			this.CloseAssistanceMenus();
			this.showAssistanceMenuAction.Schedule();
			return;
		}
		var (start, end) = this.GetSelection();
		if (!this.IsEffectivelyVisible || start != end)
			return;

		// show predefined variable menu
		var text = this.Text ?? "";
		var popupToOpen = (Popup?)null;
		if (this.predefinedVars.IsNotEmpty())
		{
			var varNameRange = StringInterpolationFormatSyntaxHighlighting.FindVariableNameRange(text, start);
			if (varNameRange.IsClosed)
			{
				var filterText = this.Text?[(varNameRange.Start!.Value + 1)..(varNameRange.End!.Value - 1)]?.ToLower() ?? "";
				this.filteredPredefinedVars.Clear();
				// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
				if (string.IsNullOrEmpty(filterText))
					this.filteredPredefinedVars.AddAll(this.predefinedVars);
				else
					this.filteredPredefinedVars.AddAll(this.predefinedVars.Where(it => it.Name.ToLower().Contains(filterText)));
				if (this.filteredPredefinedVars.IsNotEmpty())
					popupToOpen = this.SetupPredefinedVarsPopup();
			}
		}

		// open menu
		if (popupToOpen is not null)
		{
			this.GetCaretBounds()?.Let(caretBounds =>
			{
				popupToOpen.PlacementRect = caretBounds.Inflate(this.FindResourceOrDefault<double>("Double/InputAssistancePopup.Offset"));
				popupToOpen.Open();
			});
		}
	}


	/// <inheritdox/>
    protected override Type StyleKeyOverride => typeof(TextBox);


	/// <inheritdox/>
	protected override SyntaxHighlightingDefinitionSet SyntaxHighlightingDefinitionSet => StringInterpolationFormatSyntaxHighlighting.CreateDefinitionSet(IAppSuiteApplication.Current);


	/// <inheritdox/>
	protected override bool TryConvertToObject(string text, out string? obj)
	{
		obj = text;
		return true;
	}


	// Check whether at least one assistance has been opened or not.
    bool UpdateHasOpenedAssistanceMenus()
    {
	    if (this.predefinedVarsPopup?.IsOpen == true)
	    {
		    this.SetAndRaise(HasOpenedAssistanceMenusProperty, ref this.hasOpenedAssistanceMenus, true);
		    return true;
	    }
	    this.SetAndRaise(HasOpenedAssistanceMenusProperty, ref this.hasOpenedAssistanceMenus, false);
	    return false;
    }
}


/// <summary>
/// Predefined variable of string interpolation for <see cref="StringInterpolationFormatTextBox"/>.
/// </summary>
public class StringInterpolationVariable : AvaloniaObject
{
	/// <summary>
	/// Property of <see cref="DisplayName"/>.
	/// </summary>
	public static readonly StyledProperty<string> DisplayNameProperty = AvaloniaProperty.Register<StringInterpolationVariable, string>(nameof(DisplayName), "");
	/// <summary>
	/// Property of <see cref="Name"/>.
	/// </summary>
	public static readonly StyledProperty<string> NameProperty = AvaloniaProperty.Register<StringInterpolationVariable, string>(nameof(Name), "");


	/// <summary>
	/// Get or set display name of variable.
	/// </summary>
	public string DisplayName
	{
		get => this.GetValue(DisplayNameProperty);
		set => this.SetValue(DisplayNameProperty, value);
	}


	/// <summary>
	/// Get or set name of variable.
	/// </summary>
	public string Name
	{
		get => this.GetValue(NameProperty);
		set => this.SetValue(NameProperty, value);
	}
}