using System.Diagnostics;
using OneBoardInlineTranslate.Diagnostics;
using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Services;

namespace OneBoardInlineTranslate;

internal sealed class HotkeyCoordinator : IDisposable
{
    private readonly GlobalHotkeyService _hotkeys;
    private readonly ForegroundWindowService _foregroundWindows;
    private readonly KeyboardInputService _keyboardInput;
    private readonly SelectedTextCaptureService _captureService;
    private readonly TextReplacementService _replacementService;
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
        LocalDiagnosticLogger logger,
        OverlayWindow overlay)
    {
        _hotkeys = hotkeys;
        _foregroundWindows = foregroundWindows;
        _keyboardInput = keyboardInput;
        _captureService = captureService;
        _replacementService = replacementService;
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
            await _keyboardInput.WaitForHotkeyReleaseAsync(
                eventArgs.TriggerVirtualKey,
                _lifetime.Token);

            if (!ForegroundWindowService.IsStillForeground(context))
            {
                throw new SourceWindowChangedException();
            }

            if (eventArgs.Action == HotkeyAction.Capture)
            {
                await CaptureOnlyAsync(context, _lifetime.Token);
            }
            else
            {
                await CaptureAndReplaceAsync(context, stopwatch, _lifetime.Token);
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception exception)
        {
            var processName = context?.ProcessName ?? "unknown";
            var record = DiagnosticRecord.Create(
                processName,
                CaptureMethod.Unavailable,
                false,
                stopwatch.ElapsedMilliseconds,
                exception);
            await _logger.WriteAsync(record, CancellationToken.None);
            _overlay.ShowFailure(processName, stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private async Task CaptureOnlyAsync(ForegroundContext context, CancellationToken cancellationToken)
    {
        var result = await _captureService.CaptureAsync(context, cancellationToken);
        if (result.Success && !ForegroundWindowService.IsStillForeground(context))
        {
            result = CaptureResult.Failed(
                result.LatencyMilliseconds,
                new SourceWindowChangedException(),
                result.Method);
        }

        await _logger.WriteAsync(
            DiagnosticRecord.Create(
                context.ProcessName,
                result.Method,
                result.Success,
                result.LatencyMilliseconds,
                result.Exception),
            cancellationToken);
        _overlay.ShowCapture(context, result);
    }

    private async Task CaptureAndReplaceAsync(
        ForegroundContext context,
        Stopwatch operationStopwatch,
        CancellationToken cancellationToken)
    {
        var capture = await _captureService.CaptureAsync(context, cancellationToken);
        if (!capture.Success)
        {
            await _logger.WriteAsync(
                DiagnosticRecord.Create(
                    context.ProcessName,
                    capture.Method,
                    false,
                    operationStopwatch.ElapsedMilliseconds,
                    capture.Exception),
                cancellationToken);
            _overlay.ShowCapture(context, capture, "REPLACEMENT FAILED");
            return;
        }

        var replacementText = PhaseZeroTransformer.Transform(capture.Text);
        var replacement = await _replacementService.ReplaceAsync(
            context,
            replacementText,
            cancellationToken);

        var combinedResult = replacement.Success
            ? new CaptureResult(
                true,
                capture.Text,
                capture.Method,
                operationStopwatch.ElapsedMilliseconds,
                null)
            : new CaptureResult(
                false,
                string.Empty,
                capture.Method,
                operationStopwatch.ElapsedMilliseconds,
                replacement.Exception);

        await _logger.WriteAsync(
            DiagnosticRecord.Create(
                context.ProcessName,
                capture.Method,
                replacement.Success,
                operationStopwatch.ElapsedMilliseconds,
                replacement.Exception),
            cancellationToken);
        _overlay.ShowCapture(
            context,
            combinedResult,
            replacement.Success ? "TEST REPLACEMENT COMPLETE" : "REPLACEMENT FAILED");
    }
}
