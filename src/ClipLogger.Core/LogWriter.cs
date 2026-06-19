namespace ClipLogger.Core;

public class LogWriter
{
    private readonly Func<DateTime> _clock;

    public string FolderPath { get; }
    public string CurrentFilePath { get; private set; } = "";
    public DateTime CurrentFileStarted { get; private set; }
    public int EntryCount { get; private set; }

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
        EntryCount = 0;
        CurrentFilePath = Path.Combine(FolderPath, LogFileNamer.MakeFileName(now));
        if (!File.Exists(CurrentFilePath))
            File.WriteAllText(CurrentFilePath, "");
    }

    public void Append(string text, string? source = null)
    {
        File.AppendAllText(CurrentFilePath, EntryFormatter.Format(_clock(), text, source));
        EntryCount++;
    }

    public void ResetStartTime() => CurrentFileStarted = _clock();
}
