using Avalonia;
using Avalonia.Controls;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// State of indicator of inserting item.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class ItemInsertionIndicator
{
    /// <summary>
    /// Property to get or set whether indicator of inserting item after current item is visible or not.
    /// </summary>
    public static readonly AttachedProperty<bool> IsInsertingItemAfterProperty = AvaloniaProperty.RegisterAttached<ItemInsertionIndicator, Control, bool>("IsInsertingItemAfter");
    /// <summary>
    /// Property to get or set whether indicator of inserting item before current item is visible or not.
    /// </summary>
    public static readonly AttachedProperty<bool> IsInsertingItemBeforeProperty = AvaloniaProperty.RegisterAttached<ItemInsertionIndicator, Control, bool>("IsInsertingItemBefore");


    // Constructor.
    ItemInsertionIndicator()
    { }


    /// <summary>
    /// Clear insertion state from all realized containers in given <see cref="ItemsControl"/>.
    /// </summary>
    /// <param name="itemsControl"><see cref="ItemsControl"/>.</param>
    public static void ClearInsertingItems(ItemsControl itemsControl)
    {
        foreach (var container in itemsControl.GetRealizedContainers())
        {
            container.SetValue(IsInsertingItemAfterProperty, false);
            container.SetValue(IsInsertingItemBeforeProperty, false);
        }
    }


    /// <summary>
    /// Get whether indicator of inserting item after current item is visible or not.
    /// </summary>
    /// <param name="control"><see cref="Control"/>.</param>
    /// <returns>True if indicator is visible.</returns>
    public static bool IsInsertingItemAfter(Control control) =>
        control.GetValue(IsInsertingItemAfterProperty);


    /// <summary>
    /// Get whether indicator of inserting item before current item is visible or not.
    /// </summary>
    /// <param name="control"><see cref="Control"/>.</param>
    /// <returns>True if indicator is visible.</returns>
    public static bool IsInsertingItemBefore(Control control) =>
        control.GetValue(IsInsertingItemBeforeProperty);


    /// <summary>
    /// Set whether indicator of inserting item after current item is visible or not.
    /// </summary>
    /// <param name="control"><see cref="Control"/>.</param>
    /// <param name="isInserting">Is inserting item after or not.</param>
    public static void SetInsertingItemAfter(Control control, bool isInserting) =>
        control.SetValue(IsInsertingItemAfterProperty, isInserting);


    /// <summary>
    /// Set whether indicator of inserting item before current item is visible or not.
    /// </summary>
    /// <param name="control"><see cref="Control"/>.</param>
    /// <param name="isInserting">Is inserting item before or not.</param>
    public static void SetInsertingItemBefore(Control control, bool isInserting) =>
        control.SetValue(IsInsertingItemBeforeProperty, isInserting);
}
