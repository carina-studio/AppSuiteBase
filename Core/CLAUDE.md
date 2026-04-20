# Core — Architecture

## Entry Point: `AppSuiteApplication`

`AppSuiteApplication.cs` (~6000 lines) is the abstract singleton base class every consumer application must subclass. It manages:
- Application lifecycle (startup, shutdown, restart)
- Configuration and theming (light/dark, accent color)
- Window management and dialog orchestration
- Logging (NLog), auto-update, permissions, pro-version activation
- Privacy policy / user agreement enforcement
- External dependency management and script execution

Platform-specific logic is split into partial classes:
- `AppSuiteApplication.Windows.cs`
- `AppSuiteApplication.MacOS.cs`
- `AppSuiteApplication.Linux.cs`

The interface `IAppSuiteApplication` provides the abstraction; `IAppSuiteApplication.Current` is the static singleton accessor.

## Key Namespaces

- **`Controls/`** — 90+ custom Avalonia controls: dialogs (agreement, file selection, app update), `MainWindow` base classes, `TutorialPresenter`, `NotificationPresenter`, specialized input controls.
- **`ViewModels/`** — MVVM view models: `ApplicationOptions`, `ApplicationInfo`, `ApplicationUpdater`, `MainWindowViewModel`. All extend `ViewModel<TApp>`.
- **`Data/`** — Profile system: `BaseProfile<TApp>`, `BaseProfileManager`, `IProfileManager`. Profiles are JSON-serialized; persistence uses an IO task factory for thread safety.
- **`Scripting/`** — Script compilation and execution engine: `IScript`, `IScriptManager`, context-based execution, mock/empty implementations for testing.
- **`Converters/`** — 16 XAML value converters (enum, file size, time span, layout/thickness helpers).
- **`Native/`** — P/Invoke and interop: `Native.Win32` (Win32 API), `Native.MacOS` (NSOpenPanel, NSURL, osascript via dynamic lib loading).

## Patterns

- **MVVM** throughout: `ViewModel<TApp>` base, Avalonia bindings, `INotifyPropertyChanged`.
- **Template Method**: `AppSuiteApplication`, `MainWindowViewModel`, `BaseProfile` all define abstract/virtual hooks (`OnLoad`, `OnSave`, `OnPrepareStarting`, etc.) for subclasses.
- **Partial classes** for platform separation of `AppSuiteApplication`.
- **Generic base classes** with `TApp : IAppSuiteApplication` type constraints propagated across the framework.

## Testing Infrastructure

`Core.Tests/` provides:
- `ApplicationBasedTests` — base class that bootstraps a real `MockAppSuiteApplication` singleton for integration-style tests.
- `MockAppSuiteApplication` — concrete test double of `AppSuiteApplication`.

`Tests/` is a full runnable Avalonia WinExe application used for visual/manual testing of controls and dialogs.
