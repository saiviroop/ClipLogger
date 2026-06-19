using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ClipLogger.Core;

namespace ClipLogger.App;

/// <summary>
/// Live view of the current log file: auto-refreshes as entries are captured,
/// with a case-insensitive search/filter box.
/// </summary>
public class ViewerForm : Form
{
    private readonly Func<string> _getCurrentPath;
    private readonly TextBox _search = new() { Dock = DockStyle.Fill };
    private readonly TextBox _body = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(30, 30, 30),
        ForeColor = Color.Gainsboro,
        Font = new Font("Consolas", 10f),
        BorderStyle = BorderStyle.None
    };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };

    private string _lastPath = "";
    private DateTime _lastWrite = DateTime.MinValue;
    private string _lastFilter = "\0"; // force first load

    public ViewerForm(Func<string> getCurrentPath)
    {
        _getCurrentPath = getCurrentPath;

        Text = "Clip Logger — Viewer";
        Width = 780;
        Height = 540;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(420, 300);

        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 38,
            ColumnCount = 3,
            Padding = new Padding(8, 7, 8, 3)
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));

        var lbl = new Label { Text = "Search:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
        var openBtn = new Button { Text = "Open Folder", Dock = DockStyle.Fill };
        openBtn.Click += (_, _) => OpenFolder();
        _search.TextChanged += (_, _) => ReloadView(force: true);

        top.Controls.Add(lbl, 0, 0);
        top.Controls.Add(_search, 1, 0);
        top.Controls.Add(openBtn, 2, 0);

        var bodyPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 8), BackColor = Color.FromArgb(30, 30, 30) };
        bodyPanel.Controls.Add(_body);

        Controls.Add(bodyPanel);
        Controls.Add(top);

        _timer.Tick += (_, _) => ReloadView(force: false);
        Load += (_, _) => { ReloadView(force: true); _timer.Start(); };
        FormClosing += (_, _) => _timer.Stop();
    }

    private void ReloadView(bool force)
    {
        var path = _getCurrentPath();
        try
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                _body.Text = "(no log file yet)";
                return;
            }

            var write = File.GetLastWriteTimeUtc(path);
            var filter = _search.Text;
            if (!force && path == _lastPath && write == _lastWrite && filter == _lastFilter)
                return;

            _lastPath = path;
            _lastWrite = write;
            _lastFilter = filter;

            var content = ReadShared(path);
            content = LogSearch.Filter(content, filter);

            _body.Text = content.Length == 0 ? "(no matching entries)" : content;
            Text = $"Clip Logger — Viewer   ·   {Path.GetFileName(path)}";

            if (string.IsNullOrWhiteSpace(filter))
                ScrollToBottom();
        }
        catch
        {
            // transient read error (file mid-write) — try again next tick
        }
    }

    private void ScrollToBottom()
    {
        _body.SelectionStart = _body.TextLength;
        _body.SelectionLength = 0;
        _body.ScrollToCaret();
    }

    private static string ReadShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);
        return sr.ReadToEnd();
    }

    private void OpenFolder()
    {
        var dir = Path.GetDirectoryName(_getCurrentPath());
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
    }
}
