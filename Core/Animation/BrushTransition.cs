using Avalonia.Animation;
using Avalonia.Media;
using System;

namespace CarinaStudio.AppSuite.Animation
{
    /// <summary>
    /// Implementation of <see cref="ITransition"/> for <see cref="IBrush"/>.
    /// </summary>
    public class BrushTransition : Transition<IBrush?>
    {
        // Static fields.
        static readonly Animators.SolidColorBrushAnimator SolidColorBrushAnimator = new();
        

        // Fields.
        Avalonia.Animation.BrushTransition? defaultTransition;


        /// <inheritdoc/>
        public override IObservable<IBrush?> DoTransition(IObservable<double> progress, IBrush? oldValue, IBrush? newValue)
        {
            if ((oldValue == null || oldValue is ISolidColorBrush) && (newValue == null || newValue is ISolidColorBrush))
                return new AnimatorTransitionObservable<IBrush?, Animators.SolidColorBrushAnimator>(SolidColorBrushAnimator, progress, this.Easing, oldValue, newValue);
            if (this.defaultTransition == null)
                this.defaultTransition = new();
            return this.defaultTransition.DoTransition(progress, oldValue, newValue);
        }
    }
}