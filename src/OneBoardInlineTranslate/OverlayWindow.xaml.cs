using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate;

public partial class OverlayWindow : Window
{
    private static readonly TimeSpan VisibleDuration = TimeSpan.FromSeconds(6);
    private readonly DispatcherTimer _hideTimer;
    private HwndSource? _source;
    private bool _windowHookInstalled;

    internal OverlayWindow()
    {
        InitializeComponent();
        _hideTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = VisibleDuration
        };
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            Hide();
        };
    }

    internal nint EnsureWindowHandle()
    {
        var handle = new WindowInteropHelper(this).EnsureHandle();
        ConfigureNonActivatingWindow(handle);
        return handle;
    }

    internal void ShowCapture(
        ForegroundContext context,
        CaptureResult result,
        string status = "CAPTURED")
    {
        StatusText.Text = status;
        StatusText.Foreground = result.Success
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x84, 0xD8, 0xFF))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x91, 0x91));
        ProcessText.Text = $"Process: {context.ProcessName}";
        CapturedText.Text = result.Success ? result.Text : "Capture failed — selection was not changed.";
        MethodText.Text = $"Method: {result.Method}";
        TimingText.Text = $"{result.LatencyMilliseconds} ms";

        ShowWithoutActivation(context.WindowHandle);
    }

    internal void ShowFailure(string processName, long latencyMilliseconds)
    {
        var fallbackContext = new ForegroundContext(nint.Zero, 0, 0, processName);
        var result = CaptureResult.Failed(
            latencyMilliseconds,
            new InvalidOperationException("The operation failed."));
        ShowCapture(fallbackContext, result, "FAILED");
    }

    protected override void OnClosed(EventArgs eventArgs)
    {
        if (_source is not null)
        {
            _source.RemoveHook(WindowProcedure);
        }

        base.OnClosed(eventArgs);
    }

    private void ConfigureNonActivatingWindow(nint handle)
    {
        var extendedStyle = NativeMethods.GetWindowLong(handle, NativeMethods.GwlExStyle);
        NativeMethods.SetWindowLong(
            handle,
            NativeMethods.GwlExStyle,
            extendedStyle |
            NativeMethods.WsExNoActivate |
            NativeMethods.WsExToolWindow |
            NativeMethods.WsExTransparent);

        _source ??= HwndSource.FromHwnd(handle);
        if (_source is not null && !_windowHookInstalled)
        {
            _source.AddHook(WindowProcedure);
            _windowHookInstalled = true;
        }
    }

    private nint WindowProcedure(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == NativeMethods.WmMouseActivate)
        {
            handled = true;
            return new nint(NativeMethods.MaNoActivate);
        }

        return nint.Zero;
    }

    private void ShowWithoutActivation(nint sourceWindow)
    {
        _hideTimer.Stop();
        if (!IsVisible)
        {
            Show();
        }

        UpdateLayout();
        var overlayHandle = new WindowInteropHelper(this).Handle;
        ConfigureNonActivatingWindow(overlayHandle);

        var monitor = NativeMethods.MonitorFromWindow(
            sourceWindow == nint.Zero ? overlayHandle : sourceWindow,
            NativeMethods.MonitorDefaultToNearest);
        var monitorInfo = NativeMethods.MonitorInfo.Create();

        if (monitor != nint.Zero &&
            NativeMethods.GetMonitorInfo(monitor, ref monitorInfo) &&
            NativeMethods.GetWindowRect(overlayHandle, out var windowRect))
        {
            var width = windowRect.Right - windowRect.Left;
            var height = windowRect.Bottom - windowRect.Top;
            const int edgeOffset = 16;
            var x = monitorInfo.WorkArea.Right - width - edgeOffset;
            var y = monitorInfo.WorkArea.Top + edgeOffset;
            NativeMethods.SetWindowPos(
                overlayHandle,
                NativeMethods.HwndTopmost,
                x,
                y,
                width,
                height,
                NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
        }

        _hideTimer.Start();
    }
}
