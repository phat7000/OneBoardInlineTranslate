using System.Net;
using System.Net.Http;
using System.Windows;
using OneBoardInlineTranslate.Diagnostics;
using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.OCR;
using OneBoardInlineTranslate.Security;
using OneBoardInlineTranslate.Services;
using OneBoardInlineTranslate.Views;
using MessageBox = System.Windows.MessageBox;

namespace OneBoardInlineTranslate;

public partial class App : System.Windows.Application
{
    private SingleInstanceGuard? _singleInstance;
    private GlobalHotkeyService? _hotkeys;
    private HotkeyCoordinator? _coordinator;
    private TrayIconService? _tray;
    private SettingsService? _settings;
    private DpapiCredentialStore? _credentials;
    private TranslationService? _translation;
    private StartupService? _startup;
    private HttpClient? _httpClient;
    private OverlayWindow? _overlay;

    protected override async void OnStartup(StartupEventArgs eventArgs)
    {
        base.OnStartup(eventArgs);
        _singleInstance = SingleInstanceGuard.Acquire();
        if (!_singleInstance.OwnsInstance)
        {
            Shutdown(0);
            return;
        }

        try
        {
            _settings = new SettingsService();
            var loadedSettings = await _settings.LoadAsync();
            _credentials = new DpapiCredentialStore();
            _startup = new StartupService();
            var clipboard = new ClipboardService();
            _overlay = new OverlayWindow(clipboard);
            MainWindow = _overlay;
            var windowHandle = _overlay.EnsureWindowHandle();

            var keyboard = new KeyboardInputService();
            var foregroundWindows = new ForegroundWindowService();
            var captureService = new SelectedTextCaptureService(
                new UiaSelectionReader(),
                new ClipboardSelectionReader(keyboard));
            var replacementService = new TextReplacementService(keyboard);
            _httpClient = new HttpClient(new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                PooledConnectionLifetime = TimeSpan.FromMinutes(10)
            })
            {
                Timeout = TimeSpan.FromSeconds(25)
            };
            var integrationMode = eventArgs.Args.Contains("--integration-test", StringComparer.OrdinalIgnoreCase);
            _translation = new TranslationService(
                _settings,
                _credentials,
                new LanguageDetector(),
                _httpClient,
                integrationMode ? new DeterministicTranslationProvider() : null);

            var runtimeSettings = integrationMode ? new AppSettings() : loadedSettings;
            _hotkeys = new GlobalHotkeyService(
                windowHandle,
                runtimeSettings.Hotkeys,
                paused: !integrationMode && runtimeSettings.IsPaused);
            var reply = new ReplyService(_translation, clipboard, replacementService);
            _coordinator = new HotkeyCoordinator(
                _hotkeys,
                foregroundWindows,
                keyboard,
                captureService,
                replacementService,
                _translation,
                _settings,
                reply,
                new RegionSelectionService(),
                new ScreenCaptureService(),
                new WindowsOcrService(),
                new LocalDiagnosticLogger(),
                _overlay);

            _tray = new TrayIconService(
                () => Dispatcher.BeginInvoke(OpenSettings),
                () => Dispatcher.BeginInvoke(TogglePause),
                () => Dispatcher.BeginInvoke(ShowAbout),
                () => Dispatcher.BeginInvoke(() => Shutdown(0)));
            _tray.SetPaused(runtimeSettings.IsPaused && !integrationMode);
            _settings.Changed += Settings_Changed;
            ShowHotkeyAvailability();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"OneBoard Inline Translate could not start.\n\n{exception.GetType().Name}",
                "OneBoard Inline Translate",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs eventArgs)
    {
        if (_settings is not null)
        {
            _settings.Changed -= Settings_Changed;
        }

        _coordinator?.Dispose();
        _hotkeys?.Dispose();
        _tray?.Dispose();
        _httpClient?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(eventArgs);
    }

    private void OpenSettings()
    {
        if (_settings is null || _credentials is null || _translation is null || _startup is null)
        {
            return;
        }

        var existing = Windows.OfType<SettingsWindow>().FirstOrDefault();
        if (existing is not null)
        {
            existing.Activate();
            return;
        }

        new SettingsWindow(_settings, _credentials, _translation, _startup).Show();
    }

    private async void TogglePause()
    {
        if (_settings is null)
        {
            return;
        }

        var updated = _settings.Current.Clone();
        updated.IsPaused = !updated.IsPaused;
        await _settings.SaveAsync(updated);
    }

    private void Settings_Changed(object? sender, EventArgs eventArgs)
    {
        if (_settings is null || _hotkeys is null || _tray is null)
        {
            return;
        }

        if (_settings.Current.IsPaused)
        {
            _hotkeys.Pause();
        }
        else
        {
            _hotkeys.Resume(_settings.Current.Hotkeys);
        }

        _tray.SetPaused(_settings.Current.IsPaused);
        ShowHotkeyAvailability();
    }

    private void ShowHotkeyAvailability()
    {
        if (_hotkeys is not null && _tray is not null && _hotkeys.UnavailableHotkeys.Count > 0)
        {
            _tray.ShowNotice(
                "Some hotkeys are unavailable",
                string.Join(Environment.NewLine, _hotkeys.UnavailableHotkeys));
        }
    }

    private static void ShowAbout()
    {
        MessageBox.Show(
            "OneBoard Inline Translate\nVersion 1.0.0\n\nNo translation history. No telemetry. Never auto-sends.\n\nhttps://github.com/phat7000/OneBoardInlineTranslate",
            "About OneBoard Inline Translate",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
