using System;
using System.IO;
using ClipLogger.Core;
using Xunit;

public class ConfigTests
{
    [Fact]
    public void Defaults_AreSixtyMinutesNoAutoStart()
    {
        var c = new Config();
        Assert.Equal(60, c.CheckInMinutes);
        Assert.False(c.AutoStart);
        Assert.Equal("", c.LogFolder);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsValues()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cliptest_{Guid.NewGuid():N}", "config.json");
        try
        {
            var c = new Config { LogFolder = @"C:\logs", CheckInMinutes = 30, AutoStart = true };
            c.Save(path);

            var loaded = Config.Load(path);
            Assert.Equal(@"C:\logs", loaded.LogFolder);
            Assert.Equal(30, loaded.CheckInMinutes);
            Assert.True(loaded.AutoStart);
        }
        finally
        {
            var dir = Path.GetDirectoryName(path)!;
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cliptest_{Guid.NewGuid():N}", "missing.json");
        var loaded = Config.Load(path);
        Assert.Equal(60, loaded.CheckInMinutes);
    }
}
