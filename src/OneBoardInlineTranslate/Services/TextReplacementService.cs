using System.Diagnostics;
using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Services;

internal sealed class TextReplacementService(KeyboardInputService keyboardInput)
{
    // SendInput queues keystrokes. Keep temporary text available until Chromium/Electron/Office
    // consumers have synchronously handled the paste message, then restore the old clipboard.
    private static readonly TimeSpan PasteSettleDelay = TimeSpan.FromMilliseconds(350);

    internal async Task<ReplacementResult> ReplaceAsync(
        ForegroundContext context,
        string replacementText,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(replacementText);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await ClipboardTransaction.RunAsync(
                async transaction =>
                {
                    if (!ForegroundWindowService.IsStillForeground(context))
                    {
                        throw new SourceWindowChangedException();
                    }

                    await transaction.PutUnicodeTextAsync(replacementText, cancellationToken);

                    // This is the final check before Ctrl+V. We deliberately never synthesize Enter.
                    if (!ForegroundWindowService.IsStillForeground(context))
                    {
                        throw new SourceWindowChangedException();
                    }

                    keyboardInput.SendPaste();
                    await Task.Delay(PasteSettleDelay, cancellationToken);
                    return true;
                },
                cancellationToken);

            if (!ForegroundWindowService.IsStillForeground(context))
            {
                throw new SourceWindowChangedException();
            }

            return new ReplacementResult(true, stopwatch.ElapsedMilliseconds, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new ReplacementResult(false, stopwatch.ElapsedMilliseconds, exception);
        }
    }
}
