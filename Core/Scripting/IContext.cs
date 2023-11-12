using CarinaStudio.AppSuite.Controls;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.Scripting;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global

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
    string? GetString(string key);
    
    
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
    void PrepareStrings(Action<IDictionary<string, string>> preparation);


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
    /// Show dialog to let user select one or more items from list of items.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="items">List of items.</param>
    /// <returns>Indices of selected items.</returns>
    IList<int> ShowMultipleItemsSelectionDialog(string? message, IList items);
    
    
    /// <summary>
    /// Show dialog to let user select one or more items from list of items.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="items">List of items.</param>
    /// <param name="defaultItemIndex">Index of default item.</param>
    /// <returns>Indices of selected items.</returns>
    IList<int> ShowMultipleItemsSelectionDialog(string? message, IList items, int defaultItemIndex);
    
    
    /// <summary>
    /// Show dialog to let user select an item from list of items.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="items">List of items.</param>
    /// <returns>Index of selected item, or -1 if no item selected.</returns>
    int ShowSingleItemSelectionDialog(string? message, IList items);
    
    
    /// <summary>
    /// Show dialog to let user select an item from list of items.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="items">List of items.</param>
    /// <param name="defaultItemIndex">Index of default item.</param>
    /// <returns>Index of selected item, or -1 if no item selected.</returns>
    int ShowSingleItemSelectionDialog(string? message, IList items, int defaultItemIndex);
    
    
    /// <summary>
    /// Show message dialog.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <returns>Result of dialog.</returns>
    MessageDialogResult ShowMessageDialog(object? message);


    /// <summary>
    /// Show message dialog.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="icon">Icon.</param>
    /// <returns>Result of dialog.</returns>
    MessageDialogResult ShowMessageDialog(object? message, MessageDialogIcon icon);
    
    
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
    string? ShowTextInputDialog(object? message);
    
    
    /// <summary>
    /// Show text input dialog.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="initialText">Initial text.</param>
    /// <returns>User input text.</returns>
    string? ShowTextInputDialog(object? message, string? initialText);
}