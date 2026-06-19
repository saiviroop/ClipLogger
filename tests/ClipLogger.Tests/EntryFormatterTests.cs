using System;
using ClipLogger.Core;
using Xunit;

public class EntryFormatterTests
{
    [Fact]
    public void Format_ProducesTimestampTextFourBlankLinesAndSeparator()
    {
        var nl = Environment.NewLine;
        var result = EntryFormatter.Format(new DateTime(2026, 6, 18, 14, 32, 5), "hello world");

        var expected =
            $"[2026-06-18 14:32:05]{nl}" +
            $"hello world{nl}" +
            $"{nl}{nl}{nl}{nl}" +
            $"{EntryFormatter.Separator}{nl}";

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Separator_IsFortyDashes()
    {
        Assert.Equal(new string('-', 40), EntryFormatter.Separator);
    }

    [Fact]
    public void Format_WithSource_AddsFromSuffixToHeader()
    {
        var nl = Environment.NewLine;
        var result = EntryFormatter.Format(new DateTime(2026, 6, 18, 14, 32, 5), "hello", "Notepad");
        Assert.StartsWith($"[2026-06-18 14:32:05]  (from: Notepad){nl}hello", result);
    }

    [Fact]
    public void Format_WithBlankSource_OmitsFromSuffix()
    {
        var result = EntryFormatter.Format(new DateTime(2026, 6, 18, 14, 32, 5), "hello", "   ");
        Assert.StartsWith("[2026-06-18 14:32:05]", result);
        Assert.DoesNotContain("(from:", result);
    }
}
