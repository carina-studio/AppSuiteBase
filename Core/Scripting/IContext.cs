using CarinaStudio.AppSuite.Controls;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Context of running script.
/// </summary>
public interface IContext
{
    /// <summary>
    /// Get data for running script.
    /// </summary>
    IDictionary<string, object> Data { get; }


    /// <summary>
    /// Get logger.
    /// </summary>
    ILogger Logger { get; }
}


/// <summary>
/// <see cref="IContext"/> which allows interaction with user.
/// </summary>
public interface IUserInteractiveContext : IContext
{
    /// <summary>
    /// Show message dialog.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="icon">Icon.</param>
    /// <param name="buttons">Buttons.</param>
    /// <returns>Result of dialog.</returns>
    MessageDialogResult ShowMessageDialog(object? message, MessageDialogIcon icon = MessageDialogIcon.Information, MessageDialogButtons buttons = MessageDialogButtons.OK);


    /// <summary>
    /// Show text input dialog.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="initialText">Initial text.</param>
    /// <returns>User input text.</returns>
    string? ShowTextInputDialog(object? message, string? initialText = null);
}


/// <summary>
/// Extensions for <see cref="IContext"/>.
/// </summary>
public static class ContextExtensions
{
    /// <summary>
    /// Show message dialog.
    /// </summary>
    /// <param name="context">Context.</param>
    /// <param name="message">Message.</param>
    /// <returns>Result of dialog.</returns>
    public static MessageDialogResult ShowMessageDialog(this IUserInteractiveContext context, object? message) =>
        context.ShowMessageDialog(message, MessageDialogIcon.Information, MessageDialogButtons.OK);
    

    /// <summary>
    /// Show message dialog.
    /// </summary>
    /// <param name="context">Context.</param>
    /// <param name="message">Message.</param>
    /// <param name="icon">Icon.</param>
    /// <returns>Result of dialog.</returns>
    public static MessageDialogResult ShowMessageDialog(this IUserInteractiveContext context, object? message, MessageDialogIcon icon) =>
        context.ShowMessageDialog(message, icon, MessageDialogButtons.OK);
    

    /// <summary>
    /// Show text input dialog.
    /// </summary>
    /// <param name="context">Context.</param>
    /// <param name="message">Message.</param>
    /// <returns>User input text.</returns>
    public static string? ShowTextInputDialog(this IUserInteractiveContext context, object? message) =>
        context.ShowTextInputDialog(message, null);
}