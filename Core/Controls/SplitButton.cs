using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Extended <see cref="Avalonia.Controls.SplitButton"/>.
/// </summary>
public class SplitButton: Avalonia.Controls.SplitButton
{
    /// <summary>
    /// Define <see cref="DropDownMenu"/> property.
    /// </summary>
    public static readonly StyledProperty<ContextMenu?> DropDownMenuProperty = AvaloniaProperty.Register<SplitButton, ContextMenu?>(nameof(DropDownMenu));
    /// <summary>
    /// Define <see cref="IsDropDownOpen"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsDropDownOpenProperty = AvaloniaProperty.Register<SplitButton, bool>(nameof(IsDropDownOpen));
    /// <summary>
    /// Define <see cref="SecondaryButtonClick"/> event.
    /// </summary>
    public static readonly RoutedEvent SecondaryButtonClickEvent = RoutedEvent.Register<SplitButton, RoutedEventArgs>(nameof(SecondaryButtonClick), RoutingStrategies.Direct);
    
    
    // Fields.
    IDisposable? dropDownMenuMinWidthValueToken;


    /**
     * Get or set menu to be opened when clicking the secondary button.
     */
    public ContextMenu? DropDownMenu
    {
        get => this.GetValue(DropDownMenuProperty);
        set => this.SetValue(DropDownMenuProperty, value);
    }
    
    
    /**
     * Get or set whether <see cref="DropDownMenu"/> or manual controlled drop down control is open or not.
     */
    public bool IsDropDownOpen
    {
        get => this.GetValue(IsDropDownOpenProperty);
        set => this.SetValue(IsDropDownOpenProperty, value);
    } 


    /// <inheritdoc/>
    protected override void OnClickSecondary(RoutedEventArgs? e)
    {
        // raise event
        var clickArgs = new RoutedEventArgs(SecondaryButtonClickEvent);
        this.RaiseEvent(clickArgs);
        if (clickArgs.Handled)
            return;

        // open menu or flyout
        if (!this.OpenDropDownMenu())
            base.OnClickSecondary(e);
    }
    
    
    // Called when drop down menu closed.
    void OnDropDownMenuClosed(object? sender, RoutedEventArgs e) =>
        this.SetValue(IsDropDownOpenProperty, false);
    
    
    // Called when drop down menu opened.
    void OnDropDownMenuOpened(object? sender, RoutedEventArgs e) =>
        this.SetValue(IsDropDownOpenProperty, true);


    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == DropDownMenuProperty)
        {
            if (e.OldValue is ContextMenu oldMenu)
            {
                oldMenu.Closed -= this.OnDropDownMenuClosed;
                oldMenu.Opened -= this.OnDropDownMenuOpened;
            }
            if (e.NewValue is ContextMenu newMenu)
            {
                newMenu.Closed += this.OnDropDownMenuClosed;
                newMenu.Opened += this.OnDropDownMenuOpened;
            }
            this.UpdateDropDownMenuMinWidth();
        }
        else if (e.Property == IsDropDownOpenProperty)
        {
            if (this.GetValue(DropDownMenuProperty) is not { } menu)
                return;
            if ((bool)e.NewValue!)
            {
                if (this.OpenDropDownMenu())
                    this.PseudoClasses.Add(":flyout-open");
            }
            else
            {
                menu.Close();
                this.PseudoClasses.Remove(":flyout-open");
            }
        }
    }


    /// <inheritdoc/>
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        this.UpdateDropDownMenuMinWidth();
    }


    /// <summary>
    /// Open the <see cref="DropDownMenu"/>.
    /// </summary>
    /// <returns>True if drop down menu has been opened.</returns>
    public bool OpenDropDownMenu()
    {
        this.VerifyAccess();
        if (this.GetValue(DropDownMenuProperty) is { } menu)
        {
            if (!menu.IsOpen)
            {
                this.UpdateDropDownMenuMinWidth(true);
                menu.Placement = PlacementMode.Bottom;
                menu.PlacementTarget = this;
                menu.Open(this);
            }
            return true;
        }
        return false;
    }


    /// <summary>
    /// Raised when user clicked the secondary button.
    /// </summary>
    public event EventHandler<RoutedEventArgs> SecondaryButtonClick
    {
        add => this.AddHandler(SecondaryButtonClickEvent, value);
        remove => this.RemoveHandler(SecondaryButtonClickEvent, value);
    }


    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(Avalonia.Controls.SplitButton);
    
    
    // Update min width of drop down menu.
    void UpdateDropDownMenuMinWidth(bool forceUpdate = false)
    {
        this.dropDownMenuMinWidthValueToken?.Dispose();
        this.dropDownMenuMinWidthValueToken = this.GetValue(DropDownMenuProperty)?.Let(menu =>
        {
            if (menu.IsOpen || forceUpdate)
            {
                var shadowMargin = IAppSuiteApplication.CurrentOrNull?.FindResourceOrDefault<Thickness>("Thickness/Popup.Shadow.Margin") ?? default;
                return menu.SetValue(MinWidthProperty, this.Bounds.Width + shadowMargin.Left + shadowMargin.Right, BindingPriority.Template);
            }
            return null;
        });
    }
}