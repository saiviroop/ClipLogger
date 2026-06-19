namespace ClipLogger.Core;

public static class LogSearch
{
    /// <summary>
    /// Filters a log file's text to the entries containing <paramref name="term"/>
    /// (case-insensitive). An empty/whitespace term returns the content unchanged.
    /// </summary>
    public static string Filter(string content, string term)
    {
        if (string.IsNullOrWhiteSpace(term) || string.IsNullOrEmpty(content))
            return content;

        var entries = content.Split(new[] { EntryFormatter.Separator }, StringSplitOptions.None);
        var kept = new List<string>();
        foreach (var entry in entries)
        {
            if (entry.Contains(term, StringComparison.OrdinalIgnoreCase))
                kept.Add(entry);
        }

        return string.Join(EntryFormatter.Separator, kept);
    }
}
