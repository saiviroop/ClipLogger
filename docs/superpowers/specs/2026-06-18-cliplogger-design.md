# ClipLogger — Design Spec

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
- **Distribution:** single self-contained `.exe`, no installer.
- **Rationale:** C# gives concise, reliable support for tray icon (`NotifyIcon`),
  global hotkey (`RegisterHotKey` via P/Invoke), clipboard, timers, and dialogs.
  C++/Win32 would only win on file size, at 3–5× the code and risk.

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
- **Change Folder** — pick a new save location (applies to subsequent files).
- **Exit** — quit the app (unregister hotkey, remove tray icon).

### 1-hour check-in
- Each file tracks the time it was created.
- When a file has been active for **1 hour**, show a prompt:
  *"This log file has been active for 1 hour. Continue with it, or start a new
  file?"* → **Continue** / **New File**.
- **Continue** resets the 1-hour timer on the same file.
- **New File** starts a fresh file (new timer).

## Components

- **TrayApp** — owns the `NotifyIcon`, context menu, and app lifecycle.
- **HotkeyManager** — registers/unregisters the global hotkey, raises an event on
  press.
- **ClipboardCapture** — simulates copy, reads clipboard text.
- **LogWriter** — owns the current file path, formats and appends entries,
  creates new files.
- **SessionTimer** — tracks per-file elapsed time, fires the 1-hour check-in.
- **Config** — load/save the chosen folder (and any settings).

## Data Flow

1. User selects text → presses `Ctrl+Alt+C`.
2. `HotkeyManager` raises event → `TrayApp` asks `ClipboardCapture` for text.
3. `ClipboardCapture` simulates Ctrl+C, returns clipboard text.
4. `TrayApp` passes text to `LogWriter` → appends formatted entry to current file.
5. `SessionTimer` independently fires after 1 hour → `TrayApp` shows check-in.

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
- **Config:** round-trip save/load of folder path.
- **SessionTimer:** verify the 1-hour fire (with an injectable/short interval for
  tests).
- **Manual end-to-end:** select text in several apps (browser, VS Code, terminal),
  press hotkey, confirm correct appends; verify tray menu actions and the 1-hour
  prompt (using a shortened interval during testing).

## Out of Scope (v1) / YAGNI

- Capturing images/screenshots (text only).
- Clipboard protection/restore (Option B).
- Configurable hotkey UI (hardcoded `Ctrl+Alt+C` for v1; conflict reported).
- Configurable format / separators / blank-line count.
- Log rotation, compression, encryption.
- Auto-start on Windows login.

## Known Trade-offs

- Text only; some apps ignore simulated `Ctrl+C`.
- `Ctrl+Alt+C` overwrites the clipboard (by design choice).
- Plain-text on disk — user must avoid capturing secrets.
- Self-contained `.exe` is a few MB.
