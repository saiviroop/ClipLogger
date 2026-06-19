using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ClipLogger.App;

public static class ClipboardCapture
{
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion U; }

    // The union MUST size to its largest member (MOUSEINPUT). If it only held
    // KEYBDINPUT, Marshal.SizeOf<INPUT>() would be 32 (not 40) on x64 and SendInput
    // would reject every call with ERROR_INVALID_PARAMETER (87) because cbSize is wrong.
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT { public uint uMsg; public ushort wParamL; public ushort wParamH; }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12; // Alt
    private const int VK_C = 0x43;

    /// <summary>
    /// Copies the currently-selected text in the focused window and returns it, or null
    /// if nothing was copied (no selection, or a target that ignores a synthetic Ctrl+C).
    /// Waits for the hotkey modifiers to be released first so the injected Ctrl+C is seen
    /// as a clean copy rather than Ctrl+Alt+C.
    /// </summary>
    public static string? CaptureSelectedText()
    {
        // Wait for the user to let go of Ctrl/Alt (held from pressing the hotkey).
        WaitForModifiersReleased(1000);

        var previous = SafeGetText();
        SafeClear();
        SendCtrlC();

        for (int i = 0; i < 30; i++)
        {
            Thread.Sleep(50);
            var text = SafeGetText();
            if (!string.IsNullOrEmpty(text))
                return text; // captured (and left on the clipboard — Option A)
        }

        // Nothing copied: restore the previous clipboard and report failure with context.
        DebugLog.Write($"capture FAILED: no text copied. foreground={ForegroundInfo()}");
        if (!string.IsNullOrEmpty(previous))
            SafeSetText(previous);
        return null;
    }

    private static void WaitForModifiersReleased(int timeoutMs)
    {
        int waited = 0;
        while (waited < timeoutMs && (IsDown(VK_CONTROL) || IsDown(VK_MENU) || IsDown(VK_C)))
        {
            Thread.Sleep(20);
            waited += 20;
        }
    }

    private static bool IsDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    private static void SendCtrlC()
    {
        SendInputs(
            Key(VK_CONTROL, false),
            Key(VK_C, false),
            Key(VK_C, true),
            Key(VK_CONTROL, true));
    }

    private static uint SendInputs(params INPUT[] inputs)
    {
        for (int i = 0; i < inputs.Length; i++) inputs[i].type = INPUT_KEYBOARD;
        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT Key(int vk, bool up) => new INPUT
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = (ushort)vk,
                wScan = 0,
                dwFlags = up ? KEYEVENTF_KEYUP : 0,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            }
        }
    };

    private static string ForegroundInfo()
    {
        try
        {
            var h = GetForegroundWindow();
            var title = new StringBuilder(256);
            var cls = new StringBuilder(256);
            GetWindowText(h, title, title.Capacity);
            GetClassName(h, cls, cls.Capacity);
            return $"class='{cls}' title='{title}'";
        }
        catch { return "<unavailable>"; }
    }

    private static string? SafeGetText()
    {
        for (int i = 0; i < 5; i++)
        {
            try { return Clipboard.ContainsText() ? Clipboard.GetText() : null; }
            catch { Thread.Sleep(20); }
        }
        return null;
    }

    private static void SafeClear()
    {
        for (int i = 0; i < 5; i++)
        {
            try { Clipboard.Clear(); return; }
            catch { Thread.Sleep(20); }
        }
    }

    private static void SafeSetText(string text)
    {
        for (int i = 0; i < 5; i++)
        {
            try { Clipboard.SetText(text); return; }
            catch { Thread.Sleep(20); }
        }
    }
}
