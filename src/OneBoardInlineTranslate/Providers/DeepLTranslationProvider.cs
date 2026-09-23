using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Services;

namespace OneBoardInlineTranslate.Providers;

internal sealed class DeepLTranslationProvider : ITranslationProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly Uri _endpoint;
    private readonly ILanguageDetector _detector;

    internal DeepLTranslationProvider(
        HttpClient httpClient,
        string apiKey,
        string endpoint,
        ILanguageDetector detector)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        var defaultEndpoint = apiKey.EndsWith(":fx", StringComparison.Ordinal)
            ? "https://api-free.deepl.com/v2/translate"
            : "https://api.deepl.com/v2/translate";
        _endpoint = ProviderHttp.ValidateEndpoint(endpoint, defaultEndpoint);
        _detector = detector;
    }

    public string Id => "deepl";

    public string DisplayName => TranslationProviderNames.DeepL;

    public async Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new ProviderException(ProviderFailure.InvalidApiKey);
        }

        var fields = new List<KeyValuePair<string, string>>
        {
            new("text", request.Text),
            new("target_lang", ProviderLanguageCodes.ToDeepL(request.TargetLanguage))
        };
        if (request.SourceLanguage is not null)
        {
            fields.Add(new("source_lang", ProviderLanguageCodes.ToDeepL(request.SourceLanguage)));
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, _endpoint);
        message.Headers.Add("Authorization", "DeepL-Auth-Key " + _apiKey);
        message.Content = new FormUrlEncodedContent(fields);
        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await ProviderHttp.EnsureSuccessAsync(response, inspectGoogleError: false, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var translation = document.RootElement.GetProperty("translations")[0];
        var translated = translation.GetProperty("text").GetString();
        if (string.IsNullOrEmpty(translated))
        {
            throw new InvalidOperationException("DeepL returned an empty translation.");
        }

        var detectedCode = translation.TryGetProperty("detected_source_language", out var detected)
            ? detected.GetString()
            : request.SourceLanguage?.Code;
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
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new ProviderException(ProviderFailure.InvalidApiKey);
        }

        var baseUri = _endpoint.GetLeftPart(UriPartial.Authority);
        var sourceTask = GetLanguagesAsync(new Uri(baseUri + "/v2/languages?type=source"), cancellationToken);
        var targetTask = GetLanguagesAsync(new Uri(baseUri + "/v2/languages?type=target"), cancellationToken);
        await Task.WhenAll(sourceTask, targetTask);
        return new ProviderLanguageCapabilities(
            Id,
            await sourceTask,
            await targetTask,
            DateTimeOffset.UtcNow);
    }

    private async Task<IReadOnlyList<Language>> GetLanguagesAsync(
        Uri uri,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, uri);
        message.Headers.Add("Authorization", "DeepL-Auth-Key " + _apiKey);
        using var response = await _httpClient.SendAsync(
            message,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        await ProviderHttp.EnsureSuccessAsync(response, inspectGoogleError: false, cancellationToken);
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return ProviderHttp.DistinctLanguages(document.RootElement.EnumerateArray().Select(item =>
                LanguageCatalog.Resolve(
                    item.GetProperty("language").GetString() ?? string.Empty,
                    item.TryGetProperty("name", out var name) ? name.GetString() : null)));
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new ProviderException(ProviderFailure.ProviderUnavailable);
        }
    }
}
