using System.Windows;
using OneBoardInlineTranslate.Diagnostics;
using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Services;

namespace OneBoardInlineTranslate;

public partial class App : Application
{
    private GlobalHotkeyService? _hotkeys;
    private HotkeyCoordinator? _coordinator;

    protected override void OnStartup(StartupEventArgs eventArgs)
    {
        base.OnStartup(eventArgs);

        try
        {
            var overlay = new OverlayWindow();
            MainWindow = overlay;
            var windowHandle = overlay.EnsureWindowHandle();

            var keyboard = new KeyboardInputService();
            var foregroundWindows = new ForegroundWindowService();
            var captureService = new SelectedTextCaptureService(
                new UiaSelectionReader(),
                new ClipboardSelectionReader(keyboard));
            var replacementService = new TextReplacementService(keyboard);
            var logger = new LocalDiagnosticLogger();

            _hotkeys = new GlobalHotkeyService(windowHandle);
            _coordinator = new HotkeyCoordinator(
                _hotkeys,
                foregroundWindows,
                keyboard,
                captureService,
                replacementService,
                logger,
                overlay);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"OneBoard Inline Translate could not start.\n\n{exception.GetType().Name}: {exception.Message}",
                "OneBoard Inline Translate — Phase 0",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs eventArgs)
    {
        _coordinator?.Dispose();
        _hotkeys?.Dispose();
        base.OnExit(eventArgs);
    }
}
