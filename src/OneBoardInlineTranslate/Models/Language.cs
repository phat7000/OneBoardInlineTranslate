namespace OneBoardInlineTranslate.Models;

internal sealed record Language(string Code, string DisplayName)
{
    internal static Language Vietnamese { get; } = new("vi", "Vietnamese");
    internal static Language English { get; } = new("en", "English");
    internal static Language SimplifiedChinese { get; } = new("zh-Hans", "Simplified Chinese");

    internal static IReadOnlyList<Language> Supported { get; } =
        [Vietnamese, English, SimplifiedChinese];

    internal static Language FromCode(string? code) => Supported.FirstOrDefault(
        language => string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase))
        ?? Vietnamese;
}
