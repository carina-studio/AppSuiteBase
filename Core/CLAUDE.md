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

`Shutdown(int delay, ApplicationShutdownReason reason)`, the 3-argument `Restart(..., ApplicationShutdownReason reason)`, and the overridable `OnPrepareShuttingDownAsync(ApplicationShutdownReason reason)` carry an `ApplicationShutdownReason` describing *why* the app is shutting down (`None`, `Critical`, `UpdatingApplication`). It replaces the former `isCritical` boolean: "critical" is now `reason == ApplicationShutdownReason.Critical`, which still means asynchronous shutdown preparation is not allowed (`OnPrepareShuttingDownAsync` must complete synchronously). `UpdatingApplication` is set by `ShowAppUpdateDialogAsync` before shutting down to apply an auto-update, letting a subclass react in `OnPrepareShuttingDownAsync` (e.g. releasing files that the external auto-updater must replace). The active reason is also exposed as the read-only `[ThreadSafe]` `ShutdownReason` property (`None` until shutdown starts), raising `PropertyChanged` when set. It is captured on the first `ShutdownAsync` call when the reason is non-`None`; a later call that escalates an in-progress shutdown to `Critical` overwrites it, keeping it consistent with `IsCriticalShutdownStarted`.

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

## Backdrop System

`BackdropTarget`, `Backdrop`, and the `BackdropEffect` enum (in `Controls/`) implement a manual "frosted glass" backdrop: a `BackdropTarget` snapshots its scrollable content into a bitmap, and one or more `Backdrop` overlays sample + blur the region behind themselves from it.

**`BackdropTarget`** is a `Decorator` wrapping the content (typically a `ScrollViewer`). It lazily renders its `Child` into a **single shared** cached `RenderTargetBitmap` (`PrepareBackdrop`), reused by every registered consumer. `internal DrawBackdrop(context, consumer, srcRect, destRect)` is called by a consumer during its render: `srcRect`/`destRect` are in the **consumer's** coordinate space, and the target maps `srcRect` into the bitmap's **pixel** space (Skia's `DrawImage` samples the source rect in pixels, not DIPs) before blitting.

- **Fixed 96-DPI snapshot (hard constraint).** `RenderTargetBitmap.Render` uses `ImmediateRenderer` (not the compositor) and **corrupts any content that has a `BoxShadow` unless the bitmap DPI is exactly 96** — the blur leaks a scale onto following siblings. So the snapshot is always created at `new Vector(96, 96)`; HiDPI-sharp and reduced-resolution snapshots are impossible while box-shadowed content must render correctly (verified empirically — both up- and down-scaling corrupt). On HiDPI this means the backdrop is captured at DIP resolution and upscaled when drawn (soft), which the blur hides.
- **Invalidation.** Auto-invalidation listens for the **bubbling** `ScrollViewer.ScrollChangedEvent` (one handler on the target catches any descendant scrollable — `ScrollViewer`, `ListBox`, nested — and `ScrollChanged` fires on offset **and** extent/viewport changes, so item add/remove is covered too), gated by the `AutoInvalidate` property. `OnSizeChanged` and the public `Invalidate()` also trigger it. `SceneInvalidated` was deliberately **not** used: `CompositingRenderer` always reports a full-window `DirtyRect` with no locality, so it cannot scope to a consumer.
- **`BackgroundColor`.** A `Color` filled (via a cached `ImmutableSolidColorBrush`) behind the snapshot inside `DrawBackdrop`, so a `Backdrop`'s blur samples a real color instead of transparency where the snapshot does not cover — e.g. at content-boundary edges, which otherwise make the blur fade. Changing it repaints consumers (`InvalidateConsumers`) **without** dropping the cached snapshot.

**`Backdrop`** is a `Decorator` that draws the blurred backdrop behind its own content. It hosts an internal `BackdropLayer` visual (added to `VisualChildren` *before* `Child`, which `Decorator` appends) carrying an `ImmutableBlurEffect`; the layer draws the sampled backdrop and the blur applies to it alone, keeping `Child` sharp (`DrawingContext` has **no** inline blur — blur must be a `Visual.Effect`). The `BackdropLayer` (not the `Backdrop`) is the registered consumer; the `Target` property connects it to a `BackdropTarget`.

- **`BackdropEffect`** (the enum, exposed as the `Backdrop.BackdropEffect` property — named so to avoid colliding with `Visual.Effect`; `Blur` / `LensBlur` / `None`, default `None`): `None` draws nothing; `Blur` samples the region directly behind; `LensBlur` samples a *larger* region (inflated `srcRect`) into the same dest. Both inflate the sampled region by `BlurRadius` on all sides so the blur has real content at the edges instead of fading — the layer does not clip to bounds, so the over-draw is included in the effect's input bounds.
- **GPU-only.** Drawing is skipped on the software/CPU pipeline (snapshot+blur is only worthwhile on the GPU). The public static `Backdrop.IsSupported` detects this by **reflecting** the internal `AvaloniaLocator.Current` for a registered `IPlatformGraphics` (those members are `internal` in the ref assembly but `public` at runtime, so the reflection uses `Public | NonPublic`; the result is cached; fail-fast on Avalonia upgrade). `IsBackdropEffectActive` (read-only `DirectProperty`) = `Target != null && BackdropEffect != None && IsSupported && isBackdropEffectEnabled` (the last factor is the `EnableBackdropEffect` toggle below). `ApplicationOptions.IsBackdropEffectSupported` ANDs `Backdrop.IsSupported` with the `EnableBackdropEffect` configuration, so an options UI can hide the backdrop strength setting both where the pipeline is unsupported and where the effect has been turned off.
- **`EnableBackdropEffect` (runtime on/off).** `ConfigurationKeys.EnableBackdropEffect` (`bool`, default `true`) globally enables/disables the effect. When `false`, `RenderBackdrop` short-circuits (draws nothing) and `IsBackdropEffectActive` reports `false`. `Backdrop` reads the flag on attach — falling back to the key's default value when no `IAppSuiteApplication` singleton is present — and subscribes to `Configuration.SettingChanged`; a runtime change re-evaluates the flag and invalidates the **`BackdropLayer`** (not the `Backdrop` decorator, which draws nothing itself), so the surface repaints immediately. The subscription is removed on detach.
- **`Strength` → internal layer opacity (`DefaultBackdropEffectStrength`).** `Strength` (`StyledProperty<double>`, range `[0, 1]`, coerced) is the public knob for backdrop visibility. It **default-binds to the `SettingKeys.DefaultBackdropEffectStrength` setting** (`double`, default `0.5`): on attach the setting value is applied to `Strength` via `SetValue(StrengthProperty, …, BindingPriority.Style)` (re-applied on setting change; the disposable token is cleared on detach to avoid pinning the control alive). `Style` priority sits *below* `LocalValue`, so an explicit `Strength` value/binding on the consumer wins. `Strength` maps **non-linearly** to the **`BackdropLayer`'s** `Opacity` via `UpdateBackdropLayerOpacity()` (called from the constructor and on `Strength` change): `Math.Pow(strength, LayerOpacityGamma=0.8) * MaxLayerOpacity=0.5`, i.e. effective opacity range `[0, 0.5]`. The opacity lives on the **layer**, not the `Backdrop` decorator, so a consumer-set `Opacity`/`OpacityMask` on the `Backdrop` composes independently. `BackdropLayer.Render` **skips** the snapshot+draw work when its `Opacity <= 0` (strength 0); this is safe because `Opacity` is an `AffectsRender` property in Avalonia, so changing it re-records the layer's draw list on the next frame — no manual `InvalidateVisual` is needed. The control clamps the setting to `[0, 1]`, and `ApplicationOptions.DefaultBackdropEffectStrength` (label *"Backdrop Effect"*) now also exposes the full `[0, 1]` range — the earlier narrowed UX bounds (`Min/MaxDefaultBackdropEffectStrength` = `[0.25, 0.75]`) were **removed**, since the `Backdrop` maps strength to opacity itself.

**Effect z-order caveat.** A visual carrying a live `Effect` does **not** reliably respect child z-order in the 11.3 compositor — the blur layer composites *over* an immediately-following sibling (effects are queued as deferred commands, not applied in simple child order). So an overlay meant to sit *on top* of the blur (tint/frame) must be placed **behind** the `Backdrop` and shown through its transparency (the `Strength`-driven layer opacity — lower `Strength` lets more of the tint show through), not stacked in front of it.

## Theming: Selectors for StyleKey-Overriding Controls

Several custom controls override `StyleKeyOverride` to a base Avalonia type (e.g. `TabItem`, `TabControl`, `ListBox`) so they receive the Fluent `ControlTheme` — Avalonia (verified on 12.0.4) resolves control themes by exact style key with **no base-type fallback**, so removing such an override strips the Fluent theme (foreground pseudo-classes, padding baselines) entirely.

Style selectors match a control's **StyleKey**, not its CLR type. The Avalonia 11 XAML compiler masked this by substituting a selector's target type with its compile-time `StyleKeyOverride`; the Avalonia 12 compiler does not, so a derived-type selector like `controls|TabItem` compiles to the derived type and **silently never matches** — the control just falls back to the Fluent template (this is how the TabItem header font size regressed during the Avalonia 12 upgrade).

Convention (see the `TabItem` styles in `Themes/Base-Styles.axaml`): write the selector against the **style key type** (`Selector="TabItem"`), and put `TargetType="controls:TabItem"` on the inner `ControlTemplate` so `TemplateBinding`s to the derived control's properties still compile. A narrower template `TargetType` than the selector type is accepted by the compiler and is safe as long as only the derived control is used with that style in practice.

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
