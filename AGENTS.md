# AGENTS.md

This file provides guidance to AI coding agents when working with code in this repository.

## Workflow

When a change affects the architecture of a library project (`Core`, `Fonts`, `SyntaxHighlighting`), update the corresponding `AGENTS.md` in that project's folder so the documentation stays in sync with the code.

## What This Is

AppSuiteBase is a **cross-platform desktop application framework** built on [Avalonia UI](https://avaloniaui.net/). It is a reusable foundation library for building complex GUI applications — not an end-user application itself. Applications extend its abstract base classes and interfaces.

- GitHub: https://github.com/carina-studio/AppSuiteBase/
- License: MIT, Copyright 2021–2026 Carina Studio
- Current version: 3.0.2.328 (set in `Directory.Build.props`)

## Build Commands

```bash
# Build entire solution
dotnet build AppSuiteBase.sln -c Release

# Build a specific project
dotnet build Core -c Release

# Run unit tests
dotnet test Core.Tests
dotnet test Packaging.Tests

# Pack NuGet packages (outputs to ./Packages)
dotnet pack Core -c Release -o ./Packages --no-build

# Build all packages (Core, Fonts, SyntaxHighlighting)
./BuildPackages.sh
```

**SDK**: .NET 9.0.0 (pinned in `global.json`, `rollForward: latestMajor`, `allowPrerelease: true`)
**Target frameworks**: `net9.0` for libraries; `net10.0` for the `Tests` executable

## Project Structure

| Project | Purpose |
|---|---|
| `Core/` | Main framework library — controls, app lifecycle, view models, scripting, converters |
| `Core.Tests/` | NUnit unit tests for `Core` |
| `Tests/` | Full WinExe test application (renders dialogs, wizards, main window) |
| `Fonts/` | Embedded font assets (Inter, Noto Sans/Serif/SC/TC) |
| `SyntaxHighlighting/` | Syntax highlighting controls and themes |
| `Packaging/` | CLI packaging/installer tool |
| `Packaging.Tests/` | Tests for Packaging |

Shared build configuration lives in `Directory.Build.props`: assembly version, nullable reference types, unsafe blocks, and `InternalsVisibleTo` between projects.

## Code Conventions

### General
- Nullable reference types are enabled (`#nullable enable`) everywhere.
- Compare native handles against `IntPtr.Zero` explicitly (`handle == IntPtr.Zero`), not `default`.
- Do not combine assignment and evaluation into the same expression — assign in its own statement, then use the value (e.g. lazy caching is `Field ??= Create(); return Field;`, never `return Field ??= Create();` or an expression-bodied member doing both).
- Unsafe blocks are allowed globally (set in `Directory.Build.props`).
- All public async methods return `Task`/`ValueTask`; UI-thread operations use `Dispatcher.UIThread`.
- `[ThreadSafe]` attributes mark thread-safe members explicitly.
- Internal APIs are shared between trusted assemblies via `InternalsVisibleTo` in `Directory.Build.props`.
- **Never pass `default` as an argument** — always use an explicit value (e.g. `CancellationToken.None`, `TimeSpan.Zero`).
- **`.Setup()` for `IDisposable` initialization** — when creating an `IDisposable` and setting its properties immediately, do not use object-initializer syntax (`new Foo { Prop = value }`): if the initializer throws, the instance is never disposed. Use the `.Setup(it => ...)` extension instead, which guarantees `Dispose()` is called when the setup action throws.
- **Time units** — milliseconds are the default. Bare `Timeout` / `Delay` / `Interval` names are always milliseconds; do not append `Ms`. Use a unit suffix only when the value is **not** in milliseconds (`SomethingSeconds`, `SomethingMicroseconds`, `SomethingTicks`).
- When a property needs custom accessor logic (validation, change notification, etc.) but `ObservableProperty.Register` is not in use, prefer the C# `field` keyword over a manually-declared backing field.
- When extending a type, prefer an extension **property** inside an `extension(T value)` block over a `GetX()`-style extension method, whenever the accessor is a pure, side-effect-free projection that reads naturally as a property — so call sites read `value.BaseName` rather than `value.GetBaseName()`.

### Method Body Layout
- Inside any code block (method body, `if`/`else`, `for`/`while`/`foreach`, `try`/`catch`/`finally`, lambda body, etc.), group related statements into **logical blocks** separated by a single blank line.
- **Every** logical block is preceded by a single-line `//` comment describing what the block does — including the leading (first) block.
- Exception: when an enclosing block contains only one logical block, the leading comment is optional.
- When you split a logical block into 2 or more sub-blocks (e.g. for readability), each resulting sub-block must have its own leading comment — adding sub-blocks without comments is not allowed.

### File and Type Organization
- One type per file; file name matches the type name exactly.
- A trailing newline at end of file is **not** required — most source files end directly after the closing brace. Do not add one to an existing file, and do not report its absence as a finding.
- Each subsystem gets its own subfolder under `Core/` (e.g. `Scripting/`, `Data/`, `UsageData/`).
- Namespace matches the folder path: `CarinaStudio.AppSuite.<Subfolder>`.
- Subfolder/namespace names use **noun-first** ordering (e.g. `UsageData`, not `DataUsage`).
- Companion types for an interface (`Extensions`, enums) go in separate files in the same folder.
- `extension` blocks (C# 14 extension members) are placed **first** in the containing class, before all other members; they are not sorted with other members. Members inside an `extension` block are ordered alphabetically.
- Inner types are placed **near the top** of the containing type — after the public Avalonia property/event/converter registration fields (if any) and **before** the `// Constants.` data-member group — and ordered **alphabetically** by name among themselves.
- Data members are grouped near the top of the type in this order: (1) constants under a `// Constants.` header, (2) static fields under a `// Static Fields.` header, (3) instance/private fields under a `// Fields.` header. Each group is ordered **alphabetically** by name (case-insensitive). Headers for empty groups are omitted.
- Properties and methods follow the data members, ordered **alphabetically** by name and interleaved together (not grouped by kind).
- **Blank lines between members** — two blank lines between members of a top-level type; one blank line between members of an inner (nested) type.
- Enum values are ordered **alphabetically**. Exception: struct fields with `[StructLayout(LayoutKind.Sequential)]` must preserve their memory-layout order and cannot be reordered.

### Interfaces and Managers
- Subsystem interfaces are named `IXxxManager` and extend `IApplicationObject<IAppSuiteApplication>`.
- Every public member carries an XML doc comment (`/// <summary>`); use `/// <inheritdoc/>` in implementations.
- No backend-specific terminology in interface doc comments — implementation details stay in the concrete class.
- Extension method classes are named `XxxExtensions` and placed in their own file.

### Classes
- Prefer C# primary constructors for simple classes (e.g. `class Foo(IAppSuiteApplication app) : BaseClass(app), IFoo`).
- No-op/mock implementations are named `MockXxx`, marked `internal`, and extend `BaseApplicationObject<IAppSuiteApplication>`.
- Sensitive fields and methods in obfuscated assemblies are annotated with `[Obfuscation(Exclude = false)]`.

### Manager Registration in `AppSuiteApplication`
- Each manager has a backing field (`IXxxManager? xxxManager`), a public property, and a protected virtual `XxxManagerImplType` property.
- `XxxManagerImplType` is decorated with `[DynamicallyAccessedMembers(...)]` and suppressed with `// ReSharper disable UnassignedGetOnlyAutoProperty`.
- Initialization follows the product manager pattern: call `InitializeAsync` and get `Default` via reflection, fall back to `MockXxxManager` on failure or when no impl type is provided.
- `IAppSuiteApplication` exposes the manager as a read-only property alongside the other managers.

### Platform-Specific Code (`#pragma warning disable CA1416`)
- Suppress CA1416 only when calling APIs that the .NET runtime annotates with `[SupportedOSPlatform("windows")]` (e.g. `Registry`, `WindowsIdentity`).
- Custom P/Invoke definitions in `Native.Win32` do **not** carry that annotation and do not require CA1416 suppression at their call sites.

### Localized Strings

String resources live in `Core/Strings/` and `SyntaxHighlighting/Strings/`, one file per culture plus optional `-Linux` / `-OSX` overrides. Quoting follows the target locale, not the source text:

| Locale | Quoting a name in prose |
|---|---|
| `zh-TW`, `ja-JP` | `AAA「BBB」CCC` — corner brackets, no surrounding whitespace |
| `zh-CN` | `AAA “BBB” CCC` — curly double quotes, one half-width space on each side |
| `Default` (en) | `AAA 'BBB' CCC` |

- Corner brackets (`「」`) are **wrong in `zh-CN`**, which uses `“”`.
- Drop the surrounding space when the quote sits next to `，`, `。`, `、` or `…` — the full-width punctuation already carries it (`…并存放至 “BBB”。`, `正在执行 “{0}”…`).
- **File names and file paths take ASCII single quotes in every locale**: `AAA 'Path' BBB`, spaced the same way. This overrides the locale quoting above, so a placeholder holding a path is never wrapped in `「」` or `“”`. An *alias* for a location — the macOS "Applications" folder, for instance — is a name, not a path, and keeps the locale quotes.
- A product or application name substituted from a placeholder takes no quotes at all: `无法启用 {0}，请尝试再次启用。`
- Document titles use `《…》` in `zh-CN` / `zh-TW`, `「…」` in `ja-JP`.

### Project-Specific Rules

Rules that apply only within one project — everything above applies solution-wide. Each project's own `AGENTS.md` documents its architecture, not its rules.

- **`Packaging`** —
  - New commands are added as private methods dispatched from `Run()`; keep the dispatch table in `Run()` alphabetically ordered by command name.
  - Use `SmallestSize` compression when writing ZIP archives (consistent with existing diff-package logic).

## Code Review Checklist

### Correctness
- Logic is correct for all paths, including edge cases (empty collections, null values, zero counts).
- Multi-step operations that must be atomic are protected by a lock or semaphore across all steps, not just individual operations.
- State mutations under a lock do not leak mutable references that can be read or written outside the lock.
- `async`/`await` is used correctly — no fire-and-forget unless intentional; no `.Result` or `.Wait()` blocking on async code.
- `CancellationToken` is propagated through all async calls; `OperationCanceledException` is not swallowed.
- `IDisposable` resources are disposed in all paths, including error paths.

### Thread Safety
- Shared mutable fields accessed from multiple threads are protected consistently.
- No TOCTOU (time-of-check/time-of-use) races — check and act happen under the same lock or synchronization primitive.
- Background-thread methods are marked `[CalledOnBackgroundThread]`; UI-thread calls are dispatched via `SynchronizationContext` or guarded with `CheckAccess()`.

### Error Handling
- Exceptions are not silently swallowed — at minimum log the error.
- Expected failure paths (file missing, manager unavailable) are logged at `Warning`; unexpected exceptions at `Error`.
- Best-effort operations (e.g. cleanup) catch and log per-item rather than aborting the entire operation.

### Style
- All coding style rules above are followed (naming, formatting, nullability, patterns).
- Unused `using` directives removed; correct namespaces imported for any new types introduced.
- `default` is not passed as an argument — explicit values used instead.
- **Member ordering** is correct: `extension` blocks → inner types → constants → static fields → instance fields → properties and methods interleaved alphabetically. Verify after adding, renaming, or moving any member.
- Every logical block inside a code block carries its own leading `//` comment, per *Method Body Layout*.
- Localized strings use the target locale's quoting — `「」` for zh-TW/ja-JP, `“”` for zh-CN, ASCII `'…'` for file names and paths in every locale.

### Documentation
- Check whether the change affects the architecture of a library project (`Core`, `Fonts`, `SyntaxHighlighting`). If so, the corresponding project's `AGENTS.md` should be updated to match.
