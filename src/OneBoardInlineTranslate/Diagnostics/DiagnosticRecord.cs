using System.Text.Json.Serialization;
using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Diagnostics;

internal sealed record DiagnosticRecord(
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("process")] string Process,
    [property: JsonPropertyName("captureMethod")] string CaptureMethod,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("latencyMs")] long LatencyMilliseconds,
    [property: JsonPropertyName("exceptionType")] string? ExceptionType)
{
    internal static DiagnosticRecord Create(
        string process,
        CaptureMethod captureMethod,
        bool success,
        long latencyMilliseconds,
        Exception? exception) =>
        new(
            DateTimeOffset.UtcNow,
            process,
            captureMethod.ToString(),
            success,
            latencyMilliseconds,
            exception?.GetType().FullName);
}
