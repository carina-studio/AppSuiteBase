using Avalonia;
using Avalonia.Controls;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Attached properties for elements in toolbar.
/// </summary>
public static class ToolbarElement
{
    /// <summary>
    /// Property to indicate the role of element in toolbar.
    /// </summary>
    public static readonly AttachedProperty<ToolbarElementRole> RoleProperty = AvaloniaProperty.RegisterAttached<Control, ToolbarElementRole>("Role", typeof(ToolbarElement), ToolbarElementRole.None);
    
    
    /// <summary>
    /// Get the role of the element in toolbar.
    /// </summary>
    /// <param name="control">The element control.</param>
    /// <returns>The role of the element.</returns>
    public static ToolbarElementRole GetRole(Control control) => 
        control.GetValue(RoleProperty);


    /// <summary>
    /// Set the role of the element in toolbar.
    /// </summary>
    /// <param name="control">The element control.</param>
    /// <param name="role">The role of the element.</param>
    public static void SetRole(Control control, ToolbarElementRole role) =>
        control.SetValue(RoleProperty, role);
}


/// <summary>
/// Role of element in toolbar.
/// </summary>
public enum ToolbarElementRole
{
    /// <summary>
    /// None.
    /// </summary>
    None,
    /// <summary>
    /// Badge.
    /// </summary>
    Badge,
    /// <summary>
    /// Button.
    /// </summary>
    Button,
    /// <summary>
    /// Button with drop-down icon.
    /// </summary>
    ButtonWithDropDownIcon,
    /// <summary>
    /// Drop-down icon.
    /// </summary>
    DropDownIcon,
    /// <summary>
    /// Horizontal separator.
    /// </summary>
    HorizontalSeparator,
    /// <summary>
    /// Icon.
    /// </summary>
    Icon,
    /// <summary>
    /// Input control.
    /// </summary>
    Input,
    /// <summary>
    /// Label.
    /// </summary>
    Label,
    /// <summary>
    /// Progress ring.
    /// </summary>
    ProgressRing,
    /// <summary>
    /// Vertical separator.
    /// </summary>
    Separator,
    /// <summary>
    /// Button with smaller size.
    /// </summary>
    SmallButton,
    /// <summary>
    /// Horizontal separator with smaller size.
    /// </summary>
    SmallHorizontalSeparator,
    /// <summary>
    /// Label with smaller size.
    /// </summary>
    SmallLabel,
    /// <summary>
    /// Vertical separator with smaller size.
    /// </summary>
    SmallSeparator,
}