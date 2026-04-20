# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Workflow

When solving a bug or adding a feature, **always present a plan first** and wait for explicit user approval before making any code changes.

After a code change is confirmed (by user approval or during code review), check whether the change affects the architecture of a library project (`Core`, `Fonts`, `SyntaxHighlighting`). If so, ask the user whether to update the corresponding `CLAUDE.md` in that project's folder, and update it only upon confirmation.

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
- Unsafe blocks are allowed globally (set in `Directory.Build.props`).
- All public async methods return `Task`/`ValueTask`; UI-thread operations use `Dispatcher.UIThread`.
- `[ThreadSafe]` attributes mark thread-safe members explicitly.
- Internal APIs are shared between trusted assemblies via `InternalsVisibleTo` in `Directory.Build.props`.

### File and Type Organization
- One type per file; file name matches the type name exactly.
- Each subsystem gets its own subfolder under `Core/` (e.g. `Scripting/`, `Data/`, `UsageData/`).
- Namespace matches the folder path: `CarinaStudio.AppSuite.<Subfolder>`.
- Subfolder/namespace names use **noun-first** ordering (e.g. `UsageData`, not `DataUsage`).
- Companion types for an interface (`Extensions`, enums) go in separate files in the same folder.
- Inner types within a class/file are ordered **alphabetically** by name.
- Members within a type (enum values, properties, methods) are also ordered **alphabetically**. Exception: struct fields with `[StructLayout(LayoutKind.Sequential)]` must preserve their memory-layout order and cannot be reordered.

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
