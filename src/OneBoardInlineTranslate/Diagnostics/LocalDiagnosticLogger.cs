using System.Text;
using System.Text.Json;
using System.IO;

namespace OneBoardInlineTranslate.Diagnostics;

internal sealed class LocalDiagnosticLogger
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly string _logDirectory;

    internal LocalDiagnosticLogger(string? localApplicationData = null)
    {
        var baseDirectory = localApplicationData ??
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _logDirectory = Path.Combine(baseDirectory, "OneBoardInlineTranslate", "logs");
    }

    internal string LogDirectory => _logDirectory;

    internal async Task<bool> WriteAsync(DiagnosticRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(_logDirectory);
            var path = Path.Combine(_logDirectory, $"phase0-{record.Timestamp:yyyyMMdd}.jsonl");
            var line = Serialize(record) + Environment.NewLine;
            await File.AppendAllTextAsync(path, line, Utf8WithoutBom, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return false;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    internal static string Serialize(DiagnosticRecord record) => JsonSerializer.Serialize(record);
}
