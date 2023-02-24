using System;
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
    /// Get string with given key defined in this context or application.
    /// </summary>
    /// <param name="key">Key of string.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <returns>String with given key defined in this context/application, or default value if string cannot be found.</returns>
    string? GetString(string key, string? defaultValue);


    /// <summary>
    /// Get logger.
    /// </summary>
    ILogger Logger { get; }


    /// <summary>
    /// Prepare strings for specific culture.
    /// </summary>
    /// <param name="cultureName">Name of culture, or Null for default culture.</param>
    /// <param name="preparation">Action to prepare strings.</param>
    void PrepareStrings(string? cultureName, Action<IDictionary<string, string>> preparation);
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
    /// Get string with given key defined in this context or application.
    /// </summary>
    /// <param name="context">Context.</param>
    /// <param name="key">Key of string.</param>
    /// <returns>String with given key defined in this context/application.</returns>
    public static string? GetString(this IContext context, string key) =>
        context.GetString(key, null);
    

    /// <summary>
    /// Prepare strings for default culture.
    /// </summary>
    /// <param name="context">Context.</param>
    /// <param name="preparation">Action to prepare strings.</param>
    public static void PrepareStrings(this IContext context, Action<IDictionary<string, string>> preparation) =>
        context.PrepareStrings(null, preparation);


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