namespace ClipLogger.Core;

public interface IRunKeyStore
{
    void Set(string name, string value);
    void Remove(string name);
    string? Get(string name);
}
