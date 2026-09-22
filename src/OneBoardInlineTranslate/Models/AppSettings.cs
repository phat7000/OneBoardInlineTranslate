namespace OneBoardInlineTranslate.Models;

internal sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;

    public string PreferredLanguage { get; set; } = Language.Vietnamese.Code;

    public bool StartWithWindows { get; set; }

    public bool IsPaused { get; set; }

    public HotkeySettings Hotkeys { get; set; } = new();

    public ProviderConfiguration TranslationProvider { get; set; } = new();

    internal AppSettings Clone() => new()
    {
        SchemaVersion = SchemaVersion,
        PreferredLanguage = PreferredLanguage,
        StartWithWindows = StartWithWindows,
        IsPaused = IsPaused,
        Hotkeys = Hotkeys.Clone(),
        TranslationProvider = new ProviderConfiguration
        {
            Provider = TranslationProvider.Provider,
            Endpoint = TranslationProvider.Endpoint,
            Region = TranslationProvider.Region
        }
    };
}

internal sealed class HotkeySettings
{
    public string Understand { get; set; } = "Alt+Q";

    public string TranslateToEnglish { get; set; } = "Alt+E";

    public string TranslateToChinese { get; set; } = "Alt+C";

    public string Reply { get; set; } = "Alt+R";

    public string OcrTranslate { get; set; } = "Alt+Shift+Q";

    internal HotkeySettings Clone() => new()
    {
        Understand = Understand,
        TranslateToEnglish = TranslateToEnglish,
        TranslateToChinese = TranslateToChinese,
        Reply = Reply,
        OcrTranslate = OcrTranslate
    };

    internal IEnumerable<(OperationType Operation, string Gesture)> Enumerate()
    {
        yield return (OperationType.Understand, Understand);
        yield return (OperationType.TranslateToEnglish, TranslateToEnglish);
        yield return (OperationType.TranslateToChinese, TranslateToChinese);
        yield return (OperationType.Reply, Reply);
        yield return (OperationType.OcrTranslate, OcrTranslate);
    }
}
