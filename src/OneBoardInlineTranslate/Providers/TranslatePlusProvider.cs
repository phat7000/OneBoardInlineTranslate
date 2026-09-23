using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Services;

namespace OneBoardInlineTranslate.Providers;

internal sealed class TranslatePlusProvider : ITranslationProvider
{
    internal const string TranslateEndpoint = "https://api.translateplus.io/v2/translate";
    internal const string LanguagesEndpoint = "https://api.translateplus.io/v2/supported-languages";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILanguageDetector _detector;

    internal TranslatePlusProvider(HttpClient httpClient, string apiKey, ILanguageDetector detector)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _detector = detector;
    }

    public string Id => "translateplus";

    public string DisplayName => TranslationProviderNames.TranslatePlus;

    public async Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken)
    {
        EnsureApiKey();
        using var message = new HttpRequestMessage(HttpMethod.Post, TranslateEndpoint);
        message.Headers.Add("X-API-KEY", _apiKey);
        message.Content = JsonContent.Create(new
        {
            text = request.Text,
            source = request.SourceLanguage is null
                ? "auto"
                : ProviderLanguageCodes.ToTranslatePlus(request.SourceLanguage),
            target = ProviderLanguageCodes.ToTranslatePlus(request.TargetLanguage)
        });

        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(
            message,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        ThrowIfInvalidCredential(response);
        await ProviderHttp.EnsureSuccessAsync(response, inspectGoogleError: false, cancellationToken);
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var translation = document.RootElement.GetProperty("translations");
            var translated = translation.GetProperty("translation").GetString();
            if (string.IsNullOrEmpty(translated))
            {
                throw new ProviderException(ProviderFailure.ProviderUnavailable);
            }

            var source = translation.TryGetProperty("source", out var detected)
                ? detected.GetString()
                : request.SourceLanguage?.Code;
            return new TranslationResult
            {
                Text = translated,
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
        message.Headers.Add("X-API-KEY", _apiKey);
        using var response = await _httpClient.SendAsync(
            message,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        ThrowIfInvalidCredential(response);
        await ProviderHttp.EnsureSuccessAsync(response, inspectGoogleError: false, cancellationToken);
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var languages = document.RootElement.GetProperty("supported_languages")
                .EnumerateObject()
                .Where(item => !string.Equals(item.Value.GetString(), "auto", StringComparison.OrdinalIgnoreCase))
                .Select(item => LanguageCatalog.Resolve(item.Value.GetString() ?? string.Empty, item.Name));
            var normalized = ProviderHttp.DistinctLanguages(languages);
            return new ProviderLanguageCapabilities(Id, normalized, normalized, DateTimeOffset.UtcNow);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new ProviderException(ProviderFailure.ProviderUnavailable);
        }
    }

    private void EnsureApiKey()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new ProviderException(ProviderFailure.InvalidApiKey);
        }
    }

    private static void ThrowIfInvalidCredential(HttpResponseMessage response)
    {
        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
        {
            throw new ProviderException(ProviderFailure.InvalidApiKey);
        }
    }
}
