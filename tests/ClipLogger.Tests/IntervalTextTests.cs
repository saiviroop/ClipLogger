using ClipLogger.Core;
using Xunit;

public class IntervalTextTests
{
    [Theory]
    [InlineData(1, "1 minute")]
    [InlineData(30, "30 minutes")]
    [InlineData(60, "1 hour")]
    [InlineData(90, "90 minutes")]
    [InlineData(120, "2 hours")]
    public void Describe_FormatsHumanReadable(int minutes, string expected)
    {
        Assert.Equal(expected, IntervalText.Describe(minutes));
    }
}
