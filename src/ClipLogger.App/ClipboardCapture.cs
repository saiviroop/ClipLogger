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

    /// <summary>Simulates Ctrl+C in the focused window and returns the resulting clipboard text, or null.</summary>
    public static string? CaptureSelectedText()
    {
        SendCleanCtrlC();
        Thread.Sleep(120); // let the target app populate the clipboard

        if (Clipboard.ContainsText())
        {
            var t = Clipboard.GetText();
            return string.IsNullOrEmpty(t) ? null : t;
        }
        return null;
    }

    // The hotkey is Ctrl+Alt+C, so Ctrl and Alt are physically held. Release Alt first so the
    // target sees a clean Ctrl+C (Alt held would make it Ctrl+Alt+C and break copy in many apps).
    private static void SendCleanCtrlC()
    {
        var inputs = new[]
        {
            Key(VK_MENU, true),     // Alt up
            Key(VK_CONTROL, false), // Ctrl down
            Key(VK_C, false),       // C down
            Key(VK_C, true),        // C up
            Key(VK_CONTROL, true),  // Ctrl up
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

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
}
