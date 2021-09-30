using CarinaStudio.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        string? title;


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
		public async Task<TResult> ShowDialog(Window owner)
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
        protected abstract Task<TResult> ShowDialogCore(Avalonia.Controls.Window owner);


        /// <summary>
        /// Get <see cref="SynchronizationContext"/>.
        /// </summary>
        public SynchronizationContext SynchronizationContext { get; } = SynchronizationContext.Current ?? throw new InvalidOperationException("No SynchronizationContext on current thread.");


        /// <summary>
		/// Get or set title of dialog.
		/// </summary>
		public string? Title
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
