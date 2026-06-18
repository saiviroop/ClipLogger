using System.Collections.Generic;
using ClipLogger.Core;

public class FakeRunKeyStore : IRunKeyStore
{
    public readonly Dictionary<string, string> Values = new();
    public void Set(string name, string value) => Values[name] = value;
    public void Remove(string name) => Values.Remove(name);
    public string? Get(string name) => Values.TryGetValue(name, out var v) ? v : null;
}
