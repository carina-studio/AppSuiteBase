# Fonts — Architecture

## Purpose

Provides centralized access to embedded font families for use across the AppSuite framework. Depends on `Core`.

## Key Class: `BuiltInFonts` (`Media/BuiltInFonts.cs`)

Static service with lazy-initialized `FontFamily` properties for every embedded font:

- **From this project**: `IBMPlexMono`, `Roboto`, `RobotoMono`, `SourceCodePro`
- **From Core resources**: `Inter`, `NotoSans`, `NotoSansMono`, `NotoSansSC`, `NotoSansTC`, `NotoSerif`
- **`FontFamilies`** — read-only collection of all available fonts
- **`OpenStream()`** — utility to load a font file stream for a given family, weight, and style

## Embedded Resources (`Resources/Fonts/`)

48 TTF files covering 12 font families with weight/style variants:

| Family | Type | Notes |
|---|---|---|
| IBM Plex Mono | Monospace | 6 variants |
| Roboto Mono | Monospace | 6 variants |
| Source Code Pro | Monospace | 6 variants |
| Roboto | Sans-serif | 6 variants |
| Inter | Sans-serif | 2 variants |
| Noto Sans SC / TC | CJK Sans-serif | Simplified/Traditional Chinese |
| Noto Serif | Serif | 3 variants |

## Design Note

All `FontFamily` properties use lazy initialization with caching to avoid repeated instantiation cost.
