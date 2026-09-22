namespace OneBoardInlineTranslate.Models;

internal sealed record ForegroundContext(
    nint WindowHandle,
    uint ProcessId,
    uint ThreadId,
    string ProcessName);
