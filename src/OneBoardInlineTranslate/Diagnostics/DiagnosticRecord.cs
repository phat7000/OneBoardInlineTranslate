using System.Text.Json.Serialization;
using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Diagnostics;

internal sealed record DiagnosticRecord(
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("operation")] string Operation,
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("process")] string Process,
    [property: JsonPropertyName("captureMethod")] string CaptureMethod,
    [property: JsonPropertyName("captureLatencyMs")] long CaptureLatencyMilliseconds,
    [property: JsonPropertyName("providerLatencyMs")] long ProviderLatencyMilliseconds,
    [property: JsonPropertyName("outputLatencyMs")] long OutputLatencyMilliseconds,
    [property: JsonPropertyName("totalLatencyMs")] long TotalLatencyMilliseconds,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("exceptionType")] string? ExceptionType)
{
    internal static DiagnosticRecord Create(
        OperationType operation,
        string provider,
        string process,
        CaptureMethod captureMethod,
        long captureLatencyMilliseconds,
        long providerLatencyMilliseconds,
        long outputLatencyMilliseconds,
        long totalLatencyMilliseconds,
        bool success,
        Exception? exception) =>
        new(
            DateTimeOffset.UtcNow,
            operation.ToString(),
            provider,
            process,
            captureMethod.ToString(),
            captureLatencyMilliseconds,
            providerLatencyMilliseconds,
            outputLatencyMilliseconds,
            totalLatencyMilliseconds,
            success,
            exception?.GetType().FullName);
}
