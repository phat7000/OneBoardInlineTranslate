namespace OneBoardInlineTranslate.Models;

internal sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 2;

    public string PreferredLanguage { get; set; } = Language.Vietnamese.Code;

    public bool StartWithWindows { get; set; }

    public bool IsPaused { get; set; }

    public HotkeySettings Hotkeys { get; set; } = new();

    public ProviderConfiguration TranslationProvider { get; set; } = new();

    public string QuickTarget1 { get; set; } = Language.English.Code;

    public string QuickTarget2 { get; set; } = Language.SimplifiedChinese.Code;

    public List<string> RecentLanguages { get; set; } = [];

    public ResultWindowMode ResultWindowMode { get; set; } = ResultWindowMode.Popup;

    public PopupSizePreset PopupSizePreset { get; set; } = PopupSizePreset.Auto;

    public double PopupWidth { get; set; } = 460;

    public double PopupHeight { get; set; } = 360;

    public PopupPositionMode PopupPositionMode { get; set; } = PopupPositionMode.AutoNearSelection;

    public double? PopupCustomLeft { get; set; }

    public double? PopupCustomTop { get; set; }

    public WindowBoundsSettings PinnedBounds { get; set; } = new();

    public Dictionary<string, ProviderLanguageCacheEntry> ProviderLanguageCache { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public LocalTranslationSettings LocalTranslation { get; set; } = new();

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
        },
        QuickTarget1 = QuickTarget1,
        QuickTarget2 = QuickTarget2,
        RecentLanguages = [.. RecentLanguages],
        ResultWindowMode = ResultWindowMode,
        PopupSizePreset = PopupSizePreset,
        PopupWidth = PopupWidth,
        PopupHeight = PopupHeight,
        PopupPositionMode = PopupPositionMode,
        PopupCustomLeft = PopupCustomLeft,
        PopupCustomTop = PopupCustomTop,
        PinnedBounds = PinnedBounds.Clone(),
        ProviderLanguageCache = ProviderLanguageCache.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Clone(),
            StringComparer.OrdinalIgnoreCase),
        LocalTranslation = LocalTranslation.Clone()
    };
}

internal enum ResultWindowMode
{
    Popup,
    Pinned,
    Hidden
}

internal enum PopupSizePreset
{
    Auto,
    Small,
    Medium,
    Large,
    Custom
}

internal enum PopupPositionMode
{
    AutoNearSelection,
    TopRight,
    BottomRight,
    TopLeft,
    BottomLeft,
    Custom
}

internal sealed class WindowBoundsSettings
{
    public double? Left { get; set; }

    public double? Top { get; set; }

    public double Width { get; set; } = 520;

    public double Height { get; set; } = 340;

    public string Monitor { get; set; } = string.Empty;

    internal WindowBoundsSettings Clone() => new()
    {
        Left = Left,
        Top = Top,
        Width = Width,
        Height = Height,
        Monitor = Monitor
    };
}

internal sealed class LocalTranslationSettings
{
    public int MaximumLoadedModels { get; set; } = 2;

    internal LocalTranslationSettings Clone() => new()
    {
        MaximumLoadedModels = MaximumLoadedModels
    };
}

internal sealed class ProviderLanguageCacheEntry
{
    public DateTimeOffset FetchedAt { get; set; }

    public string ConfigurationKey { get; set; } = string.Empty;

    public List<CachedLanguage> SourceLanguages { get; set; } = [];

    public List<CachedLanguage> TargetLanguages { get; set; } = [];

    internal ProviderLanguageCacheEntry Clone() => new()
    {
        FetchedAt = FetchedAt,
        ConfigurationKey = ConfigurationKey,
        SourceLanguages = SourceLanguages.Select(item => item.Clone()).ToList(),
        TargetLanguages = TargetLanguages.Select(item => item.Clone()).ToList()
    };
}

internal sealed class CachedLanguage
{
    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string NativeName { get; set; } = string.Empty;

    internal CachedLanguage Clone() => new()
    {
        Code = Code,
        DisplayName = DisplayName,
        NativeName = NativeName
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
