using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Extension methods for <see cref="NativeMenuItem"/>.
/// </summary>
public static class NativeMenuItemExtensions
{
    /// <summary>
    /// Set empty icon to the menu item.
    /// </summary>
    /// <param name="menuItem"><see cref="NativeMenuItem"/>.</param>
    public static void UseEmptyIcon(this NativeMenuItem menuItem) =>
        menuItem.Icon = IAppSuiteApplication.CurrentOrNull?.FindResourceOrDefault<Bitmap>("Bitmap/Empty");
}