using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Providers;

internal static class ProviderLanguageCodes
{
    internal static string ToGoogle(Language language) => language.Code switch
    {
        "zh-Hans" => "zh-CN",
        "zh-Hant" => "zh-TW",
        "fil" => "tl",
        _ => language.Code
    };

    internal static string ToLangbly(Language language) => language.Code switch
    {
        "zh-Hans" => "zh",
        "zh-Hant" => "zh-TW",
        "fil" => "tl",
        _ => language.Code
    };

    internal static string ToTranslatePlus(Language language) => language.Code switch
    {
        "zh-Hans" => "zh-CN",
        "zh-Hant" => "zh-TW",
        "fil" => "tl",
        _ => language.Code
    };

    internal static string ToLibreTranslate(Language language) => language.Code switch
    {
        "zh-Hans" => "zh",
        "zh-Hant" => "zt",
        "fil" => "tl",
        _ => language.Code
    };

    internal static string ToDeepL(Language language) => language.Code switch
    {
        "zh-Hans" => "ZH-HANS",
        "zh-Hant" => "ZH-HANT",
        "en-GB" => "EN-GB",
        "en-US" => "EN-US",
        "pt-BR" => "PT-BR",
        "pt-PT" => "PT-PT",
        _ => language.Code.ToUpperInvariant()
    };
}
