using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Infrastructure;

internal sealed class GlobalHotkeyService : IDisposable
{
    private const int FirstHotkeyId = 0x5100;
    private readonly nint _windowHandle;
    private readonly HwndSource _source;
    private readonly Dictionary<int, RegisteredHotkey> _registrations = [];
    private readonly List<string> _unavailable = [];
    private bool _disposed;

    internal GlobalHotkeyService(nint windowHandle, HotkeySettings settings, bool paused = false)
    {
        _windowHandle = windowHandle;
        _source = HwndSource.FromHwnd(windowHandle)
            ?? throw new InvalidOperationException("The application window source is unavailable.");
        _source.AddHook(WindowProcedure);
        if (!paused)
        {
            ApplySettings(settings);
        }
        else
        {
            IsPaused = true;
        }
    }

    internal event EventHandler<HotkeyPressedEventArgs>? Pressed;

    internal IReadOnlyList<string> UnavailableHotkeys => _unavailable;

    internal bool IsPaused { get; private set; }

    internal void ApplySettings(HotkeySettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        UnregisterAll();
        _unavailable.Clear();
        var used = new HashSet<(uint Modifiers, int Key)>();
        var id = FirstHotkeyId;
        foreach (var (operation, text) in settings.Enumerate())
        {
            if (!HotkeyGesture.TryParse(text, out var gesture) || gesture is null)
            {
                _unavailable.Add($"{operation}: invalid gesture '{text}'");
                id++;
                continue;
            }

            var key = (gesture.Modifiers, gesture.VirtualKey);
            if (!used.Add(key))
            {
                _unavailable.Add($"{operation}: {gesture.DisplayText} duplicates another OneBoard hotkey");
                id++;
                continue;
            }

            if (!NativeMethods.RegisterHotKey(
                    _windowHandle,
                    id,
                    gesture.Modifiers,
                    checked((uint)gesture.VirtualKey)))
            {
                var error = new Win32Exception(Marshal.GetLastWin32Error());
                _unavailable.Add($"{operation}: {gesture.DisplayText} unavailable ({error.NativeErrorCode})");
                id++;
                continue;
            }

            _registrations[id] = new RegisteredHotkey(operation, gesture);
            id++;
        }

        IsPaused = false;
    }

    internal void Pause()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        UnregisterAll();
        IsPaused = true;
    }

    internal void Resume(HotkeySettings settings) => ApplySettings(settings);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        UnregisterAll();
        _source.RemoveHook(WindowProcedure);
    }

    private nint WindowProcedure(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == NativeMethods.WmHotkey && _registrations.TryGetValue(wParam.ToInt32(), out var registration))
        {
            handled = true;
            Pressed?.Invoke(this, new HotkeyPressedEventArgs(
                registration.Operation,
                registration.Gesture.VirtualKey));
        }

        return nint.Zero;
    }

    private void UnregisterAll()
    {
        foreach (var id in _registrations.Keys)
        {
            NativeMethods.UnregisterHotKey(_windowHandle, id);
        }

        _registrations.Clear();
    }

    private sealed record RegisteredHotkey(OperationType Operation, HotkeyGesture Gesture);
}

internal sealed class HotkeyPressedEventArgs(OperationType operation, int triggerVirtualKey) : EventArgs
{
    internal OperationType Operation { get; } = operation;

    internal int TriggerVirtualKey { get; } = triggerVirtualKey;
}
