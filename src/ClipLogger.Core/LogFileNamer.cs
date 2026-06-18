namespace ClipLogger.Core;

public static class LogFileNamer
{
    public static string MakeFileName(DateTime when) => $"cliplog-{when:yyyy-MM-dd_HH-mm}.txt";
}
