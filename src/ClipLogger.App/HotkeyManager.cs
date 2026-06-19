using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClipLogger.App;

public class HotkeyManager : NativeWindow, IDisposable
{
    /// <summary>Raised with the id of the hotkey that was pressed.</summary>
    public event Action<int>? HotkeyPressed;

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;

    private const int WM_HOTKEY = 0x0312;
    private readonly List<int> _ids = new();

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public HotkeyManager() => CreateHandle(new CreateParams());

    public bool Register(int id, uint modifiers, uint vk)
    {
        if (RegisterHotKey(Handle, id, modifiers, vk))
        {
            _ids.Add(id);
            return true;
        }
        return false;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
            HotkeyPressed?.Invoke((int)m.WParam);
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        foreach (var id in _ids)
            UnregisterHotKey(Handle, id);
        _ids.Clear();
        DestroyHandle();
    }
}
