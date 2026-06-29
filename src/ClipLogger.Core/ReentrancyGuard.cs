namespace ClipLogger.Core;

/// <summary>
/// A one-slot re-entrancy guard. <see cref="TryEnter"/> succeeds only when the
/// guard is idle; a second call returns false until <see cref="Exit"/> is called.
/// Used to stop a UI timer from opening a second check-in prompt while one is
/// already showing (a modal MessageBox keeps pumping WM_TIMER messages).
/// </summary>
public class ReentrancyGuard
{
    private bool _entered;

    public bool TryEnter()
    {
        if (_entered) return false;
        _entered = true;
        return true;
    }

    public void Exit() => _entered = false;
}
