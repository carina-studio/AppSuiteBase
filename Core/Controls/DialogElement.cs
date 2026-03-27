using Avalonia;
using Avalonia.Controls;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Provide attached properties for dialog elements.
/// </summary>
public static class DialogElement
{
    /// <summary>
    /// Define property for type of icon in dialog.
    /// </summary>
    public static readonly AttachedProperty<DialogIconType> IconTypeProperty = AvaloniaProperty.RegisterAttached<Avalonia.Controls.Image, DialogIconType>("IconType", typeof(DialogElement), DialogIconType.None);
    /// <summary>
    /// Define property for type of separator in dialog.
    /// </summary>
    public static readonly AttachedProperty<DialogSeparatorType> SeparatorTypeProperty = AvaloniaProperty.RegisterAttached<Separator, DialogSeparatorType>("SeparatorType", typeof(DialogElement), DialogSeparatorType.None);
    /// <summary>
    /// Define property for role of text in dialog.
    /// </summary>
    public static readonly AttachedProperty<DialogTextRole> TextRoleProperty = AvaloniaProperty.RegisterAttached<TextBlock, DialogTextRole>("TextRole", typeof(DialogElement), DialogTextRole.None);
    
    
    /// <summary>
    /// Get the type of the icon in dialog.
    /// </summary>
    /// <param name="image"><see cref="Avalonia.Controls.Image"/> which show the icon.</param>
    /// <returns>The type of the icon.</returns>
    public static DialogIconType GetIconType(Avalonia.Controls.Image image) => 
        image.GetValue(IconTypeProperty);
    
    
    /// <summary>
    /// Get the type of the separator in dialog.
    /// </summary>
    /// <param name="separator">The <see cref="Separator"/>.</param>
    /// <returns>The type of the separator.</returns>
    public static DialogSeparatorType GetSeparatorType(Separator separator) => 
        separator.GetValue(SeparatorTypeProperty);
    
    
    /// <summary>
    /// Get the role of the text in dialog.
    /// </summary>
    /// <param name="textBlock"><see cref="TextBlock"/> which show the text.</param>
    /// <returns>The role of the text.</returns>
    public static DialogTextRole GetTextRole(TextBlock textBlock) => 
        textBlock.GetValue(TextRoleProperty);
    
    
    /// <summary>
    /// Set the type of the icon in dialog.
    /// </summary>
    /// <param name="image"><see cref="Avalonia.Controls.Image"/> which show the icon.</param>
    /// <param name="type">The type of the icon.</param>
    public static void SetIconType(Avalonia.Controls.Image image, DialogIconType type) => 
        image.SetValue(IconTypeProperty, type);
    
    
    /// <summary>
    /// Set the type of the separator in dialog.
    /// </summary>
    /// <param name="separator">The <see cref="Separator"/>.</param>
    /// <param name="type">The type of the separator.</param>
    public static void SetSeparatorType(Separator separator, DialogSeparatorType type) => 
        separator.SetValue(SeparatorTypeProperty, type);
    
    
    /// <summary>
    /// Set the role of the text in dialog.
    /// </summary>
    /// <param name="textBlock"><see cref="TextBlock"/> which show the text.</param>
    /// <param name="role">The role of the text.</param>
    public static void SetTextRole(TextBlock textBlock, DialogTextRole role) => 
        textBlock.SetValue(TextRoleProperty, role);
}


/// <summary>
/// Type of icon in dialog.
/// </summary>
public enum DialogIconType
{
    /// <summary>
    /// None.
    /// </summary>
    None,
    /// <summary>
    /// Icon with default size.
    /// </summary>
    Default,
    /// <summary>
    /// Icon with large size.
    /// </summary>
    Large,
    /// <summary>
    /// Icon for description below a label.
    /// </summary>
    DescriptionBelowLabel,
    /// <summary>
    /// Icon in the icon button.
    /// </summary>
    IconControlButton,
}


/// <summary>
/// Type of separator in dialog.
/// </summary>
public enum DialogSeparatorType
{
    /// <summary>
    /// None.
    /// </summary>
    None,
    /// <summary>
    /// Separator between items.
    /// </summary>
    Item,
    /// <summary>
    /// Separator between inner items.
    /// </summary>
    InnerItem,
    /// <summary>
    /// Separator with micro size.
    /// </summary>
    Micro,
    /// <summary>
    /// Separator with small size.
    /// </summary>
    Small,
    /// <summary>
    /// Separator with default size.
    /// </summary>
    Default,
    /// <summary>
    /// Separator with large size.
    /// </summary>
    Large,
}


/// <summary>
/// Role of text in dialog.
/// </summary>
public enum DialogTextRole
{
    /// <summary>
    /// None.
    /// </summary>
    None,
    /// <summary>
    /// Default.
    /// </summary>
    Default,
    /// <summary>
    /// Header of an items group.
    /// </summary>
    ItemsGroupHeader,
    /// <summary>
    /// Label.
    /// </summary>
    Label,
    /// <summary>
    /// Label above an input control.
    /// </summary>
    LabelAboveInputControl,
    /// <summary>
    /// Label next to an icon.
    /// </summary>
    LabelNextToIcon,
    /// <summary>
    /// Description below a checkbox.
    /// </summary>
    DescriptionBelowCheckBox,
    /// <summary>
    /// Description below a label.
    /// </summary>
    DescriptionBelowLabel,
    /// <summary>
    /// Description with error below a label.
    /// </summary>
    ErrorDescriptionBelowLabel,
    /// <summary>
    /// Description with warning below a label.
    /// </summary>
    WarningDescriptionBelowLabel,
}