using System.Runtime.InteropServices;
using System.Windows;
using Clipboard = System.Windows.Clipboard;
using DataObject = System.Windows.DataObject;
using TextDataFormat = System.Windows.TextDataFormat;

namespace OneBoardInlineTranslate.Services;

internal interface IClipboardService
{
    Task CopyTextAsync(string text, CancellationToken cancellationToken);
}

internal sealed class ClipboardService : IClipboardService
{
    public async Task CopyTextAsync(string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        Exception? lastException = null;
        for (var attempt = 0; attempt < 40; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var data = new DataObject();
                data.SetText(text, TextDataFormat.UnicodeText);
                data.SetData("ExcludeClipboardContentFromMonitorProcessing", new byte[] { 1 }, autoConvert: false);
                Clipboard.SetDataObject(data, copy: true);
                return;
            }
            catch (ExternalException exception)
            {
                lastException = exception;
            }

            await Task.Delay(15, cancellationToken);
        }

        throw lastException ?? new InvalidOperationException("The clipboard is currently unavailable.");
    }
}
