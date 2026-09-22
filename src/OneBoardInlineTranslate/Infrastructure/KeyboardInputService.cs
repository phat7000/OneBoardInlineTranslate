using System.ComponentModel;
using System.Runtime.InteropServices;

namespace OneBoardInlineTranslate.Infrastructure;

internal sealed class KeyboardInputService
{
    private static readonly TimeSpan ModifierReleaseTimeout = TimeSpan.FromMilliseconds(900);

    internal async Task WaitForHotkeyReleaseAsync(int triggerVirtualKey, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + ModifierReleaseTimeout;
        while (IsDown(NativeMethods.VkMenu) || IsDown(triggerVirtualKey))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException("The hotkey keys were not released in time.");
            }

            await Task.Delay(10, cancellationToken);
        }
    }

    internal void SendCopy() => SendControlChord(NativeMethods.VkC);

    internal void SendPaste() => SendControlChord(NativeMethods.VkV);

    private static bool IsDown(int virtualKey) =>
        (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    private static void SendControlChord(int virtualKey)
    {
        var inputs = new[]
        {
            Key(NativeMethods.VkControl, keyUp: false),
            Key(virtualKey, keyUp: false),
            Key(virtualKey, keyUp: true),
            Key(NativeMethods.VkControl, keyUp: true)
        };

        var sent = NativeMethods.SendInput(
            checked((uint)inputs.Length),
            inputs,
            Marshal.SizeOf<NativeMethods.Input>());

        if (sent != inputs.Length)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows did not accept the complete keyboard chord.");
        }
    }

    private static NativeMethods.Input Key(int virtualKey, bool keyUp) => new()
    {
        Type = NativeMethods.InputKeyboard,
        Data = new NativeMethods.InputUnion
        {
            Keyboard = new NativeMethods.KeyboardInput
            {
                VirtualKey = checked((ushort)virtualKey),
                Flags = keyUp ? NativeMethods.KeyeventfKeyup : 0
            }
        }
    };
}
