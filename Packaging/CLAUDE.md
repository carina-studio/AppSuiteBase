# CLAUDE.md — Packaging

## What This Is

`CarinaStudio.AppSuite.Packaging` is a **class library** that provides CLI-invokable packaging helpers for Carina Studio AppSuite application releases. It automates:

- Generating **differential (diff) packages** between two versions
- Creating **package manifest JSON** files for distribution/auto-update
- Extracting **version information** from `.csproj` files

It is consumed by build pipelines and the `Packaging.Tests` test project. It is **not** a GUI application.

## Project Metadata

| Property | Value |
|---|---|
| NuGet ID | `CarinaStudio.AppSuite.Packaging` |
| Root namespace | `CarinaStudio.AppSuite.Packaging` |
| Target frameworks | `net8.0`, `net9.0`, `net10.0` |
| Output type | Class library |
| Key dependency | `CarinaStudio.AppBase.Core` |

## Structure

The project is intentionally minimal — a single source file at the root:

```
Packaging/
├── Packaging.csproj
└── PackagingTool.cs      ← entire library implementation
```

All logic lives in `PackagingTool.cs`. Do not split into subfolders unless the file grows to a size that genuinely warrants it.

## Key Types

### `PackagingTool`

The sole public class. Call `Run(IList<string> args)` with a command as the first argument.

| Command | Description |
|---|---|
| `create-diff-packages` | Compare previous and current package ZIPs; output a ZIP containing only changed/new files |
| `create-package-manifest` | Emit a `PackageManifest.json` (or `PackageManifest-{platform}.json`) with SHA-256 checksums and GitHub download URLs |
| `get-current-version` | Extract `<AssemblyVersion>` or `<Version>` from a `.csproj` |
| `get-current-informational-version` | Extract informational version from a `.csproj` |
| `get-previous-version` | Scan the `Packages/` directory to find the previous release version |

### `PackagingResult` (enum)

Return code from `Run()`:

| Value | Meaning |
|---|---|
| `Success` | Operation completed |
| `InvalidArgument` | Bad or missing CLI arguments |
| `UnclassifiedError` | IO, parsing, or other errors |
| `FileOrDirectoryNotFound` | Missing package file or directory |
| `ProjectNotFound` | Cannot locate the `.csproj` file |

## Package Naming Convention

```
{AppName}-[{PrevVersion}-]{Version}-{PlatformId}[-fx-dependent].zip
```

- Standard package: `MyApp-2.0.0-win-x64.zip`
- Framework-dependent: `MyApp-2.0.0-win-x64-fx-dependent.zip`
- Diff package: `MyApp-1.0.0-2.0.0-win-x64.zip`

Supported platform identifiers: `win-x86`, `win-x64`, `win-arm64`, `osx-x64`, `osx-arm64`, `linux-x64`, `linux-arm64`

## Code Conventions

Follow all conventions in the root [`CLAUDE.md`](../CLAUDE.md). Additional notes specific to this project:

- Every public member must have an XML doc comment (`/// <summary>`); keep the generated `.xml` doc file in sync.
- New commands are added as private methods dispatched from `Run()`; keep the dispatch table in `Run()` alphabetically ordered by command name.
- Use `SmallestSize` compression when writing ZIP archives (consistent with existing diff-package logic).
