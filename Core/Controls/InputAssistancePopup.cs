using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
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
            it.BindToResource(MaxHeightProperty, "Double/InputAssistancePopup.Content.MaxHeight");
            it.BindToResource(MinWidthProperty, "Double/InputAssistancePopup.Content.MinWidth");
        });
        this.Child = new Border().Also(it =>
        {
            it.BindToResource(Border.BackgroundProperty, "MenuFlyoutPresenterBackground");
            it.BindToResource(Border.BorderBrushProperty, "MenuFlyoutPresenterBorderBrush");
            it.BindToResource(Border.BorderThicknessProperty, "MenuFlyoutPresenterBorderThemeThickness");
            it.Child = this.ItemListBox;
            it.BindToResource(Border.CornerRadiusProperty, "OverlayCornerRadius");
            it.BindToResource(Border.PaddingProperty, "Thickness/InputAssistancePopup.Padding");
        });
        this.Opened += (_ , _) => this.ItemListBox.SelectFirstItem();
        this.Placement = PlacementMode.Bottom;
        this.PlacementConstraintAdjustment = PopupPositionerConstraintAdjustment.FlipY | PopupPositionerConstraintAdjustment.ResizeY | PopupPositionerConstraintAdjustment.SlideX;
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