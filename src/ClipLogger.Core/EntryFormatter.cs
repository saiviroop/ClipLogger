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
