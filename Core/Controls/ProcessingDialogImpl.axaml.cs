using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to show processing indicator.
/// </summary>
class ProcessingDialogImpl : Dialog
{
    /// <summary>
    /// Define <see cref="ActualMessage"/> property.
    /// </summary>
    public static readonly DirectProperty<ProcessingDialogImpl, string?> ActualMessageProperty = AvaloniaProperty.RegisterDirect<ProcessingDialogImpl, string?>(nameof(ActualMessage), d => d.actualMessage);
    /// <summary>
    /// Define <see cref="IsCancellable"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsCancellableProperty = AvaloniaProperty.Register<ProcessingDialogImpl, bool>(nameof(IsCancellable), false);
    /// <summary>
    /// Define <see cref="IsCancellationRequested"/> property.
    /// </summary>
    public static readonly DirectProperty<ProcessingDialogImpl, bool> IsCancellationRequestedProperty = AvaloniaProperty.RegisterDirect<ProcessingDialogImpl, bool>(nameof(IsCancellationRequested), d => d.isCancellationRequested);
    /// <summary>
    /// Define <see cref="Message"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> MessageProperty = AvaloniaProperty.Register<ProcessingDialogImpl, string?>(nameof(Message), null);
    
    
    // Fields.
    string? actualMessage;
    readonly CachedResource<string?> defaultMessage;
    bool isCancellationRequested;
    bool isCompleted;
    
    
    // Constructor.
    public ProcessingDialogImpl()
    {
        this.defaultMessage = new(this, "String/Common.Processing");
        AvaloniaXamlLoader.Load(this);
    }


    /// <summary>
    /// Get the actual message to show.
    /// </summary>
    public string? ActualMessage => this.actualMessage;


    /// <summary>
    /// Request cancelling the processing.
    /// </summary>
    public void Cancel()
    {
        if (!isCancellationRequested && this.GetValue(IsCancellableProperty))
        {
            this.SetAndRaise(IsCancellationRequestedProperty, ref this.isCancellationRequested, true);
            this.CancellationRequested?.Invoke(this, EventArgs.Empty);
        }
    }


    /// <summary>
    /// Raised when cancellation has been requested by user.
    /// </summary>
    public event EventHandler? CancellationRequested;
    
    
    /// <summary>
    /// Complete the processing and close dialog.
    /// </summary>
    public void Complete()
    {
        this.isCompleted = true;
        this.Close();
    }
    
    
    /// <summary>
    /// Get or set whether cancellation of the processing can be requested by user or not.
    /// </summary>
    public bool IsCancellable
    {
        get => this.GetValue(IsCancellableProperty);
        set => this.SetValue(IsCancellableProperty, value);
    }


    /// <summary>
    /// Check whether cancellation has been requested by user or not.
    /// </summary>
    public bool IsCancellationRequested => this.isCancellationRequested;


    /// <summary>
    /// Get or set the message.
    /// </summary>
    public string? Message
    {
        get => this.GetValue(MessageProperty);
        set => this.SetValue(MessageProperty, value);
    }


    /// <inheritdoc/>.
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!isCompleted)
            e.Cancel = true;
        base.OnClosing(e);
    }


    /// <inheritdoc/>.
    protected override void OnOpening(EventArgs e)
    {
        base.OnOpening(e);
        if (this.ActualTransparencyLevel != WindowTransparencyLevel.Transparent
            && this.Content is Border rootBorder)
        {
            rootBorder.CornerRadius = default;
        }
        this.SelectActualMessage();
    }


    /// <inheritdoc/>.
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == MessageProperty)
        {
            if (this.IsOpened)
                this.SelectActualMessage();
        }
    }


    // Select actual message to be shown.
    void SelectActualMessage()
    {
        var message = this.GetValue(MessageProperty);
        if (string.IsNullOrEmpty(message))
            message = this.defaultMessage.Value;
        this.SetAndRaise(ActualMessageProperty, ref this.actualMessage, message);
    }
}
