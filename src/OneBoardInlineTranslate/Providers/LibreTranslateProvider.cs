using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Services;

namespace OneBoardInlineTranslate.Providers;

internal sealed class LibreTranslateProvider : ITranslationProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly Uri _endpoint;
    private readonly ILanguageDetector _detector;

    internal LibreTranslateProvider(
        HttpClient httpClient,
        string apiKey,
        string endpoint,
        ILanguageDetector detector)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _endpoint = ProviderHttp.ValidateEndpoint(endpoint, "http://localhost:5000/translate");
        _detector = detector;
    }

    public string Id => "libretranslate";

    public string DisplayName => TranslationProviderNames.LibreTranslate;

    public async Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken)
    {
        var source = request.SourceLanguage?.Code ?? "auto";
        var payload = new
        {
            q = request.Text,
            source,
            target = ProviderLanguageCodes.ToLibreTranslate(request.TargetLanguage),
            format = "text",
            api_key = string.IsNullOrWhiteSpace(_apiKey) ? null : _apiKey
        };

        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.PostAsJsonAsync(_endpoint, payload, cancellationToken);
        await ProviderHttp.EnsureSuccessAsync(response, inspectGoogleError: false, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var translated = root.GetProperty("translatedText").GetString();
        if (string.IsNullOrEmpty(translated))
        {
            throw new InvalidOperationException("LibreTranslate returned an empty translation.");
        }

        string? detectedCode = request.SourceLanguage?.Code;
        if (root.TryGetProperty("detectedLanguage", out var detected))
        {
            detectedCode = detected.ValueKind == JsonValueKind.Object &&
                           detected.TryGetProperty("language", out var language)
                ? language.GetString()
                : detected.ValueKind == JsonValueKind.String
                    ? detected.GetString()
                    : detectedCode;
        }

        return new TranslationResult
        {
            Text = translated,
            SourceLanguage = ProviderHttp.ResolveLanguage(detectedCode, _detector, request.Text),
            TargetLanguage = request.TargetLanguage,
            ProviderId = Id,
            Latency = stopwatch.Elapsed
        };
    }

    public async Task<ProviderHealth> TestAsync(CancellationToken cancellationToken)
    {
        var result = await TranslateAsync(new TranslationRequest
        {
            Text = "Hello",
            SourceLanguage = Language.English,
            TargetLanguage = Language.Vietnamese
        }, cancellationToken);
        return new ProviderHealth(true, DisplayName, "Connected", (long)result.Latency.TotalMilliseconds);
    }

    public async Task<ProviderLanguageCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        var endpointText = _endpoint.ToString();
        var languagesUri = endpointText.EndsWith("/translate", StringComparison.OrdinalIgnoreCase)
            ? new Uri(endpointText[..^"/translate".Length] + "/languages")
            : new Uri(_endpoint, "languages");
        using var response = await _httpClient.GetAsync(
            languagesUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        await ProviderHttp.EnsureSuccessAsync(response, inspectGoogleError: false, cancellationToken);
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var languages = document.RootElement.EnumerateArray().Select(item => LanguageCatalog.Resolve(
                item.GetProperty("code").GetString() ?? string.Empty,
                item.TryGetProperty("name", out var name) ? name.GetString() : null));
            var normalized = ProviderHttp.DistinctLanguages(languages);
            return new ProviderLanguageCapabilities(Id, normalized, normalized, DateTimeOffset.UtcNow);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new ProviderException(ProviderFailure.ProviderUnavailable);
        }
    }
}
