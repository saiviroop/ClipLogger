using System;
using ClipLogger.Core;
using Xunit;

public class CheckInSchedulerTests
{
    private static readonly DateTime Start = new(2026, 6, 18, 14, 0, 0);

    [Fact]
    public void IsDue_FalseBeforeInterval()
    {
        Assert.False(CheckInScheduler.IsDue(Start, Start.AddMinutes(59), 60));
    }

    [Fact]
    public void IsDue_TrueAtInterval()
    {
        Assert.True(CheckInScheduler.IsDue(Start, Start.AddMinutes(60), 60));
    }

    [Fact]
    public void IsDue_TrueAfterInterval()
    {
        Assert.True(CheckInScheduler.IsDue(Start, Start.AddMinutes(125), 60));
    }
}
