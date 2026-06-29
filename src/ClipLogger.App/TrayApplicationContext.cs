using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ClipLogger.Core;

namespace ClipLogger.App;

public class TrayApplicationContext : ApplicationContext
{
    private const int HOTKEY_CAPTURE = 1;
    private const int HOTKEY_VIEWER = 2;
    private const uint VK_C = 0x43;
    private const uint VK_V = 0x56;

    private readonly Config _config;
    private readonly NotifyIcon _tray;
    private readonly HotkeyManager _hotkey;
    private readonly System.Windows.Forms.Timer _checkInTimer;
    private readonly ReentrancyGuard _checkInPrompt = new();
    private readonly AutoStartManager _autoStart;
    private readonly Icon _icon;
    private LogWriter _writer;
    private ViewerForm? _viewer;
    private bool _logging = true;
    private ToolStripMenuItem _toggleItem = null!;

    public TrayApplicationContext(Config config)
    {
        _config = config;
        _autoStart = new AutoStartManager(new RegistryRunKeyStore());
        _writer = new LogWriter(_config.LogFolder);
        _icon = TrayIconFactory.Create();

        _hotkey = new HotkeyManager();
        _hotkey.HotkeyPressed += OnHotkey;
        var captureReg = _hotkey.Register(HOTKEY_CAPTURE, HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_ALT, VK_C);
        _hotkey.Register(HOTKEY_VIEWER, HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_ALT, VK_V);
        DebugLog.Write($"startup: RegisterHotKey(Ctrl+Alt+C)={captureReg}; logFolder={_config.LogFolder}");
        if (!captureReg)
        {
            MessageBox.Show(
                "Could not register Ctrl+Alt+C - another application may already be using it.",
                "Clip Logger", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        _tray = new NotifyIcon
        {
            Icon = _icon,
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
        _tray.DoubleClick += (_, _) => ShowViewer();
        UpdateTrayText();
        _tray.ShowBalloonTip(2500, "Clip Logger",
            "Running. Ctrl+Alt+C captures selected text · Ctrl+Alt+V opens the viewer.", ToolTipIcon.Info);

        _checkInTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _checkInTimer.Tick += OnCheckTick;
        _checkInTimer.Start();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open Viewer", null, (_, _) => ShowViewer());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("New File", null, (_, _) => NewFile());
        _toggleItem = new ToolStripMenuItem("Stop logging", null, (_, _) => ToggleLogging());
        menu.Items.Add(_toggleItem);
        menu.Items.Add("Open Log Folder", null, (_, _) => OpenFolder());
        menu.Items.Add("Settings...", null, (_, _) => OpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());
        return menu;
    }

    private void OnHotkey(int id)
    {
        if (id == HOTKEY_VIEWER) { ShowViewer(); return; }
        if (id != HOTKEY_CAPTURE || !_logging) return;

        var source = ClipboardCapture.ActiveWindowTitle();
        var text = ClipboardCapture.CaptureSelectedText();
        if (text == null)
        {
            _tray.ShowBalloonTip(1500, "Clip Logger", "Nothing captured (no text selected).", ToolTipIcon.Warning);
            return;
        }
        try
        {
            _writer.Append(text, source);
            UpdateTrayText();
            _tray.ShowBalloonTip(800, "Clip Logger", $"Captured (entry {_writer.EntryCount}).", ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            _logging = false;
            _toggleItem.Text = "Start logging";
            _tray.ShowBalloonTip(3000, "Clip Logger",
                "Write failed (logging paused): " + ex.Message, ToolTipIcon.Error);
        }
    }

    private void ShowViewer()
    {
        if (_viewer == null || _viewer.IsDisposed)
        {
            _viewer = new ViewerForm(() => _writer.CurrentFilePath);
            _viewer.Show();
        }
        else
        {
            if (_viewer.WindowState == FormWindowState.Minimized)
                _viewer.WindowState = FormWindowState.Normal;
            _viewer.Show();
        }
        _viewer.Activate();
        _viewer.BringToFront();
    }

    private void NewFile()
    {
        _writer = new LogWriter(_config.LogFolder);
        UpdateTrayText();
        _tray.ShowBalloonTip(1000, "Clip Logger", "Started a new file.", ToolTipIcon.Info);
    }

    private void ToggleLogging()
    {
        _logging = !_logging;
        _toggleItem.Text = _logging ? "Stop logging" : "Start logging";
        UpdateTrayText();
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
            {
                _writer = new LogWriter(_config.LogFolder);
                UpdateTrayText();
            }
        }
    }

    private void OnCheckTick(object? sender, EventArgs e)
    {
        if (!_logging) return;
        if (!CheckInScheduler.IsDue(_writer.CurrentFileStarted, DateTime.Now, _config.CheckInMinutes))
            return;

        // MessageBox.Show pumps its own modal message loop, so the WinForms timer
        // keeps firing Tick while the prompt is open. Without this guard a new
        // dialog would stack on top every interval until the user answers.
        if (!_checkInPrompt.TryEnter()) return;
        try
        {
            var msg = $"This log file has been active for {IntervalText.Describe(_config.CheckInMinutes)}.\n\n" +
                      "Continue with it (Yes), or start a new file (No)?";
            var result = MessageBox.Show(msg, "Clip Logger", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No) NewFile();
            else _writer.ResetStartTime();
        }
        finally
        {
            _checkInPrompt.Exit();
        }
    }

    private void UpdateTrayText()
    {
        var state = _logging ? "" : " (stopped)";
        var text = $"Clip Logger{state} — {_writer.EntryCount} entries";
        if (text.Length > 63) text = text.Substring(0, 63);
        _tray.Text = text;
    }

    private void ExitApp()
    {
        _checkInTimer.Stop();
        _viewer?.Close();
        _hotkey.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _icon.Dispose();
        ExitThread();
    }
}
