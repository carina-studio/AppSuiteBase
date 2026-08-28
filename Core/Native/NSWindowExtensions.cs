using CarinaStudio.MacOS.AppKit;
using CarinaStudio.MacOS.ObjectiveC;

namespace CarinaStudio.AppSuite.Native;

/// <summary>
/// Extensions for <see cref="NSWindow"/>.
/// </summary>
/// <remarks>Backport of members which are unavailable in <c>CarinaStudio.AppBase.MacOS</c> 2.3.3, drop it once this branch can take 2.4.4 or later.</remarks>
static class NSWindowExtensions
{
    // Static fields.
    static readonly Class? NSWindowClass = Platform.IsMacOS
        ? Class.GetClass("NSWindow").AsNonNull()
        : null;
    static Property? ToolbarStyleProperty;


    /// <summary>
    /// Set the style of toolbar of the window.
    /// </summary>
    /// <param name="window"><see cref="NSWindow"/>.</param>
    /// <param name="style">Style of toolbar.</param>
    public static void SetToolbarStyle(this NSWindow window, NSWindowToolbarStyle style)
    {
        ToolbarStyleProperty ??= NSWindowClass!.GetProperty("toolbarStyle").AsNonNull();
        window.SetProperty(ToolbarStyleProperty, (int)style);
    }
}
