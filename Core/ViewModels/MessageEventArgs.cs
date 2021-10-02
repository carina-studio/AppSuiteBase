using System;

namespace CarinaStudio.AppSuite.ViewModels
{
	/// <summary>
	/// Data for view-model message related events.
	/// </summary>
	public class MessageEventArgs : EventArgs
	{
		/// <summary>
		/// Initialize new <see cref="MessageEventArgs"/> instance.
		/// </summary>
		/// <param name="message">Message.</param>
		public MessageEventArgs(string message) => this.Message = message;


		/// <summary>
		/// Get message.
		/// </summary>
		public string Message { get; }
	}
}
