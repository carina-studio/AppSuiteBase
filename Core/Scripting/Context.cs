using CarinaStudio.AppSuite.Controls;
using CarinaStudio.AppSuite.Diagnostics;
using CarinaStudio.Collections;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Base implementation of <see cref="IContext"/>.
/// </summary>
public class Context : IContext
{
    // Data of context.
    class DataImpl : IDictionary<string, object>
    {
        // Fields.
        readonly ConcurrentDictionary<string, object> dictionary = new();

        /// <inheritdoc/>
        public void Add(KeyValuePair<string, object> item)
        {
            if (this.dictionary.TryAdd(item.Key, item.Value))
                throw new ArgumentException($"Value with key '{item.Key}' is already added before.");
        }
        
        /// <inheritdoc/>
        public void Add(string key, object value)
        {
            if (this.dictionary.TryAdd(key, value))
                throw new ArgumentException($"Value with key '{key}' is already added before.");
        }

        /// <inheritdoc/>
        public void Clear() =>
            this.dictionary.Clear();

        /// <inheritdoc/>
        public bool Contains(KeyValuePair<string, object> item)
        {
            if (this.dictionary.TryGetValue(item.Key, out var value))
                return value.Equals(item.Value);
            return false;
        }
        
        /// <inheritdoc/>
        public bool ContainsKey(string key) =>
            this.dictionary.ContainsKey(key);

        /// <inheritdoc/>
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) =>
            ((IDictionary<string, object>)this.dictionary).CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public int Count => this.dictionary.Count;
        
        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() =>
            this.dictionary.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() =>
            this.dictionary.GetEnumerator();
        
        /// <inheritdoc/>
        public bool IsReadOnly => false;
        
        /// <inheritdoc/>
        public ICollection<string> Keys => this.dictionary.Keys;
        
        public bool Remove(KeyValuePair<string, object> item) =>
            ((IDictionary<string, object>)this.dictionary).Remove(item);
        
        /// <inheritdoc/>
        public bool Remove(string key) =>
            ((IDictionary<string, object>)this.dictionary).Remove(key);
        
        /// <inheritdoc/>
        public object this[string key]
        {
            get => this.dictionary[key];
            set => this.dictionary[key] = value;
        }

        // Try adding value.
        [Obsolete("Use Add() or indexer instead.")]
        public bool TryAdd(string key, object value) =>
            this.dictionary.TryAdd(key, value);
        
        /// <inheritdoc/>
        public bool TryGetValue(string key, [NotNullWhen(true)] out object? value) =>
            this.dictionary.TryGetValue(key, out value);

        // Try removing value.
        [Obsolete("Use Remove() instead.")]
        public bool TryRemove(string key, [NotNullWhen(true)] out object? value) =>
            this.dictionary.TryRemove(key, out value);

        /// <inheritdoc/>
        public ICollection<object> Values => this.dictionary.Values;
    }
    
    
    // Fields.
    volatile ConcurrentDictionary<string, string>? defaultStringTable;
    volatile ConcurrentDictionary<string, IDictionary<string, string>>? stringTablesWithCulture;
    readonly object stringTableSyncLock = new();


    /// <summary>
    /// Initialize new <see cref="Context"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <param name="loggerName">Name for logger.</param>
    public Context(IAppSuiteApplication app, string loggerName)
    {
        Guard.VerifyInternalCall();
        this.Application = app;
        this.Logger = ScriptManager.Default.CreateScriptLogger(loggerName);
    }


    /// <summary>
    /// Get application.
    /// </summary>
    public IAppSuiteApplication Application { get; }


    /// <summary>
    /// Dispose and clear all values from <see cref="Data"/>.
    /// </summary>
    public void ClearAndDisposeData()
    {
        Guard.VerifyInternalCall();
        if (this.Data.IsEmpty())
            return;
        var values = this.Data.Values.ToArray();
        this.Data.Clear();
        foreach (var value in values)
        {
            if (value is IDisposable disposable)
                disposable.Dispose();
            else if (value is IAsyncDisposable asyncDisposable)
#pragma warning disable CA2012
                _ = asyncDisposable.DisposeAsync();
#pragma warning restore CA2012
        }
    }


    /// <inheritdoc/>
    public IDictionary<string, object> Data { get; } = new DataImpl();
    
    
    /// <inheritdoc/>
    public string? GetString(string key) =>
        this.GetString(key, null);


    /// <inheritdoc/>
    public string? GetString(string key, string? defaultValue)
    {
        var app = this.Application;
        if (this.stringTablesWithCulture?.TryGetValue(app.CultureInfo.Name, out var t) == true
            && t.TryGetValue(key, out string? s))
        {
            return s;
        }
        if (this.defaultStringTable?.TryGetValue(key, out s) == true)
            return s;
        return app.GetString(key, defaultValue);
    }


    /// <inheritdoc/>
    public ILogger Logger { get; }
    
    
    /// <inheritdoc/>
    public void PrepareStrings(Action<IDictionary<string, string>> preparation) =>
        this.PrepareStrings(null, preparation);


    /// <inheritdoc/>
    // ReSharper disable NonAtomicCompoundOperator
    public void PrepareStrings(string? cultureName, Action<IDictionary<string, string>> preparation)
    {
        if (cultureName == null)
        {
            lock (this.stringTableSyncLock)
                this.defaultStringTable ??= new();
            preparation(this.defaultStringTable);
        }
        else
        {
            var table = this.stringTableSyncLock.Lock(() =>
            {
                if (this.stringTablesWithCulture?.TryGetValue(cultureName, out IDictionary<string, string>? table) == true)
                    return table;
                table = new ConcurrentDictionary<string, string>();
                this.stringTablesWithCulture ??= new();
                this.stringTablesWithCulture[cultureName] = table;
                return table;
            });
            preparation(table);
        }
    }
    // ReSharper restore NonAtomicCompoundOperator
}


/// <summary>
/// Base implementation of <see cref="IUserInteractiveContext"/>.
/// </summary>
public class UserInteractiveContext : Context, IUserInteractiveContext
{
    /// <summary>
    /// Initialize new <see cref="UserInteractiveContext"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <param name="loggerName">Name for logger.</param>
    public UserInteractiveContext(IAppSuiteApplication app, string loggerName) : base(app, loggerName)
    { }


    // Get default result of message dialog.
    static MessageDialogResult GetDefaultMessageDialogResult(MessageDialogButtons buttons) => buttons switch
    {
        MessageDialogButtons.OK => MessageDialogResult.OK,
        MessageDialogButtons.OKCancel
        or MessageDialogButtons.YesNoCancel => MessageDialogResult.Cancel,
        MessageDialogButtons.YesNo => MessageDialogResult.No,
        _ => throw new ArgumentException($"Unknown type of message dialog buttons: {buttons}."),
    };


    /// <summary>
    /// Check whether showing message dialog is allowed or not.
    /// </summary>
    public bool IsShowingMessageDialogAllowed { get; private set; } = true;


    /// <summary>
    /// Check whether showing text input dialog is allowed or not.
    /// </summary>
    public bool IsShowingTextInputDialogAllowed { get; private set; } = true;
    
    
    /// <inheritdoc/>
    public MessageDialogResult ShowMessageDialog(object? message) =>
        this.ShowMessageDialog(message, MessageDialogIcon.Information, MessageDialogButtons.OK);
    
    
    /// <inheritdoc/>
    public MessageDialogResult ShowMessageDialog(object? message, MessageDialogIcon icon) =>
        this.ShowMessageDialog(message, icon, MessageDialogButtons.OK);


    /// <inheritdoc/>
    public MessageDialogResult ShowMessageDialog(object? message, MessageDialogIcon icon, MessageDialogButtons buttons)
    {
        if (!this.IsShowingMessageDialogAllowed)
            return GetDefaultMessageDialogResult(buttons);
        var result = GetDefaultMessageDialogResult(buttons);
        if (this.Application.CheckAccess())
        {
            var window = this.Application.LatestActiveMainWindow;
            if (window != null)
            {
                var taskCompletionSource = new TaskCompletionSource();
                this.Application.SynchronizationContext.Post(async () =>
                {
                    var dialog = new MessageDialog
                    {
                        Buttons = buttons,
                        DoNotAskOrShowAgain = false,
                        Icon = icon,
                        Message = message,
                    };
                    result = await dialog.ShowDialog(window);
                    this.IsShowingMessageDialogAllowed = !dialog.DoNotAskOrShowAgain.GetValueOrDefault();
                    taskCompletionSource.SetResult();
                });
                while (!taskCompletionSource.Task.IsCompleted)
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            }
        }
        else
        {
            new object().Lock(syncLock =>
            {
                this.Application.SynchronizationContext.Post(async () =>
                {
                    var window = this.Application.LatestActiveMainWindow;
                    if (window != null)
                    {
                        var dialog = new MessageDialog
                        {
                            Buttons = buttons,
                            DoNotAskOrShowAgain = false,
                            Icon = icon,
                            Message = message,
                        };
                        result = await dialog.ShowDialog(window);
                        this.IsShowingMessageDialogAllowed = !dialog.DoNotAskOrShowAgain.GetValueOrDefault();
                    }
                    lock (syncLock)
                        Monitor.Pulse(syncLock);
                });
                Monitor.Wait(syncLock);
            });
        }
        return result;
    }
    
    
    /// <inheritdoc/>
    public string? ShowTextInputDialog(object? message) =>
        this.ShowTextInputDialog(message, null);


    /// <inheritdoc/>
    public string? ShowTextInputDialog(object? message, string? initialText)
    {
        if (!this.IsShowingTextInputDialogAllowed)
            return null;
        var result = (string?)null;
        if (this.Application.CheckAccess())
        {
            var window = this.Application.LatestActiveMainWindow;
            if (window != null)
            {
                var taskCompletionSource = new TaskCompletionSource();
                this.Application.SynchronizationContext.Post(async () =>
                {
                    var dialog = new TextInputDialog()
                    {
                        CheckBoxMessage = this.Application.GetObservableString("Common.DoNotShowAgain"),
                        IsCheckBoxChecked = false,
                        InitialText = initialText,
                        Message = message,
                    };
                    result = await dialog.ShowDialog(window);
                    this.IsShowingTextInputDialogAllowed = !dialog.IsCheckBoxChecked.GetValueOrDefault();
                    taskCompletionSource.SetResult();
                });
                while (!taskCompletionSource.Task.IsCompleted)
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            }
        }
        else
        {
            new object().Lock(syncLock =>
            {
                this.Application.SynchronizationContext.Post(async () =>
                {
                    var window = this.Application.LatestActiveMainWindow;
                    if (window != null)
                    {
                        var dialog = new TextInputDialog()
                        {
                            CheckBoxMessage = this.Application.GetObservableString("Common.DoNotShowAgain"),
                            IsCheckBoxChecked = false,
                            InitialText = initialText,
                            Message = message,
                        };
                        result = await dialog.ShowDialog(window);
                        this.IsShowingTextInputDialogAllowed = !dialog.IsCheckBoxChecked.GetValueOrDefault();
                    }
                    lock (syncLock)
                        Monitor.Pulse(syncLock);
                });
                Monitor.Wait(syncLock);
            });
        }
        return result;
    }
}