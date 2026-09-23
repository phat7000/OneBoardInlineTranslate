using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Services;

namespace OneBoardInlineTranslate.Providers;

internal sealed class GoogleCloudTranslationProvider : ITranslationProvider
{
    internal const string Endpoint = "https://translation.googleapis.com/language/translate/v2";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILanguageDetector _detector;

    internal GoogleCloudTranslationProvider(
        HttpClient httpClient,
        string apiKey,
        ILanguageDetector detector)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _detector = detector;
    }

    public string Id => "google-cloud-translation";

    public string DisplayName => TranslationProviderNames.GoogleCloud;

    public async Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new ProviderException(ProviderFailure.InvalidApiKey);
        }

        var payload = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["q"] = request.Text,
            ["target"] = ToGoogleCode(request.TargetLanguage),
            ["format"] = "text"
        };
        if (request.SourceLanguage is not null)
        {
            payload["source"] = ToGoogleCode(request.SourceLanguage);
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        message.Headers.Add("X-Goog-Api-Key", _apiKey);
        message.Content = JsonContent.Create(payload);

        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(
            message,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        await ProviderHttp.EnsureSuccessAsync(response, inspectGoogleError: true, cancellationToken);

        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var translation = document.RootElement
                .GetProperty("data")
                .GetProperty("translations")[0];
            var translated = translation.GetProperty("translatedText").GetString();
            if (string.IsNullOrEmpty(translated))
            {
                throw new ProviderException(ProviderFailure.ProviderUnavailable);
            }

            var detectedCode = translation.TryGetProperty("detectedSourceLanguage", out var detected)
                ? detected.GetString()
                : request.SourceLanguage?.Code;
            return new TranslationResult
            {
                Text = WebUtility.HtmlDecode(translated),
                SourceLanguage = ProviderHttp.ResolveLanguage(detectedCode, _detector, request.Text),
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

    private static string ToGoogleCode(Language language) => language.Code switch
    {
        "zh-Hans" => "zh-CN",
        _ => language.Code
    };
}
