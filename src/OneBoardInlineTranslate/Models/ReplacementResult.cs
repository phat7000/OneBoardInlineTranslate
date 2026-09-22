namespace OneBoardInlineTranslate.Models;

internal sealed record ReplacementResult(
    bool Success,
    long LatencyMilliseconds,
    Exception? Exception);
