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
            if (fbd.ShowDialog() != DialogResult.OK) return; // cancelled -> exit
            config.LogFolder = fbd.SelectedPath;
            config.Save(ConfigPath);
        }

        Application.Run(new TrayApplicationContext(config));
    }
}
