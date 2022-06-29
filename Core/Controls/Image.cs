using Avalonia;
using Avalonia.Media;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Extended <see cref="Image"/>.
/// </summary>
public class Image : Avalonia.Controls.Image
{
    /// <inheritdoc/>
    protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourceProperty)
        {
            (change.OldValue.Value as AvaloniaObject)?.Let(it => 
            {
                it.PropertyChanged -= this.OnSourcePropertyChanged;
                if (it is DrawingImage drawingImage)
                    drawingImage.Drawing.PropertyChanged -= this.OnSourcePropertyChanged;
            });
            (change.NewValue.Value as AvaloniaObject)?.Let(it => 
            {
                it.PropertyChanged += this.OnSourcePropertyChanged;
                if (it is DrawingImage drawingImage)
                    drawingImage.Drawing.PropertyChanged += this.OnSourcePropertyChanged;
            });
        }
    }


    // Called when property of source changed.
    void OnSourcePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) =>
        this.InvalidateVisual();
}