using System;
using System.Collections.Generic;
using System.IO;
using ClipLogger.Core;
using Xunit;

public class LogWriterTests
{
    private static string TempDir() => Path.Combine(Path.GetTempPath(), $"cliptest_{Guid.NewGuid():N}");

    [Fact]
    public void Constructor_CreatesDatedFile()
    {
        var dir = TempDir();
        try
        {
            var fixedTime = new DateTime(2026, 6, 18, 14, 32, 5);
            var w = new LogWriter(dir, () => fixedTime);

            Assert.True(File.Exists(Path.Combine(dir, "cliplog-2026-06-18_14-32.txt")));
            Assert.Equal(fixedTime, w.CurrentFileStarted);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Append_WritesFormattedEntry()
    {
        var dir = TempDir();
        try
        {
            var fixedTime = new DateTime(2026, 6, 18, 14, 32, 5);
            var w = new LogWriter(dir, () => fixedTime);
            w.Append("captured text");

            var content = File.ReadAllText(w.CurrentFilePath);
            Assert.Contains("[2026-06-18 14:32:05]", content);
            Assert.Contains("captured text", content);
            Assert.Contains(EntryFormatter.Separator, content);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Append_IncrementsEntryCount_AndNewFileResetsIt()
    {
        var dir = TempDir();
        try
        {
            var w = new LogWriter(dir, () => new DateTime(2026, 6, 18, 14, 32, 5));
            Assert.Equal(0, w.EntryCount);
            w.Append("one");
            w.Append("two");
            Assert.Equal(2, w.EntryCount);
            w.StartNewFile();
            Assert.Equal(0, w.EntryCount);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Append_WithSource_WritesSourceInEntry()
    {
        var dir = TempDir();
        try
        {
            var w = new LogWriter(dir, () => new DateTime(2026, 6, 18, 14, 32, 5));
            w.Append("captured text", "Notepad");
            var content = File.ReadAllText(w.CurrentFilePath);
            Assert.Contains("(from: Notepad)", content);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void StartNewFile_ResetsStartedTime()
    {
        var dir = TempDir();
        try
        {
            var times = new Queue<DateTime>(new[]
            {
                new DateTime(2026, 6, 18, 14, 0, 0),
                new DateTime(2026, 6, 18, 15, 30, 0),
            });
            var w = new LogWriter(dir, () => times.Dequeue());
            w.StartNewFile();
            Assert.Equal(new DateTime(2026, 6, 18, 15, 30, 0), w.CurrentFileStarted);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void ResetStartTime_RebasesStartedTime()
    {
        var dir = TempDir();
        try
        {
            var times = new Queue<DateTime>(new[]
            {
                new DateTime(2026, 6, 18, 14, 0, 0),
                new DateTime(2026, 6, 18, 16, 0, 0),
            });
            var w = new LogWriter(dir, () => times.Dequeue());
            w.ResetStartTime();
            Assert.Equal(new DateTime(2026, 6, 18, 16, 0, 0), w.CurrentFileStarted);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
}
