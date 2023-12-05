using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using CarinaStudio.Controls;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Popup for input assistance.
/// </summary>
public class InputAssistancePopup : Popup
{
    /// <summary>
    /// Initialize new <see cref="InputAssistancePopup"/> instance.
    /// </summary>
    public InputAssistancePopup()
    {
        this.Focusable = false;
        this.ItemListBox = new ListBox().Also(it =>
        {
            it.Background = Brushes.Transparent;
			it.BorderThickness = new Thickness(0);
            it.Bind(MaxHeightProperty, this.GetResourceObservable("Double/InputAssistancePopup.Content.MaxHeight"));
        });
        this.Child = new Border().Also(it =>
        {
            it.Bind(Border.BackgroundProperty, this.GetResourceObservable("MenuFlyoutPresenterBackground"));
            it.Bind(Border.BorderBrushProperty, this.GetResourceObservable("MenuFlyoutPresenterBorderBrush"));
            it.Bind(Border.BorderThicknessProperty, this.GetResourceObservable("MenuFlyoutPresenterBorderThemeThickness"));
            it.Child = this.ItemListBox;
            it.Bind(Border.CornerRadiusProperty, this.GetResourceObservable("OverlayCornerRadius"));
            it.Bind(Border.PaddingProperty, this.GetResourceObservable("Thickness/InputAssistancePopup.Padding"));
        });
        this.Opened += (_ , _) => this.ItemListBox.SelectFirstItem();
    }


    /// <summary>
    /// ListBox for items in popup.
    /// </summary>
    public ListBox ItemListBox { get; }


    /// <inheritdoc/>
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        if (this.TryGetResource("ItemsPanelTemplate/StackPanel", out ItemsPanelTemplate? template))
            this.ItemListBox.ItemsPanel = template;
        base.OnAttachedToLogicalTree(e);
    }


    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(Popup);
}