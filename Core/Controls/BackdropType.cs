namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Type of backdrop drawn by a <see cref="Backdrop"/>.
/// </summary>
public enum BackdropType
{
    /// <summary>
    /// Draw the backdrop with a blur effect.
    /// </summary>
    Blur,
    /// <summary>
    /// Draw the backdrop with a blur effect while sampling a larger region behind the <see cref="Backdrop"/> to produce a lens-like effect.
    /// </summary>
    LensBlur,
    /// <summary>
    /// Draw nothing.
    /// </summary>
    None,
}
