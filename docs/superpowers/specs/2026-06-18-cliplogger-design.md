# Clip Logger — Design Spec

**Date:** 2026-06-18
**Status:** Approved (design), pending implementation plan
**Author:** jl@omnisheets.ai

## Problem

When debugging a project, the user needs to collect many pieces of context —
network logs, application logs, error messages, snippets — by copying them from
various windows. Manually pasting each one into a single Notepad file, adding
spacing, and separating entries is tedious and error-prone.

## Goal

A small Windows tray application that, on a dedicated hotkey, captures the
currently-selected text and appends it to a single, dated, well-formatted `.txt`
log file — leaving normal copy/paste behavior untouched.

## Platform & Technology

- **OS:** Windows 11 (also fine on Windows 10).
- **Language:** C# / .NET (WinForms for tray + dialogs).
- **App build:** self-contained `.exe` (the running application).
- **Distribution:** a proper **installer** (`Setup.exe`) built with **Inno Setup**.
  The installer offers a **"Start Clip Logger on login"** checkbox, installs the
  app, and adds a Start menu shortcut.
- **Rationale:** C# gives concise, reliable support for tray icon (`NotifyIcon`),
  global hotkey (`RegisterHotKey` via P/Invoke), clipboard, timers, and dialogs.
  C++/Win32 would only win on file size, at 3–5× the code and risk. Inno Setup is
  free, simple, and the standard for lightweight Windows installers.

## Core Behavior

### Startup
1. On first run, prompt the user to choose a **folder** to save logs in.
   Persist this choice (e.g. local config file) for subsequent runs.
2. Create the first log file named with date + time to avoid collisions:
   `cliplog-YYYY-MM-DD_HH-mm.txt` (e.g. `cliplog-2026-06-18_14-32.txt`).
3. Show a **tray icon** near the clock. Logging starts in the *active* state.

### Capture hotkey — `Ctrl+Alt+C`
- Registered globally via `RegisterHotKey`.
- On press (while logging is active):
  1. Simulate `Ctrl+C` to copy the currently-selected text in the active window.
  2. Read the resulting text from the clipboard.
  3. Append a formatted entry to the current log file.
- The captured text **remains on the clipboard** (Option A — no clipboard
  protection/restore).
- **Normal `Ctrl+C` is never intercepted or modified.** Text selection works
  normally everywhere.
- If the clipboard has no text after the simulated copy (e.g. nothing selected,
  or an app that ignores simulated copy), skip silently / show a brief tray tip.

### Entry format
Each appended entry:
```
[YYYY-MM-DD HH:mm:ss]
<captured text>




----------------------------------------
```
- One timestamp line.
- The captured text.
- 4 blank lines.
- A separator line of `-` characters (40 dashes).

### Tray icon menu (right-click)
- **New File** — close current file, start a fresh `cliplog-…` file immediately.
  (For "I've captured what I needed; start clean.")
- **Stop / Start logging** — toggle. When stopped, `Ctrl+Alt+C` is ignored until
  started again. Menu label reflects current state.
- **Open Log Folder** — open the save folder in File Explorer.
- **Settings…** — open the Settings dialog (see below).
- **Exit** — quit the app (unregister hotkey, remove tray icon).

### Settings dialog
A small dialog reachable from the tray menu, exposing:
- **Log folder** — view/change the save location (applies to subsequent files).
- **Check-in interval** — user-configurable duration for the periodic prompt
  (e.g. 30 min / 1 hr / 2 hr, or a numeric minutes field). Default: 60 minutes.
- **Start Clip Logger on login** — checkbox to enable/disable auto-start. Toggling
  it adds/removes a per-user registry entry under
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- Settings persist to the local config file and apply immediately.

### Configurable check-in
- Each file tracks the time it was created.
- When a file has been active for the **configured interval** (default 1 hour),
  show a prompt:
  *"This log file has been active for {interval}. Continue with it, or start a new
  file?"* → **Continue** / **New File**.
- **Continue** resets the timer on the same file.
- **New File** starts a fresh file (new timer).
- Changing the interval in Settings re-bases the running timer.

### Auto-start on login (user-specific)
- Implemented via the per-user `HKCU\…\Run` registry key (no admin rights needed),
  pointing to the installed app `.exe`.
- The **installer** sets this if the user ticks "Start Clip Logger on login".
- The **Settings dialog** can enable/disable it at any time, keeping the registry
  entry and the saved setting in sync.

## Components

- **TrayApp** — owns the `NotifyIcon`, context menu, and app lifecycle.
- **HotkeyManager** — registers/unregisters the global hotkey, raises an event on
  press.
- **ClipboardCapture** — simulates copy, reads clipboard text.
- **LogWriter** — owns the current file path, formats and appends entries,
  creates new files.
- **SessionTimer** — tracks per-file elapsed time, fires the configurable check-in.
- **Config** — load/save the chosen folder, check-in interval, and auto-start flag.
- **AutoStartManager** — reads/writes the `HKCU\…\Run` registry entry.
- **SettingsDialog** — UI for folder, interval, and auto-start.
- **Installer (Inno Setup script)** — packages the app, offers the auto-start
  checkbox, creates shortcuts.

## Data Flow

1. User selects text → presses `Ctrl+Alt+C`.
2. `HotkeyManager` raises event → `TrayApp` asks `ClipboardCapture` for text.
3. `ClipboardCapture` simulates Ctrl+C, returns clipboard text.
4. `TrayApp` passes text to `LogWriter` → appends formatted entry to current file.
5. `SessionTimer` independently fires after the configured interval → `TrayApp`
   shows the check-in prompt.

## Error Handling

- **No text captured:** skip the entry; optionally show a brief balloon tip.
- **File write failure** (folder removed, permissions, drive unplugged): show a
  tray notification and pause logging until the user picks a valid folder.
- **Hotkey registration fails** (already in use): notify the user and offer to
  pick a different combo (future enhancement; v1 may just report the conflict).
- **Folder no longer exists on startup:** re-prompt for a folder.

## Testing Strategy

- **LogWriter:** unit-test entry formatting (timestamp, text, 4 blank lines,
  separator) and new-file creation/naming.
- **Config:** round-trip save/load of folder path, interval, and auto-start flag.
- **SessionTimer:** verify the configured-interval fire (with an injectable/short
  interval for tests).
- **AutoStartManager:** verify enable/disable writes and removes the `HKCU\…\Run`
  entry correctly.
- **Manual end-to-end:** select text in several apps (browser, VS Code, terminal),
  press hotkey, confirm correct appends; verify tray menu actions, Settings
  (folder/interval/auto-start), the check-in prompt (using a shortened interval
  during testing), and an install/uninstall run of the Setup.exe including the
  auto-start checkbox.

## Out of Scope (v1) / YAGNI

- Capturing images/screenshots (text only).
- Clipboard protection/restore (Option B).
- Configurable hotkey UI (hardcoded `Ctrl+Alt+C` for v1; conflict reported).
- Configurable format / separators / blank-line count.
- Log rotation, compression, encryption.

## In Scope (added in revision)

- **Configurable check-in interval** (Settings; default 60 min).
- **Auto-start on login** (per-user; installer checkbox + Settings toggle).
- **Settings dialog** (folder, interval, auto-start).
- **Inno Setup installer** (`Setup.exe`) with auto-start checkbox + shortcuts.

## Distribution & Repository

- Source hosted on GitHub under **`saiviroop/ClipLogger`** (new repo).
- Repo to be created and code pushed **after the app builds and runs** — not as an
  empty repo. Requires one-time GitHub auth on this machine (`gh` CLI install +
  login, or a Personal Access Token).

## Known Trade-offs

- Text only; some apps ignore simulated `Ctrl+C`.
- `Ctrl+Alt+C` overwrites the clipboard (by design choice).
- Plain-text on disk — user must avoid capturing secrets.
- App `.exe` is a few MB; the installer adds a small amount on top.
