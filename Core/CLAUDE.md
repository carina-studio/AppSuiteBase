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

### Shutdown Reason

`Shutdown(int delay, ApplicationShutdownReason reason)`, the 3-argument `Restart(..., ApplicationShutdownReason reason)`, and the overridable `OnPrepareShuttingDownAsync(ApplicationShutdownReason reason)` carry an `ApplicationShutdownReason` describing *why* the app is shutting down (`None`, `Critical`, `UpdatingApplication`). It replaces the former `isCritical` boolean: "critical" is now `reason == ApplicationShutdownReason.Critical`, which still means asynchronous shutdown preparation is not allowed (`OnPrepareShuttingDownAsync` must complete synchronously). `UpdatingApplication` is set by `ShowAppUpdateDialogAsync` before shutting down to apply an auto-update, letting a subclass react in `OnPrepareShuttingDownAsync` (e.g. releasing files that the external auto-updater must replace). The reason is threaded through the shutdown flow only — it is not exposed as a property.

### Restart After System Reboot (Windows)

The framework relaunches the app after a Windows system reboot using a `RunOnce` registry entry. `RunOnce` is used in place of `RegisterApplicationRestart` because the OS-level "Restart apps" sign-in setting is off by default on most installs, which makes `RegisterApplicationRestart` unreliable in practice.

The relaunch is always-on for AppSuite-based applications on Windows. The `RestoreMainWindowsAfterSystemReboot` property is a separate, orthogonal concern: when `true`, the relaunch command line includes the `-restore-main-windows` argument so that main windows are restored on the next run; when `false`, the app is still relaunched but without window restoration.

Lifecycle:
1. **On launch** (during `OnPrepareStartingAsync`): any stale entry left from a previous shutdown is removed via `RemoveRunOnceEntryForRestartOnWindows()`. Defensive cleanup — under normal flow `RunOnce` self-deletes when it fires, so this is usually a no-op.
2. **On OS-initiated shutdown** (Avalonia's `ShutdownRequested` event fires — i.e. `WM_QUERYENDSESSION`): the handler sets `shutdownSource = System` (matching the macOS behavior) and calls `WriteRunOnceEntryForRestartOnWindows()`, which writes an entry to `HKCU\Software\Microsoft\Windows\CurrentVersion\RunOnce` pointing at the current exe with the `-restarted-by-system` argument always appended, plus `-restore-main-windows` when `RestoreMainWindowsAfterSystemReboot == true`. The entry name is `CarinaStudio.<AssemblyName>.RestoreAfterReboot`, namespaced by assembly name so multiple AppSuite-based apps do not collide.
3. **On graceful, app-initiated shutdown** (`ShutdownAsync` called with a non-`Critical` reason *and* `shutdownSource == ShutdownSource.Application` — e.g. File → Exit, in-app `Restart()`): the entry is removed via `RemoveRunOnceEntryForRestartOnWindows()`. Critical shutdowns skip cleanup. **Importantly**, OS-initiated shutdowns also reach `ShutdownAsync` (Avalonia drives it after `ShutdownRequested`) but their `shutdownSource == ShutdownSource.System` — the cleanup must be skipped there, otherwise the entry just written in step 2 would be wiped before reboot.
4. **On next login** (when an OS shutdown actually proceeded): Windows fires `RunOnce` once and auto-deletes the entry. The app launches with `-restarted-by-system` (and optionally `-restore-main-windows`). The argument parser sets `LaunchOptionKeys.IsRestartedBySystem`, and if `-restore-main-windows` was included, `IsRestoringMainWindowsRequested` drives main-window restoration.
5. **Duplicate suppression for late system relaunch**: Windows can take minutes to fire `RunOnce` after sign-in. If the user manually launches the app before that, the system-fired instance will arrive later and find an existing multi-instance server already running. `SendArgumentsToMultiInstancesServer` short-circuits when `LaunchOptionKeys.IsRestartedBySystem == true` — it connects to verify the server is alive, then returns without forwarding the args (which would otherwise inject `-restore-main-windows` into the user's already-open session and trigger redundant window restoration). The late instance silently shuts down; the user's session is untouched.

Force-kill paths (Task Manager → End Process, `taskkill /F`, antivirus, crash) bypass both hooks: nothing is written if no shutdown was in progress, and if a force-kill occurs *during* an OS shutdown the entry survives, which is correct because the user did request the reboot.

**Known limitation — cancelled system shutdown.** If `WM_QUERYENDSESSION` fires (entry written, `shutdownSource` becomes `System`) and the shutdown is then cancelled by another app, `shutdownSource` is sticky and subsequent app-initiated graceful exits will *also* see it as `System` — so the cleanup branch in step 3 is skipped and the stale entry survives until either (a) the next system reboot consumes it, or (b) the launch-time cleanup in step 1 removes it on a manual relaunch. In the meantime, a spurious relaunch may occur on the next login. This trade-off is accepted: gating cleanup on `shutdownSource` is what makes the primary case (system reboot relaunches the app) work correctly.

All registry operations are best-effort and exception-safe; failures never propagate.

### Default Font Configuration

Wired in `BuildApplication` via `FontManagerOptions.DefaultFamilyName`. The value is a **composite font family name** (comma-separated, parsed by Avalonia as primary + fallback chain): `Inter` as the Latin face, followed by `Noto Sans SC` / `Noto Sans TC` for CJK glyph coverage. The order of the two CJK faces is variant-aware — Simplified-first for `ChineseVariant.Default`, Traditional-first for `ChineseVariant.Taiwan` — driven by `_LaunchChineseVariant` (resolved from `SettingKeys.Culture` earlier in the same builder).

The composite-name approach replaces an earlier `FontFallbacks` block keyed by CJK `UnicodeRange`s. Both approaches solved glyph routing, but the composite-name form makes the variant ordering live next to the primary family and removes the duplicate Unicode-range bookkeeping. Embedded font collections (`fonts:Inter`, `fonts:Noto`) are registered via `ConfigureFonts` immediately above and must stay in sync with the family names referenced here.

**Line height is *not* normalized here.** Inter and the Noto Sans CJK faces have different intrinsic ascent/descent, so per-run line height still diverges across scripts even with the composite default. Uniform line height across mixed-script text is the responsibility of the `TextBlock` layer (explicit `LineHeight`). Do not try to "fix" the per-script height divergence inside `FontManagerOptions` — Avalonia's `TextLayout` computes line height from each run's resolved typeface, so the only levers that work at this layer are (a) matched-metric fonts, and (b) per-control `LineHeight`.

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
