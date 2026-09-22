using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Services;
using Color = System.Windows.Media.Color;

namespace OneBoardInlineTranslate;

public partial class OverlayWindow : Window
{
    private static readonly TimeSpan VisibleDuration = TimeSpan.FromSeconds(12);
    private readonly DispatcherTimer _hideTimer;
    private readonly IClipboardService _clipboard;
    private HwndSource? _source;
    private bool _windowHookInstalled;
    private string _translationText = string.Empty;

    internal OverlayWindow(IClipboardService clipboard)
    {
        InitializeComponent();
        _clipboard = clipboard;
        _hideTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = VisibleDuration };
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

    internal void ShowTranslation(ForegroundContext context, string original, TranslationResult result)
    {
        StatusText.Text = "TRANSLATED";
        StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x17, 0x20, 0x33));
        SourceLanguageText.Text = result.SourceLanguage.DisplayName.ToUpperInvariant();
        OriginalText.Text = original;
        TargetLanguageText.Text = result.TargetLanguage.DisplayName.ToUpperInvariant();
        TranslatedText.Text = result.Text;
        _translationText = result.Text;
        MetadataText.Text = $"{result.ProviderId} · {result.Latency.TotalMilliseconds:0} ms";
        ShowWithoutActivation(context.WindowHandle);
    }

    internal void ShowMessage(ForegroundContext? context, string title, string message, bool isError = false)
    {
        StatusText.Text = title.ToUpperInvariant();
        StatusText.Foreground = new SolidColorBrush(isError
            ? Color.FromRgb(0xDC, 0x26, 0x26)
            : Color.FromRgb(0x17, 0x20, 0x33));
        SourceLanguageText.Text = string.Empty;
        OriginalText.Text = string.Empty;
        TargetLanguageText.Text = string.Empty;
        TranslatedText.Text = message;
        _translationText = message;
        MetadataText.Text = string.Empty;
        ShowWithoutActivation(context?.WindowHandle ?? nint.Zero);
    }

    protected override void OnClosed(EventArgs eventArgs)
    {
        if (_source is not null)
        {
            _source.RemoveHook(WindowProcedure);
        }

        base.OnClosed(eventArgs);
    }

    private async void Copy_Click(object sender, RoutedEventArgs eventArgs)
    {
        try
        {
            await _clipboard.CopyTextAsync(_translationText, CancellationToken.None);
            MetadataText.Text = "Copied";
        }
        catch
        {
            MetadataText.Text = "Clipboard unavailable";
        }
    }

    private void Close_Click(object sender, RoutedEventArgs eventArgs)
    {
        _hideTimer.Stop();
        Hide();
    }

    private void ConfigureNonActivatingWindow(nint handle)
    {
        var extendedStyle = NativeMethods.GetWindowLong(handle, NativeMethods.GwlExStyle);
        NativeMethods.SetWindowLong(
            handle,
            NativeMethods.GwlExStyle,
            extendedStyle | NativeMethods.WsExNoActivate | NativeMethods.WsExToolWindow);

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
            NativeMethods.GetWindowRect(overlayHandle, out var overlayRect))
        {
            NativeMethods.Rect sourceRect;
            if (sourceWindow == nint.Zero || !NativeMethods.GetWindowRect(sourceWindow, out sourceRect))
            {
                sourceRect = monitorInfo.WorkArea;
            }

            var work = new PixelRect(
                monitorInfo.WorkArea.Left,
                monitorInfo.WorkArea.Top,
                monitorInfo.WorkArea.Right,
                monitorInfo.WorkArea.Bottom);
            var source = new PixelRect(sourceRect.Left, sourceRect.Top, sourceRect.Right, sourceRect.Bottom);
            var width = overlayRect.Right - overlayRect.Left;
            var height = overlayRect.Bottom - overlayRect.Top;
            var (x, y) = OverlayPositioner.Place(source, work, width, height);
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
