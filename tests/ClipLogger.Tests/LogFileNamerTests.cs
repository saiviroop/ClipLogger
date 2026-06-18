using System;
using ClipLogger.Core;
using Xunit;

public class LogFileNamerTests
{
    [Fact]
    public void MakeFileName_UsesDateAndTime()
    {
        var name = LogFileNamer.MakeFileName(new DateTime(2026, 6, 18, 14, 32, 0));
        Assert.Equal("cliplog-2026-06-18_14-32.txt", name);
    }
}
