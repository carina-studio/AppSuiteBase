# AGENTS.md — Packaging

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
| Target frameworks | Whatever `Directory.Build.props` sets — currently `net9.0`, `net10.0` |
| Output type | Class library |
| Key dependency | `CarinaStudio.AppBase.Core` (version pinned by `$(AppBaseVersion)`) |

This project **follows the solution-wide target frameworks** — do not declare `<TargetFrameworks>`
in `Packaging.csproj`. It targeted `net8.0` in the past; that was dropped deliberately, so do not
add it back to widen support for older consumers.

Version properties (`AssemblyVersion`, `Version`, `PackageVersion`) *are* overridden here, because
the package is versioned independently of the rest of the solution. Everything else — target
frameworks, authors, company, copyright, license, project URL, nullable — comes from the root
`Directory.Build.props`.

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
| `create-package-manifest` | Emit a `PackageManifest.json` (or `PackageManifest-{platform}.json`) with SHA-256 checksums and download URLs on GitHub or Cloudflare R2 |
| `get-current-version` | Extract `<AssemblyVersion>` or `<Version>` from a `.csproj` |
| `get-current-informational-version` | Extract informational version from a `.csproj` |
| `get-previous-version` | Scan the `Packages/` directory to find the previous release version |

### `create-package-manifest` Arguments

```
create-package-manifest [{Storage}] [{Platform}] {Repository} {Version} [{InformationalVersion}]
```

| Argument | Description |
|---|---|
| `Storage` | Where the packages are hosted, case-insensitive: `github` (default) or `cloudflare` |
| `Platform` | Include only packages whose platform identifier starts with it (e.g. `win`, `osx-arm64`), and name the manifest `PackageManifest-{Platform}.json` |
| `Repository` | Name of the GitHub repository, which is also the folder name on Cloudflare R2 |
| `Version` | Version of the release; packages are read from `Packages/{Version}/` |
| `InformationalVersion` | Informational version of the release; used instead of `Version` as `{Tag}` in URIs |

When only one argument precedes `Repository`, it is taken as `Storage` if it names one, otherwise as `Platform`. An unknown `Storage` is rejected with `InvalidArgument` before any file is written.

Package URIs by storage, where `{Tag}` is `InformationalVersion` if given, otherwise `Version`:

- `github` — `https://github.com/carina-studio/{Repository}/releases/download/{Tag}/{FileName}`
- `cloudflare` — `https://packages.carinastudio.net/{Repository}/{Tag}/{FileName}`

`PageUri` always points to the GitHub release page `https://github.com/carina-studio/{Repository}/releases/tag/{Tag}`, whichever storage is used.

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

Follow the conventions in the root [`AGENTS.md`](../AGENTS.md), including the `Packaging` entry under *Project-Specific Rules*.
