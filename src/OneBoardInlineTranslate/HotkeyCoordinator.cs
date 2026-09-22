using System.Diagnostics;
using System.Net.Http;
using OneBoardInlineTranslate.Diagnostics;
using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.OCR;
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
                await OcrTranslateAsync(context, _lifetime.Token);
            }
            else
            {
                await HandleSelectionAsync(eventArgs.Operation, context, stopwatch, _lifetime.Token);
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception exception)
        {
            await LogAsync(context, CaptureMethod.Unavailable, false, stopwatch.ElapsedMilliseconds, exception);
            _overlay.ShowMessage(context, "Translation unavailable", ToSafeMessage(exception), isError: true);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private async Task HandleSelectionAsync(
        OperationType operation,
        ForegroundContext context,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        var capture = await _captureService.CaptureAsync(context, cancellationToken);
        if (!capture.Success)
        {
            await LogAsync(context, capture.Method, false, stopwatch.ElapsedMilliseconds, capture.Exception);
            _overlay.ShowMessage(context, "No selection", "Select non-empty text and try again.", isError: true);
            return;
        }

        var target = operation switch
        {
            OperationType.TranslateToEnglish => Language.English,
            OperationType.TranslateToChinese => Language.SimplifiedChinese,
            _ => Language.FromCode(_settings.Current.PreferredLanguage)
        };
        var translation = await _translation.TranslateAsync(new TranslationRequest
        {
            Text = capture.Text,
            TargetLanguage = target
        }, cancellationToken);

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

        await LogAsync(context, capture.Method, true, stopwatch.ElapsedMilliseconds, null);
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
            _overlay.ShowMessage(context, "Translation inserted", "Selected text was replaced. Nothing was sent.");
        }
        else
        {
            _overlay.ShowTranslation(context, capture.Text, translation);
        }
    }

    private async Task OcrTranslateAsync(ForegroundContext context, CancellationToken cancellationToken)
    {
        var region = await _regionSelection.SelectAsync(cancellationToken);
        if (region is null)
        {
            return;
        }

        await Task.Delay(90, cancellationToken);
        var bitmap = _screenCapture.Capture(region.Value);
        var text = await _ocr.RecognizeAsync(bitmap, cancellationToken);
        var target = Language.FromCode(_settings.Current.PreferredLanguage);
        var result = await _translation.TranslateAsync(new TranslationRequest
        {
            Text = text,
            TargetLanguage = target
        }, cancellationToken);
        _overlay.ShowTranslation(context, text, result);
    }

    private Task LogAsync(
        ForegroundContext? context,
        CaptureMethod method,
        bool success,
        long latency,
        Exception? exception) =>
        _logger.WriteAsync(
            DiagnosticRecord.Create(
                context?.ProcessName ?? "unknown",
                method,
                success,
                latency,
                exception),
            CancellationToken.None);

    private static string ToSafeMessage(Exception exception) => exception switch
    {
        SourceWindowChangedException => "The source window changed, so OneBoard stopped safely.",
        SelectionUnavailableException => "Select non-empty text and try again.",
        TimeoutException => "The source application did not respond before the safety timeout.",
        HttpRequestException => "The translation provider could not be reached. Check provider settings and your network.",
        InvalidOperationException => "Check translation provider or OCR settings and try again.",
        _ => $"The operation stopped safely ({exception.GetType().Name})."
    };
}
