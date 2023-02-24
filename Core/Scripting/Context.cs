using CarinaStudio.AppSuite.Controls;
using CarinaStudio.Collections;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Base implementation of <see cref="IContext"/>.
/// </summary>
public class Context : IContext
{
    /// <summary>
    /// Initialize new <see cref="Context"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <param name="loggerName">Name for logger.</param>
    public Context(IAppSuiteApplication app, string loggerName)
    {
        this.Application = app;
        this.Logger = ScriptManager.Default.CreateScriptLogger(loggerName);
    }


    /// <inheritdoc/>
    public IAppSuiteApplication Application { get; }


    /// <summary>
    /// Dispose and clear all values from <see cref="Data"/>.
    /// </summary>
    public void ClearAndDisposeData()
    {
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
    public IDictionary<string, object> Data { get; } = new ConcurrentDictionary<string, object>();


    /// <inheritdoc/>
    public ILogger Logger { get; }
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
        _ => throw new NotImplementedException(),
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
                    var dialog = new MessageDialog()
                    {
                        Buttons = (MessageDialogButtons)buttons,
                        DoNotAskOrShowAgain = false,
                        Icon = (MessageDialogIcon)icon,
                        Message = message,
                    };
                    result = (MessageDialogResult)await dialog.ShowDialog(window);
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
                        var dialog = new MessageDialog()
                        {
                            Buttons = (MessageDialogButtons)buttons,
                            DoNotAskOrShowAgain = false,
                            Icon = (MessageDialogIcon)icon,
                            Message = message,
                        };
                        result = (MessageDialogResult)await dialog.ShowDialog(window);
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
                        DoNotShowAgain = false,
                        InitialText = initialText,
                        Message = message,
                    };
                    result = await dialog.ShowDialog(window);
                    this.IsShowingTextInputDialogAllowed = !dialog.DoNotShowAgain.GetValueOrDefault();
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
                            DoNotShowAgain = false,
                            InitialText = initialText,
                            Message = message,
                        };
                        result = await dialog.ShowDialog(window);
                        this.IsShowingTextInputDialogAllowed = !dialog.DoNotShowAgain.GetValueOrDefault();
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