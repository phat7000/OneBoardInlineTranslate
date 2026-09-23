namespace OneBoardInlineTranslate.Models;

internal sealed record Language(
    string Code,
    string DisplayName,
    string NativeName = "",
    bool IsRightToLeft = false)
{
    public string SearchText => $"{DisplayName} {NativeName} {Code}";

    public string PickerLabel => $"{DisplayName}  ·  {Code}";

    internal static Language Vietnamese => LanguageCatalog.Get("vi");

    internal static Language English => LanguageCatalog.Get("en");

    internal static Language SimplifiedChinese => LanguageCatalog.Get("zh-Hans");

    internal static IReadOnlyList<Language> Supported => LanguageCatalog.All;

    internal static Language FromCode(string? code) =>
        LanguageCatalog.Find(code) ?? Vietnamese;
}

internal static class LanguageCatalog
{
    private static readonly IReadOnlyList<Language> BuiltIn =
    [
        new("vi", "Vietnamese", "Tiếng Việt"),
        new("en", "English", "English"),
        new("zh-Hans", "Chinese Simplified", "简体中文"),
        new("zh-Hant", "Chinese Traditional", "繁體中文"),
        new("ja", "Japanese", "日本語"),
        new("ko", "Korean", "한국어"),
        new("th", "Thai", "ไทย"),
        new("fr", "French", "Français"),
        new("de", "German", "Deutsch"),
        new("es", "Spanish", "Español"),
        new("af", "Afrikaans", "Afrikaans"),
        new("sq", "Albanian", "Shqip"),
        new("am", "Amharic", "አማርኛ"),
        new("ar", "Arabic", "العربية", true),
        new("hy", "Armenian", "Հայերեն"),
        new("az", "Azerbaijani", "Azərbaycanca"),
        new("eu", "Basque", "Euskara"),
        new("be", "Belarusian", "Беларуская"),
        new("bn", "Bengali", "বাংলা"),
        new("bs", "Bosnian", "Bosanski"),
        new("bg", "Bulgarian", "Български"),
        new("ca", "Catalan", "Català"),
        new("ceb", "Cebuano", "Cebuano"),
        new("hr", "Croatian", "Hrvatski"),
        new("cs", "Czech", "Čeština"),
        new("da", "Danish", "Dansk"),
        new("nl", "Dutch", "Nederlands"),
        new("en-GB", "English (United Kingdom)", "English"),
        new("en-US", "English (United States)", "English"),
        new("eo", "Esperanto", "Esperanto"),
        new("et", "Estonian", "Eesti"),
        new("fi", "Finnish", "Suomi"),
        new("fil", "Filipino", "Filipino"),
        new("gl", "Galician", "Galego"),
        new("ka", "Georgian", "ქართული"),
        new("el", "Greek", "Ελληνικά"),
        new("gu", "Gujarati", "ગુજરાતી"),
        new("ht", "Haitian Creole", "Kreyòl ayisyen"),
        new("ha", "Hausa", "Hausa"),
        new("he", "Hebrew", "עברית", true),
        new("hi", "Hindi", "हिन्दी"),
        new("hu", "Hungarian", "Magyar"),
        new("is", "Icelandic", "Íslenska"),
        new("id", "Indonesian", "Bahasa Indonesia"),
        new("ga", "Irish", "Gaeilge"),
        new("it", "Italian", "Italiano"),
        new("jv", "Javanese", "Basa Jawa"),
        new("kn", "Kannada", "ಕನ್ನಡ"),
        new("kk", "Kazakh", "Қазақша"),
        new("km", "Khmer", "ខ្មែរ"),
        new("lo", "Lao", "ລາວ"),
        new("lv", "Latvian", "Latviešu"),
        new("lt", "Lithuanian", "Lietuvių"),
        new("mk", "Macedonian", "Македонски"),
        new("ms", "Malay", "Bahasa Melayu"),
        new("ml", "Malayalam", "മലയാളം"),
        new("mt", "Maltese", "Malti"),
        new("mr", "Marathi", "मराठी"),
        new("mn", "Mongolian", "Монгол"),
        new("my", "Myanmar (Burmese)", "မြန်မာ"),
        new("ne", "Nepali", "नेपाली"),
        new("nb", "Norwegian", "Norsk"),
        new("fa", "Persian", "فارسی", true),
        new("pl", "Polish", "Polski"),
        new("pt", "Portuguese", "Português"),
        new("pt-BR", "Portuguese (Brazil)", "Português (Brasil)"),
        new("pt-PT", "Portuguese (Portugal)", "Português (Portugal)"),
        new("pa", "Punjabi", "ਪੰਜਾਬੀ"),
        new("ro", "Romanian", "Română"),
        new("ru", "Russian", "Русский"),
        new("sr", "Serbian", "Српски"),
        new("sk", "Slovak", "Slovenčina"),
        new("sl", "Slovenian", "Slovenščina"),
        new("so", "Somali", "Soomaali"),
        new("sw", "Swahili", "Kiswahili"),
        new("sv", "Swedish", "Svenska"),
        new("ta", "Tamil", "தமிழ்"),
        new("te", "Telugu", "తెలుగు"),
        new("tr", "Turkish", "Türkçe"),
        new("uk", "Ukrainian", "Українська"),
        new("ur", "Urdu", "اردو", true),
        new("uz", "Uzbek", "Oʻzbekcha"),
        new("cy", "Welsh", "Cymraeg"),
        new("zu", "Zulu", "IsiZulu")
    ];

    private static readonly Dictionary<string, Language> ByCode = BuiltIn.ToDictionary(
        language => language.Code,
        StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["zh"] = "zh-Hans",
        ["zh-CN"] = "zh-Hans",
        ["zh-CHS"] = "zh-Hans",
        ["zh-TW"] = "zh-Hant",
        ["zh-HK"] = "zh-Hant",
        ["zh-CHT"] = "zh-Hant",
        ["iw"] = "he",
        ["in"] = "id",
        ["tl"] = "fil",
        ["no"] = "nb"
    };

    internal static IReadOnlyList<Language> All { get; } = BuiltIn;

    internal static IReadOnlyList<string> FeaturedCodes { get; } = ["vi", "en", "zh-Hans"];

    internal static string NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "und";
        }

        var value = code.Trim().Replace('_', '-');
        if (Aliases.TryGetValue(value, out var alias))
        {
            return alias;
        }

        if (ByCode.TryGetValue(value, out var known))
        {
            return known.Code;
        }

        try
        {
            return System.Globalization.CultureInfo.GetCultureInfo(value).Name;
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            return value;
        }
    }

    internal static Language Get(string code) => Find(code) ??
        throw new KeyNotFoundException($"Language '{code}' is not in the built-in catalog.");

    internal static Language? Find(string? code)
    {
        var normalized = NormalizeCode(code);
        return ByCode.GetValueOrDefault(normalized);
    }

    internal static Language Resolve(string code, string? displayName = null, string? nativeName = null)
    {
        var normalized = NormalizeCode(code);
        return ByCode.GetValueOrDefault(normalized) ?? new Language(
            normalized,
            string.IsNullOrWhiteSpace(displayName) ? normalized : displayName.Trim(),
            nativeName?.Trim() ?? string.Empty);
    }

    internal static IReadOnlyList<Language> OrderForPicker(
        IEnumerable<Language> languages,
        IEnumerable<string>? recentCodes = null)
    {
        var distinct = languages
            .GroupBy(language => NormalizeCode(language.Code), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToDictionary(language => language.Code, StringComparer.OrdinalIgnoreCase);
        var result = new List<Language>();
        AddCodes(FeaturedCodes);
        AddCodes(recentCodes ?? []);
        result.AddRange(distinct.Values
            .Where(language => !result.Any(item => string.Equals(
                item.Code,
                language.Code,
                StringComparison.OrdinalIgnoreCase)))
            .OrderBy(language => language.DisplayName, StringComparer.CurrentCultureIgnoreCase));
        return result;

        void AddCodes(IEnumerable<string> codes)
        {
            foreach (var code in codes)
            {
                var normalized = NormalizeCode(code);
                if (distinct.TryGetValue(normalized, out var language) &&
                    !result.Any(item => string.Equals(item.Code, normalized, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(language);
                }
            }
        }
    }

    internal static IReadOnlyList<Language> Search(
        IEnumerable<Language> languages,
        string? query,
        IEnumerable<string>? recentCodes = null)
    {
        var ordered = OrderForPicker(languages, recentCodes);
        if (string.IsNullOrWhiteSpace(query))
        {
            return ordered;
        }

        var value = query.Trim();
        return ordered.Where(language =>
            language.DisplayName.Contains(value, StringComparison.CurrentCultureIgnoreCase) ||
            language.NativeName.Contains(value, StringComparison.CurrentCultureIgnoreCase) ||
            language.Code.Contains(value, StringComparison.OrdinalIgnoreCase)).ToArray();
    }
}
