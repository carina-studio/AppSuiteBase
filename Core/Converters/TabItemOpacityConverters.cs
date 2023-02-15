using Avalonia.Data.Converters;

namespace CarinaStudio.AppSuite.Converters;

static class TabItemOpacityConverters
{
    /// <summary>
    /// Convert from IsPointerOver to Opacity.
    /// </summary>
    public static readonly IValueConverter IsPointerOverToOpacity = new FuncValueConverter<bool, double>(isPointerOver =>
        isPointerOver ? 1 : 0);
    

    /// <summary>
    /// Convert from IsSelected to Opacity.
    /// </summary>
    public static readonly IValueConverter IsSelectedToOpacity = new FuncValueConverter<bool, double>(isSelected =>
        isSelected ? 1 : 0);
}