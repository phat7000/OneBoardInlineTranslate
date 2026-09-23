using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Services;
using Color = System.Windows.Media.Color;
using Forms = System.Windows.Forms;

namespace OneBoardInlineTranslate;

public partial class OverlayWindow : Window
{
    private static readonly TimeSpan VisibleDuration = TimeSpan.FromSeconds(12);
    private readonly DispatcherTimer _hideTimer;
    private readonly DispatcherTimer _boundsSaveTimer;
    private readonly IClipboardService _clipboard;
    private readonly ISettingsService _settings;
    private HwndSource? _source;
    private bool _windowHookInstalled;
    private bool _positioning;
    private string _translationText = string.Empty;

    internal OverlayWindow(IClipboardService clipboard, ISettingsService settings)
    {
        InitializeComponent();
        _clipboard = clipboard;
        _settings = settings;
        _hideTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = VisibleDuration };
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            Hide();
        };
        _boundsSaveTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(450)
        };
        _boundsSaveTimer.Tick += async (_, _) =>
        {
            _boundsSaveTimer.Stop();
            await SavePinnedBoundsAsync();
        };
        LocationChanged += (_, _) => SchedulePinnedBoundsSave();
        SizeChanged += (_, _) => SchedulePinnedBoundsSave();
    }

    internal nint EnsureWindowHandle()
    {
        var handle = new WindowInteropHelper(this).EnsureHandle();
        ConfigureWindow(handle, pinned: false);
        return handle;
    }

    internal void ShowTranslation(ForegroundContext context, string original, TranslationResult result)
    {
        StatusText.Text = result.UsedPivot ? "PIVOT VIA ENGLISH" : string.Empty;
        StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
        LanguageRouteText.Text = $"{result.SourceLanguage.Code.ToUpperInvariant()}  →  {result.TargetLanguage.Code.ToUpperInvariant()}";
        SourceLanguageText.Text = result.SourceLanguage.DisplayName.ToUpperInvariant();
        OriginalText.Text = original;
        TargetLanguageText.Text = result.TargetLanguage.DisplayName.ToUpperInvariant();
        TranslatedText.Text = result.Text;
        _translationText = result.Text;
        MetadataText.Text = $"{result.ProviderId} · {result.Latency.TotalMilliseconds:0} ms";
        ShowForCurrentMode(context.WindowHandle);
    }

    internal void ShowMessage(ForegroundContext? context, string title, string message, bool isError = false)
    {
        StatusText.Text = title.ToUpperInvariant();
        StatusText.Foreground = new SolidColorBrush(isError
            ? Color.FromRgb(0xDC, 0x26, 0x26)
            : Color.FromRgb(0x64, 0x74, 0x8B));
        LanguageRouteText.Text = isError ? "ATTENTION" : "ONEBOARD";
        SourceLanguageText.Text = string.Empty;
        OriginalText.Text = string.Empty;
        TargetLanguageText.Text = string.Empty;
        TranslatedText.Text = message;
        _translationText = message;
        MetadataText.Text = string.Empty;
        ShowForCurrentMode(context?.WindowHandle ?? nint.Zero);
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

    private async void Unpin_Click(object sender, RoutedEventArgs eventArgs)
    {
        var updated = _settings.Current.Clone();
        updated.ResultWindowMode = ResultWindowMode.Popup;
        await _settings.SaveAsync(updated);
        Hide();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs eventArgs)
    {
        if (eventArgs.LeftButton == MouseButtonState.Pressed &&
            _settings.Current.ResultWindowMode == ResultWindowMode.Pinned)
        {
            DragMove();
        }
    }

    private void ConfigureWindow(nint handle, bool pinned)
    {
        var extendedStyle = NativeMethods.GetWindowLong(handle, NativeMethods.GwlExStyle);
        extendedStyle |= NativeMethods.WsExToolWindow;
        extendedStyle = pinned
            ? extendedStyle & ~NativeMethods.WsExNoActivate
            : extendedStyle | NativeMethods.WsExNoActivate;
        NativeMethods.SetWindowLong(handle, NativeMethods.GwlExStyle, extendedStyle);

        ShowActivated = pinned;
        Focusable = pinned;
        ResizeMode = pinned ? ResizeMode.CanResizeWithGrip : ResizeMode.NoResize;
        SizeToContent = pinned ? SizeToContent.Manual : SizeToContent.Height;
        UnpinButton.Visibility = pinned ? Visibility.Visible : Visibility.Collapsed;

        _source ??= HwndSource.FromHwnd(handle);
        if (_source is not null && !_windowHookInstalled)
        {
            _source.AddHook(WindowProcedure);
            _windowHookInstalled = true;
        }
    }

    private nint WindowProcedure(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == NativeMethods.WmMouseActivate &&
            _settings.Current.ResultWindowMode != ResultWindowMode.Pinned)
        {
            handled = true;
            return new nint(NativeMethods.MaNoActivate);
        }

        return nint.Zero;
    }

    private void ShowForCurrentMode(nint sourceWindow)
    {
        var mode = _settings.Current.ResultWindowMode;
        _hideTimer.Stop();
        _positioning = true;
        try
        {
            var pinned = mode == ResultWindowMode.Pinned;
            ApplyRequestedSize(pinned, mode == ResultWindowMode.Hidden);
            if (!IsVisible)
            {
                Show();
            }

            UpdateLayout();
            var handle = new WindowInteropHelper(this).Handle;
            ConfigureWindow(handle, pinned);
            if (pinned)
            {
                PlacePinned(handle, sourceWindow);
            }
            else
            {
                PlacePopup(handle);
                _hideTimer.Start();
            }
        }
        finally
        {
            _positioning = false;
        }
    }

    private void ApplyRequestedSize(bool pinned, bool compactHidden)
    {
        if (pinned)
        {
            Width = _settings.Current.PinnedBounds.Width;
            Height = _settings.Current.PinnedBounds.Height;
            MaxHeight = double.PositiveInfinity;
            return;
        }

        MaxHeight = 720;
        var preset = compactHidden ? PopupSizePreset.Small : _settings.Current.PopupSizePreset;
        (Width, Height) = preset switch
        {
            PopupSizePreset.Small => (360, 220),
            PopupSizePreset.Medium => (460, 340),
            PopupSizePreset.Large => (620, 480),
            PopupSizePreset.Custom => (_settings.Current.PopupWidth, _settings.Current.PopupHeight),
            _ => (_settings.Current.PopupWidth, double.NaN)
        };
        SizeToContent = preset == PopupSizePreset.Auto ? SizeToContent.Height : SizeToContent.Manual;
    }

    private void PlacePopup(nint handle)
    {
        NativeMethods.GetCursorPos(out var cursor);
        var cursorRect = new NativeMethods.Rect
        {
            Left = cursor.X,
            Top = cursor.Y,
            Right = cursor.X + 1,
            Bottom = cursor.Y + 1
        };
        var monitor = NativeMethods.MonitorFromRect(ref cursorRect, NativeMethods.MonitorDefaultToNearest);
        var monitorInfo = NativeMethods.MonitorInfo.Create();
        if (monitor == nint.Zero ||
            !NativeMethods.GetMonitorInfo(monitor, ref monitorInfo) ||
            !NativeMethods.GetWindowRect(handle, out var overlayRect))
        {
            return;
        }

        var work = ToPixelRect(monitorInfo.WorkArea);
        var source = new PixelRect(cursor.X, cursor.Y, cursor.X + 1, cursor.Y + 1);
        var width = overlayRect.Right - overlayRect.Left;
        var height = overlayRect.Bottom - overlayRect.Top;
        var position = OverlayPositioner.Place(
            _settings.Current.PopupPositionMode,
            source,
            work,
            width,
            height,
            ToNullableInt(_settings.Current.PopupCustomLeft),
            ToNullableInt(_settings.Current.PopupCustomTop));
        NativeMethods.SetWindowPos(
            handle,
            NativeMethods.HwndTopmost,
            position.X,
            position.Y,
            width,
            height,
            NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
    }

    private void PlacePinned(nint handle, nint sourceWindow)
    {
        var saved = _settings.Current.PinnedBounds;
        var screen = Forms.Screen.AllScreens.FirstOrDefault(candidate =>
            string.Equals(candidate.DeviceName, saved.Monitor, StringComparison.OrdinalIgnoreCase)) ??
            Forms.Screen.FromHandle(sourceWindow == nint.Zero ? handle : sourceWindow);
        var work = new PixelRect(
            screen.WorkingArea.Left,
            screen.WorkingArea.Top,
            screen.WorkingArea.Right,
            screen.WorkingArea.Bottom);
        var dpi = NativeMethods.GetDpiForWindow(handle);
        var scale = (dpi == 0 ? 96 : dpi) / 96.0;
        var width = (int)Math.Round(saved.Width * scale);
        var height = (int)Math.Round(saved.Height * scale);
        var left = saved.Left is null ? work.Right - width - 16 : (int)Math.Round(saved.Left.Value);
        var top = saved.Top is null ? work.Top + 16 : (int)Math.Round(saved.Top.Value);
        var placed = OverlayPositioner.ClampBounds(
            new PixelRect(left, top, left + width, top + height),
            work);
        NativeMethods.SetWindowPos(
            handle,
            NativeMethods.HwndTopmost,
            placed.Left,
            placed.Top,
            placed.Width,
            placed.Height,
            NativeMethods.SwpShowWindow);
    }

    private void SchedulePinnedBoundsSave()
    {
        if (_positioning || !IsVisible || _settings.Current.ResultWindowMode != ResultWindowMode.Pinned)
        {
            return;
        }

        _boundsSaveTimer.Stop();
        _boundsSaveTimer.Start();
    }

    private async Task SavePinnedBoundsAsync()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == nint.Zero || !NativeMethods.GetWindowRect(handle, out var rect))
        {
            return;
        }

        var updated = _settings.Current.Clone();
        var dpi = NativeMethods.GetDpiForWindow(handle);
        if (dpi == 0)
        {
            dpi = 96;
        }
        updated.PinnedBounds.Left = rect.Left;
        updated.PinnedBounds.Top = rect.Top;
        updated.PinnedBounds.Width = (rect.Right - rect.Left) * 96.0 / dpi;
        updated.PinnedBounds.Height = (rect.Bottom - rect.Top) * 96.0 / dpi;
        updated.PinnedBounds.Monitor = Forms.Screen.FromHandle(handle).DeviceName;
        await _settings.SaveAsync(updated);
    }

    private static PixelRect ToPixelRect(NativeMethods.Rect rect) =>
        new(rect.Left, rect.Top, rect.Right, rect.Bottom);

    private static int? ToNullableInt(double? value) => value is null ? null : (int)Math.Round(value.Value);
}
