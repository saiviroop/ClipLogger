using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ClipLogger.App;

public static class ClipboardCapture
{
    [DllImport("user32.dll")] private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_MENU = 0x12; // Alt
    private const ushort VK_C = 0x43;

    /// <summary>
    /// Copies the currently-selected text in the focused window and returns it.
    /// Returns null if nothing was actually copied (no selection, or the target
    /// ignores a simulated Ctrl+C) — in that case the previous clipboard is restored.
    /// </summary>
    public static string? CaptureSelectedText()
    {
        // Snapshot the existing clipboard so we can (a) detect a real change and
        // (b) restore it if the copy produces nothing.
        var previous = SafeGetText();

        // Clear first: this is what makes a failed copy detectable instead of us
        // silently returning whatever stale text was already on the clipboard.
        SafeClear();

        SendCleanCtrlC();

        // Condition-based wait: poll until the selection actually lands on the
        // clipboard, up to ~1.2s. Most apps respond within ~50-150ms.
        for (int i = 0; i < 24; i++)
        {
            Thread.Sleep(50);
            var text = SafeGetText();
            if (!string.IsNullOrEmpty(text))
                return text; // selection captured (and remains on the clipboard — Option A)
        }

        // Nothing new was copied — put the user's original clipboard back and report failure.
        if (!string.IsNullOrEmpty(previous))
            SafeSetText(previous);
        return null;
    }

    // The hotkey is Ctrl+Alt+C, so Ctrl and Alt are physically held when this runs.
    // Release BOTH so the target sees a clean Ctrl+C (a lingering Alt makes the target
    // read it as Ctrl+Alt+C, which is not a copy), then issue a fresh Ctrl+C.
    private static void SendCleanCtrlC()
    {
        SendInputs(Key(VK_MENU, true), Key(VK_CONTROL, true)); // Alt up, Ctrl up
        Thread.Sleep(40);                                      // let the modifier release settle
        SendInputs(Key(VK_CONTROL, false), Key(VK_C, false), Key(VK_C, true), Key(VK_CONTROL, true));
    }

    private static void SendInputs(params INPUT[] inputs)
        => SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());

    private static INPUT Key(ushort vk, bool up) => new INPUT
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = vk,
                wScan = 0,
                dwFlags = up ? KEYEVENTF_KEYUP : 0,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            }
        }
    };

    // Clipboard can be momentarily locked by another process; retry a few times.
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
