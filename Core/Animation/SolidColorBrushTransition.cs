using Avalonia.Animation;
using Avalonia.Media;
using CarinaStudio.AppSuite.Animation.Animators;
using System;

namespace CarinaStudio.AppSuite.Animation
{
    /// <summary>
    /// Implementation of <see cref="ITransition"/> for <see cref="ISolidColorBrush"/>.
    /// </summary>
    public class SolidColorBrushTransition : Transition<IBrush?>
    {
        // Static fields.
        static readonly SolidColorBrushAnimator SolidColorBrushAnimator = new SolidColorBrushAnimator();


        /// <inheritdoc/>
        public override IObservable<IBrush?> DoTransition(IObservable<double> progress, IBrush? oldValue, IBrush? newValue)
        {
            if ((oldValue == null || oldValue is ISolidColorBrush) && (newValue == null || newValue is ISolidColorBrush))
                return new AnimatorTransitionObservable<IBrush?, SolidColorBrushAnimator>(SolidColorBrushAnimator, progress, this.Easing, oldValue, newValue);
            return new MutableObservableValue<IBrush?>();
        }
    }
}
