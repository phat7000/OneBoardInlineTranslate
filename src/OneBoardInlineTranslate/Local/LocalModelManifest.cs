using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Local;

internal sealed record LocalModelManifestEntry(
    string Id,
    string Version,
    string SourceLanguage,
    string TargetLanguage,
    Uri DownloadUri,
    long DownloadSize,
    string Sha256,
    string License,
    string EngineType)
{
    public string DisplayName =>
        $"{LanguageCatalog.Resolve(SourceLanguage).DisplayName} → {LanguageCatalog.Resolve(TargetLanguage).DisplayName}";
}

internal sealed record LocalRuntimeComponent(
    string FileName,
    Uri DownloadUri,
    long DownloadSize,
    string Sha256,
    string License);

internal static class LocalModelManifest
{
    internal const string EngineVersion = "python-3.12.10-ctranslate2-4.8.2";
    internal const string EngineType = "CTranslate2 / Argos-compatible OPUS-MT";

    internal static IReadOnlyList<LocalRuntimeComponent> RuntimeComponents { get; } =
    [
        new(
            "python-3.12.10-embed-amd64.zip",
            new Uri("https://www.python.org/ftp/python/3.12.10/python-3.12.10-embed-amd64.zip"),
            11_133_606,
            "4ACBED6DD1C744B0376E3B1CF57CE906F9DC9E95E68824584C8099A63025A3C3",
            "Python Software Foundation License 2.0"),
        new(
            "ctranslate2-4.8.2-cp312-cp312-win_amd64.whl",
            new Uri("https://files.pythonhosted.org/packages/4e/23/e3b5322ff7368fcbed181ea4c209149416e7940b5b04971d5ee4084afe1a/ctranslate2-4.8.2-cp312-cp312-win_amd64.whl"),
            19_222_069,
            "D94421D565D0DE61C032998F737A18942B0F2BEF40C0424B1846EC6F67300105",
            "MIT"),
        new(
            "sentencepiece-0.2.1-cp312-cp312-win_amd64.whl",
            new Uri("https://files.pythonhosted.org/packages/2d/81/92df5673c067148c2545b1bfe49adfd775bcc3a169a047f5a0e6575ddaca/sentencepiece-0.2.1-cp312-cp312-win_amd64.whl"),
            1_054_671,
            "4CDC7C36234FDA305E85C32949C5211FAAF8DD886096C7CEA289DDC12A2D02DE",
            "Apache-2.0"),
        new(
            "setuptools-75.8.2-py3-none-any.whl",
            new Uri("https://files.pythonhosted.org/packages/a9/38/7d7362e031bd6dc121e5081d8cb6aa6f6fedf2b67bf889962134c6da4705/setuptools-75.8.2-py3-none-any.whl"),
            1_229_385,
            "558E47C15F1811C1FA7ADBD0096669BF76C1D3F433F58324DF69F3F5ECAC4E8F",
            "MIT")
    ];

    internal static IReadOnlyList<LocalModelManifestEntry> Models { get; } =
    [
        new(
            "vi-en",
            "1.9",
            "vi",
            "en",
            new Uri("https://argos-net.com/v1/translate-vi_en-1_9.argosmodel"),
            65_895_840,
            "EBD51B7189B13ECCB9238A5777DE1D343008C4C05284A8B89610242364433953",
            "CC-BY-4.0",
            EngineType),
        new(
            "en-vi",
            "1.9",
            "en",
            "vi",
            new Uri("https://argos-net.com/v1/translate-en_vi-1_9.argosmodel"),
            67_770_159,
            "86957101AA4099AA9A1A7492E41987D938D3CF0FDAF4FB684C0797A9D567DD16",
            "CC-BY-4.0",
            EngineType),
        new(
            "en-zh-Hans",
            "1.9",
            "en",
            "zh-Hans",
            new Uri("https://argos-net.com/v1/translate-en_zh-1_9.argosmodel"),
            70_743_021,
            "433E7C4F034D87FBE2353161E05F18646D7999452F801A4E1F0378522B9850AB",
            "CC-BY-4.0",
            EngineType),
        new(
            "zh-Hans-en",
            "1.9",
            "zh-Hans",
            "en",
            new Uri("https://argos-net.com/v1/translate-zh_en-1_9.argosmodel"),
            74_481_402,
            "62E7AF5A3A48B530E47B7B3E5C78C2DE79073ECD815750D2BF3AB35B4A67DA2D",
            "CC-BY-4.0",
            EngineType)
    ];

    internal static LocalModelManifestEntry? Find(string sourceLanguage, string targetLanguage)
    {
        var source = LanguageCatalog.NormalizeCode(sourceLanguage);
        var target = LanguageCatalog.NormalizeCode(targetLanguage);
        return Models.FirstOrDefault(model =>
            string.Equals(model.SourceLanguage, source, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(model.TargetLanguage, target, StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed record ModelDownloadProgress(string ItemName, long BytesReceived, long TotalBytes)
{
    internal double Percentage => TotalBytes <= 0 ? 0 : Math.Clamp(BytesReceived * 100d / TotalBytes, 0, 100);
}
