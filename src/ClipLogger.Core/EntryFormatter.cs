namespace ClipLogger.Core;

public static class EntryFormatter
{
    public static readonly string Separator = new string('-', 40);

    public static string Format(DateTime timestamp, string text, string? source = null)
    {
        var nl = Environment.NewLine;
        var ts = timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        var header = string.IsNullOrWhiteSpace(source) ? $"[{ts}]" : $"[{ts}]  (from: {source})";
        return $"{header}{nl}{text}{nl}{nl}{nl}{nl}{nl}{Separator}{nl}";
    }
}
