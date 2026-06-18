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

        Text = "Clip Logger - Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 200);

        _folderBox.Text = _config.LogFolder;
        _interval.Value = Math.Clamp(_config.CheckInMinutes, 1, 1440);
        _autoStartBox.Checked = _autoStart.IsEnabled();

        var folderLabel = new Label { Text = "Log folder:", Left = 12, Top = 18, AutoSize = true };
        var browse = new Button { Text = "Browse...", Left = 320, Top = 36, Width = 80 };
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
