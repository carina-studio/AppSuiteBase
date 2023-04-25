using Avalonia;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Represents a tutorial of how to use the application.
/// </summary>
public class Tutorial : AvaloniaObject
{
    /// <summary>
    /// Property of <see cref="Anchor"/>.
    /// </summary>
    public static readonly StyledProperty<Visual?> AnchorProperty = AvaloniaProperty.Register<Tutorial, Visual?>(nameof(Anchor));
    /// <summary>
    /// Property of <see cref="Description"/>.
    /// </summary>
    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<Tutorial, string?>(nameof(Description));
    /// <summary>
    /// Property of <see cref="Icon"/>.
    /// </summary>
    public static readonly StyledProperty<IImage?> IconProperty = AvaloniaProperty.Register<Tutorial, IImage?>(nameof(Icon));
    /// <summary>
    /// Property of <see cref="IsSkippingAllTutorialsAllowed"/>.
    /// </summary>
    public static readonly StyledProperty<bool> IsSkippingAllTutorialsAllowedProperty = AvaloniaProperty.Register<Tutorial, bool>(nameof(IsSkippingAllTutorialsAllowed), true);
    /// <summary>
    /// Property of <see cref="IsVisible"/>.
    /// </summary>
    public static readonly DirectProperty<Tutorial, bool> IsVisibleProperty = AvaloniaProperty.RegisterDirect<Tutorial, bool>(nameof(IsVisible), o => o.isVisible);
    /// <summary>
    /// Property of <see cref="Presenter"/>.
    /// </summary>
    public static readonly DirectProperty<Tutorial, TutorialPresenter?> PresenterProperty = AvaloniaProperty.RegisterDirect<Tutorial, TutorialPresenter?>(nameof(Presenter), o => o.presenter);


    // Fields.
    bool isVisible;
    TutorialPresenter? presenter;


    /// <summary>
    /// Get or set the <see cref="Visual"/> should be anchor for showing the tutorial.
    /// </summary>
    public Visual? Anchor
    {
        get => this.GetValue(AnchorProperty);
        set => this.SetValue(AnchorProperty, value);
    }


    // Cancel
    internal void Cancel()
    {
        // check state
        this.VerifyAccess();
        if (!this.isVisible)
            return;
        
        // update state
        this.SetAndRaise(IsVisibleProperty, ref this.isVisible, false);
        this.SetAndRaise(PresenterProperty, ref this.presenter, null);
        
        // raise event
        this.Cancelled?.Invoke(this, EventArgs.Empty);
    }


    /// <summary>
    /// Raised when tutorial has been cancelled.
    /// </summary>
    public event EventHandler? Cancelled;


    /// <summary>
    /// Get or set description of tutorial.
    /// </summary>
    public string? Description
    {
        get => this.GetValue(DescriptionProperty);
        set => this.SetValue(DescriptionProperty, value);
    }


    // Dismiss.
    internal void Dismiss()
    {
        // check state
        this.VerifyAccess();
        if (!this.isVisible)
            return;
        
        // update state
        this.SetAndRaise(IsVisibleProperty, ref this.isVisible, false);
        this.SetAndRaise(PresenterProperty, ref this.presenter, null);
        
        // raise event
        this.Dismissed?.Invoke(this, EventArgs.Empty);
    }


    /// <summary>
    /// Raised when tutorial dismissed.
    /// </summary>
    public event EventHandler? Dismissed;


    /// <summary>
    /// Get or set icon of tutorial.
    /// </summary>
    public IImage? Icon
    {
        get => this.GetValue(IconProperty);
        set => this.SetValue(IconProperty, value);
    }


    /// <summary>
    /// Get or set whether skipping all tutorials is allowed or not.
    /// </summary>
    public bool IsSkippingAllTutorialsAllowed
    {
        get => this.GetValue(IsSkippingAllTutorialsAllowedProperty);
        set => this.SetValue(IsSkippingAllTutorialsAllowedProperty, value);
    }


    /// <summary>
    /// Check whether tutorial is visible to user or not.
    /// </summary>
    public bool IsVisible => this.isVisible;


    /// <summary>
    /// Get <see cref="TutorialPresenter"/> which hosts the tutorial.
    /// </summary>
    public TutorialPresenter? Presenter => this.presenter;


    // Skip all tutorials.
    internal void RequestSkippingAllTutorials() =>
        this.SkippingAllTutorialRequested?.Invoke(this, EventArgs.Empty);


    // Show the tutorial.
    internal void Show(TutorialPresenter presenter)
    {
        // check state
        this.VerifyAccess();
        if (this.presenter != null)
        {
            if (this.presenter != presenter)
                throw new InvalidOperationException("Tutorial is already hosted by another presenter.");
            return;
        }

        // update state
        this.SetAndRaise(PresenterProperty, ref this.presenter, presenter);
        this.SetAndRaise(IsVisibleProperty, ref this.isVisible, true);

        // raise event
        this.Shown?.Invoke(this, EventArgs.Empty);
    }


    /// <summary>
    /// Called when tutorial is just shown to user.
    /// </summary>
    public event EventHandler? Shown;


    /// <summary>
    /// Raised when user request skipping all tutorials.
    /// </summary>
    public event EventHandler? SkippingAllTutorialRequested;
}