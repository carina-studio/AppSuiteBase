using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using CarinaStudio.Controls;

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
            it.Bind(Control.MaxHeightProperty, this.GetResourceObservable("Double/InputAssistancePopup.Content.MaxHeight"));
            it.VirtualizationMode = ItemVirtualizationMode.None;
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
        this.Opened += (_ , e) => this.ItemListBox.SelectFirstItem();
    }


    /// <summary>
    /// ListBox for items in popup.
    /// </summary>
    public ListBox ItemListBox { get; }
}