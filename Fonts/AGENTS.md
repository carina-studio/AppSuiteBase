# Fonts — Architecture

## Purpose

Provides centralized access to embedded font families for use across the AppSuite framework. Depends on `Core`.

## Key Class: `BuiltInFonts` (`Media/BuiltInFonts.cs`)

Static service with lazy-initialized `FontFamily` properties for every embedded font:

- **From this project**: `IBMPlexMono`, `Roboto`, `RobotoMono`, `SourceCodePro`
- **From Core resources**: `Inter`, `NotoSans`, `NotoSansJP`, `NotoSansMono`, `NotoSansSC`, `NotoSansTC`, `NotoSerif`
- **`FontFamilies`** — read-only collection of all available fonts
- **`OpenStream()`** — utility to load a font file stream for a given family, weight, and style

Adding a family means four alphabetically-ordered edits: the backing field, the `FontFamily` property, an entry in `FontFamilies`, and — for a Core-resource face — a `nameof(...)` arm in the Noto or Inter group of `OpenStream`. A `#if DEBUG` static initializer validates all of it at first use: it walks every public `FontFamily` property, throws if a family loaded no typefaces, and opens each font's stream, so a wrong resource URI or file name fails the Debug build rather than shipping. When the new face is added for a new application culture, follow the *Changing or adding an embedded font* checklist in `Core/AGENTS.md`, which covers the `Core`-side work this depends on.

## Embedded Resources (`Resources/Fonts/`)

48 TTF files covering 12 font families with weight/style variants:

| Family | Type | Notes |
|---|---|---|
| IBM Plex Mono | Monospace | 6 variants |
| Roboto Mono | Monospace | 6 variants |
| Source Code Pro | Monospace | 6 variants |
| Roboto | Sans-serif | 6 variants |
| Inter | Sans-serif | 2 variants |
| Noto Sans JP / SC / TC | CJK Sans-serif | Japanese / Simplified Chinese / Traditional Chinese |
| Noto Serif | Serif | 3 variants |

## Design Note

All `FontFamily` properties use lazy initialization with caching to avoid repeated instantiation cost.
