using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Services;

namespace OneBoardInlineTranslate.Providers;

internal sealed class AzureTranslatorProvider : ITranslationProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _region;
    private readonly Uri _endpoint;
    private readonly ILanguageDetector _detector;

    internal AzureTranslatorProvider(
        HttpClient httpClient,
        string apiKey,
        string region,
        string endpoint,
        ILanguageDetector detector)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _region = region;
        _endpoint = ProviderHttp.ValidateEndpoint(endpoint, "https://api.cognitive.microsofttranslator.com");
        _detector = detector;
    }

    public string Id => "azure-translator";

    public string DisplayName => TranslationProviderNames.Azure;

    public async Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new ProviderException(ProviderFailure.InvalidApiKey);
        }

        var path = $"translate?api-version=3.0&to={Uri.EscapeDataString(request.TargetLanguage.Code)}";
        if (request.SourceLanguage is not null)
        {
            path += $"&from={Uri.EscapeDataString(request.SourceLanguage.Code)}";
        }

        var uri = new Uri(_endpoint.ToString().TrimEnd('/') + "/" + path);
        using var message = new HttpRequestMessage(HttpMethod.Post, uri);
        message.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
        if (!string.IsNullOrWhiteSpace(_region))
        {
            message.Headers.Add("Ocp-Apim-Subscription-Region", _region.Trim());
        }

        message.Content = JsonContent.Create(new[] { new { Text = request.Text } });
        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await ProviderHttp.EnsureSuccessAsync(response, inspectGoogleError: false, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var item = document.RootElement[0];
        var translated = item.GetProperty("translations")[0].GetProperty("text").GetString();
        if (string.IsNullOrEmpty(translated))
        {
            throw new InvalidOperationException("Azure Translator returned an empty translation.");
        }

        var detectedCode = item.TryGetProperty("detectedLanguage", out var detected)
            ? detected.GetProperty("language").GetString()
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
        var uri = new Uri(_endpoint.ToString().TrimEnd('/') + "/languages?api-version=3.0&scope=translation");
        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await ProviderHttp.EnsureSuccessAsync(response, inspectGoogleError: false, cancellationToken);
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var languages = document.RootElement.GetProperty("translation").EnumerateObject()
                .Select(item => LanguageCatalog.Resolve(
                    item.Name,
                    item.Value.TryGetProperty("name", out var name) ? name.GetString() : null,
                    item.Value.TryGetProperty("nativeName", out var native) ? native.GetString() : null));
            var normalized = ProviderHttp.DistinctLanguages(languages);
            return new ProviderLanguageCapabilities(Id, normalized, normalized, DateTimeOffset.UtcNow);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new ProviderException(ProviderFailure.ProviderUnavailable);
        }
    }
}
