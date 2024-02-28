using CarinaStudio.Controls;
using CarinaStudio.Threading;
using System;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to show processing indicator.
/// </summary>
public class ProcessingDialog : CommonDialog<object?>
{
    // Fields.
    ProcessingDialogImpl? dialog;
    bool isCancellable;
    object? message;
    
    
    /// <summary>
    /// Raised when cancellation has been requested by user.
    /// </summary>
    public event EventHandler? CancellationRequested;


    /// <summary>
    /// Complete and close the dialog.
    /// </summary>
    public void Complete()
    {
        this.VerifyAccess();
        if (dialog is not null)
            dialog.Complete();
        else
            throw new InvalidOperationException("Dialog is not showing.");
    }


    /// <summary>
    /// Get or set whether cancellation of the processing can be requested by user or not.
    /// </summary>
    public bool IsCancellable
    {
        get => this.isCancellable;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.isCancellable = value;
        }
    }
    
    
    /// <summary>
    /// Get or set message shown in dialog.
    /// </summary>
    public object? Message
    {
        get => this.message;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.message = value;
        }
    }
    
    
    // Called when cancellation requested by user.
    void OnCancellationRequested(object? sender, EventArgs e) =>
        this.CancellationRequested?.Invoke(this, e);
    
    
    /// <summary>
    /// Show dialog.
    /// </summary>
    /// <param name="owner">Owner window.</param>
    /// <returns>Task to showing dialog.</returns>
    public new Task ShowDialog(Avalonia.Controls.Window? owner) => 
        base.ShowDialog(owner);
    
    
    /// <inheritdoc/>
    protected override async Task<object?> ShowDialogCore(Avalonia.Controls.Window? owner)
    {
        var messageBindingToken = default(IDisposable);
        this.dialog = new ProcessingDialogImpl().Also(it =>
        {
            messageBindingToken = this.BindValueToDialog(it, ProcessingDialogImpl.MessageProperty, this.message);
            it.CancellationRequested += this.OnCancellationRequested;
            it.IsCancellable = this.isCancellable;
            it.Topmost = owner?.Topmost ?? true;
        });
        try
        {
            await (owner is not null
                ? this.dialog.ShowDialog(owner)
                : this.dialog.ShowDialog<object?>());
        }
        finally
        {
            this.dialog = this.dialog?.Let(it =>
            {
                messageBindingToken?.Dispose();
                it.CancellationRequested -= this.OnCancellationRequested;
                return default(ProcessingDialogImpl);
            });
        }
        return null;
    }
}