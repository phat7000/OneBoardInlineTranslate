using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Services;

internal sealed class ClipboardSelectionReader(KeyboardInputService keyboardInput)
{
    private static readonly TimeSpan CopyTimeout = TimeSpan.FromMilliseconds(1_500);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(15);

    internal Task<string> ReadAsync(ForegroundContext context, CancellationToken cancellationToken) =>
        ClipboardTransaction.RunAsync(
            transaction => ReadInsideTransactionAsync(transaction, context, cancellationToken),
            cancellationToken);

    private async Task<string> ReadInsideTransactionAsync(
        ClipboardTransaction transaction,
        ForegroundContext context,
        CancellationToken cancellationToken)
    {
        if (!ForegroundWindowService.IsStillForeground(context))
        {
            throw new SourceWindowChangedException();
        }

        var baselineSequence = await transaction.PutSentinelAsync(cancellationToken);

        // Check again immediately before input injection so a late window switch cannot receive Ctrl+C.
        if (!ForegroundWindowService.IsStillForeground(context))
        {
            throw new SourceWindowChangedException();
        }

        keyboardInput.SendCopy();
        var deadline = DateTime.UtcNow + CopyTimeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (NativeMethods.GetClipboardSequenceNumber() != baselineSequence)
            {
                while (DateTime.UtcNow < deadline)
                {
                    if (ClipboardTransaction.TryReadUnicodeText(out var text))
                    {
                        if (text.Length == 0)
                        {
                            throw new SelectionUnavailableException();
                        }

                        return text;
                    }

                    await Task.Delay(PollInterval, cancellationToken);
                }

                throw new SelectionUnavailableException();
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        throw new ClipboardCaptureTimeoutException();
    }
}
