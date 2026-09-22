namespace OneBoardInlineTranslate.Models;

internal sealed class TranslationRequest
{
    internal required string Text { get; init; }

    internal Language? SourceLanguage { get; init; }

    internal required Language TargetLanguage { get; init; }

    public override string ToString() =>
        $"TranslationRequest(Source={SourceLanguage?.Code ?? "auto"}, Target={TargetLanguage.Code}, Length={Text.Length})";
}

internal sealed class TranslationResult
{
    internal required string Text { get; init; }

    internal required Language SourceLanguage { get; init; }

    internal required Language TargetLanguage { get; init; }

    internal required string ProviderId { get; init; }

    internal TimeSpan Latency { get; init; }

    public override string ToString() =>
        $"TranslationResult(Provider={ProviderId}, Source={SourceLanguage.Code}, Target={TargetLanguage.Code}, Length={Text.Length})";
}

internal sealed record ProviderHealth(bool IsHealthy, string ProviderId, string Status);

internal sealed class ProviderConfiguration
{
    public string Provider { get; set; } = "None";

    public string Endpoint { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;
}
