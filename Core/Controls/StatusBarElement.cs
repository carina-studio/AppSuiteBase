using Avalonia;
using Avalonia.Controls;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Provide attached properties for elements in status bar.
/// </summary>
public static class StatusBarElement
{
    /// <summary>
    /// Property to indicate the role of element in status bar.
    /// </summary>
    public static readonly AttachedProperty<StatusBarElementRole> RoleProperty = AvaloniaProperty.RegisterAttached<Control, StatusBarElementRole>("Role", typeof(StatusBarElement), StatusBarElementRole.None);
    
    
    /// <summary>
    /// Get the role of the element in status bar.
    /// </summary>
    /// <param name="control">The element control.</param>
    /// <returns>The role of the element.</returns>
    public static StatusBarElementRole GetRole(Control control) => 
        control.GetValue(RoleProperty);


    /// <summary>
    /// Set the role of the element in status bar.
    /// </summary>
    /// <param name="control">The element control.</param>
    /// <param name="role">The role of the element.</param>
    public static void SetRole(Control control, StatusBarElementRole role) =>
        control.SetValue(RoleProperty, role);
}


/// <summary>
/// Role of element in status bar.
/// </summary>
public enum StatusBarElementRole
{
    /// <summary>
    /// None.
    /// </summary>
    None,
    /// <summary>
    /// Button.
    /// </summary>
    Button,
    /// <summary>
    /// Drop-down icon.
    /// </summary>
    DropDownIcon,
    /// <summary>
    /// Icon.
    /// </summary>
    Icon,
    /// <summary>
    /// Icon with label.
    /// </summary>
    IconWithLabel,
    /// <summary>
    /// Label.
    /// </summary>
    Label,
    /// <summary>
    /// Separator.
    /// </summary>
    Separator,
}