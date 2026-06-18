namespace ClipLogger.Core;

public class AutoStartManager
{
    public const string ValueName = "ClipLogger";

    private readonly IRunKeyStore _store;
    public AutoStartManager(IRunKeyStore store) => _store = store;

    public bool IsEnabled() => _store.Get(ValueName) != null;
    public void Enable(string exePath) => _store.Set(ValueName, $"\"{exePath}\"");
    public void Disable() => _store.Remove(ValueName);
}
