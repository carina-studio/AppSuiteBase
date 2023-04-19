using System;
using CarinaStudio.AppSuite.Controls;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.Scripting;

// ReSharper disable UnusedMember.Global

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
    /// <returns>String with given key defined in this context/application, or Null if string cannot be found.</returns>
    public string? GetString(string key) =>
        this.GetString(key, null);
    
    
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
    /// Prepare strings for default culture.
    /// </summary>
    /// <param name="preparation">Action to prepare strings.</param>
    public void PrepareStrings(Action<IDictionary<string, string>> preparation) =>
        this.PrepareStrings(null, preparation);


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
    /// <returns>Result of dialog.</returns>
    public MessageDialogResult ShowMessageDialog(object? message) =>
        this.ShowMessageDialog(message, MessageDialogIcon.Information, MessageDialogButtons.OK);
    
    
    /// <summary>
    /// Show message dialog.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="icon">Icon.</param>
    /// <returns>Result of dialog.</returns>
    public MessageDialogResult ShowMessageDialog(object? message, MessageDialogIcon icon) =>
        this.ShowMessageDialog(message, icon, MessageDialogButtons.OK);
    
    
    /// <summary>
    /// Show message dialog.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="icon">Icon.</param>
    /// <param name="buttons">Buttons.</param>
    /// <returns>Result of dialog.</returns>
    MessageDialogResult ShowMessageDialog(object? message, MessageDialogIcon icon, MessageDialogButtons buttons);


    /// <summary>
    /// Show text input dialog.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <returns>User input text.</returns>
    public string? ShowTextInputDialog(object? message) =>
        this.ShowTextInputDialog(message, null);
    
    
    /// <summary>
    /// Show text input dialog.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="initialText">Initial text.</param>
    /// <returns>User input text.</returns>
    string? ShowTextInputDialog(object? message, string? initialText);
}