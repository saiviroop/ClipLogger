using ClipLogger.Core;
using Xunit;

public class AutoStartManagerTests
{
    [Fact]
    public void IsEnabled_FalseByDefault()
    {
        var m = new AutoStartManager(new FakeRunKeyStore());
        Assert.False(m.IsEnabled());
    }

    [Fact]
    public void Enable_WritesQuotedExePath()
    {
        var store = new FakeRunKeyStore();
        var m = new AutoStartManager(store);

        m.Enable(@"C:\Program Files\ClipLogger\ClipLogger.App.exe");

        Assert.True(m.IsEnabled());
        Assert.Equal("\"C:\\Program Files\\ClipLogger\\ClipLogger.App.exe\"",
            store.Values[AutoStartManager.ValueName]);
    }

    [Fact]
    public void Disable_RemovesEntry()
    {
        var store = new FakeRunKeyStore();
        var m = new AutoStartManager(store);
        m.Enable(@"C:\x.exe");
        m.Disable();
        Assert.False(m.IsEnabled());
    }
}
