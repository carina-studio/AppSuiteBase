using CarinaStudio.Threading;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Presenter to show <see cref="Tutorial"/>.
/// </summary>
public interface ITutorialPresenter : IThreadDependent
{
    /// <summary>
    /// Cancel <see cref="CurrentTutorial"/>.
    /// </summary>
    void CancelTutorial();


    /// <summary>
    /// Get <see cref="Tutorial"/> which is currently shown to user.
    /// </summary>
    public Tutorial? CurrentTutorial { get; }


    /// <summary>
    /// Dismiss <see cref="CurrentTutorial"/>.
    /// </summary>
    void DismissTutorial();


    /// <summary>
    /// Request skipping all remaining tutorials.
    /// </summary>
    void RequestSkippingAllTutorials();


    /// <summary>
    /// Show given tutorial.
    /// </summary>
    /// <param name="tutorial">Tutorial.</param>
    /// <returns>True if tutorial has been shown successfully.</returns>
    bool ShowTutorial(Tutorial tutorial);
}