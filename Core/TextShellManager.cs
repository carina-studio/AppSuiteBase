using Avalonia;
using Avalonia.Controls;
using CarinaStudio.Collections;
using CarinaStudio.Logging;
using CarinaStudio.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Manager of text shells installed on system.
/// </summary>
public class TextShellManager : BaseApplicationObject<IAppSuiteApplication>
{
    // Constants.
    const int RefreshInstalledTextShellsDelay = 1000;


    // Static fields.
    static TextShellManager? DefaultInstance;
    // Directories to find text shells on macOS and Linux, in order of priority.
    static readonly string[] UnixExecutableDirectories = Platform.IsMacOS
        ? [ "/bin", "/usr/bin", "/usr/local/bin", "/opt/homebrew/bin" ]
        : [ "/bin", "/usr/bin", "/usr/local/bin" ];
    // Directories to find PowerShell on macOS and Linux, in order of priority.
    static readonly string[] UnixPowerShellDirectories = Platform.IsLinux
        ? [ ..UnixExecutableDirectories, "/opt/microsoft/powershell/7", "/snap/bin" ]
        : UnixExecutableDirectories;


    // Fields.
    Window? attachedActiveWindow;
    readonly Task initTask;
    readonly SortedObservableList<TextShell> installedTextShells = new((l, r) => (int)l - (int)r);
    IDisposable? isWindowActiveObserverToken;
    readonly ScheduledAction refreshInstalledTextShellsAction;
    IDictionary<TextShell, string> textShellExePaths = new Dictionary<TextShell, string>();


    // Constructor.
    TextShellManager(IAppSuiteApplication app) : base(app)
    {
        // setup properties
        this.InstalledTextShells = ListExtensions.AsReadOnly(this.installedTextShells);

        // setup actions
        var isWindowActiveObserver = new Observer<bool>(isActive =>
        {
            if (isActive)
                this.refreshInstalledTextShellsAction!.Cancel();
            else
                this.refreshInstalledTextShellsAction!.Schedule(RefreshInstalledTextShellsDelay);
        });
        this.refreshInstalledTextShellsAction = new(() =>
        {
            if (this.Application.LatestActiveWindow?.IsActive != true)
                _ = this.UpdateInstalledTextShellsAsync(false);
        });

        // attach to application
        app.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(IAppSuiteApplication.IsShutdownStarted):
                    this.refreshInstalledTextShellsAction.Cancel();
                    this.isWindowActiveObserverToken?.Dispose();
                    break;
                case nameof(IAppSuiteApplication.LatestActiveWindow):
                    if (this.attachedActiveWindow is not null)
                        this.isWindowActiveObserverToken = this.isWindowActiveObserverToken.DisposeAndReturnNull();
                    this.attachedActiveWindow = this.Application.LatestActiveWindow;
                    if (this.attachedActiveWindow is not null)
                        this.isWindowActiveObserverToken = this.attachedActiveWindow.GetObservable(WindowBase.IsActiveProperty).Subscribe(isWindowActiveObserver);
                    else
                        this.refreshInstalledTextShellsAction.Schedule(RefreshInstalledTextShellsDelay);
                    break;
            }
        };

        // find installed text shells
        this.initTask = this.UpdateInstalledTextShellsAsync(true);
    }


    /// <summary>
    /// Create arguments to run a command by the text shell.
    /// </summary>
    /// <param name="shell">Text shell.</param>
    /// <param name="command">Command to run.</param>
    /// <returns>Arguments passed to the text shell.</returns>
    public static string CreateCommandArguments(TextShell shell, string command) => shell switch
    {
        TextShell.CommandPrompt => $"/c {command}",
        TextShell.PowerShell => $"-NoLogo -Command {command}",
        _ => $"-c \"{command}\"",
    };


    /// <summary>
    /// Get default instance.
    /// </summary>
    public static TextShellManager Default => DefaultInstance ?? throw new InvalidOperationException();


    // Find executable in the directories, or null if not found.
    static string? FindExecutable(string fileName, IEnumerable<string> directories)
    {
        foreach (var directory in directories)
        {
            var path = Path.Combine(directory, fileName);
            if (File.Exists(path))
                return path;
        }
        return null;
    }


    // Get all installed text shells and paths of their executables.
    [CalledOnBackgroundThread]
    Dictionary<TextShell, string> GetInstalledTextShells()
    {
        // define function to find text shell
        var shellMap = new Dictionary<TextShell, string>();
        void FindTextShell(TextShell shell, Func<string?> find)
        {
            try
            {
                var path = find();
                if (path is not null)
                    shellMap[shell] = path;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Error occurred while checking existence of '{shell}'", shell);
            }
        }

        // find text shells
        if (Platform.IsWindows)
        {
            // Command Prompt
            FindTextShell(TextShell.CommandPrompt, () =>
            {
                var path = Environment.GetEnvironmentVariable("ComSpec");
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    return path;
                return FindExecutable("cmd.exe", [ Environment.SystemDirectory ]);
            });

            // PowerShell 7, or Windows PowerShell
            FindTextShell(TextShell.PowerShell, () =>
            {
                var directories = (Environment.GetEnvironmentVariable("PATH") ?? "")
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(Path.IsPathFullyQualified)
                    .Append(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7"));
                return FindExecutable("pwsh.exe", directories)
                    ?? FindExecutable("powershell.exe", [ Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0") ]);
            });
        }
        else
        {
            // Unix shells
            FindTextShell(TextShell.BourneAgainShell, () => FindExecutable("bash", UnixExecutableDirectories));
            FindTextShell(TextShell.BourneShell, () => FindExecutable("sh", UnixExecutableDirectories));
            FindTextShell(TextShell.CShell, () => FindExecutable("csh", UnixExecutableDirectories));
            FindTextShell(TextShell.FriendlyInteractiveShell, () => FindExecutable("fish", UnixExecutableDirectories));
            FindTextShell(TextShell.ZShell, () => FindExecutable("zsh", UnixExecutableDirectories));

            // PowerShell 7
            FindTextShell(TextShell.PowerShell, () => FindExecutable("pwsh", UnixPowerShellDirectories));
        }

        // complete
        return shellMap;
    }


    /// <summary>
    /// Initialize default instance asynchronously. It is called by <see cref="AppSuiteApplication"/> when starting.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <returns>Task of initialization.</returns>
    public static async Task InitializeAsync(IAppSuiteApplication app)
    {
        // check state
        if (DefaultInstance is not null)
            throw new InvalidOperationException();

        // create instance and wait for finding installed text shells
        var defaultInstance = new TextShellManager(app);
        await defaultInstance.initTask;
        foreach (var shell in defaultInstance.installedTextShells)
            defaultInstance.Logger.LogDebug("'{shell}' found on system", shell);

        // complete
        DefaultInstance = defaultInstance;
    }


    /// <summary>
    /// Get list of text shells installed on system.
    /// </summary>
    public IList<TextShell> InstalledTextShells { get; }


    /// <inheritdoc/>
    protected override string LoggerCategoryName => nameof(TextShellManager);


    /// <summary>
    /// Refresh list of text shells installed on system asynchronously.
    /// </summary>
    /// <returns>Task of refreshing list.</returns>
    public Task RefreshInstalledTextShellsAsync()
    {
        this.refreshInstalledTextShellsAction.Cancel();
        return this.UpdateInstalledTextShellsAsync(false);
    }


    /// <summary>
    /// Try getting path of executable of default text shell.
    /// </summary>
    /// <param name="path">Path of executable of default text shell.</param>
    /// <returns>True if path got successfully.</returns>
    public bool TryGetDefaultTextShellPath([NotNullWhen(true)] out string? path) =>
        this.textShellExePaths.TryGetValue(this.Application.Settings.GetValueOrDefault(SettingKeys.DefaultTextShell), out path);


    /// <summary>
    /// Try getting path of executable of default text shell.
    /// </summary>
    /// <param name="defaultTextShell">Default text shell.</param>
    /// <param name="path">Path of executable of default text shell.</param>
    /// <returns>True if path got successfully.</returns>
    public bool TryGetDefaultTextShellPath(out TextShell defaultTextShell, [NotNullWhen(true)] out string? path)
    {
        defaultTextShell = this.Application.Settings.GetValueOrDefault(SettingKeys.DefaultTextShell);
        return this.textShellExePaths.TryGetValue(defaultTextShell, out path);
    }


    /// <summary>
    /// Try getting path of executable of text shell.
    /// </summary>
    /// <param name="shell">Text shell.</param>
    /// <param name="path">Path of executable of text shell.</param>
    /// <returns>True if path got successfully.</returns>
    public bool TryGetTextShellPath(TextShell shell, [NotNullWhen(true)] out string? path) =>
        this.textShellExePaths.TryGetValue(shell, out path);


    // Update list of text shells installed on system.
    async Task UpdateInstalledTextShellsAsync(bool isInit)
    {
        // find installed text shells
        this.Logger.LogTrace("Update installed text shells [start]");
        var shellMap = await Task.Run(this.GetInstalledTextShells);

        // remove text shells which are no longer installed
        var installedTextShells = this.installedTextShells;
        for (var i = installedTextShells.Count - 1; i >= 0; --i)
        {
            var shell = installedTextShells[i];
            if (!shellMap.ContainsKey(shell))
            {
                this.Logger.LogWarning("'{shell}' was removed from system", shell);
                installedTextShells.RemoveAt(i);
            }
        }

        // add text shells which are newly installed
        foreach (var shell in shellMap.Keys)
        {
            if (!installedTextShells.Contains(shell))
            {
                if (!isInit)
                    this.Logger.LogDebug("'{shell}' was added to system", shell);
                installedTextShells.Add(shell);
            }
        }
        this.textShellExePaths = shellMap;
        if (installedTextShells.IsEmpty())
            this.Logger.LogError("No text shell installed on system");

        // select another default text shell if the current one is not installed
        var settings = this.Application.Settings;
        var defaultTextShell = settings.GetValueOrDefault(SettingKeys.DefaultTextShell);
        if (shellMap.ContainsKey(defaultTextShell))
        {
            if (isInit)
                this.Logger.LogDebug("Default text shell is '{shell}'", defaultTextShell);
            else
                this.Logger.LogTrace("Default text shell is '{shell}'", defaultTextShell);
        }
        else if (installedTextShells.IsNotEmpty())
        {
            var newShell = SettingKeys.DefaultTextShell.DefaultValue;
            if (!shellMap.ContainsKey(newShell))
                newShell = installedTextShells[0];
            this.Logger.LogWarning("Set default text shell to '{newShell}' because '{shell}' is not installed", newShell, defaultTextShell);
            settings.SetValue(SettingKeys.DefaultTextShell, newShell);
        }
        this.Logger.LogTrace("Update installed text shells [end]");
    }
}