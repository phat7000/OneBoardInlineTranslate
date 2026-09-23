namespace OneBoardInlineTranslate.Models;

internal sealed record ProviderLanguageCapabilities(
    string ProviderId,
    IReadOnlyList<Language> SourceLanguages,
    IReadOnlyList<Language> TargetLanguages,
    DateTimeOffset FetchedAt)
{
    internal bool SupportsSource(string code) => SourceLanguages.Any(
        language => string.Equals(
            language.Code,
            LanguageCatalog.NormalizeCode(code),
            StringComparison.OrdinalIgnoreCase));

    internal bool SupportsTarget(string code) => TargetLanguages.Any(
        language => string.Equals(
            language.Code,
            LanguageCatalog.NormalizeCode(code),
            StringComparison.OrdinalIgnoreCase));

    internal static ProviderLanguageCapabilities Fallback(string providerId) => new(
        providerId,
        LanguageCatalog.All,
        LanguageCatalog.All,
        DateTimeOffset.UtcNow);
}
