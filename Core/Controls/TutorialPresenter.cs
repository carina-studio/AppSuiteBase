using Avalonia;
using Avalonia.Controls.Primitives;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Presenter to show <see cref="Tutorial"/>s.
/// </summary>
public abstract class TutorialPresenter : TemplatedControl
{
    /// <summary>
    /// Property of <see cref="CurrentTutorial"/>.
    /// </summary>
    public static readonly AvaloniaProperty<Tutorial?> CurrentTutorialProperty = AvaloniaProperty.RegisterDirect<TutorialPresenter, Tutorial?>(nameof(CurrentTutorial), o => o.currentTutorial);


    // Fields.
    Tutorial? currentTutorial;


    /// <summary>
    /// Get <see cref="Tutorial"/> which is currently shown to user.
    /// </summary>
    public Tutorial? CurrentTutorial { get => this.currentTutorial; }


    /// <summary>
    /// Dismiss <see cref="CurrentTutorial"/>.
    /// </summary>
    public void DismissCurrentTutorial()
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


    /// <summary>
    /// Called to dismiss tutorial.
    /// </summary>
    /// <param name="tutorial">Tutorial.</param>
    protected abstract void OnDismissTutorial(Tutorial tutorial);


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
        this.DismissCurrentTutorial();
    }


    /// <summary>
    /// Show given tutorial.
    /// </summary>
    /// <param name="tutorial">Tutorial.</param>
    public void ShowTutorial(Tutorial tutorial)
    {
        // dismiss current tutorial
        this.DismissCurrentTutorial();

        // show tutorial
        tutorial.Show(this);
        this.OnShowTutorial(tutorial);
        this.SetAndRaise<Tutorial?>(CurrentTutorialProperty, ref this.currentTutorial, tutorial);
    }
}