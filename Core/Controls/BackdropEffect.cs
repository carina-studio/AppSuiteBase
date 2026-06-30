namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Effect applied to the backdrop drawn by a <see cref="Backdrop"/>.
/// </summary>
public enum BackdropEffect
{
    /// <summary>
    /// Draw nothing.
    /// </summary>
    None,
    /// <summary>
    /// Draw the backdrop with a blur effect.
    /// </summary>
    Blur,
    /// <summary>
    /// Draw the backdrop with a blur effect while sampling a larger region behind the <see cref="Backdrop"/> to produce a lens-like effect.
    /// </summary>
    LensBlur,
}
