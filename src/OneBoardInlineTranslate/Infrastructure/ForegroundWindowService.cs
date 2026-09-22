using System.ComponentModel;
using System.Diagnostics;
using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Infrastructure;

internal sealed class ForegroundWindowService
{
    internal ForegroundContext GetCurrent()
    {
        var window = NativeMethods.GetForegroundWindow();
        if (window == nint.Zero)
        {
            throw new InvalidOperationException("Windows did not report a foreground window.");
        }

        var threadId = NativeMethods.GetWindowThreadProcessId(window, out var processId);
        if (threadId == 0 || processId == 0)
        {
            throw new Win32Exception("The foreground window process could not be resolved.");
        }

        string processName;
        try
        {
            using var process = Process.GetProcessById(checked((int)processId));
            processName = process.ProcessName;
        }
        catch
        {
            processName = $"pid-{processId}";
        }

        return new ForegroundContext(window, processId, threadId, processName);
    }

    internal static bool IsStillForeground(ForegroundContext context) =>
        NativeMethods.IsWindow(context.WindowHandle) &&
        NativeMethods.GetForegroundWindow() == context.WindowHandle;
}
