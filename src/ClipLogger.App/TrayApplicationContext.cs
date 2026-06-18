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
                "Could not register Ctrl+Alt+C - another application may already be using it.",
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
        menu.Items.Add("Settings...", null, (_, _) => OpenSettings());
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
