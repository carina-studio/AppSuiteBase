using System;

namespace CarinaStudio.AppSuite.ViewModels;

/// <summary>
/// Data for view-model message related events.
/// </summary>
/// <param name="message">Message.</param>
public class MessageEventArgs(string message) : EventArgs
{
	/// <summary>
	/// Get message.
	/// </summary>
	public string Message { get; } = message;
}

