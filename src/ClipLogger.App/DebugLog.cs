using System;
using System.IO;

namespace ClipLogger.App;

/// <summary>
/// Lightweight diagnostic log written to %APPDATA%\ClipLogger\debug.log.
/// Temporary instrumentation for diagnosing capture issues.
/// </summary>
public static class DebugLog
{
    private static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClipLogger", "debug.log");

    public static void Write(string message)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.AppendAllText(Path, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch { /* never let logging break the app */ }
    }
}
