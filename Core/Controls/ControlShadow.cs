using Avalonia;
using Avalonia.Controls;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Provide attached properties for control shadow.
/// </summary>
public static class ControlShadow
{
    /// <summary>
    /// Define property for position of shadow.
    /// </summary>
    public static readonly AttachedProperty<ControlShadowPosition> PositionProperty = AvaloniaProperty.RegisterAttached<Control, ControlShadowPosition>("Position", typeof(ControlShadow), ControlShadowPosition.None);
    /// <summary>
    /// Define property for source of shadow.
    /// </summary>
    public static readonly AttachedProperty<ControlShadowSource> SourceProperty = AvaloniaProperty.RegisterAttached<Control, ControlShadowSource>("Source", typeof(ControlShadow), ControlShadowSource.Default);
    
    
    /// <summary>
    /// Get the position of the shadow.
    /// </summary>
    /// <param name="control">The control to render the shadow.</param>
    /// <returns>The position of the shadow</returns>
    public static ControlShadowPosition GetPosition(Control control) => 
        control.GetValue(PositionProperty);
    
    
    /// <summary>
    /// Get the source of the shadow.
    /// </summary>
    /// <param name="control">The control to render the shadow.</param>
    /// <returns>The source of the shadow</returns>
    public static ControlShadowSource GetSource(Control control) => 
        control.GetValue(SourceProperty);
    
    
    /// <summary>
    /// Set the position of the shadow.
    /// </summary>
    /// <param name="control">The control to render the shadow.</param>
    /// <param name="position">The position.</param>
    public static void SetPosition(Control control, ControlShadowPosition position) => 
        control.SetValue(PositionProperty, position);
    
    
    /// <summary>
    /// Set the source of the shadow.
    /// </summary>
    /// <param name="control">The control to render the shadow.</param>
    /// <param name="source">The source.</param>
    public static void SetSource(Control control, ControlShadowSource source) => 
        control.SetValue(SourceProperty, source);
}


/// <summary>
/// Position of shadow for control.
/// </summary>
public enum ControlShadowPosition
{
    /// <summary>
    /// None.
    /// </summary>
    None,
    /// <summary>
    /// Bottom of control.
    /// </summary>
    Bottom,
    /// <summary>
    /// Left of control.
    /// </summary>
    Left,
    /// <summary>
    /// Right of control.
    /// </summary>
    Right,
    /// <summary>
    /// Top of control.
    /// </summary>
    Top,
}


/// <summary>
/// Source of shadow for control.
/// </summary>
public enum ControlShadowSource
{
    /// <summary>
    /// Default control.
    /// </summary>
    Default,
    /// <summary>
    /// Panel for control buttons in dialog.
    /// </summary>
    ControlButtonsPanel,
    /// <summary>
    /// Status bar.
    /// </summary>
    StatusBar,
    /// <summary>
    /// Toolbar,
    /// </summary>
    Toolbar,
    /// <summary>
    /// Panel in working area.
    /// </summary>
    WorkingAreaPanel,
}