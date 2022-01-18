using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    class MacOSToolTipHelper
    {
        // Fields.
        readonly Control control;
        readonly Observer<bool> isActiveObserver;
        IDisposable? isActiveObserverToken;
        Avalonia.Controls.Window? window;


        // Constructor.
        public MacOSToolTipHelper(Control control)
        {
            // close tooltip first
            ToolTip.SetIsOpen(control, false);

            // monitor current window
            this.control = control;
            this.window = control.FindAncestorOfType<Avalonia.Controls.Window>();
            this.isActiveObserver = new Observer<bool>(isActive =>
            {
                if (!isActive)
                    ToolTip.SetIsOpen(this.control, false);
            });
            this.isActiveObserverToken = this.window?.GetObservable(Avalonia.Controls.Window.IsActiveProperty)?.Subscribe(this.isActiveObserver);

            // monitor window change
            control.AttachedToVisualTree += (sender, e) =>
            {
                var newWindow = control.FindAncestorOfType<Avalonia.Controls.Window>();
                if (newWindow == window)
                    return;
                this.isActiveObserverToken?.Dispose();
                this.window = newWindow;
                this.isActiveObserverToken = newWindow?.GetObservable(Avalonia.Controls.Window.IsActiveProperty)?.Subscribe(this.isActiveObserver);
                ToolTip.SetIsOpen(control, false);
            };
            control.GetObservable(ToolTip.IsOpenProperty).Subscribe(isOpen =>
            {
                if (isOpen && this.window?.IsActive == false)
                    ToolTip.SetIsOpen(control, false);
            });
        }
    }
}