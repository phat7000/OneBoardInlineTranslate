using System.Diagnostics;
using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Services;

internal sealed class SelectedTextCaptureService(
    UiaSelectionReader uiaSelectionReader,
    ClipboardSelectionReader clipboardSelectionReader)
{
    private static readonly TimeSpan UiaTimeout = TimeSpan.FromMilliseconds(450);
    private readonly SemaphoreSlim _uiaGate = new(1, 1);

    internal async Task<CaptureResult> CaptureAsync(
        ForegroundContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (_uiaGate.Wait(0))
            {
                var uiaTask = Task.Run(
                    () =>
                    {
                        try
                        {
                            return uiaSelectionReader.TryRead(context);
                        }
                        finally
                        {
                            _uiaGate.Release();
                        }
                    },
                    CancellationToken.None);
                try
                {
                    var text = await uiaTask.WaitAsync(UiaTimeout, cancellationToken);
                    if (!string.IsNullOrEmpty(text))
                    {
                        return new CaptureResult(
                            true,
                            text,
                            CaptureMethod.UIA,
                            stopwatch.ElapsedMilliseconds,
                            null);
                    }
                }
                catch (TimeoutException)
                {
                    _ = uiaTask.ContinueWith(
                        completed => _ = completed.Exception,
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // UIA is opportunistic; clipboard fallback remains authoritative.
                }
            }

            var clipboardText = await clipboardSelectionReader.ReadAsync(context, cancellationToken);
            return new CaptureResult(
                true,
                clipboardText,
                CaptureMethod.Clipboard,
                stopwatch.ElapsedMilliseconds,
                null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return CaptureResult.Failed(
                stopwatch.ElapsedMilliseconds,
                exception,
                CaptureMethod.Clipboard);
        }
    }
}
