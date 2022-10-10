using Avalonia;
using CarinaStudio.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Base class of AppSuite common dialog.
    /// </summary>
    public abstract class CommonDialog<TResult> : IThreadDependent
    {
        // Fields.
        readonly Thread thread = Thread.CurrentThread;
        object? title;


        /// <summary>
        /// Bind given value to property of dialog.
        /// </summary>
        /// <param name="dialog">Dialog.</param>
        /// <param name="property">Property.</param>
        /// <param name="value">Value.</param>
        /// <returns><see cref="IDisposable"/> represents bound value.</returns>
        protected IDisposable BindValueToDialog(CarinaStudio.Controls.Window dialog, AvaloniaProperty<string?> property, object? value)
        {
            if (value == null)
                return EmptyDisposable.Default;
            if (value is string stringValue)
            {
                dialog.SetValue<string?>(property, stringValue);
                return EmptyDisposable.Default;
            }
            if (value is IObservable<string?> stringObservable)
                return dialog.Bind(property, stringObservable);
            if (value is IObservable<object?> observable)
            {
                return observable.Subscribe(it =>
                {
                    dialog.SetValue<string?>(property, it?.ToString());
                });
            }
            dialog.SetValue<string?>(property, value.ToString());
            return EmptyDisposable.Default;
        }


        /// <summary>
        /// Check whether current thread is the thread which object depends on or not.
        /// </summary>
        /// <returns>True if current thread is the thread which object depends on.</returns>
        public bool CheckAccess() => Thread.CurrentThread == this.thread;


        /// <summary>
        /// Check whether dialog is showing or not.
        /// </summary>
        public bool IsShowing { get; private set; }


        /// <summary>
		/// Show dialog.
		/// </summary>
		/// <param name="owner">Owner window.</param>
		/// <returns>Task to get result of dialog.</returns>
		public async Task<TResult> ShowDialog(Avalonia.Controls.Window? owner)
        {
            // check state
            this.VerifyAccess();
            this.VerifyShowing();

            // update state
            this.IsShowing = true;

            // show dialog
            try
            {
                return await this.ShowDialogCore(owner);
            }
            finally
            {
                this.IsShowing = false;
            }
        }


        /// <summary>
        /// Called to show dialog and get result.
        /// </summary>
        /// <param name="owner">Owner window.</param>
        /// <returns>Task to get result.</returns>
        protected abstract Task<TResult> ShowDialogCore(Avalonia.Controls.Window? owner);


        /// <summary>
        /// Get <see cref="SynchronizationContext"/>.
        /// </summary>
        public SynchronizationContext SynchronizationContext { get; } = SynchronizationContext.Current ?? throw new InvalidOperationException("No SynchronizationContext on current thread.");


        /// <summary>
		/// Get or set title of dialog.
		/// </summary>
		public object? Title
        {
            get => this.title;
            set
            {
                this.VerifyAccess();
                this.VerifyShowing();
                this.title = value;
            }
        }


        /// <summary>
        /// Throw <see cref="InvalidOperationException"/> if dialog is showing.
        /// </summary>
        protected void VerifyShowing()
        {
            if (this.IsShowing)
                throw new InvalidOperationException("Cannot perform operation when dialog is showing.");
        }
    }
}
