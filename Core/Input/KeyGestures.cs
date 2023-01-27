using Avalonia.Input;

namespace CarinaStudio.AppSuite.Input;

/// <summary>
/// Predefined <see cref="KeyGesture"/>s which are suitable for current operating system.
/// </summary>
public static class KeyGestures
{
    // Static fields.
    static readonly KeyModifiers PrimaryKeyModifiers = Platform.IsMacOS ? KeyModifiers.Meta : KeyModifiers.Control;


    /// <summary>
    /// Copy.
    /// </summary>
    public static readonly KeyGesture Copy = new(Key.C, PrimaryKeyModifiers);
    /// <summary>
    /// Cut.
    /// </summary>
    public static readonly KeyGesture Cut = new(Key.X, PrimaryKeyModifiers);
    /// <summary>
    /// New file.
    /// </summary>
    public static readonly KeyGesture NewFile = new(Key.N, PrimaryKeyModifiers);
    /// <summary>
    /// Open file.
    /// </summary>
    public static readonly KeyGesture OpenFile = new(Key.O, PrimaryKeyModifiers);
    /// <summary>
    /// Paste.
    /// </summary>
    public static readonly KeyGesture Paste = new(Key.V, PrimaryKeyModifiers);
    /// <summary>
    /// Save file.
    /// </summary>
    public static readonly KeyGesture SaveFile = new(Key.S, PrimaryKeyModifiers);
    /// <summary>
    /// Select all.
    /// </summary>
    public static readonly KeyGesture SelectAll = new(Key.A, PrimaryKeyModifiers);
}