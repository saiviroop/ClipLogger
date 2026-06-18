using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClipLogger.App;

public class HotkeyManager : NativeWindow, IDisposable
{
    public event EventHandler? HotkeyPressed;

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 0xC1A0;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint VK_C = 0x43;

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public HotkeyManager() => CreateHandle(new CreateParams());

    public bool Register() => RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_C);

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && (int)m.WParam == HOTKEY_ID)
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        UnregisterHotKey(Handle, HOTKEY_ID);
        DestroyHandle();
    }
}
