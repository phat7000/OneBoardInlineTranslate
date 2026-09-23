using OneBoardInlineTranslate.Models;
using System.Net.Http;
using OneBoardInlineTranslate.Providers;
using OneBoardInlineTranslate.Security;

namespace OneBoardInlineTranslate.Services;

internal interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken);

    Task<ProviderHealth> TestProviderAsync(CancellationToken cancellationToken);
}

internal interface ITranslationProvider
{
    string Id { get; }

    string DisplayName { get; }

    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken);

    Task<ProviderHealth> TestAsync(CancellationToken cancellationToken);
}

internal sealed class TranslationService : ITranslationService
{
    internal const string LegacyApiKeyCredentialName = "translation-api-key";
    private readonly ISettingsService _settings;
    private readonly ICredentialStore _credentials;
    private readonly ILanguageDetector _languageDetector;
    private readonly HttpClient _httpClient;
    private readonly ITranslationProvider? _overrideProvider;
    private readonly string _legacyCredentialProvider;

    internal TranslationService(
        ISettingsService settings,
        ICredentialStore credentials,
        ILanguageDetector languageDetector,
        HttpClient httpClient,
        ITranslationProvider? overrideProvider = null)
    {
        _settings = settings;
        _credentials = credentials;
        _languageDetector = languageDetector;
        _httpClient = httpClient;
        _overrideProvider = overrideProvider;
        _legacyCredentialProvider = settings.Current.TranslationProvider.Provider;
    }

    public async Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ArgumentException("Text is required for translation.", nameof(request));
        }

        var provider = _overrideProvider ?? await CreateConfiguredProviderAsync(cancellationToken);
        try
        {
            return await provider.TranslateAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ProviderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ProviderException(ProviderHttp.FromException(exception, cancellationToken));
        }
    }

    public async Task<ProviderHealth> TestProviderAsync(CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        ITranslationProvider? provider = null;
        try
        {
            provider = _overrideProvider ?? await CreateConfiguredProviderAsync(cancellationToken);
            return await provider.TestAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var failure = ProviderHttp.FromException(exception, cancellationToken);
            return new ProviderHealth(
                false,
                provider?.DisplayName ?? _settings.Current.TranslationProvider.Provider,
                ProviderHttp.ToStatus(failure),
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException exception)
        {
            var failure = ProviderHttp.FromException(exception, cancellationToken);
            return new ProviderHealth(
                false,
                provider?.DisplayName ?? _settings.Current.TranslationProvider.Provider,
                ProviderHttp.ToStatus(failure),
                stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<ITranslationProvider> CreateConfiguredProviderAsync(CancellationToken cancellationToken)
    {
        var configuration = _settings.Current.TranslationProvider;
        var key = await GetApiKeyAsync(configuration.Provider, cancellationToken);
        return configuration.Provider switch
        {
            TranslationProviderNames.GoogleCloud => new GoogleCloudTranslationProvider(
                _httpClient,
                key,
                _languageDetector),
            TranslationProviderNames.Azure => new AzureTranslatorProvider(
                _httpClient,
                key,
                configuration.Region,
                configuration.Endpoint,
                _languageDetector),
            TranslationProviderNames.DeepL => new DeepLTranslationProvider(
                _httpClient,
                key,
                configuration.Endpoint,
                _languageDetector),
            TranslationProviderNames.LibreTranslate => new LibreTranslateProvider(
                _httpClient,
                key,
                configuration.Endpoint,
                _languageDetector),
            _ => throw new InvalidOperationException("Configure a translation provider in Settings before translating.")
        };
    }

    internal static string GetCredentialName(string provider) => provider switch
    {
        TranslationProviderNames.GoogleCloud => "google-cloud-translation-api-key",
        TranslationProviderNames.Azure => "azure-translator-api-key",
        TranslationProviderNames.DeepL => "deepl-api-key",
        TranslationProviderNames.LibreTranslate => "libretranslate-api-key",
        _ => LegacyApiKeyCredentialName
    };

    private async Task<string> GetApiKeyAsync(string provider, CancellationToken cancellationToken)
    {
        var credentialName = GetCredentialName(provider);
        var key = await _credentials.GetAsync(credentialName, cancellationToken);
        if (string.IsNullOrEmpty(key) &&
            credentialName != LegacyApiKeyCredentialName &&
            provider == _legacyCredentialProvider &&
            provider != TranslationProviderNames.GoogleCloud)
        {
            key = await _credentials.GetAsync(LegacyApiKeyCredentialName, cancellationToken);
            if (!string.IsNullOrEmpty(key))
            {
                await _credentials.SetAsync(credentialName, key, cancellationToken);
                await _credentials.RemoveAsync(LegacyApiKeyCredentialName, cancellationToken);
            }
        }

        return key ?? string.Empty;
    }
}

internal sealed class DeterministicTranslationProvider : ITranslationProvider
{
    private readonly ILanguageDetector _detector = new LanguageDetector();

    public string Id => "test-only";

    public string DisplayName => "Test provider";

    public Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var text = request.TargetLanguage == Language.English
            ? "[TEST] " + request.Text
            : request.TargetLanguage == Language.SimplifiedChinese
                ? "【测试】" + request.Text
                : "[VI] " + request.Text;
        return Task.FromResult(new TranslationResult
        {
            Text = text,
            SourceLanguage = request.SourceLanguage ?? _detector.Detect(request.Text),
            TargetLanguage = request.TargetLanguage,
            ProviderId = Id,
            Latency = TimeSpan.Zero
        });
    }

    public Task<ProviderHealth> TestAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new ProviderHealth(true, DisplayName, "Connected", 0));
}
