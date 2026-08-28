namespace CarinaStudio.AppSuite.Native;

/// <summary>
/// Constants that specify the style of a window's toolbar.
/// </summary>
/// <remarks>Backport of <c>NSWindowToolbarStyle</c>, drop it once this branch can take <c>CarinaStudio.AppBase.MacOS</c> 2.4.4 or later.</remarks>
enum NSWindowToolbarStyle
{
    /// <summary>
    /// The system determines the style to use.
    /// </summary>
    Automatic = 0,
    /// <summary>
    /// The toolbar is shown below the title, with the title centered above it.
    /// </summary>
    Expanded = 1,
    /// <summary>
    /// The toolbar is shown below the title with toolbar items centered, as in a preferences window.
    /// </summary>
    Preference = 2,
    /// <summary>
    /// The toolbar is shown in the title bar area.
    /// </summary>
    Unified = 3,
    /// <summary>
    /// The toolbar is shown in the title bar area with reduced height.
    /// </summary>
    UnifiedCompact = 4,
}
