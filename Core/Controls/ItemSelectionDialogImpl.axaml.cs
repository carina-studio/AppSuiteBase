using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to select one or more items.
/// </summary>
class ItemSelectionDialogImpl : InputDialog<IAppSuiteApplication>
{
	/// <summary>
	/// Define <see cref="Message"/> property.
	/// </summary>
	public static readonly StyledProperty<string?> MessageProperty = AvaloniaProperty.Register<ItemSelectionDialogImpl, string?>(nameof(Message));
	

	// Fields.
	readonly CheckBox doNotAskAgainCheckBox;
	readonly ListBox itemsListBox;


	/// <summary>
	/// Initialize new <see cref="ItemSelectionDialogImpl"/> instance.
	/// </summary>
	public ItemSelectionDialogImpl()
	{
		AvaloniaXamlLoader.Load(this);
		this.doNotAskAgainCheckBox = this.Get<CheckBox>(nameof(doNotAskAgainCheckBox));
		this.itemsListBox = this.Get<ListBox>(nameof(itemsListBox)).Also(it =>
		{
			it.DoubleClickOnItem += (_, _) => this.GenerateResultCommand.TryExecute();
			it.SelectionChanged += (_, _) => this.InvalidateInput();
		});
	}
	
	
	/// <summary>
	/// Get or set whether multiple items can be selected or not.
	/// </summary>
	public bool CanSelectMultipleItems { get; set; }
	
	
	/// <summary>
	/// Get or set default selected item.
	/// </summary>
	public object? DefaultItem { get; set; }
	
	
	/// <summary>
	/// Get or set the state of "Do not ask again" check box.
	/// </summary>
	public bool? DoNotAskAgain { get; set; }


	/// <inheritdoc/>
	protected override Task<object?> GenerateResultAsync(CancellationToken cancellationToken)
	{
		var selectedItems = this.itemsListBox.SelectedItems;
		if (selectedItems is null || selectedItems.Count == 0)
			return Task.FromResult<object?>(null);
		if (this.CanSelectMultipleItems)
		{
			var itemArray = new object?[selectedItems.Count];
			selectedItems.CopyTo(itemArray, 0);
			return Task.FromResult<object?>(itemArray);
		}
		return Task.FromResult<object?>(new[] { selectedItems[0] });
	}


	/// <summary>
	/// Get or set items for selection.
	/// </summary>
	public IList? Items { get; set; }


	/// <summary>
	/// Get or set message.
	/// </summary>
	public string? Message
	{
		get => this.GetValue(MessageProperty);
		set => this.SetValue(MessageProperty, value);
	}


	/// <inheritdoc/>
	protected override void OnClosing(WindowClosingEventArgs e)
	{
		if (this.doNotAskAgainCheckBox.IsEffectivelyVisible)
			this.DoNotAskAgain = this.doNotAskAgainCheckBox.IsChecked;
		base.OnClosing(e);
	}


	/// <inheritdoc/>
	protected override void OnOpened(EventArgs e)
	{
		// call base
		base.OnOpened(e);
		
		// setup focus or close if there is no items
		this.Items.Let(it =>
		{
			if (it is null || it.Count == 0)
				this.SynchronizationContext.Post(this.Close);
			else
			{
				this.DefaultItem?.Let(defaultItem =>
				{
					if (it.Contains(defaultItem))
						this.itemsListBox.SelectedItem = defaultItem;
				});
				this.SynchronizationContext.Post(() => this.itemsListBox.Focus());
			}
		});
	}


	/// <inheritdoc/>
	protected override void OnOpening(EventArgs e)
	{
		base.OnOpening(e);
		this.DoNotAskAgain?.Let(it =>
		{
			this.Get<Panel>("doNotAskAgainCheckBoxPanel").IsVisible = true;
			this.doNotAskAgainCheckBox.IsChecked = it;
		});
		this.itemsListBox.ItemsSource = this.Items;
		this.itemsListBox.SelectionMode = this.CanSelectMultipleItems ? SelectionMode.Multiple : SelectionMode.Single;
	}


	/// <inheritdoc/>
	protected override bool OnValidateInput() =>
		base.OnValidateInput() && this.itemsListBox.SelectedItems?.Count > 0;
}
