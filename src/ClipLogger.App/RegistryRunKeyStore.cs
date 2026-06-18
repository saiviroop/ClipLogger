using Microsoft.Win32;
using ClipLogger.Core;

namespace ClipLogger.App;

public class RegistryRunKeyStore : IRunKeyStore
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public void Set(string name, string value)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key!.SetValue(name, value);
    }

    public void Remove(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }

    public string? Get(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(name) as string;
    }
}
