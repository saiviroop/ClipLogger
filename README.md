# Clip Logger

A free, lightweight **Windows clipboard logger / text-capture tray app** for
collecting debug context while you work. Press **Ctrl+Alt+C** to capture the
currently-selected text into a dated `.txt` log file, with timestamps, source
tracking, and separators between entries. Normal `Ctrl+C` is untouched — it's
**not a keylogger**: it only captures the text you've selected, and only when you
press its hotkey.

**🌐 Website:** <https://saiviroop.github.io/ClipLogger/>
&nbsp;·&nbsp; **⬇ [Download for Windows](https://github.com/saiviroop/ClipLogger/releases/latest/download/ClipLogger-Setup.exe)** (self-contained installer, no .NET needed)

## Features

- Global hotkey capture (`Ctrl+Alt+C`) — normal copy/paste unaffected
- **Live viewer** (`Ctrl+Alt+V` or tray → Open Viewer): auto-refreshing window
  with a case-insensitive search/filter box
- **Source tracking**: each entry records which app/window the text came from,
  e.g. `[2026-06-19 11:30:00]  (from: chrome — GitHub)`
- Dated log files: `cliplog-YYYY-MM-DD_HH-mm.txt` in a folder you choose
- Each entry: timestamp + your text + 4 blank lines + a dashed separator
- Distinct tray icon + live tooltip showing the entry count and logging state
- Tray menu: Open Viewer, New File, Stop/Start logging, Open Log Folder,
  Settings, Exit
- Configurable check-in interval (default 60 min) prompting Continue / New File
- Optional auto-start on login (per-user) — installer checkbox + Settings toggle
- Inno Setup installer (`ClipLogger-Setup.exe`), self-contained (no .NET needed)

## Project layout

- `src/ClipLogger.Core` — platform-neutral, fully unit-tested logic
- `src/ClipLogger.App` — WinForms tray app (hotkey, clipboard, settings)
- `tests/ClipLogger.Tests` — xUnit tests for Core
- `installer/ClipLogger.iss` — Inno Setup script

## Build & test

```
dotnet build
dotnet test
dotnet run --project src/ClipLogger.App
```

## Build the installer

```
dotnet publish src/ClipLogger.App/ClipLogger.App.csproj -c Release -r win-x64 --self-contained true -o installer/app
"%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" installer\ClipLogger.iss
```

The installer is produced at `installer/Output/ClipLogger-Setup.exe`.

## Changelog

### 1.1.0
- **Fixed**: the check-in prompt could stack endlessly — once it had been open
  for a minute it spawned a new copy every 60 s (a WinForms timer keeps firing
  while a modal dialog pumps messages). A re-entrancy guard now ensures only one
  check-in prompt is ever shown at a time.
- Added an embedded application icon (`app.ico`) so the exe and desktop shortcut
  show the clipboard glyph instead of the generic .NET icon.

## Notes / trade-offs

- Captures selected **text** only (no images). A few apps that ignore a
  simulated `Ctrl+C` (some terminals, protected fields) won't capture.
- `Ctrl+Alt+C` leaves the captured text on the clipboard (by design).
- Logs are plain text on disk — avoid capturing secrets into them.

## License

Released under the [MIT License](LICENSE). Free to use, modify, and distribute.
