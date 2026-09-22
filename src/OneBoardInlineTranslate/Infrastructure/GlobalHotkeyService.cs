using System.ComponentModel;
using System.Windows.Interop;
using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Infrastructure;

internal sealed class GlobalHotkeyService : IDisposable
{
    // RegisterHotKey reserves 0x0000-0xBFFF for application-defined identifiers.
    private const int CaptureHotkeyId = 0x5141;
    private const int ReplaceHotkeyId = 0x5142;

    private readonly nint _windowHandle;
    private readonly HwndSource _source;
    private bool _captureRegistered;
    private bool _replaceRegistered;
    private bool _disposed;

    internal GlobalHotkeyService(nint windowHandle)
    {
        _windowHandle = windowHandle;
        _source = HwndSource.FromHwnd(windowHandle)
            ?? throw new InvalidOperationException("The overlay window source is unavailable.");
        _source.AddHook(WindowProcedure);

        try
        {
            _captureRegistered = Register(CaptureHotkeyId, NativeMethods.VkQ);
            _replaceRegistered = Register(ReplaceHotkeyId, NativeMethods.VkE);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal event EventHandler<HotkeyPressedEventArgs>? Pressed;

    private bool Register(int id, int key)
    {
        if (!NativeMethods.RegisterHotKey(
                _windowHandle,
                id,
                NativeMethods.ModAlt | NativeMethods.ModNoRepeat,
                checked((uint)key)))
        {
            throw new Win32Exception($"The global hotkey Alt+{(char)key} is already in use or unavailable.");
        }

        return true;
    }

    private nint WindowProcedure(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message != NativeMethods.WmHotkey)
        {
            return nint.Zero;
        }

        var id = wParam.ToInt32();
        if (id == CaptureHotkeyId)
        {
            handled = true;
            Pressed?.Invoke(this, new HotkeyPressedEventArgs(HotkeyAction.Capture, NativeMethods.VkQ));
        }
        else if (id == ReplaceHotkeyId)
        {
            handled = true;
            Pressed?.Invoke(this, new HotkeyPressedEventArgs(HotkeyAction.Replace, NativeMethods.VkE));
        }

        return nint.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_captureRegistered)
        {
            NativeMethods.UnregisterHotKey(_windowHandle, CaptureHotkeyId);
        }

        if (_replaceRegistered)
        {
            NativeMethods.UnregisterHotKey(_windowHandle, ReplaceHotkeyId);
        }

        _source.RemoveHook(WindowProcedure);
    }
}

internal sealed class HotkeyPressedEventArgs(HotkeyAction action, int triggerVirtualKey) : EventArgs
{
    internal HotkeyAction Action { get; } = action;

    internal int TriggerVirtualKey { get; } = triggerVirtualKey;
}
