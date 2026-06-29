using ClipLogger.Core;
using Xunit;

public class ReentrancyGuardTests
{
    [Fact]
    public void TryEnter_SucceedsWhenIdle()
    {
        var guard = new ReentrancyGuard();
        Assert.True(guard.TryEnter());
    }

    [Fact]
    public void TryEnter_FailsWhileAlreadyEntered()
    {
        var guard = new ReentrancyGuard();
        Assert.True(guard.TryEnter());
        // A second entry (e.g. a timer re-firing while a modal dialog is open)
        // must be rejected so we never stack a second prompt.
        Assert.False(guard.TryEnter());
    }

    [Fact]
    public void TryEnter_SucceedsAgainAfterExit()
    {
        var guard = new ReentrancyGuard();
        Assert.True(guard.TryEnter());
        guard.Exit();
        Assert.True(guard.TryEnter());
    }
}
