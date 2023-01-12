using Avalonia;
using Avalonia.Media;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Extended <see cref="Image"/>.
/// </summary>
public class Image : Avalonia.Controls.Image
{
    // Fields.
    AvaloniaObject? attachedSource;
    bool isAttachedToVisualTree;


    /// <summary>
    /// Initialize new <see cref="Image"/> instance.
    /// </summary>
    public Image()
    {
        this.GetObservable(SourceProperty).Subscribe(source =>
        {
            if (this.attachedSource != null)
            {
                this.attachedSource.PropertyChanged -= this.OnSourcePropertyChanged;
                if (this.attachedSource is DrawingImage drawingImage)
                    drawingImage.Drawing.PropertyChanged -= this.OnSourcePropertyChanged;
            }
            this.attachedSource = (source as AvaloniaObject);
            if (this.attachedSource != null && this.isAttachedToVisualTree)
            {
                this.attachedSource.PropertyChanged += this.OnSourcePropertyChanged;
                if (this.attachedSource is DrawingImage drawingImage)
                    drawingImage.Drawing.PropertyChanged += this.OnSourcePropertyChanged;
            }
        });
    }


    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.isAttachedToVisualTree = true;
        if (this.attachedSource != null)
        {
            this.attachedSource.PropertyChanged += this.OnSourcePropertyChanged;
            if (this.attachedSource is DrawingImage drawingImage)
                drawingImage.Drawing.PropertyChanged += this.OnSourcePropertyChanged;
        }
    }


    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        this.isAttachedToVisualTree = false;
        if (this.attachedSource != null)
        {
            this.attachedSource.PropertyChanged -= this.OnSourcePropertyChanged;
            if (this.attachedSource is DrawingImage drawingImage)
                drawingImage.Drawing.PropertyChanged -= this.OnSourcePropertyChanged;
        }
        base.OnDetachedFromVisualTree(e);
    }


    // Called when property of source changed.
    void OnSourcePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) =>
        this.InvalidateVisual();
}