# Clip Logger Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Windows tray app that captures the currently-selected text on `Ctrl+Alt+C` and appends it to dated `.txt` log files with timestamps and separators, plus a configurable check-in interval, auto-start on login, a settings dialog, and an Inno Setup installer.

**Architecture:** Two projects. `ClipLogger.Core` is a platform-neutral class library holding all testable logic (entry formatting, file naming, config persistence, log writing, auto-start logic via an `IRunKeyStore` interface, check-in scheduling, interval text). `ClipLogger.App` is a WinForms (`net9.0-windows`) app holding the desktop-only pieces (tray icon, global hotkey, clipboard simulation, settings form, and the concrete registry store). An xUnit project tests Core. An Inno Setup script packages a self-contained publish into a `Setup.exe`.

**Tech Stack:** C# / .NET 9, WinForms, xUnit, Win32 P/Invoke (`RegisterHotKey`, `SendInput`), `Microsoft.Win32.Registry`, Inno Setup, GitHub (`saiviroop/ClipLogger`).

---

## File Structure

```
ClipLogger/
  ClipLogger.sln
  .gitignore
  src/
    ClipLogger.Core/
      ClipLogger.Core.csproj      # net9.0 library
      EntryFormatter.cs           # format one log entry (timestamp + text + blanks + separator)
      LogFileNamer.cs             # cliplog-YYYY-MM-DD_HH-mm.txt
      Config.cs                   # LogFolder, CheckInMinutes, AutoStart; JSON load/save
      LogWriter.cs                # owns current file; StartNewFile / Append / ResetStartTime
      IRunKeyStore.cs             # abstraction over the HKCU Run key
      AutoStartManager.cs         # Enable/Disable/IsEnabled using IRunKeyStore
      CheckInScheduler.cs         # IsDue(start, now, intervalMinutes)
      IntervalText.cs             # Describe(minutes) -> "1 hour" / "30 minutes"
    ClipLogger.App/
      ClipLogger.App.csproj       # net9.0-windows WinExe, WinForms
      Program.cs                  # entry point, first-run folder prompt
      HotkeyManager.cs            # global Ctrl+Alt+C via RegisterHotKey
      ClipboardCapture.cs         # SendInput Ctrl+C, read clipboard
      RegistryRunKeyStore.cs      # IRunKeyStore over HKCU\...\Run
      SettingsForm.cs             # folder / interval / auto-start UI
      TrayApplicationContext.cs   # NotifyIcon, menu, wiring, check-in timer
  tests/
    ClipLogger.Tests/
      ClipLogger.Tests.csproj     # net9.0 xUnit
      FakeRunKeyStore.cs          # in-memory IRunKeyStore for tests
      EntryFormatterTests.cs
      LogFileNamerTests.cs
      ConfigTests.cs
      LogWriterTests.cs
      AutoStartManagerTests.cs
      CheckInSchedulerTests.cs
      IntervalTextTests.cs
  installer/
    ClipLogger.iss               # Inno Setup script
```

---

### Task 1: Solution and project scaffolding

**Files:**
- Create: `ClipLogger.sln`, `.gitignore`, the three `.csproj` files.

- [ ] **Step 1: Create solution and projects**

Run from `C:\dev\ClipLogger`:
```bash
dotnet new sln -n ClipLogger
dotnet new classlib -n ClipLogger.Core -o src/ClipLogger.Core -f net9.0
dotnet new winforms -n ClipLogger.App -o src/ClipLogger.App -f net9.0-windows
dotnet new xunit -n ClipLogger.Tests -o tests/ClipLogger.Tests -f net9.0
```

- [ ] **Step 2: Remove template starter files**

Delete the auto-generated files we will replace:
```bash
rm -f src/ClipLogger.Core/Class1.cs
rm -f src/ClipLogger.App/Form1.cs src/ClipLogger.App/Form1.Designer.cs src/ClipLogger.App/Form1.resx
rm -f tests/ClipLogger.Tests/UnitTest1.cs
```

- [ ] **Step 3: Wire up references**

```bash
dotnet sln add src/ClipLogger.Core/ClipLogger.Core.csproj src/ClipLogger.App/ClipLogger.App.csproj tests/ClipLogger.Tests/ClipLogger.Tests.csproj
dotnet add src/ClipLogger.App/ClipLogger.App.csproj reference src/ClipLogger.Core/ClipLogger.Core.csproj
dotnet add tests/ClipLogger.Tests/ClipLogger.Tests.csproj reference src/ClipLogger.Core/ClipLogger.Core.csproj
```

- [ ] **Step 4: Set Core and Test csproj properties**

Overwrite `src/ClipLogger.Core/ClipLogger.Core.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

Overwrite `src/ClipLogger.App/ClipLogger.App.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>ClipLogger.App</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ClipLogger.Core\ClipLogger.Core.csproj" />
  </ItemGroup>
</Project>
```
(The default `Program.cs` generated by the winforms template stays for now; it is replaced in Task 9.)

- [ ] **Step 5: Create `.gitignore`**

Create `.gitignore`:
```gitignore
bin/
obj/
*.user
.vs/
installer/app/
installer/Output/
```

- [ ] **Step 6: Build the empty solution**

Run: `dotnet build`
Expected: Build succeeds (the winforms template `Form1` was removed, so its default `Program.cs` referencing it must also be handled — if the build fails referencing `Form1`, replace `src/ClipLogger.App/Program.cs` body with a temporary stub:)
```csharp
namespace ClipLogger.App;
internal static class Program
{
    [STAThread]
    static void Main() { }
}
```
Re-run: `dotnet build` → Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "chore: scaffold ClipLogger solution (Core, App, Tests)"
```

---

### Task 2: EntryFormatter (TDD)

**Files:**
- Create: `src/ClipLogger.Core/EntryFormatter.cs`
- Test: `tests/ClipLogger.Tests/EntryFormatterTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/ClipLogger.Tests/EntryFormatterTests.cs`:
```csharp
using System;
using ClipLogger.Core;
using Xunit;

public class EntryFormatterTests
{
    [Fact]
    public void Format_ProducesTimestampTextFourBlankLinesAndSeparator()
    {
        var nl = Environment.NewLine;
        var result = EntryFormatter.Format(new DateTime(2026, 6, 18, 14, 32, 5), "hello world");

        var expected =
            $"[2026-06-18 14:32:05]{nl}" +
            $"hello world{nl}" +
            $"{nl}{nl}{nl}{nl}" +
            $"{EntryFormatter.Separator}{nl}";

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Separator_IsFortyDashes()
    {
        Assert.Equal(new string('-', 40), EntryFormatter.Separator);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~EntryFormatterTests"`
Expected: FAIL — `EntryFormatter` does not exist (compile error).

- [ ] **Step 3: Write minimal implementation**

Create `src/ClipLogger.Core/EntryFormatter.cs`:
```csharp
namespace ClipLogger.Core;

public static class EntryFormatter
{
    public static readonly string Separator = new string('-', 40);

    public static string Format(DateTime timestamp, string text)
    {
        var nl = Environment.NewLine;
        var ts = timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        return $"[{ts}]{nl}{text}{nl}{nl}{nl}{nl}{nl}{Separator}{nl}";
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~EntryFormatterTests"`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add EntryFormatter for log entry formatting"
```

---

### Task 3: LogFileNamer (TDD)

**Files:**
- Create: `src/ClipLogger.Core/LogFileNamer.cs`
- Test: `tests/ClipLogger.Tests/LogFileNamerTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/ClipLogger.Tests/LogFileNamerTests.cs`:
```csharp
using System;
using ClipLogger.Core;
using Xunit;

public class LogFileNamerTests
{
    [Fact]
    public void MakeFileName_UsesDateAndTime()
    {
        var name = LogFileNamer.MakeFileName(new DateTime(2026, 6, 18, 14, 32, 0));
        Assert.Equal("cliplog-2026-06-18_14-32.txt", name);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~LogFileNamerTests"`
Expected: FAIL — `LogFileNamer` not defined.

- [ ] **Step 3: Write minimal implementation**

Create `src/ClipLogger.Core/LogFileNamer.cs`:
```csharp
namespace ClipLogger.Core;

public static class LogFileNamer
{
    public static string MakeFileName(DateTime when) => $"cliplog-{when:yyyy-MM-dd_HH-mm}.txt";
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~LogFileNamerTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add LogFileNamer for dated file names"
```

---

### Task 4: Config (TDD)

**Files:**
- Create: `src/ClipLogger.Core/Config.cs`
- Test: `tests/ClipLogger.Tests/ConfigTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/ClipLogger.Tests/ConfigTests.cs`:
```csharp
using System;
using System.IO;
using ClipLogger.Core;
using Xunit;

public class ConfigTests
{
    [Fact]
    public void Defaults_AreSixtyMinutesNoAutoStart()
    {
        var c = new Config();
        Assert.Equal(60, c.CheckInMinutes);
        Assert.False(c.AutoStart);
        Assert.Equal("", c.LogFolder);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsValues()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cliptest_{Guid.NewGuid():N}", "config.json");
        try
        {
            var c = new Config { LogFolder = @"C:\logs", CheckInMinutes = 30, AutoStart = true };
            c.Save(path);

            var loaded = Config.Load(path);
            Assert.Equal(@"C:\logs", loaded.LogFolder);
            Assert.Equal(30, loaded.CheckInMinutes);
            Assert.True(loaded.AutoStart);
        }
        finally
        {
            var dir = Path.GetDirectoryName(path)!;
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cliptest_{Guid.NewGuid():N}", "missing.json");
        var loaded = Config.Load(path);
        Assert.Equal(60, loaded.CheckInMinutes);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~ConfigTests"`
Expected: FAIL — `Config` not defined.

- [ ] **Step 3: Write minimal implementation**

Create `src/ClipLogger.Core/Config.cs`:
```csharp
using System.Text.Json;

namespace ClipLogger.Core;

public class Config
{
    public string LogFolder { get; set; } = "";
    public int CheckInMinutes { get; set; } = 60;
    public bool AutoStart { get; set; } = false;

    public void Save(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static Config Load(string path)
    {
        if (!File.Exists(path)) return new Config();
        try { return JsonSerializer.Deserialize<Config>(File.ReadAllText(path)) ?? new Config(); }
        catch { return new Config(); }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~ConfigTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add Config with JSON persistence"
```

---

### Task 5: LogWriter (TDD)

**Files:**
- Create: `src/ClipLogger.Core/LogWriter.cs`
- Test: `tests/ClipLogger.Tests/LogWriterTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/ClipLogger.Tests/LogWriterTests.cs`:
```csharp
using System;
using System.IO;
using ClipLogger.Core;
using Xunit;

public class LogWriterTests
{
    private static string TempDir() => Path.Combine(Path.GetTempPath(), $"cliptest_{Guid.NewGuid():N}");

    [Fact]
    public void Constructor_CreatesDatedFile()
    {
        var dir = TempDir();
        try
        {
            var fixedTime = new DateTime(2026, 6, 18, 14, 32, 5);
            var w = new LogWriter(dir, () => fixedTime);

            Assert.True(File.Exists(Path.Combine(dir, "cliplog-2026-06-18_14-32.txt")));
            Assert.Equal(fixedTime, w.CurrentFileStarted);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Append_WritesFormattedEntry()
    {
        var dir = TempDir();
        try
        {
            var fixedTime = new DateTime(2026, 6, 18, 14, 32, 5);
            var w = new LogWriter(dir, () => fixedTime);
            w.Append("captured text");

            var content = File.ReadAllText(w.CurrentFilePath);
            Assert.Contains("[2026-06-18 14:32:05]", content);
            Assert.Contains("captured text", content);
            Assert.Contains(EntryFormatter.Separator, content);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void StartNewFile_ResetsStartedTime()
    {
        var dir = TempDir();
        try
        {
            var times = new Queue<DateTime>(new[]
            {
                new DateTime(2026, 6, 18, 14, 0, 0),
                new DateTime(2026, 6, 18, 15, 30, 0),
            });
            var w = new LogWriter(dir, () => times.Dequeue());
            w.StartNewFile();
            Assert.Equal(new DateTime(2026, 6, 18, 15, 30, 0), w.CurrentFileStarted);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void ResetStartTime_RebasesStartedTime()
    {
        var dir = TempDir();
        try
        {
            var times = new Queue<DateTime>(new[]
            {
                new DateTime(2026, 6, 18, 14, 0, 0),
                new DateTime(2026, 6, 18, 16, 0, 0),
            });
            var w = new LogWriter(dir, () => times.Dequeue());
            w.ResetStartTime();
            Assert.Equal(new DateTime(2026, 6, 18, 16, 0, 0), w.CurrentFileStarted);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~LogWriterTests"`
Expected: FAIL — `LogWriter` not defined.

- [ ] **Step 3: Write minimal implementation**

Create `src/ClipLogger.Core/LogWriter.cs`:
```csharp
namespace ClipLogger.Core;

public class LogWriter
{
    private readonly Func<DateTime> _clock;

    public string FolderPath { get; }
    public string CurrentFilePath { get; private set; } = "";
    public DateTime CurrentFileStarted { get; private set; }

    public LogWriter(string folderPath, Func<DateTime>? clock = null)
    {
        _clock = clock ?? (() => DateTime.Now);
        FolderPath = folderPath;
        StartNewFile();
    }

    public void StartNewFile()
    {
        Directory.CreateDirectory(FolderPath);
        var now = _clock();
        CurrentFileStarted = now;
        CurrentFilePath = Path.Combine(FolderPath, LogFileNamer.MakeFileName(now));
        if (!File.Exists(CurrentFilePath))
            File.WriteAllText(CurrentFilePath, "");
    }

    public void Append(string text)
    {
        File.AppendAllText(CurrentFilePath, EntryFormatter.Format(_clock(), text));
    }

    public void ResetStartTime() => CurrentFileStarted = _clock();
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~LogWriterTests"`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add LogWriter for dated append-only log files"
```

---

### Task 6: AutoStartManager + IRunKeyStore (TDD)

**Files:**
- Create: `src/ClipLogger.Core/IRunKeyStore.cs`, `src/ClipLogger.Core/AutoStartManager.cs`
- Test: `tests/ClipLogger.Tests/FakeRunKeyStore.cs`, `tests/ClipLogger.Tests/AutoStartManagerTests.cs`

- [ ] **Step 1: Write the failing test + fake**

Create `tests/ClipLogger.Tests/FakeRunKeyStore.cs`:
```csharp
using System.Collections.Generic;
using ClipLogger.Core;

public class FakeRunKeyStore : IRunKeyStore
{
    public readonly Dictionary<string, string> Values = new();
    public void Set(string name, string value) => Values[name] = value;
    public void Remove(string name) => Values.Remove(name);
    public string? Get(string name) => Values.TryGetValue(name, out var v) ? v : null;
}
```

Create `tests/ClipLogger.Tests/AutoStartManagerTests.cs`:
```csharp
using ClipLogger.Core;
using Xunit;

public class AutoStartManagerTests
{
    [Fact]
    public void IsEnabled_FalseByDefault()
    {
        var m = new AutoStartManager(new FakeRunKeyStore());
        Assert.False(m.IsEnabled());
    }

    [Fact]
    public void Enable_WritesQuotedExePath()
    {
        var store = new FakeRunKeyStore();
        var m = new AutoStartManager(store);

        m.Enable(@"C:\Program Files\ClipLogger\ClipLogger.App.exe");

        Assert.True(m.IsEnabled());
        Assert.Equal("\"C:\\Program Files\\ClipLogger\\ClipLogger.App.exe\"",
            store.Values[AutoStartManager.ValueName]);
    }

    [Fact]
    public void Disable_RemovesEntry()
    {
        var store = new FakeRunKeyStore();
        var m = new AutoStartManager(store);
        m.Enable(@"C:\x.exe");
        m.Disable();
        Assert.False(m.IsEnabled());
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~AutoStartManagerTests"`
Expected: FAIL — `IRunKeyStore` / `AutoStartManager` not defined.

- [ ] **Step 3: Write minimal implementation**

Create `src/ClipLogger.Core/IRunKeyStore.cs`:
```csharp
namespace ClipLogger.Core;

public interface IRunKeyStore
{
    void Set(string name, string value);
    void Remove(string name);
    string? Get(string name);
}
```

Create `src/ClipLogger.Core/AutoStartManager.cs`:
```csharp
namespace ClipLogger.Core;

public class AutoStartManager
{
    public const string ValueName = "ClipLogger";

    private readonly IRunKeyStore _store;
    public AutoStartManager(IRunKeyStore store) => _store = store;

    public bool IsEnabled() => _store.Get(ValueName) != null;
    public void Enable(string exePath) => _store.Set(ValueName, $"\"{exePath}\"");
    public void Disable() => _store.Remove(ValueName);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~AutoStartManagerTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add AutoStartManager over IRunKeyStore abstraction"
```

---

### Task 7: CheckInScheduler (TDD)

**Files:**
- Create: `src/ClipLogger.Core/CheckInScheduler.cs`
- Test: `tests/ClipLogger.Tests/CheckInSchedulerTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/ClipLogger.Tests/CheckInSchedulerTests.cs`:
```csharp
using System;
using ClipLogger.Core;
using Xunit;

public class CheckInSchedulerTests
{
    private static readonly DateTime Start = new(2026, 6, 18, 14, 0, 0);

    [Fact]
    public void IsDue_FalseBeforeInterval()
    {
        Assert.False(CheckInScheduler.IsDue(Start, Start.AddMinutes(59), 60));
    }

    [Fact]
    public void IsDue_TrueAtInterval()
    {
        Assert.True(CheckInScheduler.IsDue(Start, Start.AddMinutes(60), 60));
    }

    [Fact]
    public void IsDue_TrueAfterInterval()
    {
        Assert.True(CheckInScheduler.IsDue(Start, Start.AddMinutes(125), 60));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~CheckInSchedulerTests"`
Expected: FAIL — `CheckInScheduler` not defined.

- [ ] **Step 3: Write minimal implementation**

Create `src/ClipLogger.Core/CheckInScheduler.cs`:
```csharp
namespace ClipLogger.Core;

public static class CheckInScheduler
{
    public static bool IsDue(DateTime start, DateTime now, int intervalMinutes)
        => (now - start).TotalMinutes >= intervalMinutes;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~CheckInSchedulerTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add CheckInScheduler interval logic"
```

---

### Task 8: IntervalText (TDD)

**Files:**
- Create: `src/ClipLogger.Core/IntervalText.cs`
- Test: `tests/ClipLogger.Tests/IntervalTextTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/ClipLogger.Tests/IntervalTextTests.cs`:
```csharp
using ClipLogger.Core;
using Xunit;

public class IntervalTextTests
{
    [Theory]
    [InlineData(1, "1 minute")]
    [InlineData(30, "30 minutes")]
    [InlineData(60, "1 hour")]
    [InlineData(90, "90 minutes")]
    [InlineData(120, "2 hours")]
    public void Describe_FormatsHumanReadable(int minutes, string expected)
    {
        Assert.Equal(expected, IntervalText.Describe(minutes));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~IntervalTextTests"`
Expected: FAIL — `IntervalText` not defined.

- [ ] **Step 3: Write minimal implementation**

Create `src/ClipLogger.Core/IntervalText.cs`:
```csharp
namespace ClipLogger.Core;

public static class IntervalText
{
    public static string Describe(int minutes)
    {
        if (minutes % 60 == 0)
        {
            var hours = minutes / 60;
            return hours == 1 ? "1 hour" : $"{hours} hours";
        }
        return minutes == 1 ? "1 minute" : $"{minutes} minutes";
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~IntervalTextTests"`
Expected: PASS (5 cases).

- [ ] **Step 5: Run the full test suite and commit**

Run: `dotnet test`
Expected: PASS (all Core tests green).
```bash
git add -A
git commit -m "feat: add IntervalText human-readable durations"
```

---

### Task 9: WinForms App (tray, hotkey, clipboard, settings)

> No unit tests — these are desktop-only integrations verified manually. Write all files, build, then run and verify behavior.

**Files:**
- Create: `src/ClipLogger.App/Program.cs` (replace stub), `HotkeyManager.cs`, `ClipboardCapture.cs`, `RegistryRunKeyStore.cs`, `SettingsForm.cs`, `TrayApplicationContext.cs`

- [ ] **Step 1: Program entry point**

Replace `src/ClipLogger.App/Program.cs`:
```csharp
using System;
using System.IO;
using System.Windows.Forms;
using ClipLogger.Core;

namespace ClipLogger.App;

internal static class Program
{
    public static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClipLogger", "config.json");

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var config = Config.Load(ConfigPath);

        if (string.IsNullOrEmpty(config.LogFolder) || !Directory.Exists(config.LogFolder))
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Choose a folder where Clip Logger will save log files"
            };
            if (fbd.ShowDialog() != DialogResult.OK) return; // cancelled → exit
            config.LogFolder = fbd.SelectedPath;
            config.Save(ConfigPath);
        }

        Application.Run(new TrayApplicationContext(config));
    }
}
```

- [ ] **Step 2: HotkeyManager**

Create `src/ClipLogger.App/HotkeyManager.cs`:
```csharp
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClipLogger.App;

public class HotkeyManager : NativeWindow, IDisposable
{
    public event EventHandler? HotkeyPressed;

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 0xC1A0;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint VK_C = 0x43;

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public HotkeyManager() => CreateHandle(new CreateParams());

    public bool Register() => RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_C);

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && (int)m.WParam == HOTKEY_ID)
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        UnregisterHotKey(Handle, HOTKEY_ID);
        DestroyHandle();
    }
}
```

- [ ] **Step 3: ClipboardCapture**

Create `src/ClipLogger.App/ClipboardCapture.cs`:
```csharp
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ClipLogger.App;

public static class ClipboardCapture
{
    [DllImport("user32.dll")] private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_MENU = 0x12; // Alt
    private const ushort VK_C = 0x43;

    /// <summary>Simulates Ctrl+C in the focused window and returns the resulting clipboard text, or null.</summary>
    public static string? CaptureSelectedText()
    {
        SendCleanCtrlC();
        Thread.Sleep(120); // let the target app populate the clipboard

        if (Clipboard.ContainsText())
        {
            var t = Clipboard.GetText();
            return string.IsNullOrEmpty(t) ? null : t;
        }
        return null;
    }

    // The hotkey is Ctrl+Alt+C, so Ctrl and Alt are physically held. Release Alt first so the
    // target sees a clean Ctrl+C (Alt held would make it Ctrl+Alt+C and break copy in many apps).
    private static void SendCleanCtrlC()
    {
        var inputs = new[]
        {
            Key(VK_MENU, true),     // Alt up
            Key(VK_CONTROL, false), // Ctrl down
            Key(VK_C, false),       // C down
            Key(VK_C, true),        // C up
            Key(VK_CONTROL, true),  // Ctrl up
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT Key(ushort vk, bool up) => new INPUT
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = vk,
                wScan = 0,
                dwFlags = up ? KEYEVENTF_KEYUP : 0,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            }
        }
    };
}
```

- [ ] **Step 4: RegistryRunKeyStore**

Create `src/ClipLogger.App/RegistryRunKeyStore.cs`:
```csharp
using Microsoft.Win32;
using ClipLogger.Core;

namespace ClipLogger.App;

public class RegistryRunKeyStore : IRunKeyStore
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public void Set(string name, string value)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key!.SetValue(name, value);
    }

    public void Remove(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }

    public string? Get(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(name) as string;
    }
}
```

- [ ] **Step 5: SettingsForm**

Create `src/ClipLogger.App/SettingsForm.cs`:
```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using ClipLogger.Core;

namespace ClipLogger.App;

public class SettingsForm : Form
{
    private readonly Config _config;
    private readonly AutoStartManager _autoStart;

    private readonly TextBox _folderBox = new() { Left = 12, Top = 38, Width = 300 };
    private readonly NumericUpDown _interval = new() { Left = 230, Top = 74, Width = 80, Minimum = 1, Maximum = 1440 };
    private readonly CheckBox _autoStartBox = new() { Left = 12, Top = 112, AutoSize = true, Text = "Start Clip Logger on login" };

    public SettingsForm(Config config, AutoStartManager autoStart)
    {
        _config = config;
        _autoStart = autoStart;

        Text = "Clip Logger — Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 200);

        _folderBox.Text = _config.LogFolder;
        _interval.Value = Math.Clamp(_config.CheckInMinutes, 1, 1440);
        _autoStartBox.Checked = _autoStart.IsEnabled();

        var folderLabel = new Label { Text = "Log folder:", Left = 12, Top = 18, AutoSize = true };
        var browse = new Button { Text = "Browse…", Left = 320, Top = 36, Width = 80 };
        browse.Click += (_, _) =>
        {
            using var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK) _folderBox.Text = fbd.SelectedPath;
        };

        var intervalLabel = new Label { Text = "Check-in interval (minutes):", Left = 12, Top = 78, AutoSize = true };

        var ok = new Button { Text = "OK", Left = 230, Top = 155, Width = 80, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Left = 320, Top = 155, Width = 80, DialogResult = DialogResult.Cancel };
        ok.Click += (_, _) => Apply();

        Controls.AddRange(new Control[]
        {
            folderLabel, _folderBox, browse, intervalLabel, _interval, _autoStartBox, ok, cancel
        });
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void Apply()
    {
        _config.LogFolder = _folderBox.Text;
        _config.CheckInMinutes = (int)_interval.Value;
        _config.AutoStart = _autoStartBox.Checked;

        if (_config.AutoStart) _autoStart.Enable(Application.ExecutablePath);
        else _autoStart.Disable();
    }
}
```

- [ ] **Step 6: TrayApplicationContext**

Create `src/ClipLogger.App/TrayApplicationContext.cs`:
```csharp
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ClipLogger.Core;

namespace ClipLogger.App;

public class TrayApplicationContext : ApplicationContext
{
    private readonly Config _config;
    private readonly NotifyIcon _tray;
    private readonly HotkeyManager _hotkey;
    private readonly System.Windows.Forms.Timer _checkInTimer;
    private readonly AutoStartManager _autoStart;
    private LogWriter _writer;
    private bool _logging = true;
    private ToolStripMenuItem _toggleItem = null!;

    public TrayApplicationContext(Config config)
    {
        _config = config;
        _autoStart = new AutoStartManager(new RegistryRunKeyStore());
        _writer = new LogWriter(_config.LogFolder);

        _hotkey = new HotkeyManager();
        _hotkey.HotkeyPressed += OnHotkey;
        if (!_hotkey.Register())
        {
            MessageBox.Show(
                "Could not register Ctrl+Alt+C — another application may already be using it.",
                "Clip Logger", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Clip Logger",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
        _tray.ShowBalloonTip(2000, "Clip Logger",
            "Running. Press Ctrl+Alt+C to capture selected text.", ToolTipIcon.Info);

        _checkInTimer = new System.Windows.Forms.Timer { Interval = 60_000 }; // check every minute
        _checkInTimer.Tick += OnCheckTick;
        _checkInTimer.Start();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("New File", null, (_, _) => NewFile());
        _toggleItem = new ToolStripMenuItem("Stop logging", null, (_, _) => ToggleLogging());
        menu.Items.Add(_toggleItem);
        menu.Items.Add("Open Log Folder", null, (_, _) => OpenFolder());
        menu.Items.Add("Settings…", null, (_, _) => OpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());
        return menu;
    }

    private void OnHotkey(object? sender, EventArgs e)
    {
        if (!_logging) return;
        var text = ClipboardCapture.CaptureSelectedText();
        if (text == null)
        {
            _tray.ShowBalloonTip(1500, "Clip Logger", "Nothing captured (no text selected).", ToolTipIcon.Warning);
            return;
        }
        try
        {
            _writer.Append(text);
            _tray.ShowBalloonTip(1000, "Clip Logger", "Captured.", ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            _logging = false;
            _toggleItem.Text = "Start logging";
            _tray.ShowBalloonTip(3000, "Clip Logger",
                "Write failed (logging paused): " + ex.Message, ToolTipIcon.Error);
        }
    }

    private void NewFile()
    {
        _writer = new LogWriter(_config.LogFolder);
        _tray.ShowBalloonTip(1000, "Clip Logger", "Started a new file.", ToolTipIcon.Info);
    }

    private void ToggleLogging()
    {
        _logging = !_logging;
        _toggleItem.Text = _logging ? "Stop logging" : "Start logging";
    }

    private void OpenFolder()
    {
        if (Directory.Exists(_config.LogFolder))
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_config.LogFolder}\"") { UseShellExecute = true });
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_config, _autoStart);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _config.Save(Program.ConfigPath);
            if (_writer.FolderPath != _config.LogFolder && Directory.Exists(_config.LogFolder))
                _writer = new LogWriter(_config.LogFolder);
        }
    }

    private void OnCheckTick(object? sender, EventArgs e)
    {
        if (!_logging) return;
        if (!CheckInScheduler.IsDue(_writer.CurrentFileStarted, DateTime.Now, _config.CheckInMinutes))
            return;

        var msg = $"This log file has been active for {IntervalText.Describe(_config.CheckInMinutes)}.\n\n" +
                  "Continue with it (Yes), or start a new file (No)?";
        var result = MessageBox.Show(msg, "Clip Logger", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.No) NewFile();
        else _writer.ResetStartTime();
    }

    private void ExitApp()
    {
        _checkInTimer.Stop();
        _hotkey.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        ExitThread();
    }
}
```

- [ ] **Step 7: Build**

Run: `dotnet build`
Expected: PASS (no errors). If `Microsoft.Win32.Registry` is not found, add the package:
`dotnet add src/ClipLogger.App/ClipLogger.App.csproj package Microsoft.Win32.Registry`
then rebuild.

- [ ] **Step 8: Manual smoke test**

Run: `dotnet run --project src/ClipLogger.App`
Verify:
1. A folder picker appears on first run → choose a folder.
2. A tray icon appears with a "Running" balloon.
3. Select text in Notepad/browser → press `Ctrl+Alt+C` → "Captured." balloon appears.
4. Open the chosen folder → a `cliplog-YYYY-MM-DD_HH-mm.txt` exists with the timestamp, text, 4 blank lines, and the dashed separator.
5. Tray right-click → New File creates a new file; Stop logging makes `Ctrl+Alt+C` do nothing; Start logging resumes.
6. Tray → Settings → change interval to `1`, tick "Start on login", OK. Wait ~1 min → the Continue/New File prompt appears. Verify the Run key exists:
   `reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v ClipLogger`
7. Untick auto-start in Settings → the Run value is removed.
8. Tray → Exit → icon disappears.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat: add WinForms tray app (hotkey, clipboard, settings, check-in)"
```

---

### Task 10: Inno Setup installer

**Files:**
- Create: `installer/ClipLogger.iss`

- [ ] **Step 1: Publish a self-contained build**

Run:
```bash
dotnet publish src/ClipLogger.App/ClipLogger.App.csproj -c Release -r win-x64 --self-contained true -o installer/app
```
Expected: `installer/app/ClipLogger.App.exe` plus runtime files exist.

- [ ] **Step 2: Create the Inno Setup script**

Create `installer/ClipLogger.iss`:
```iss
#define MyAppName "Clip Logger"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "saiviroop"
#define MyAppExe "ClipLogger.App.exe"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\ClipLogger
DefaultGroupName=Clip Logger
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=ClipLogger-Setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern

[Tasks]
Name: "startuplogon"; Description: "Start Clip Logger when I log in"; GroupDescription: "Startup options:"

[Files]
Source: "app\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\Clip Logger"; Filename: "{app}\{#MyAppExe}"

[Registry]
; Per-user auto-start; matches AutoStartManager (value name "ClipLogger", quoted exe path).
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "ClipLogger"; ValueData: """{app}\{#MyAppExe}"""; \
  Tasks: startuplogon; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExe}"; Description: "Launch Clip Logger now"; Flags: nowait postinstall skipifsilent
```

- [ ] **Step 3: Install Inno Setup (if needed) and compile**

Check / install:
```bash
ls "/c/Program Files (x86)/Inno Setup 6/ISCC.exe" 2>/dev/null || winget install --id JRSoftware.InnoSetup -e --accept-source-agreements --accept-package-agreements
```
Compile the installer:
```bash
"/c/Program Files (x86)/Inno Setup 6/ISCC.exe" installer/ClipLogger.iss
```
Expected: `installer/Output/ClipLogger-Setup.exe` is produced.

- [ ] **Step 4: Manual installer test**

Run `installer/Output/ClipLogger-Setup.exe`:
1. Wizard appears; the "Start Clip Logger when I log in" checkbox is present.
2. With it ticked, finish install → app launches; verify the Run key exists.
3. Uninstall via Settings → Apps → the Run value is removed (uninsdeletevalue).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add Inno Setup installer with auto-start option"
```

---

### Task 11: Publish to GitHub (`saiviroop/ClipLogger`)

> Requires one-time GitHub authentication on this machine. `gh` is not installed and there is no saved credential.

- [ ] **Step 1: Add a README**

Create `README.md`:
```markdown
# Clip Logger

A small Windows tray app for collecting debug context while you work. Press
**Ctrl+Alt+C** to capture the currently-selected text into a dated `.txt` log
file, with timestamps and separators between entries. Normal `Ctrl+C` is
untouched.

## Features
- Global hotkey capture (`Ctrl+Alt+C`)
- Dated log files: `cliplog-YYYY-MM-DD_HH-mm.txt`
- Timestamp + 4 blank lines + dashed separator per entry
- Tray menu: New File, Stop/Start, Open Folder, Settings, Exit
- Configurable check-in interval (default 60 min)
- Optional auto-start on login (per-user)
- Inno Setup installer

## Build
```
dotnet build
dotnet test
dotnet run --project src/ClipLogger.App
```

## Installer
```
dotnet publish src/ClipLogger.App/ClipLogger.App.csproj -c Release -r win-x64 --self-contained true -o installer/app
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\ClipLogger.iss
```
```
Commit it:
```bash
git add README.md
git commit -m "docs: add README"
```

- [ ] **Step 2: Authenticate GitHub (user action)**

Install `gh` and log in (run these yourself in the session with the `!` prefix, or in a terminal):
```
!winget install --id GitHub.cli -e --accept-source-agreements --accept-package-agreements
!gh auth login
```
Choose: GitHub.com → HTTPS → authenticate via browser as `saiviroop`.

- [ ] **Step 3: Create the repo and push**

```bash
gh repo create saiviroop/ClipLogger --public --source=. --remote=origin --push
```
Expected: repo created at `https://github.com/saiviroop/ClipLogger` with all commits pushed.

- [ ] **Step 4: Verify**

Run: `gh repo view saiviroop/ClipLogger --web`
Expected: the repository opens in the browser showing the code.

---

## Self-Review

**Spec coverage:**
- Plain `.txt`, user-chosen folder, dated filename → Tasks 3, 5, 9 (Program folder prompt).
- `Ctrl+Alt+C` captures selection; normal `Ctrl+C` untouched → Tasks 2, 9 (HotkeyManager, ClipboardCapture).
- Captured text stays on clipboard (Option A) → ClipboardCapture does not restore. ✓
- Timestamp + 4 blank lines + dashed separator → Task 2. ✓
- Tray menu (New File, Stop/Start, Open Folder, Settings, Exit) → Task 9. ✓
- Settings: folder, configurable interval, auto-start toggle → Tasks 4, 8, 9. ✓
- Configurable check-in prompt (Continue / New File) → Tasks 7, 8, 9. ✓
- Auto-start per-user via HKCU Run (Settings + installer) → Tasks 6, 9, 10. ✓
- Inno Setup installer with auto-start checkbox → Task 10. ✓
- GitHub `saiviroop/ClipLogger`, pushed after build → Task 11. ✓
- Error handling: no text captured, write failure pauses, hotkey conflict warning, missing folder re-prompt → Tasks 9 (OnHotkey, Program). ✓

**Placeholder scan:** No TBD/TODO; all code blocks complete.

**Type consistency:** `AutoStartManager.ValueName = "ClipLogger"` matches the installer's `ValueName: "ClipLogger"` and quoted-path format. `LogWriter` members (`FolderPath`, `CurrentFilePath`, `CurrentFileStarted`, `StartNewFile`, `Append`, `ResetStartTime`) are used consistently in Task 9. `CheckInScheduler.IsDue` and `IntervalText.Describe` signatures match their call sites.

**Known fragile area (call out during execution):** `ClipboardCapture.SendCleanCtrlC` releases Alt before sending Ctrl+C. A few apps (some terminals, protected fields) may still ignore a simulated copy — covered in the spec's trade-offs and the Task 9 manual test.
