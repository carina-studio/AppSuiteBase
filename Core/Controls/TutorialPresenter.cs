using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using System;
using System.Threading;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Presenter to show <see cref="Tutorial"/>.
/// </summary>
public abstract class TutorialPresenter : TemplatedControl, ITutorialPresenter
{
    /// <summary>
    /// Property of <see cref="CurrentTutorial"/>.
    /// </summary>
    public static readonly DirectProperty<TutorialPresenter, Tutorial?> CurrentTutorialProperty = AvaloniaProperty.RegisterDirect<TutorialPresenter, Tutorial?>(nameof(CurrentTutorial), o => o.currentTutorial);


    // Fields.
    Avalonia.Controls.Window? attachedWindow;
    Tutorial? currentTutorial;
    IDisposable? hasDialogsObserverToken;


    /// <summary>
    /// Initialize new <see cref="TutorialPresenter"/> instance.
    /// </summary>
    public TutorialPresenter()
    {
        this.SynchronizationContext = SynchronizationContext.Current ?? throw new InvalidOperationException("No synchronization context on current thread.");
    }


    /// <inheritdoc/>
    public void CancelTutorial()
    {
        // check state
        this.VerifyAccess();
        var tutorial = this.currentTutorial;
        if (tutorial == null)
            return;
        
        // dismiss
        this.OnDismissTutorial(tutorial);
        this.SetAndRaise<Tutorial?>(CurrentTutorialProperty, ref this.currentTutorial, null);
        tutorial.Cancel();
    }


    /// <summary>
    /// Get <see cref="Tutorial"/> which is currently shown to user.
    /// </summary>
    public Tutorial? CurrentTutorial { get => this.currentTutorial; }


    /// <summary>
    /// Dismiss <see cref="CurrentTutorial"/>.
    /// </summary>
    public void DismissTutorial()
    {
        // check state
        this.VerifyAccess();
        var tutorial = this.currentTutorial;
        if (tutorial == null)
            return;
        
        // dismiss
        this.OnDismissTutorial(tutorial);
        this.SetAndRaise<Tutorial?>(CurrentTutorialProperty, ref this.currentTutorial, null);
        tutorial.Dismiss();
    }


    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.attachedWindow = this.FindAncestorOfType<Avalonia.Controls.Window>();
        this.hasDialogsObserverToken = (this.attachedWindow as CarinaStudio.Controls.Window)?.GetObservable(CarinaStudio.Controls.Window.HasDialogsProperty)?.Subscribe(this.OnHasDialogsChanged);
    }


    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        this.attachedWindow = null;
        this.hasDialogsObserverToken = this.hasDialogsObserverToken.DisposeAndReturnNull();
        base.OnDetachedFromVisualTree(e);
    }


    /// <summary>
    /// Called to dismiss tutorial.
    /// </summary>
    /// <param name="tutorial">Tutorial.</param>
    protected abstract void OnDismissTutorial(Tutorial tutorial);


    // Called when Window.HasDialogs changed.
    void OnHasDialogsChanged(bool hasDialogs)
    {
        if (hasDialogs)
            this.CancelTutorial();
    }


    /// <summary>
    /// Called to show tutorial.
    /// </summary>
    /// <param name="tutorial">Tutorial.</param>
    protected abstract void OnShowTutorial(Tutorial tutorial);


    /// <summary>
    /// Request skipping all remaining tutorials.
    /// </summary>
    public void RequestSkippingAllTutorials()
    {
        this.VerifyAccess();
        this.currentTutorial?.RequestSkippingAllTutorials();
        this.DismissTutorial();
    }


    /// <summary>
    /// Show given tutorial.
    /// </summary>
    /// <param name="tutorial">Tutorial.</param>
    public bool ShowTutorial(Tutorial tutorial)
    {
        // check state
        this.VerifyAccess();
        if (this.attachedWindow == null)
            return false;
        if ((this.attachedWindow as CarinaStudio.Controls.Window)?.HasDialogs == true)
            return false;

        // dismiss current tutorial
        this.CancelTutorial();

        // show tutorial
        tutorial.Show(this);
        this.OnShowTutorial(tutorial);
        this.SetAndRaise<Tutorial?>(CurrentTutorialProperty, ref this.currentTutorial, tutorial);

        // complete
        return true;
    }


    /// <inheritdoc/>
    public SynchronizationContext SynchronizationContext { get; }
}