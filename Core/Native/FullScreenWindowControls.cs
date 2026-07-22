using CarinaStudio.MacOS.AppKit;
using CarinaStudio.MacOS.ObjectiveC;
using System;

namespace CarinaStudio.AppSuite.Native;

/// <summary>
/// Controller to show standard window buttons inside content of window in full-screen mode on macOS.
/// </summary>
/// <remarks>
/// When a window enters full-screen mode, system relocates its title bar with standard window buttons into
/// an auto-hiding overlay window (NSToolbarFullScreenWindow). The controller hides the content of that overlay
/// window and shows a new set of standard window buttons at top-left corner of content of window instead, so
/// the buttons are always visible inside the title bar area drawn by application.
/// </remarks>
class FullScreenWindowControls : IDisposable
{
    // Constants.
    const string ButtonsViewClassName = "CarinaStudioFullScreenWindowControlsView";
    const double FallbackButtonLeftInset = 13;
    const double FallbackThickTitleBarHeight = 38;
    const double MinThickTitleBarHeight = 30;
    const string OverlayWindowClassName = "NSToolbarFullScreenWindow";


    // Static fields.
    static Class? BezierPathClass;
    static Selector? BezierPathWithOvalSelector;
    static Class? ButtonsViewClass;
    static NSRect CapturedCloseButtonFrame;
    static NSRect CapturedMiniaturizeButtonFrame;
    static double CapturedThickTitleBarHeight;
    static NSRect CapturedZoomButtonFrame;
    static Selector? FillSelector;
    static Selector? SetFillSelector;
    static Selector? SetFrameSelector;
    static Selector? SetHighlightedSelector;
    static Selector? WindowDidEnterFullScreenSelector;
    static Selector? WindowWillEnterFullScreenSelector;
    static Selector? WindowWillExitFullScreenSelector;


    // Fields.
    readonly NSView buttonsView;
    NSControl? closeButton;
    bool isDisposed;
    bool isMouseInButtonGroup;
    NSControl? miniaturizeButton;
    NSWindow? overlayWindow;
    NSTrackingArea? trackingArea;
    readonly NSWindow window;
    NSControl? zoomButton;


    // Constructor.
    /// <summary>
    /// Initialize new <see cref="FullScreenWindowControls"/> instance.
    /// </summary>
    /// <param name="window">Window to show standard window buttons in full-screen mode.</param>
    public FullScreenWindowControls(NSWindow window)
    {
        // setup view to contain buttons which also receives notifications and mouse-tracking events
        this.window = window;
        var cls = GetButtonsViewClass();
        var viewHandle = NSObject.Initialize(cls.Allocate());
        this.buttonsView = NSObject.FromHandle<NSView>(viewHandle, true).AsNonNull();
        cls.TrySetClrObject(viewHandle, this);

        // register to receive full-screen notifications of window
        WindowDidEnterFullScreenSelector ??= Selector.FromName("windowDidEnterFullScreen:");
        WindowWillEnterFullScreenSelector ??= Selector.FromName("windowWillEnterFullScreen:");
        WindowWillExitFullScreenSelector ??= Selector.FromName("windowWillExitFullScreen:");
        NSNotificationCenter.Default.Let(it =>
        {
            it.AddObserver(this.buttonsView, WindowDidEnterFullScreenSelector, NSWindow.DidEnterFullScreenNotification, window);
            it.AddObserver(this.buttonsView, WindowWillEnterFullScreenSelector, NSWindow.WillEnterFullScreenNotification, window);
            it.AddObserver(this.buttonsView, WindowWillExitFullScreenSelector, NSWindow.WillExitFullScreenNotification, window);
        });

        // capture geometry of thick title bar, or attach buttons immediately if window is already in full-screen mode
        if ((window.Style & NSWindow.StyleMask.FullScreen) != 0)
            this.AttachButtonsToContentView();
        else
            this.CaptureThickTitleBarGeometry();
    }


    // Show buttons inside content view of window and hide the overlay window shown by system.
    void AttachButtonsToContentView()
    {
        // skip if buttons are already attached
        if (this.overlayWindow is not null)
            return;

        // find original buttons which were relocated into overlay window by system
        var originalCloseButton = this.window.StandardWindowButton(NSWindow.ButtonType.CloseButton).AsNonNull();
        var originalMiniaturizeButton = this.window.StandardWindowButton(NSWindow.ButtonType.MiniaturizeButton).AsNonNull();
        var originalZoomButton = this.window.StandardWindowButton(NSWindow.ButtonType.ZoomButton).AsNonNull();
        var originalTitleBarView = originalCloseButton.SuperView.AsNonNull();

        // hide content of overlay window, this is also the tripwire if system changes the structure of full-screen title bar
        this.overlayWindow = originalTitleBarView.Window.AsNonNull().Also(it =>
        {
            if (it.Class.Name != OverlayWindowClassName)
                throw new NotSupportedException($"Expect standard window buttons being relocated into {OverlayWindowClassName} but {it.Class.Name}.");
            it.ContentView.AsNonNull().IsHidden = true;
        });

        // create new buttons with same frames as shown in thick title bar in windowed mode
        var style = this.window.Style;
        var titleBarHeight = CapturedThickTitleBarHeight > 0 ? CapturedThickTitleBarHeight : FallbackThickTitleBarHeight;
        var closeButtonFrame = SelectButtonFrame(CapturedCloseButtonFrame, originalCloseButton.Frame, originalCloseButton.Frame, titleBarHeight);
        var miniaturizeButtonFrame = SelectButtonFrame(CapturedMiniaturizeButtonFrame, originalMiniaturizeButton.Frame, originalCloseButton.Frame, titleBarHeight);
        var zoomButtonFrame = SelectButtonFrame(CapturedZoomButtonFrame, originalZoomButton.Frame, originalCloseButton.Frame, titleBarHeight);
        this.closeButton = CreateStandardWindowButton(NSWindow.ButtonType.CloseButton, style, closeButtonFrame);
        this.miniaturizeButton = CreateStandardWindowButton(NSWindow.ButtonType.MiniaturizeButton, style, miniaturizeButtonFrame).Also(it =>
            it.IsEnabled = false); // window cannot be minimized in full-screen mode
        this.zoomButton = CreateStandardWindowButton(NSWindow.ButtonType.ZoomButton, style, zoomButtonFrame);

        // layout view of buttons over the title bar area drawn by application and pin it to top-left corner
        var contentView = this.window.ContentView.AsNonNull();
        SetFrameSelector ??= Selector.FromName("setFrame:");
        this.buttonsView.SendMessage(SetFrameSelector, new NSRect(0, contentView.Bounds.Size.Height - titleBarHeight, Controls.ExtendedClientAreaWindowConfiguration.SystemChromeWidth, titleBarHeight));
        this.buttonsView.AutoresizingMask = NSView.AutoresizingMaskOptions.MaxXMargin | NSView.AutoresizingMaskOptions.MinYMargin;

        // attach buttons with mouse tracking to content view, buttons are hidden and represented as circles until mouse entering the view
        this.closeButton.IsHidden = true;
        this.miniaturizeButton.IsHidden = true;
        this.zoomButton.IsHidden = true;
        this.buttonsView.AddSubView(this.closeButton);
        this.buttonsView.AddSubView(this.miniaturizeButton);
        this.buttonsView.AddSubView(this.zoomButton);
        this.trackingArea = new NSTrackingArea(default, NSTrackingArea.Options.ActiveAlways | NSTrackingArea.Options.InVisibleRect | NSTrackingArea.Options.MouseEnteredAndExited, this.buttonsView);
        this.buttonsView.AddTrackingArea(this.trackingArea);
        contentView.AddSubView(this.buttonsView);
        this.buttonsView.NeedsDisplay = true;
    }


    // Capture geometry of thick title bar to show buttons at same position in full-screen mode.
    void CaptureThickTitleBarGeometry()
    {
        // skip capturing if window is in full-screen mode
        if ((this.window.Style & NSWindow.StyleMask.FullScreen) != 0)
            return;

        // find buttons and title bar
        var closeButton = this.window.StandardWindowButton(NSWindow.ButtonType.CloseButton);
        var miniaturizeButton = this.window.StandardWindowButton(NSWindow.ButtonType.MiniaturizeButton);
        var zoomButton = this.window.StandardWindowButton(NSWindow.ButtonType.ZoomButton);
        var titleBarView = closeButton?.SuperView;
        if (closeButton is null || miniaturizeButton is null || zoomButton is null || titleBarView is null)
            return;

        // capture geometry only when title bar is thick one
        var titleBarHeight = titleBarView.Frame.Size.Height;
        if (titleBarHeight < MinThickTitleBarHeight)
            return;
        CapturedThickTitleBarHeight = titleBarHeight;
        CapturedCloseButtonFrame = closeButton.Frame;
        CapturedMiniaturizeButtonFrame = miniaturizeButton.Frame;
        CapturedZoomButtonFrame = zoomButton.Frame;
    }


    // Create a standard window button for given style of window.
    static NSControl CreateStandardWindowButton(NSWindow.ButtonType button, NSWindow.StyleMask style, NSRect frame)
    {
        SetFrameSelector ??= Selector.FromName("setFrame:");
        return NSWindow.StandardWindowButton(button, style).AsNonNull().Also(it =>
            it.SendMessage(SetFrameSelector, frame));
    }


    // Remove buttons from content view of window and restore the overlay window shown by system.
    void DetachButtonsFromContentView()
    {
        // skip if buttons are not attached
        if (this.overlayWindow is null)
            return;

        // restore content of overlay window
        this.overlayWindow.ContentView?.IsHidden = false;
        this.overlayWindow = this.overlayWindow.DisposeAndReturnNull();

        // detach view of buttons from content view
        this.trackingArea?.Let(it => this.buttonsView.RemoveTrackingArea(it));
        this.trackingArea = this.trackingArea.DisposeAndReturnNull();
        this.buttonsView.RemoveFromSuperView();

        // remove buttons
        this.closeButton?.RemoveFromSuperView();
        this.closeButton = this.closeButton.DisposeAndReturnNull();
        this.miniaturizeButton?.RemoveFromSuperView();
        this.miniaturizeButton = this.miniaturizeButton.DisposeAndReturnNull();
        this.zoomButton?.RemoveFromSuperView();
        this.zoomButton = this.zoomButton.DisposeAndReturnNull();
        this.isMouseInButtonGroup = false;
    }


    /// <inheritdoc/>
    public void Dispose()
    {
        // skip if disposed
        if (this.isDisposed)
            return;
        this.isDisposed = true;

        // unregister from notifications
        NSNotificationCenter.Default.RemoveObserver(this.buttonsView);

        // detach buttons and release view of buttons
        this.DetachButtonsFromContentView();
        GetButtonsViewClass().TrySetClrObject(this.buttonsView.Handle, null);
        ((IDisposable)this.buttonsView).Dispose();
    }


    // Draw circles to represent hidden buttons.
    void DrawButtonCircles()
    {
        // skip if buttons are not attached
        if (this.closeButton is null || this.miniaturizeButton is null || this.zoomButton is null)
            return;

        // select color of circles
        SetFillSelector ??= Selector.FromName("setFill");
        NSColor.TertiaryLabel.SendMessage(SetFillSelector);

        // draw circle for each hidden button
        DrawCircleForHiddenButton(this.closeButton);
        DrawCircleForHiddenButton(this.miniaturizeButton);
        DrawCircleForHiddenButton(this.zoomButton);
    }


    // Draw a circle to represent given button if it is hidden.
    static void DrawCircleForHiddenButton(NSControl button)
    {
        // skip if button is visible
        if (!button.IsHidden)
            return;

        // draw circle at center of frame of button
        const double diameter = 12;
        var frame = button.Frame;
        var circleRect = new NSRect(
            frame.Origin.X + (frame.Size.Width - diameter) / 2,
            frame.Origin.Y + (frame.Size.Height - diameter) / 2,
            diameter,
            diameter);
        BezierPathClass ??= Class.GetClass("NSBezierPath").AsNonNull();
        BezierPathWithOvalSelector ??= Selector.FromName("bezierPathWithOvalInRect:");
        FillSelector ??= Selector.FromName("fill");
        NSObject.SendMessage<NSObject>(BezierPathClass.Handle, BezierPathWithOvalSelector, circleRect).SendMessage(FillSelector);
    }


    // Get class of view to contain buttons, define the class if needed.
    static Class GetButtonsViewClass()
    {
        ButtonsViewClass ??= Class.DefineClass(Class.GetClass("NSView").AsNonNull(), ButtonsViewClassName, cls =>
        {
            // let standard window buttons show their symbols when mouse is inside the view
            cls.DefineMethod<IntPtr, bool>("_mouseInGroup:", (self, _, _) =>
                cls.TryGetClrObject<FullScreenWindowControls>(self, out var controls) && controls.isMouseInButtonGroup);

            // draw circles to represent hidden buttons
            cls.DefineMethod<NSRect>("drawRect:", (self, _, _) =>
            {
                if (cls.TryGetClrObject<FullScreenWindowControls>(self, out var controls))
                    controls.DrawButtonCircles();
            });

            // handle mouse entering/exiting the view
            cls.DefineMethod<IntPtr>("mouseEntered:", (self, _, _) =>
            {
                if (cls.TryGetClrObject<FullScreenWindowControls>(self, out var controls))
                    controls.OnMouseEnteredOrExitedButtonGroup(true);
            });
            cls.DefineMethod<IntPtr>("mouseExited:", (self, _, _) =>
            {
                if (cls.TryGetClrObject<FullScreenWindowControls>(self, out var controls))
                    controls.OnMouseEnteredOrExitedButtonGroup(false);
            });

            // handle full-screen notifications of window
            cls.DefineMethod<IntPtr>("windowDidEnterFullScreen:", (self, _, _) =>
            {
                if (cls.TryGetClrObject<FullScreenWindowControls>(self, out var controls))
                    controls.AttachButtonsToContentView();
            });
            cls.DefineMethod<IntPtr>("windowWillEnterFullScreen:", (self, _, _) =>
            {
                if (cls.TryGetClrObject<FullScreenWindowControls>(self, out var controls))
                    controls.CaptureThickTitleBarGeometry();
            });
            cls.DefineMethod<IntPtr>("windowWillExitFullScreen:", (self, _, _) =>
            {
                if (cls.TryGetClrObject<FullScreenWindowControls>(self, out var controls))
                    controls.DetachButtonsFromContentView();
            });
        });
        return ButtonsViewClass;
    }


    // Called when mouse cursor entered or exited the group of buttons.
    void OnMouseEnteredOrExitedButtonGroup(bool isEntered)
    {
        // update state
        if (this.isMouseInButtonGroup == isEntered)
            return;
        this.isMouseInButtonGroup = isEntered;

        // show/hide buttons, keep miniaturize button as circle because window cannot be minimized in full-screen mode
        SetHighlightedSelector ??= Selector.FromName("setHighlighted:");
        this.closeButton?.Let(it =>
        {
            it.IsHidden = !isEntered;
            it.SendMessage(SetHighlightedSelector, isEntered);
        });
        this.zoomButton?.Let(it =>
        {
            it.IsHidden = !isEntered;
            it.SendMessage(SetHighlightedSelector, isEntered);
        });
        this.buttonsView.NeedsDisplay = true;
    }


    // Select frame of button to be shown in full-screen mode.
    static NSRect SelectButtonFrame(NSRect capturedFrame, NSRect originalFrame, NSRect originalCloseButtonFrame, double titleBarHeight)
    {
        // use frame captured from thick title bar directly
        if (CapturedThickTitleBarHeight > 0)
            return capturedFrame;

        // layout button with fallback inset and center it vertically in title bar area
        var x = FallbackButtonLeftInset + (originalFrame.Origin.X - originalCloseButtonFrame.Origin.X);
        var y = (titleBarHeight - originalFrame.Size.Height) / 2;
        return new NSRect(x, y, originalFrame.Size.Width, originalFrame.Size.Height);
    }
}
