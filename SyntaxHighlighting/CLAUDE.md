# SyntaxHighlighting — Architecture

## Purpose

Provides syntax-highlighted text controls and a token-based rendering pipeline for use in AppSuite applications. Depends on `Fonts` (which transitively pulls in `Core`).

## Highlighting Engine (`Controls/Highlighting/`)

- **`SyntaxHighlighter`** — `AvaloniaObject`-based engine. Owns the text formatting pipeline via Avalonia's `TextFormatting` APIs. Key properties: `DefinitionSet`, `Background`, `Foreground`, `FontFamily`, `FontSize`, font weight/style/stretch, `IsMaxTokenCountReached`.
- **`SyntaxHighlightingDefinitionSet`** / **`SyntaxHighlightingDefinition`** — language definition schemas loaded at runtime.
- **`SyntaxHighlightingToken`** — individual token (type, position, styling).
- **`SyntaxHighlightingSpan`** — text span with associated highlighting rules.
- **`RegexSyntaxHighlighting`** — regex-based pattern matcher that produces tokens.

## Text Box Controls

Specialty input controls with live syntax highlighting:

| Class | Purpose |
|---|---|
| `SyntaxHighlightingTextBox` | Base highlighted text box |
| `SyntaxHighlightingObjectTextBox` | Object/serialized data |
| `SyntaxHighlightingValueTextBox` | Structured values |
| `RegexTextBox` | Regex pattern editor with live validation |
| `StringInterpolationFormatTextBox` | String interpolation format |
| `DateTimeFormatTextBox` | Datetime format specification |
| `TimeSpanFormatTextBox` | Timespan format specification |

## Display Controls

- **`SyntaxHighlightingTextBlock`** — read-only highlighted text display.
- **`SelectableSyntaxHighlightingTextBlock`** — user-selectable highlighted text.

## Service Initialization (`Controls/SyntaxHighlighting.cs`)

Static entry point: `SyntaxHighlighting.InitializeAsync(IAppSuiteApplication app)`. Handles:
- Dynamic Avalonia XAML resource loading
- Culture/theme switching
- Trimming prevention for compiled Avalonia resources
