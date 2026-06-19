using ClipLogger.Core;
using Xunit;

public class LogSearchTests
{
    private static string Build(params string[] texts)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var t in texts)
            sb.Append(EntryFormatter.Format(new System.DateTime(2026, 6, 18, 14, 0, 0), t));
        return sb.ToString();
    }

    [Fact]
    public void Filter_EmptyTerm_ReturnsContentUnchanged()
    {
        var content = Build("alpha", "beta");
        Assert.Equal(content, LogSearch.Filter(content, "   "));
    }

    [Fact]
    public void Filter_KeepsOnlyMatchingEntries()
    {
        var content = Build("alpha error here", "beta ok", "gamma error too");
        var result = LogSearch.Filter(content, "error");
        Assert.Contains("alpha error here", result);
        Assert.Contains("gamma error too", result);
        Assert.DoesNotContain("beta ok", result);
    }

    [Fact]
    public void Filter_IsCaseInsensitive()
    {
        var content = Build("Network Log line");
        var result = LogSearch.Filter(content, "network");
        Assert.Contains("Network Log line", result);
    }
}
