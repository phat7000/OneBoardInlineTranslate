namespace OneBoardInlineTranslate;

internal static class PhaseZeroTransformer
{
    private const string TestPrefix = "[TEST] ";

    internal static string Transform(string selectedText)
    {
        ArgumentNullException.ThrowIfNull(selectedText);
        return TestPrefix + selectedText;
    }
}
