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
using CarinaStudio.AppSuite.Controls.Presenters;
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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// <see cref="TextBox"/> which accept regular expression.
/// </summary>
public class RegexTextBox : SyntaxHighlightingObjectTextBox<Regex>
{
	/// <summary>
	/// Property of <see cref="HasOpenedAssistanceMenus"/>.
	/// </summary>
	public static readonly DirectProperty<RegexTextBox, bool> HasOpenedAssistanceMenusProperty = AvaloniaProperty.RegisterDirect<RegexTextBox, bool>(nameof(HasOpenedAssistanceMenus), tb => tb.hasOpenedAssistanceMenus);
	/// <summary>
	/// Property of <see cref="IgnoreCase"/>.
	/// </summary>
	public static readonly StyledProperty<bool> IgnoreCaseProperty = AvaloniaProperty.Register<RegexTextBox, bool>(nameof(IgnoreCase), true);
	/// <summary>
	/// Property of <see cref="IsInputAssistanceEnabled"/>.
	/// </summary>
	public static readonly StyledProperty<bool> IsInputAssistanceEnabledProperty = AvaloniaProperty.Register<RegexTextBox, bool>(nameof(IsInputAssistanceEnabled), true);
	/// <summary>
	/// Property of <see cref="Object"/>.
	/// </summary>
	public static new readonly DirectProperty<RegexTextBox, Regex?> ObjectProperty = AvaloniaProperty.RegisterDirect<RegexTextBox, Regex?>(nameof(Object), t => t.Object, (t, o) => t.Object = o);
	/// <summary>
	/// Property of <see cref="PhraseInputAssistanceProvider"/>.
	/// </summary>
	public static readonly DirectProperty<RegexTextBox, IPhraseInputAssistanceProvider?> PhraseInputAssistanceProviderProperty = AvaloniaProperty.RegisterDirect<RegexTextBox, IPhraseInputAssistanceProvider?>(nameof(PhraseInputAssistanceProvider), t => t.phraseInputAssistanceProvider, (t, o) => t.PhraseInputAssistanceProvider = o);


	// Grouping construct.
	enum GroupingConstruct
	{
		NamedGroup,
		// ReSharper disable once IdentifierTypo
		NoncapturingGroup,
		ZeroWidthPositiveLookaheadAssertion,
		ZeroWidthNegativeLookaheadAssertion,
		ZeroWidthPositiveLookbehindAssertion,
		ZeroWidthNegativeLookbehindAssertion,
	}
	
	
	// Static fields.
	static MethodInfo? HandleTextInputMethod;


	// Fields.
	readonly ObservableList<string> candidatePhrases = new();
	CancellationTokenSource? candidatePhrasesSelectionCTS;
	InputAssistancePopup? candidatePhrasesPopup;
	InputAssistancePopup? escapedCharactersPopup;
	readonly ObservableList<ListBoxItem> filteredPredefinedGroupListBoxItems = new();
	readonly SortedObservableList<RegexGroup> filteredPredefinedGroups = new((x, y) => string.Compare(x.Name, y.Name, true, CultureInfo.InvariantCulture));
	InputAssistancePopup? groupingConstructsPopup;
	bool hasOpenedAssistanceMenus;
	bool isBackSlashPressed;
	bool isEscapeKeyHandled;
	bool isTextInputtedBeforeOpeningAssistanceMenu;
	IPhraseInputAssistanceProvider? phraseInputAssistanceProvider;
	readonly ObservableList<RegexGroup> predefinedGroups = new();
	InputAssistancePopup? predefinedGroupsPopup;
	readonly Queue<ListBoxItem> recycledListBoxItems = new();
	Range<int> selectedGroupNameRange;
	Range<int> selectedPhraseRange;
	readonly ScheduledAction showAssistanceMenuAction;
	TextPresenter? textPresenter;
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
	readonly ScheduledAction updateSelectedTokensAction;


	/// <summary>
	/// Initialize new <see cref="RegexTextBox"/> instance.
	/// </summary>
	public RegexTextBox()
	{
		SyntaxHighlighting.VerifyInitialization();
		this.AcceptsWhiteSpaces = true;
		this.PseudoClasses.Add(":regexTextBox");
		this.filteredPredefinedGroups.CollectionChanged += this.OnFilteredPredefinedGroupChanged;
		this.predefinedGroups.CollectionChanged += this.OnPredefinedGroupChanged;
		this.InputGroupNameCommand = new Command<string>(this.InputGroupName);
		this.InputStringCommand = new Command<string>(this.InputString);
		this.MaxLength = 1024;
		this.Bind(WatermarkProperty, this.GetResourceObservable("String/RegexTextBox.Watermark"));
		this.showAssistanceMenuAction = new(this.ShowAssistanceMenu);
		this.updateSelectedTokensAction = new(this.UpdateSelectedTokens);

		// attach to self
		this.AddHandler(KeyDownEvent, this.OnPreviewKeyDown, RoutingStrategies.Tunnel);
		this.AddHandler(KeyUpEvent, this.OnPreviewKeyUp, RoutingStrategies.Tunnel);
	}


	/// <summary>
	/// Raised when one of assistance menus has been opened.
	/// </summary>
	public event EventHandler? AssistanceMenuOpened;
	
	
	// Cancel candidate phrases selection.
	void CancelSelectingCandidatePhrases()
	{
		if (this.candidatePhrasesSelectionCTS is not null)
		{
			this.candidatePhrasesSelectionCTS.Cancel();
			this.candidatePhrasesSelectionCTS.Dispose();
			this.candidatePhrasesSelectionCTS = null;
		}
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
	/// Close all assistance menus.
	/// </summary>
	public void CloseAssistanceMenus()
	{
		this.CancelSelectingCandidatePhrases();
		this.candidatePhrasesPopup?.Close();
		this.escapedCharactersPopup?.Close();
		this.groupingConstructsPopup?.Close();
		this.predefinedGroupsPopup?.Close();
		this.showAssistanceMenuAction.Cancel();
	}


	// Create listbox item for assistance menu.
	ListBoxItem CreateListBoxItem(char escapedChar) =>
		new ListBoxItem().Also(it =>
		{
			it.Content = new Grid().Also(panel => 
			{
				var opacityObservable = this.GetResourceObservable("Double/TextBox.Assistance.MenuItem.Description.Opacity");
				panel.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto).Also(columnDefinition =>
				{
					columnDefinition.SharedSizeGroup = "Character";
				}));
				panel.ColumnDefinitions.Add(new(0, GridUnitType.Auto));
				panel.ColumnDefinitions.Add(new(0, GridUnitType.Auto));
				panel.Children.Add(new Avalonia.Controls.TextBlock().Also(it =>
				{
					it.Bind(Avalonia.Controls.TextBlock.FontFamilyProperty, this.GetObservable(FontFamilyProperty));
					it.Text = $"\\{escapedChar}";
					it.VerticalAlignment = VerticalAlignment.Center;
				}));
				panel.Children.Add(new Separator().Also(it => 
				{
					it.Classes.Add("Dialog_Separator");
					Grid.SetColumn(it, 1);
				}));
				panel.Children.Add(new Avalonia.Controls.TextBlock().Also(it =>
				{
					it.Bind(OpacityProperty, opacityObservable);
					it.Bind(Avalonia.Controls.TextBlock.TextProperty, this.GetResourceObservable($"String/RegexTextBox.EscapedCharacter.{escapedChar}"));
					it.VerticalAlignment = VerticalAlignment.Center;
					Grid.SetColumn(it, 2);
				}));
			});
			it.DataContext = escapedChar;
		});
	ListBoxItem CreateListBoxItem(GroupingConstruct groupingConstruct) =>
		new ListBoxItem().Also(it =>
		{
			it.Content = new Grid().Also(panel => 
			{
				var opacityObservable = this.GetResourceObservable("Double/TextBox.Assistance.MenuItem.Description.Opacity");
				panel.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto).Also(columnDefinition =>
				{
					columnDefinition.SharedSizeGroup = "Keyword";
				}));
				panel.ColumnDefinitions.Add(new(0, GridUnitType.Auto));
				panel.ColumnDefinitions.Add(new(0, GridUnitType.Auto));
				panel.Children.Add(new Avalonia.Controls.TextBlock().Also(it =>
				{
					it.Bind(Avalonia.Controls.TextBlock.FontFamilyProperty, this.GetObservable(FontFamilyProperty));
					it.Text = $"(?{GetGroupingConstructKeyword(groupingConstruct)})";
					it.VerticalAlignment = VerticalAlignment.Center;
				}));
				panel.Children.Add(new Separator().Also(it => 
				{
					it.Classes.Add("Dialog_Separator");
					Grid.SetColumn(it, 1);
				}));
				panel.Children.Add(new Avalonia.Controls.TextBlock().Also(it =>
				{
					it.Bind(Avalonia.Controls.TextBlock.OpacityProperty, opacityObservable);
					it.Bind(Avalonia.Controls.TextBlock.TextProperty, this.GetResourceObservable($"String/GroupingConstruct.{groupingConstruct}"));
					it.VerticalAlignment = VerticalAlignment.Center;
					Grid.SetColumn(it, 2);
				}));
			});
			it.DataContext = groupingConstruct;
		});
	[DynamicDependency(nameof(RegexGroup.DisplayName), typeof(RegexGroup))]
	[DynamicDependency(nameof(RegexGroup.Name), typeof(RegexGroup))]
	ListBoxItem CreateListBoxItem(RegexGroup group) =>
		this.recycledListBoxItems.Count > 0
			? this.recycledListBoxItems.Dequeue().Also(it => it.DataContext = group)
			: new ListBoxItem().Also(it =>
			{
				it.Content = new Grid().Also(panel => 
				{
					panel.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto).Also(columnDefinition =>
					{
						columnDefinition.SharedSizeGroup = "Character";
					}));
					panel.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto).Also(columnDefinition =>
					{
						columnDefinition.SharedSizeGroup = "Separator";
					}));
					panel.ColumnDefinitions.Add(new(0, GridUnitType.Auto));
					panel.Children.Add(new Avalonia.Controls.TextBlock().Also(it =>
					{
#pragma warning disable IL2026
						it.Bind(Avalonia.Controls.TextBlock.TextProperty, new Binding { Path = nameof(RegexGroup.Name) });
#pragma warning restore IL2026
						it.VerticalAlignment = VerticalAlignment.Center;
					}));
					var displayNameTextBlock = new Avalonia.Controls.TextBlock().Also(it =>
					{
#pragma warning disable IL2026
						it.Bind(IsVisibleProperty, new Binding { Path = nameof(RegexGroup.DisplayName), Converter = StringConverters.IsNotNullOrEmpty });
						it.Bind(Avalonia.Controls.TextBlock.TextProperty, new Binding { Path = nameof(RegexGroup.DisplayName) });
#pragma warning restore IL2026
						it.Bind(OpacityProperty, this.GetResourceObservable("Double/TextBox.Assistance.MenuItem.Description.Opacity"));
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
				it.DataContext = group;
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
	

	// Get keyword of grouping construct.
	static string GetGroupingConstructKeyword(GroupingConstruct groupingConstruct) => groupingConstruct switch
	{
		GroupingConstruct.NamedGroup => "<>",
		GroupingConstruct.NoncapturingGroup => ":",
		GroupingConstruct.ZeroWidthNegativeLookaheadAssertion => "!",
		GroupingConstruct.ZeroWidthNegativeLookbehindAssertion => "<!",
		GroupingConstruct.ZeroWidthPositiveLookaheadAssertion => "=",
		GroupingConstruct.ZeroWidthPositiveLookbehindAssertion => "<=",
		_ => throw new NotSupportedException(),
	};


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


    /// <summary>
    /// Get or set whether case in <see cref="Regex"/> can be ignored or not.
    /// </summary>
    public bool IgnoreCase
	{
		get => this.GetValue(IgnoreCaseProperty);
		set => this.SetValue(IgnoreCaseProperty, value);
	}


	// Input grouping construct.
	void InputGroupingConstruct(GroupingConstruct groupingConstruct)
	{
		var keyword = GetGroupingConstructKeyword(groupingConstruct);
		this.InputString(keyword);
		if (keyword == "<>")
			--this.CaretIndex;
	}


	// Input given group name.
	void InputGroupName(string name)
	{
		var groupNameRange = RegexSyntaxHighlighting.FindGroupNameRange(this.Text, Math.Min(this.SelectionStart, this.SelectionEnd));
		if (groupNameRange.IsClosed)
		{
			this.SelectionStart = groupNameRange.Start!.Value;
			this.SelectionEnd = groupNameRange.End!.Value;
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


	// Input a phrase.
	unsafe void InputPhrase(string phrase)
	{
		// check state
		this.updateSelectedTokensAction.ExecuteIfScheduled();
		var selectedPhraseRange = this.selectedPhraseRange;
		if (!selectedPhraseRange.IsClosed)
			return;
		var text = this.Text;
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(phrase))
			return;

		// find proper selection
		Func<char, char, bool> checkCharEquality = this.IgnoreCase
			? static (x, y) => char.ToLowerInvariant(x) == char.ToLowerInvariant(y)
			: static (x, y) => x == y;
		var selection = (text.AsMemory(), phrase.AsMemory()).PinAs((char* textPtr, char* phrasePtr) =>
		{
			// get state
			var phraseLength = phrase.Length;
			var (selectionStart, selectionEnd) = this.GetSelection();
			
			// find selection start
			var newSelectionStart = phraseLength >= (selectionStart - selectedPhraseRange.Start!.Value)
				? selectedPhraseRange.Start!.Value
				: selectionStart - phraseLength;
			while (newSelectionStart < selectionStart)
			{
				var isPrefixMatched = true;
				for (var i = newSelectionStart; i < selectionStart; ++i)
				{
					if (!checkCharEquality(textPtr[i], phrasePtr[i - newSelectionStart]))
					{
						isPrefixMatched = false;
						break;
					}
				}
				if (isPrefixMatched)
					break;
				++newSelectionStart;
			}
			
			// complete
			return new Range<int>(newSelectionStart, Math.Max(newSelectionStart, selectionEnd));
		});
		
		// insert phrase
		if (selection.IsClosed)
		{
			this.SelectionStart = selection.Start!.Value;
			this.SelectionEnd = selection.End!.Value;
		}
		this.SelectedText = phrase;
	}


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
		get => this.GetValue(IsInputAssistanceEnabledProperty);
		set => this.SetValue(IsInputAssistanceEnabledProperty, value);
	}
	

	/// <inheritdoc/>
	public override Regex? Object
	{
		get => (Regex?)((ObjectTextBox)this).Object;
		set => ((ObjectTextBox)this).Object = value;
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
				this.CloseAssistanceMenus();
		}, 200);
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
			if (isBackspace)
				this.isTextInputtedBeforeOpeningAssistanceMenu = true;
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
		base.OnKeyUp(e);
	}


	// Called when predefined groups changed.
	void OnPredefinedGroupChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
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
				if (this.candidatePhrasesPopup?.IsOpen == true)
				{
					this.candidatePhrasesPopup.ItemListBox.SelectNextItem();
					isKeyForAssistantPopup = true;
					e.Handled = true;
				}
				if (this.escapedCharactersPopup?.IsOpen == true)
				{
					this.escapedCharactersPopup.ItemListBox.SelectNextItem();
					isKeyForAssistantPopup = true;
					e.Handled = true;
				}
				else if (this.groupingConstructsPopup?.IsOpen == true)
				{
					this.groupingConstructsPopup.ItemListBox.SelectNextItem();
					isKeyForAssistantPopup = true;
					e.Handled = true;
				}
				else if (this.predefinedGroupsPopup?.IsOpen == true)
				{
					this.predefinedGroupsPopup.ItemListBox.SelectNextItem();
					isKeyForAssistantPopup = true;
					e.Handled = true;
				}
				break;
			case Key.Enter:
				if (this.candidatePhrasesPopup?.IsOpen == true 
				    || this.escapedCharactersPopup?.IsOpen == true
				    || this.groupingConstructsPopup?.IsOpen == true
				    || this.predefinedGroupsPopup?.IsOpen == true)
				{
					isKeyForAssistantPopup = true;
					e.Handled = true;
				}
				break;
			case Key.FnUpArrow:
			case Key.Up:
				if (this.candidatePhrasesPopup?.IsOpen == true)
				{
					this.candidatePhrasesPopup.ItemListBox.SelectPreviousItem();
					isKeyForAssistantPopup = true;
					e.Handled = true;
				}
				if (this.escapedCharactersPopup?.IsOpen == true)
				{
					this.escapedCharactersPopup.ItemListBox.SelectPreviousItem();
					isKeyForAssistantPopup = true;
					e.Handled = true;
				}
				else if (this.groupingConstructsPopup?.IsOpen == true)
				{
					this.groupingConstructsPopup.ItemListBox.SelectPreviousItem();
					isKeyForAssistantPopup = true;
					e.Handled = true;
				}
				else if (this.predefinedGroupsPopup?.IsOpen == true)
				{
					this.predefinedGroupsPopup.ItemListBox.SelectPreviousItem();
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
			if (this.candidatePhrasesPopup?.IsOpen == true)
			{
				if (this.candidatePhrasesPopup.ItemListBox.SelectedItem is string phrase)
					this.InputPhrase(phrase);
				this.CloseAssistanceMenus();
			}
			if (this.escapedCharactersPopup?.IsOpen == true)
			{
				(this.escapedCharactersPopup.ItemListBox.SelectedItem as ListBoxItem)?.Let(item =>
				{
					if (item.DataContext is char c)
						this.InputString(c.ToString());
				});
				this.CloseAssistanceMenus();
			}
			else if (this.groupingConstructsPopup?.IsOpen == true)
			{
				(this.groupingConstructsPopup.ItemListBox.SelectedItem as ListBoxItem)?.Let(item =>
				{
					if (item.DataContext is GroupingConstruct groupingConstruct)
						this.InputGroupingConstruct(groupingConstruct);
				});
				this.CloseAssistanceMenus();
			}
			else if (this.predefinedGroupsPopup?.IsOpen == true)
			{
				(this.predefinedGroupsPopup.ItemListBox.SelectedItem as ListBoxItem)?.Let(item =>
				{
					if (item.DataContext is RegexGroup group)
						this.InputGroupName(group.Name);
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
		if (property == IgnoreCaseProperty)
			this.Validate();
		else if (property == IsInputAssistanceEnabledProperty)
		{
			if (!(bool)change.NewValue!)
			{
				this.isTextInputtedBeforeOpeningAssistanceMenu = false;
				this.CloseAssistanceMenus();
			}
		}
		else if (property == ObjectProperty)
		{
			if (change.NewValue is Regex regex && ((regex.Options & RegexOptions.IgnoreCase) != 0) != this.IgnoreCase)
			{
				var options = regex.Options;
				if (this.IgnoreCase)
					options |= RegexOptions.IgnoreCase;
				else
					options &= ~RegexOptions.IgnoreCase;
				this.Object = new Regex(regex.ToString(), options);
			}
		}
		else if (property == SelectionEndProperty || property == SelectionStartProperty)
		{
			this.updateSelectedTokensAction.Schedule();
			this.showAssistanceMenuAction.Schedule();
		}
		else if (property == TextProperty)
			this.updateSelectedTokensAction.Schedule();
	}


	/// <inheritdoc/>
    protected override void OnTextInput(TextInputEventArgs e)
	{
		// no need to handle
		var s = e.Text;
		if (this.IsReadOnly || !this.IsInputAssistanceEnabled || string.IsNullOrEmpty(s))
		{
			this.isTextInputtedBeforeOpeningAssistanceMenu = false;
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
		HandleTextInputMethod ??= typeof(TextBox).GetMethod("HandleTextInput", BindingFlags.Instance | BindingFlags.NonPublic, [ typeof(string) ]);
		switch (s[0])
		{
			case '(':
				if (prevChar1 != '\\' && nextChar1 == '\0' && HandleTextInputMethod is not null)
				{
					HandleTextInputMethod.Invoke(this, [ "()" ]);
					e.Handled = true;
				}
				break;
			case ')':
				if (prevChar1 != '\\' && nextChar1 == ')' && nextChar2 == '\0')
					e.Handled = true;
				break;
			case '[':
				if (prevChar1 != '\\' && HandleTextInputMethod is not null)
				{
					HandleTextInputMethod.Invoke(this, [ "[]" ]);
					e.Handled = true;
				}
				break;
			case ']':
				if (prevChar1 != '\\' && nextChar1 == ']')
					e.Handled = true;
				break;
			case '{':
				if (prevChar1 != '\\' && HandleTextInputMethod is not null)
				{
					HandleTextInputMethod.Invoke(this, [ "{}" ]);
					e.Handled = true;
				}
				break;
			case '}':
				if (prevChar1 != '\\' && nextChar1 == '}')
					e.Handled = true;
				break;
			case '<':
				if (prevChar1 == '?' && prevChar2 == '(' && HandleTextInputMethod is not null)
				{
					HandleTextInputMethod.Invoke(this, [ "<>" ]);
					e.Handled = true;
				}
				break;
			case '>':
				if (prevChar1 != '\\' && nextChar1 == '>')
					e.Handled = true;
				break;
			case '\\':
				this.isBackSlashPressed = true;
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
		this.isTextInputtedBeforeOpeningAssistanceMenu = true;
		this.showAssistanceMenuAction.Reschedule();
	}
	
	
	// Select candidate phrases and open the menu.
	async Task OpenCandidatePhrasesMenuAsync()
	{
		// cancel current selection
		this.CancelSelectingCandidatePhrases();
		
		// check state
		if (this.phraseInputAssistanceProvider is null)
		{
			this.candidatePhrasesPopup?.Close();
			this.candidatePhrases.Clear();
			return;
		}
		
		// check selection
		var (selectionStart, selectionEnd) = this.GetSelection();
		if (selectionStart != selectionEnd)
		{
			this.candidatePhrasesPopup?.Close();
			this.candidatePhrases.Clear();
			return;
		}
		
		// get prefix and postfix to select phrases
		this.updateSelectedTokensAction.ExecuteIfScheduled();
		var selectedPhraseRange = this.selectedPhraseRange;
		if (!selectedPhraseRange.IsClosed || selectionStart <= selectedPhraseRange.Start)
		{
			this.candidatePhrasesPopup?.Close();
			this.candidatePhrases.Clear();
			return;
		}
		var text = this.Text.AsNonNull();
		var prefix = text[selectedPhraseRange.Start!.Value..selectionStart];
		var postfix = selectionStart < selectedPhraseRange.End
			? text[selectionStart..selectedPhraseRange.End!.Value]
			: null;
		
		// select phrases
		IList<string> selectedPhrases;
		var cts = new CancellationTokenSource();
		this.candidatePhrasesSelectionCTS = cts;
		try
		{
			selectedPhrases = await this.phraseInputAssistanceProvider.SelectCandidatePhrasesAsync(prefix, postfix, cts.Token);
		}
		catch (Exception ex)
		{
			if (ex is not TaskCanceledException && this.candidatePhrasesSelectionCTS == cts)
				throw;
			selectedPhrases = Array.Empty<string>();
		}
		if (this.candidatePhrasesSelectionCTS != cts)
			return;
		this.candidatePhrasesSelectionCTS.Dispose();
		this.candidatePhrasesSelectionCTS = null;
		
		// open menu
		this.candidatePhrases.Clear();
		this.candidatePhrases.AddRange(selectedPhrases);
		if (this.candidatePhrases.IsNotEmpty())
		{
			this.SetupCandidatePhrasesPopup().Let(popup =>
			{
				this.GetCaretBounds()?.Let(caretBounds =>
				{
					popup.PlacementRect = caretBounds.Inflate(this.FindResourceOrDefault<double>("Double/InputAssistancePopup.Offset"));
					popup.Open();
				});
			});
		}
	}


	/// <summary>
	/// Get or set <see cref="IPhraseInputAssistanceProvider"/> for phrase input assistance.
	/// </summary>
	public IPhraseInputAssistanceProvider? PhraseInputAssistanceProvider
	{
		get => this.phraseInputAssistanceProvider;
		set
		{
			this.VerifyAccess();
			if (this.phraseInputAssistanceProvider == value)
				return;
			this.SetAndRaise(PhraseInputAssistanceProviderProperty, ref this.phraseInputAssistanceProvider, value);
		}
	}


	/// <summary>
	/// Predefined list of <see cref="RegexGroup"/> for input assistance.
	/// </summary>
	public IList<RegexGroup> PredefinedGroups => this.predefinedGroups;
	
	
	/// <inheritdoc/>
	protected override void RaiseObjectChanged(Regex? oldValue, Regex? newValue) =>
		this.RaisePropertyChanged(ObjectProperty, oldValue, newValue);
	
	
	// Setup menu for candidate phrases.
	InputAssistancePopup SetupCandidatePhrasesPopup()
	{
		if (this.candidatePhrasesPopup != null)
			return this.candidatePhrasesPopup;
		var rootPanel = this.FindDescendantOfType<Panel>().AsNonNull();
		this.candidatePhrasesPopup = new InputAssistancePopup().Also(menu =>
		{
			menu.Closed += (_, _) =>
			{
				this.UpdateHasOpenedAssistanceMenus();
				this.CancelSelectingCandidatePhrases();
			};
			menu.ItemListBox.Let(it =>
			{
				it.DoubleClickOnItem += (_, e) =>
				{
					menu.Close();
					if (e.Item is string phrase)
						this.InputPhrase(phrase);
				};
				it.ItemsSource = this.candidatePhrases;
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
		rootPanel.Children.Insert(0, this.candidatePhrasesPopup);
		return this.candidatePhrasesPopup;
	}


	// Setup menu for escaped characters.
	InputAssistancePopup SetupEscapedCharactersPopup()
	{
		if (this.escapedCharactersPopup != null)
			return this.escapedCharactersPopup;
		var rootPanel = this.FindDescendantOfType<Panel>().AsNonNull();
		this.escapedCharactersPopup = new InputAssistancePopup().Also(menu =>
		{
			menu.Closed += (_, _) => this.UpdateHasOpenedAssistanceMenus();
			menu.ItemListBox.Let(it =>
			{
				Grid.SetIsSharedSizeScope(it, true);
				it.DoubleClickOnItem += (_, e) =>
				{
					menu.Close();
					if (e.Item is ListBoxItem item && item.DataContext is char c)
						this.InputString(c.ToString());
				};
				it.ItemsSource = new[] {
					this.CreateListBoxItem('d'),
					this.CreateListBoxItem('s'),
					this.CreateListBoxItem('w'),
					this.CreateListBoxItem('b'),
					this.CreateListBoxItem('D'),
					this.CreateListBoxItem('S'),
					this.CreateListBoxItem('W'),
					this.CreateListBoxItem('B'),
				};
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
		rootPanel.Children.Insert(0, this.escapedCharactersPopup);
		return this.escapedCharactersPopup;
	}


	// Setup menu for grouping constructs.
	InputAssistancePopup SetupGroupingConstructsPopup()
	{
		if (this.groupingConstructsPopup != null)
			return this.groupingConstructsPopup;
		var rootPanel = this.FindDescendantOfType<Panel>().AsNonNull();
		this.groupingConstructsPopup = new InputAssistancePopup().Also(menu =>
		{
			menu.Closed += (_, _) => this.UpdateHasOpenedAssistanceMenus();
			menu.ItemListBox.Let(it =>
			{
				Grid.SetIsSharedSizeScope(it, true);
				it.DoubleClickOnItem += (_, e) =>
				{
					menu.Close();
					if (e.Item is ListBoxItem item && item.DataContext is GroupingConstruct groupingConstruct)
						this.InputGroupingConstruct(groupingConstruct);
				};
				it.ItemsSource = new[] {
					this.CreateListBoxItem(GroupingConstruct.NamedGroup),
					this.CreateListBoxItem(GroupingConstruct.NoncapturingGroup),
					this.CreateListBoxItem(GroupingConstruct.ZeroWidthPositiveLookaheadAssertion),
					this.CreateListBoxItem(GroupingConstruct.ZeroWidthNegativeLookaheadAssertion),
					this.CreateListBoxItem(GroupingConstruct.ZeroWidthPositiveLookbehindAssertion),
					this.CreateListBoxItem(GroupingConstruct.ZeroWidthNegativeLookbehindAssertion),
				};
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
		rootPanel.Children.Insert(0, this.groupingConstructsPopup);
		return this.groupingConstructsPopup;
	}


	// Setup menu of predefined groups.
	InputAssistancePopup SetupPredefinedGroupsPopup()
	{
		if (this.predefinedGroupsPopup != null)
			return this.predefinedGroupsPopup;
		var rootPanel = this.FindDescendantOfType<Panel>().AsNonNull();
		this.predefinedGroupsPopup = new InputAssistancePopup().Also(menu =>
		{
			menu.Closed += (_, _) => this.UpdateHasOpenedAssistanceMenus();
			menu.ItemListBox.Let(it =>
			{
				Grid.SetIsSharedSizeScope(it, true);
				it.DoubleClickOnItem += (_, e) =>
				{
					menu.Close();
					if (e.Item is ListBoxItem item && item.DataContext is RegexGroup group)
						this.InputGroupName(group.Name);
				};
				it.ItemsPanel = this.FindResourceOrDefault<ItemsPanelTemplate>("ItemsPanelTemplate/StackPanel"); // [Workaround] Prevent crashing caused by VirtualizationStackPanel
				it.ItemsSource = this.filteredPredefinedGroupListBoxItems;
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
		rootPanel.Children.Insert(0, this.predefinedGroupsPopup);
		return this.predefinedGroupsPopup;
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
		if (!this.IsInputAssistanceEnabled || !this.IsEffectivelyVisible || start != end)
		{
			this.isBackSlashPressed = false;
			return;
		}

		// show nothing in comment
		if (this.textPresenter is SyntaxHighlightingTextPresenter shTextPresenter)
		{
			shTextPresenter.FindSpanAndToken(end, out _, out var token);
			switch (token?.Name)
			{
				case RegexSyntaxHighlighting.EndOfLineComment:
				case RegexSyntaxHighlighting.InlineComment:
					return;
			}
		}

		// update selected tokens if needed
		this.updateSelectedTokensAction.ExecuteIfScheduled();

		// show predefined groups menu
		var text = this.Text ?? "";
		var textLength = text.Length;
		var popupToOpen = (Popup?)null;
		if (this.predefinedGroups.IsNotEmpty() && this.selectedGroupNameRange.IsClosed)
		{
			var filterText = this.Text?[this.selectedGroupNameRange.Start!.Value..this.selectedGroupNameRange.End!.Value]?.ToLower() ?? "";
			this.filteredPredefinedGroups.Clear();
			// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
			if (string.IsNullOrEmpty(filterText))
				this.filteredPredefinedGroups.AddAll(this.predefinedGroups);
			else
				this.filteredPredefinedGroups.AddAll(this.predefinedGroups.Where(it => it.Name.ToLower().Contains(filterText)));
			if (this.filteredPredefinedGroups.IsNotEmpty())
				popupToOpen = this.SetupPredefinedGroupsPopup();
		}

		// show grouping constructs menu
		if (popupToOpen is null && start >= 2 && start < textLength
		    && text[start - 1] == '?' && text[start - 2] == '(' && text[start] == ')')
		{
			popupToOpen = this.SetupGroupingConstructsPopup();
		}

		// show escaped characters menu
		if (this.isBackSlashPressed)
		{
			this.isBackSlashPressed = false;
			if (popupToOpen is null && start > 0 && text[start - 1] == '\\' && (start <= 1 || text[start - 2] != '\\'))
				popupToOpen = this.SetupEscapedCharactersPopup();
		}

		// show phrases menu
		if (popupToOpen is null
		    && this.selectedPhraseRange.IsClosed
		    && this.phraseInputAssistanceProvider is not null
		    && this.isTextInputtedBeforeOpeningAssistanceMenu)
		{
			popupToOpen = this.SetupCandidatePhrasesPopup();
		}

		// open menu
		this.isTextInputtedBeforeOpeningAssistanceMenu = false;
		if (popupToOpen is not null)
		{
			if (popupToOpen == this.candidatePhrasesPopup)
				_ = this.OpenCandidatePhrasesMenuAsync();
			else
			{
				this.GetCaretBounds()?.Let(caretBounds =>
				{
					popupToOpen.PlacementRect = caretBounds.Inflate(this.FindResourceOrDefault<double>("Double/InputAssistancePopup.Offset"));
					popupToOpen.Open();
				});
			}
		}
	}


	/// <inheritdoc/>
	protected override SyntaxHighlightingDefinitionSet SyntaxHighlightingDefinitionSet => RegexSyntaxHighlighting.CreateDefinitionSet(IAppSuiteApplication.Current);


	// Check whether at least one assistance has been opened or not.
	bool UpdateHasOpenedAssistanceMenus()
	{
		if (this.candidatePhrasesPopup?.IsOpen == true 
		    || this.escapedCharactersPopup?.IsOpen == true
		    || this.groupingConstructsPopup?.IsOpen == true
		    || this.predefinedGroupsPopup?.IsOpen == true)
		{
			this.SetAndRaise(HasOpenedAssistanceMenusProperty, ref this.hasOpenedAssistanceMenus, true);
			return true;
		}
		this.SetAndRaise(HasOpenedAssistanceMenusProperty, ref this.hasOpenedAssistanceMenus, false);
		return false;
	}
	
	
	// Update tokens around current selection.
	void UpdateSelectedTokens()
	{
		// get text and selection
		var text = this.Text;
		var textLength = text?.Length ?? 0;
		var (selectionStart, selectionEnd) = this.GetSelection();
		var hasSelection = selectionEnd - selectionStart > 0;
		
		// find range of group name
		this.selectedGroupNameRange = hasSelection
			? default
			: RegexSyntaxHighlighting.FindGroupNameRange(text, selectionStart);
		
		// find range of character classes
		var selectedCharacterClassesRange = hasSelection || this.selectedGroupNameRange.IsClosed
			? default 
			: RegexSyntaxHighlighting.FindCharacterClassesRange(text, selectionStart);
		
		// find range of quantifier
		var selectedQuantifierRange = hasSelection || this.selectedGroupNameRange.IsClosed || selectedCharacterClassesRange.IsClosed
			? default 
			: RegexSyntaxHighlighting.FindQuantifierRange(text, selectionStart);

		// find range of phrase
		if (hasSelection 
		    || textLength <= 0
		    || selectedCharacterClassesRange.IsClosed 
		    || this.selectedGroupNameRange.IsClosed
		    || selectedQuantifierRange.IsClosed)
		{
			this.selectedPhraseRange = default;
		}
		else
			this.selectedPhraseRange = RegexSyntaxHighlighting.FindPhraseRange(text, selectionStart);
#if DEBUG
		System.Diagnostics.Debug.WriteLine($"CharClasses: {selectedCharacterClassesRange}, GroupName: {this.selectedGroupNameRange}, Phrase: {this.selectedPhraseRange}, Quantifier: {selectedQuantifierRange}");
#endif
	}


	/// <inheritdoc/>
    protected override bool TryConvertToObject(string text, out Regex? obj)
    {
        try
        {
			obj = new Regex(text, this.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
			this.SyntaxErrorRange = Range<int>.Empty;
			return true;
        }
		catch (Exception ex)
		{
			if (ex is RegexParseException regexParseEx)
			{
				var offset = Math.Max(0, regexParseEx.Offset - 1);
				this.SyntaxErrorRange = new(offset, offset + 1);
			}
			else
				this.SyntaxErrorRange = Range<int>.Universal;
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
		get => this.GetValue(DisplayNameProperty);
		set => this.SetValue(DisplayNameProperty, value);
	}


	/// <summary>
	/// Get or set name of group.
	/// </summary>
	public string Name
	{
		get => this.GetValue(NameProperty);
		set => this.SetValue(NameProperty, value);
	}
}
