using Avalonia;
using Avalonia.Controls;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Provide attached properties for dialog elements.
/// </summary>
public static class DialogElement
{
    /// <summary>
    /// Define property for role of text in dialog.
    /// </summary>
    public static readonly AttachedProperty<DialogTextRole> TextRoleProperty = AvaloniaProperty.RegisterAttached<TextBlock, DialogTextRole>("TextRole", typeof(DialogElement), DialogTextRole.None);
    
    
    /// <summary>
    /// Get the role of the text in dialog.
    /// </summary>
    /// <param name="textBlock"><see cref="TextBlock"/> which show the text.</param>
    /// <returns>The role of the text.</returns>
    public static DialogTextRole GetTextRole(TextBlock textBlock) => 
        textBlock.GetValue(TextRoleProperty);
    
    
    /// <summary>
    /// Set the role of the text in dialog.
    /// </summary>
    /// <param name="textBlock"><see cref="TextBlock"/> which show the text.</param>
    /// <param name="value">The role of the text.</param>
    public static void SetTextRole(TextBlock textBlock, DialogTextRole value) => 
        textBlock.SetValue(TextRoleProperty, value);
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