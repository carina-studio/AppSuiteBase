namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// State of progress shown on taskbar icon.
/// </summary>
public enum TaskbarIconProgressState
{
    /// <summary>
    /// No progress.
    /// </summary>
    None = 0,
    /// <summary>
    /// Indeterminate.
    /// </summary>
    Indeterminate = 1,
    /// <summary>
    /// Normal progress.
    /// </summary>
    Normal = 2,
    /// <summary>
    /// Error.
    /// </summary>
    Error = 4,
    /// <summary>
    /// Paused.
    /// </summary>
    Paused = 8,
}
