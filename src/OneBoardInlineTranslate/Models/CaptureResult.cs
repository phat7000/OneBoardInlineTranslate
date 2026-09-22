namespace OneBoardInlineTranslate.Models;

internal sealed record CaptureResult(
    bool Success,
    string Text,
    CaptureMethod Method,
    long LatencyMilliseconds,
    Exception? Exception)
{
    internal static CaptureResult Failed(
        long latencyMilliseconds,
        Exception exception,
        CaptureMethod method = CaptureMethod.Unavailable) =>
        new(false, string.Empty, method, latencyMilliseconds, exception);
}
