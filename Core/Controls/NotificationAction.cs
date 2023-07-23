using Avalonia;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Action of <see cref="Notification"/>.
/// </summary>
public class NotificationAction : AvaloniaObject
{
    /// <summary>
    /// Property of <see cref="Command"/>.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty.Register<NotificationAction, ICommand?>(nameof(Command));
    /// <summary>
    /// Property of <see cref="CommandParameter"/>.
    /// </summary>
    public static readonly StyledProperty<object?> CommandParameterProperty = AvaloniaProperty.Register<NotificationAction, object?>(nameof(CommandParameter));
    /// <summary>
    /// Property of <see cref="Name"/>.
    /// </summary>
    public static readonly StyledProperty<string?> NameProperty = AvaloniaProperty.Register<NotificationAction, string?>(nameof(Name));
    
    
    /// <summary>
    /// Get or set command of action.
    /// </summary>
    public ICommand? Command
    {
        get => this.GetValue(CommandProperty);
        set => this.SetValue(CommandProperty, value);
    }
    
    
    /// <summary>
    /// Get or set command parameter of action.
    /// </summary>
    public object? CommandParameter
    {
        get => this.GetValue(CommandParameterProperty);
        set => this.SetValue(CommandParameterProperty, value);
    }
    
    
    /// <summary>
    /// Get or set name of action.
    /// </summary>
    public string? Name
    {
        get => this.GetValue(NameProperty);
        set => this.SetValue(NameProperty, value);
    }
}