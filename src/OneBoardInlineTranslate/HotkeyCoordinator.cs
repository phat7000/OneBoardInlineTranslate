using System.Diagnostics;
using System.Net.Http;
using OneBoardInlineTranslate.Diagnostics;
using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.OCR;
using OneBoardInlineTranslate.Providers;
using OneBoardInlineTranslate.Services;

namespace OneBoardInlineTranslate;

internal sealed class HotkeyCoordinator : IDisposable
{
    private readonly GlobalHotkeyService _hotkeys;
    private readonly ForegroundWindowService _foregroundWindows;
    private readonly KeyboardInputService _keyboardInput;
    private readonly SelectedTextCaptureService _captureService;
    private readonly TextReplacementService _replacementService;
    private readonly ITranslationService _translation;
    private readonly ISettingsService _settings;
    private readonly ReplyService _reply;
    private readonly IRegionSelectionService _regionSelection;
    private readonly IScreenCaptureService _screenCapture;
    private readonly IOcrService _ocr;
    private readonly LocalDiagnosticLogger _logger;
    private readonly OverlayWindow _overlay;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();

    internal HotkeyCoordinator(
        GlobalHotkeyService hotkeys,
        ForegroundWindowService foregroundWindows,
        KeyboardInputService keyboardInput,
        SelectedTextCaptureService captureService,
        TextReplacementService replacementService,
        ITranslationService translation,
        ISettingsService settings,
        ReplyService reply,
        IRegionSelectionService regionSelection,
        IScreenCaptureService screenCapture,
        IOcrService ocr,
        LocalDiagnosticLogger logger,
        OverlayWindow overlay)
    {
        _hotkeys = hotkeys;
        _foregroundWindows = foregroundWindows;
        _keyboardInput = keyboardInput;
        _captureService = captureService;
        _replacementService = replacementService;
        _translation = translation;
        _settings = settings;
        _reply = reply;
        _regionSelection = regionSelection;
        _screenCapture = screenCapture;
        _ocr = ocr;
        _logger = logger;
        _overlay = overlay;
        _hotkeys.Pressed += OnHotkeyPressed;
    }

    public void Dispose()
    {
        _hotkeys.Pressed -= OnHotkeyPressed;
        _lifetime.Cancel();
        _lifetime.Dispose();
        _operationGate.Dispose();
    }

    private async void OnHotkeyPressed(object? sender, HotkeyPressedEventArgs eventArgs)
    {
        if (!await _operationGate.WaitAsync(0))
        {
            return;
        }

        ForegroundContext? context = null;
        var stopwatch = Stopwatch.StartNew();
        var timing = new OperationTiming(
            eventArgs.Operation,
            _settings.Current.TranslationProvider.Provider);
        var shouldLog = true;
        try
        {
            context = _foregroundWindows.GetCurrent();
            await _keyboardInput.WaitForHotkeyReleaseAsync(eventArgs.TriggerVirtualKey, _lifetime.Token);
            if (!ForegroundWindowService.IsStillForeground(context))
            {
                throw new SourceWindowChangedException();
            }

            if (eventArgs.Operation == OperationType.OcrTranslate)
            {
                await OcrTranslateAsync(context, timing, _lifetime.Token);
            }
            else
            {
                await HandleSelectionAsync(eventArgs.Operation, context, timing, _lifetime.Token);
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            // Normal application shutdown.
            shouldLog = false;
        }
        catch (Exception exception)
        {
            timing.Exception = exception;
            var failureOutputStopwatch = Stopwatch.StartNew();
            try
            {
                _overlay.ShowMessage(context, "Translation unavailable", ToSafeMessage(exception), isError: true);
            }
            finally
            {
                timing.OutputLatencyMilliseconds += failureOutputStopwatch.ElapsedMilliseconds;
            }
        }
        finally
        {
            if (shouldLog)
            {
                timing.TotalLatencyMilliseconds = stopwatch.ElapsedMilliseconds;
                await LogAsync(context, timing);
            }

            _operationGate.Release();
        }
    }

    private async Task HandleSelectionAsync(
        OperationType operation,
        ForegroundContext context,
        OperationTiming timing,
        CancellationToken cancellationToken)
    {
        var capture = await _captureService.CaptureAsync(context, cancellationToken);
        timing.CaptureMethod = capture.Method;
        timing.CaptureLatencyMilliseconds = capture.LatencyMilliseconds;
        if (!capture.Success)
        {
            timing.Exception = capture.Exception;
            var failureOutputStopwatch = Stopwatch.StartNew();
            try
            {
                _overlay.ShowMessage(context, "No selection", "Select non-empty text and try again.", isError: true);
            }
            finally
            {
                timing.OutputLatencyMilliseconds = failureOutputStopwatch.ElapsedMilliseconds;
            }

            return;
        }

        var target = operation switch
        {
            OperationType.TranslateToEnglish => LanguageCatalog.Resolve(_settings.Current.QuickTarget1),
            OperationType.TranslateToChinese => LanguageCatalog.Resolve(_settings.Current.QuickTarget2),
            _ => LanguageCatalog.Resolve(_settings.Current.PreferredLanguage)
        };
        TranslationResult translation;
        var providerStopwatch = Stopwatch.StartNew();
        try
        {
            translation = await _translation.TranslateAsync(new TranslationRequest
            {
                Text = capture.Text,
                TargetLanguage = target
            }, cancellationToken);
            timing.Provider = translation.ProviderId;
            timing.ProviderLatencyMilliseconds = (long)translation.Latency.TotalMilliseconds;
        }
        finally
        {
            if (timing.ProviderLatencyMilliseconds == 0)
            {
                timing.ProviderLatencyMilliseconds = providerStopwatch.ElapsedMilliseconds;
            }
        }

        var outputStopwatch = Stopwatch.StartNew();
        try
        {
            switch (operation)
            {
                case OperationType.Understand:
                    _overlay.ShowTranslation(context, capture.Text, translation);
                    break;
                case OperationType.TranslateToEnglish:
                case OperationType.TranslateToChinese:
                    await ReplaceOrShowAsync(context, capture, translation, cancellationToken);
                    break;
                case OperationType.Reply:
                    _reply.Open(context, capture.Text, translation, target);
                    break;
                default:
                    throw new InvalidOperationException("The requested operation is unsupported.");
            }
        }
        finally
        {
            timing.OutputLatencyMilliseconds = outputStopwatch.ElapsedMilliseconds;
        }

        timing.Success = true;
    }

    private async Task ReplaceOrShowAsync(
        ForegroundContext context,
        CaptureResult capture,
        TranslationResult translation,
        CancellationToken cancellationToken)
    {
        if (!ForegroundWindowService.IsStillForeground(context))
        {
            _overlay.ShowTranslation(context, capture.Text, translation);
            return;
        }

        var replacement = await _replacementService.ReplaceAsync(
            context,
            translation.Text,
            cancellationToken);
        if (replacement.Success)
        {
            if (ResultWindowPolicy.ShowQuickReplacementConfirmation(_settings.Current.ResultWindowMode))
            {
                _overlay.ShowMessage(context, "Translation inserted", "Selected text was replaced. Nothing was sent.");
            }
        }
        else
        {
            _overlay.ShowTranslation(context, capture.Text, translation);
        }
    }

    private async Task OcrTranslateAsync(
        ForegroundContext context,
        OperationTiming timing,
        CancellationToken cancellationToken)
    {
        var captureStopwatch = Stopwatch.StartNew();
        var region = await _regionSelection.SelectAsync(cancellationToken);
        if (region is null)
        {
            timing.CaptureMethod = CaptureMethod.OCR;
            timing.CaptureLatencyMilliseconds = captureStopwatch.ElapsedMilliseconds;
            return;
        }

        await Task.Delay(90, cancellationToken);
        var bitmap = _screenCapture.Capture(region.Value);
        var text = await _ocr.RecognizeAsync(bitmap, cancellationToken);
        timing.CaptureMethod = CaptureMethod.OCR;
        timing.CaptureLatencyMilliseconds = captureStopwatch.ElapsedMilliseconds;
        var target = LanguageCatalog.Resolve(_settings.Current.PreferredLanguage);
        TranslationResult result;
        var providerStopwatch = Stopwatch.StartNew();
        try
        {
            result = await _translation.TranslateAsync(new TranslationRequest
            {
                Text = text,
                TargetLanguage = target
            }, cancellationToken);
            timing.Provider = result.ProviderId;
            timing.ProviderLatencyMilliseconds = (long)result.Latency.TotalMilliseconds;
        }
        finally
        {
            if (timing.ProviderLatencyMilliseconds == 0)
            {
                timing.ProviderLatencyMilliseconds = providerStopwatch.ElapsedMilliseconds;
            }
        }

        var outputStopwatch = Stopwatch.StartNew();
        try
        {
            _overlay.ShowTranslation(context, text, result);
        }
        finally
        {
            timing.OutputLatencyMilliseconds = outputStopwatch.ElapsedMilliseconds;
        }

        timing.Success = true;
    }

    private Task LogAsync(ForegroundContext? context, OperationTiming timing) =>
        _logger.WriteAsync(
            DiagnosticRecord.Create(
                timing.Operation,
                timing.Provider,
                context?.ProcessName ?? "unknown",
                timing.CaptureMethod,
                timing.CaptureLatencyMilliseconds,
                timing.ProviderLatencyMilliseconds,
                timing.OutputLatencyMilliseconds,
                timing.TotalLatencyMilliseconds,
                timing.Success,
                timing.Exception),
            CancellationToken.None);

    private static string ToSafeMessage(Exception exception) => exception switch
    {
        SourceWindowChangedException => "The source window changed, so OneBoard stopped safely.",
        SelectionUnavailableException => "Select non-empty text and try again.",
        TimeoutException => "The source application did not respond before the safety timeout.",
        ProviderException providerException => providerException.Message,
        HttpRequestException => "The translation provider could not be reached. Check provider settings and your network.",
        InvalidOperationException => "Check translation provider or OCR settings and try again.",
        _ => $"The operation stopped safely ({exception.GetType().Name})."
    };

    private sealed class OperationTiming(OperationType operation, string provider)
    {
        internal OperationType Operation { get; } = operation;

        internal string Provider { get; set; } = provider;

        internal CaptureMethod CaptureMethod { get; set; } = CaptureMethod.Unavailable;

        internal long CaptureLatencyMilliseconds { get; set; }

        internal long ProviderLatencyMilliseconds { get; set; }

        internal long OutputLatencyMilliseconds { get; set; }

        internal long TotalLatencyMilliseconds { get; set; }

        internal bool Success { get; set; }

        internal Exception? Exception { get; set; }
    }
}
