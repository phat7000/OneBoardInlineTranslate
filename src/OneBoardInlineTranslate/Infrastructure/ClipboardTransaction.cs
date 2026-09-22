using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OneBoardInlineTranslate.Infrastructure;

internal sealed class ClipboardTransaction
{
    private const string SentinelFormat = "OneBoardInlineTranslate.Phase0.Sentinel";
    private const int ClipboardRetryCount = 40;
    private const int RestoreRetryCount = 200;
    private static readonly TimeSpan ClipboardRetryDelay = TimeSpan.FromMilliseconds(15);

    private DataObject? _originalDataObject;
    private bool _restoreAttempted;

    private ClipboardTransaction(DataObject? originalDataObject)
    {
        _originalDataObject = originalDataObject;
    }

    internal static async Task<TResult> RunAsync<TResult>(
        Func<ClipboardTransaction, Task<TResult>> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        EnsureStaThread();

        // Materialize every advertised format before mutation. If any format cannot be copied,
        // creation throws while the user's clipboard is still untouched.
        var transaction = await CreateAsync(cancellationToken);
        TResult? result = default;
        ExceptionDispatchInfo? actionFailure = null;

        try
        {
            result = await action(transaction);
        }
        catch (Exception exception)
        {
            actionFailure = ExceptionDispatchInfo.Capture(exception);
        }

        var restoreFailure = await transaction.RestoreAsync();
        if (restoreFailure is not null)
        {
            throw new ClipboardRestoreException(restoreFailure);
        }

        actionFailure?.Throw();
        return result!;
    }

    internal async Task<uint> PutSentinelAsync(CancellationToken cancellationToken)
    {
        var data = new DataObject();
        data.SetData(SentinelFormat, Guid.NewGuid().ToString("N"), autoConvert: false);
        await PutDataObjectAsync(data, cancellationToken);
        return NativeMethods.GetClipboardSequenceNumber();
    }

    internal Task PutUnicodeTextAsync(string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        var data = new DataObject();
        data.SetText(text, TextDataFormat.UnicodeText);
        return PutDataObjectAsync(data, cancellationToken);
    }

    internal Task PutDataObjectAsync(DataObject data, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);
        ExcludeTemporaryDataFromClipboardMonitoring(data);
        return SetDataObjectAsync(data, copy: false, ClipboardRetryCount, cancellationToken);
    }

    internal static bool TryReadUnicodeText(out string text)
    {
        EnsureStaThread();
        try
        {
            if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                text = Clipboard.GetText(TextDataFormat.UnicodeText);
                return true;
            }
        }
        catch (ExternalException)
        {
            // The caller retries while another clipboard owner finishes rendering.
        }

        text = string.Empty;
        return false;
    }

    private static async Task<ClipboardTransaction> CreateAsync(CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        for (var attempt = 0; attempt < ClipboardRetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureStaThread();
            try
            {
                return new ClipboardTransaction(CloneDataObject(Clipboard.GetDataObject()));
            }
            catch (ExternalException exception)
            {
                lastException = exception;
            }

            if (attempt + 1 < ClipboardRetryCount)
            {
                await Task.Delay(ClipboardRetryDelay, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("The clipboard snapshot could not be created.");
    }

    private static async Task SetDataObjectAsync(
        DataObject data,
        bool copy,
        int retryCount,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        for (var attempt = 0; attempt < retryCount; attempt++)
        {
            // Restoration supplies CancellationToken.None so cancellation can never skip cleanup.
            cancellationToken.ThrowIfCancellationRequested();
            EnsureStaThread();
            try
            {
                Clipboard.SetDataObject(data, copy);
                return;
            }
            catch (ExternalException exception)
            {
                lastException = exception;
            }

            if (attempt + 1 < retryCount)
            {
                await Task.Delay(ClipboardRetryDelay, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Clipboard data could not be set.");
    }

    private async Task<Exception?> RestoreAsync()
    {
        if (_restoreAttempted)
        {
            return null;
        }

        _restoreAttempted = true;
        try
        {
            if (_originalDataObject is null)
            {
                await ClearClipboardAsync();
            }
            else
            {
                // copy:true eagerly persists our snapshot, so it cannot depend on the original
                // external owner's delayed-rendering lifetime and survives this process exiting.
                await SetDataObjectAsync(
                    _originalDataObject,
                    copy: true,
                    RestoreRetryCount,
                    CancellationToken.None);
            }

            _originalDataObject = null;
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static async Task ClearClipboardAsync()
    {
        Exception? lastException = null;
        for (var attempt = 0; attempt < RestoreRetryCount; attempt++)
        {
            EnsureStaThread();
            try
            {
                Clipboard.Clear();
                return;
            }
            catch (ExternalException exception)
            {
                lastException = exception;
            }

            if (attempt + 1 < RestoreRetryCount)
            {
                await Task.Delay(ClipboardRetryDelay, CancellationToken.None);
            }
        }

        throw lastException ?? new InvalidOperationException("The empty clipboard could not be restored.");
    }

    private static DataObject? CloneDataObject(IDataObject? source)
    {
        if (source is null)
        {
            return null;
        }

        var snapshot = new DataObject();
        var formats = source.GetFormats(autoConvert: false)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (formats.Length == 0)
        {
            return null;
        }

        foreach (var format in formats)
        {
            var value = source.GetData(format, autoConvert: false)
                ?? throw new NotSupportedException(
                    $"Clipboard format '{format}' could not be materialized safely.");
            snapshot.SetData(format, CloneClipboardValue(value, format), autoConvert: false);
        }

        return snapshot;
    }

    private static object CloneClipboardValue(object value, string format)
    {
        switch (value)
        {
            case string text:
                return text;
            case string[] strings:
                return strings.ToArray();
            case byte[] bytes:
                return bytes.ToArray();
            case char[] characters:
                return characters.ToArray();
            case MemoryStream memoryStream:
                return new MemoryStream(memoryStream.ToArray(), writable: false);
            case Stream stream:
                return CloneStream(stream);
            case BitmapSource bitmapSource:
                {
                    var bitmapClone = bitmapSource.Clone();
                    bitmapClone.Freeze();
                    return bitmapClone;
                }
            case Uri uri:
                return uri;
            case ICloneable cloneable:
                return cloneable.Clone()
                    ?? throw new NotSupportedException(
                        $"Clipboard format '{format}' returned an empty clone.");
            default:
                {
                    var valueType = value.GetType();
                    if (valueType.IsValueType)
                    {
                        return value;
                    }

                    throw new NotSupportedException(
                        $"Clipboard format '{format}' uses unsupported data type '{valueType.FullName}'.");
                }
        }
    }

    private static MemoryStream CloneStream(Stream source)
    {
        var originalPosition = source.CanSeek ? source.Position : 0;
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        try
        {
            var copy = new MemoryStream();
            source.CopyTo(copy);
            copy.Position = 0;
            return copy;
        }
        finally
        {
            if (source.CanSeek)
            {
                source.Position = originalPosition;
            }
        }
    }

    private static void ExcludeTemporaryDataFromClipboardMonitoring(DataObject data)
    {
        // Windows treats the presence of this registered format, regardless of its data, as an
        // instruction to exclude every format in this item from history and cloud synchronization.
        data.SetData(
            "ExcludeClipboardContentFromMonitorProcessing",
            new byte[] { 1 },
            autoConvert: false);
    }

    private static void EnsureStaThread()
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new InvalidOperationException("Clipboard operations must run on the WPF STA thread.");
        }
    }
}
