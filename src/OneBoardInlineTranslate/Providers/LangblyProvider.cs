using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Services;

namespace OneBoardInlineTranslate.Providers;

internal sealed class LangblyProvider : ITranslationProvider
{
    internal const string GlobalEndpoint = "https://api.langbly.com";
    internal const string EuEndpoint = "https://eu.langbly.com";
    internal const string TranslatePath = "/language/translate/v2";
    internal const string LanguagesPath = "/language/translate/v2/languages";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly Uri _baseEndpoint;
    private readonly ILanguageDetector _detector;

    internal LangblyProvider(
        HttpClient httpClient,
        string apiKey,
        string region,
        string customEndpoint,
        ILanguageDetector detector)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _baseEndpoint = ResolveEndpoint(region, customEndpoint);
        _detector = detector;
    }

    public string Id => "langbly";

    public string DisplayName => TranslationProviderNames.Langbly;

    internal Uri TranslateEndpoint => new(_baseEndpoint.ToString().TrimEnd('/') + TranslatePath);

    internal Uri LanguagesEndpoint => new(_baseEndpoint.ToString().TrimEnd('/') + LanguagesPath);

    public async Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken)
    {
        EnsureApiKey();
        var payload = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["q"] = request.Text,
            ["target"] = ProviderLanguageCodes.ToLangbly(request.TargetLanguage),
            ["format"] = "text"
        };
        if (request.SourceLanguage is not null)
        {
            payload["source"] = ProviderLanguageCodes.ToLangbly(request.SourceLanguage);
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, TranslateEndpoint);
        message.Headers.Add("X-API-Key", _apiKey);
        message.Content = JsonContent.Create(payload);
        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(
            message,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        ThrowIfInvalidCredential(response);
        await ProviderHttp.EnsureSuccessAsync(response, inspectGoogleError: true, cancellationToken);
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var translation = document.RootElement.GetProperty("data").GetProperty("translations")[0];
            var translated = translation.GetProperty("translatedText").GetString();
            if (string.IsNullOrEmpty(translated))
            {
                throw new ProviderException(ProviderFailure.ProviderUnavailable);
            }

            var source = translation.TryGetProperty("detectedSourceLanguage", out var detected)
                ? detected.GetString()
                : request.SourceLanguage?.Code;
            return new TranslationResult
            {
                Text = WebUtility.HtmlDecode(translated),
                SourceLanguage = ProviderHttp.ResolveLanguage(source, _detector, request.Text),
                TargetLanguage = request.TargetLanguage,
                ProviderId = Id,
                Latency = stopwatch.Elapsed
            };
        }
        catch (ProviderException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new ProviderException(ProviderFailure.ProviderUnavailable);
        }
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
        EnsureApiKey();
        using var message = new HttpRequestMessage(HttpMethod.Get, LanguagesEndpoint);
        message.Headers.Add("X-API-Key", _apiKey);
        using var response = await _httpClient.SendAsync(
            message,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        ThrowIfInvalidCredential(response);
        await ProviderHttp.EnsureSuccessAsync(response, inspectGoogleError: true, cancellationToken);
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var languages = document.RootElement.GetProperty("data").GetProperty("languages")
                .EnumerateArray()
                .Select(item => LanguageCatalog.Resolve(
                    item.GetProperty("language").GetString() ?? string.Empty,
                    item.TryGetProperty("name", out var name) ? name.GetString() : null));
            var normalized = ProviderHttp.DistinctLanguages(languages);
            return new ProviderLanguageCapabilities(Id, normalized, normalized, DateTimeOffset.UtcNow);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new ProviderException(ProviderFailure.ProviderUnavailable);
        }
    }

    internal static Uri ResolveEndpoint(string region, string customEndpoint) => region.Trim() switch
    {
        "EU" => new Uri(EuEndpoint),
        "Custom" => ProviderHttp.ValidateEndpoint(customEndpoint, GlobalEndpoint),
        _ => new Uri(GlobalEndpoint)
    };

    private void EnsureApiKey()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new ProviderException(ProviderFailure.InvalidApiKey);
        }
    }

    private static void ThrowIfInvalidCredential(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new ProviderException(ProviderFailure.InvalidApiKey);
        }
    }
}
