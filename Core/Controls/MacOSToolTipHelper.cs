using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;

namespace CarinaStudio.AppSuite.Controls;

class MacOSToolTipHelper
{
    // Fields.
    readonly WeakReference<Control> controlRef;
    readonly Observer<bool> isWindowActiveObserver;
    IDisposable? isWindowActiveObserverToken;
    WeakReference<Avalonia.Controls.Window>? windowRef;


    // Constructor.
    public MacOSToolTipHelper(Control control)
    {
        // close tooltip first
        ToolTip.SetIsOpen(control, false);

        // monitor current window
        this.controlRef = new(control);
        this.isWindowActiveObserver = new Observer<bool>(isActive =>
        {
            if (!isActive)
                Dispatcher.UIThread.Post(this.CloseToolTip, DispatcherPriority.Background);
        });
        (TopLevel.GetTopLevel(control) as Window)?.Let(window =>
        {
            this.windowRef = new(window);
            this.isWindowActiveObserverToken = window.GetObservable(WindowBase.IsActiveProperty).Subscribe(this.isWindowActiveObserver);
        });

        // monitor window change
        control.AttachedToVisualTree += this.OnControlAttachedToVisualTree;
        control.DetachedFromVisualTree += this.OnControlDetachedFromVisualTree;
        control.GetObservable(ToolTip.IsOpenProperty).Subscribe(isOpen =>
        {
            if (isOpen)
                Dispatcher.UIThread.Post(this.CloseToolTip, DispatcherPriority.Background);
        });
    }


    // Close tool tip if needed.
    void CloseToolTip()
    {
        if (this.windowRef?.TryGetTarget(out var window) == true 
            && !window.IsActive
            && this.controlRef.TryGetTarget(out var control))
        {
            ToolTip.SetIsOpen(control, false);
        }
    }


    // Called when control attached to visual tree.
    void OnControlAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (!this.controlRef.TryGetTarget(out var control))
            return;
        Avalonia.Controls.Window? window = null;
        this.windowRef?.TryGetTarget(out window);
        var newWindow = control.FindAncestorOfType<Avalonia.Controls.Window>();
        if (ReferenceEquals(newWindow, window))
            return;
        this.isWindowActiveObserverToken?.Dispose();
        if (newWindow is null)
            this.windowRef = null;
        else
            this.windowRef = new(newWindow);
        this.isWindowActiveObserverToken = newWindow?.GetObservable(WindowBase.IsActiveProperty).Subscribe(this.isWindowActiveObserver);
        ToolTip.SetIsOpen(control, false);
    }
    
    
    // Called when control detached from visual tree.
    void OnControlDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        this.isWindowActiveObserverToken?.Dispose();
        this.windowRef = null;
    }
}