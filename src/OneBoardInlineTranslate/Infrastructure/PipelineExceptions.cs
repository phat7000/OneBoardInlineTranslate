namespace OneBoardInlineTranslate.Infrastructure;

internal sealed class SelectionUnavailableException : Exception
{
    internal SelectionUnavailableException()
        : base("No non-empty selected text was available.")
    {
    }
}

internal sealed class ClipboardCaptureTimeoutException : TimeoutException
{
    internal ClipboardCaptureTimeoutException()
        : base("The source application did not update the clipboard before the safety timeout.")
    {
    }
}

internal sealed class ClipboardRestoreException : Exception
{
    internal ClipboardRestoreException(Exception innerException)
        : base("The original clipboard could not be restored.", innerException)
    {
    }
}

internal sealed class SourceWindowChangedException : Exception
{
    internal SourceWindowChangedException()
        : base("The foreground window changed while the operation was in progress.")
    {
    }
}
