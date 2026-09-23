using OneBoardInlineTranslate.Models;
using System.Net.Http;
using OneBoardInlineTranslate.Local;
using OneBoardInlineTranslate.Providers;
using OneBoardInlineTranslate.Security;

namespace OneBoardInlineTranslate.Services;

internal interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken);

    Task<ProviderHealth> TestProviderAsync(CancellationToken cancellationToken);

    Task<ProviderLanguageCapabilities> GetLanguageCapabilitiesAsync(
        bool forceRefresh,
        CancellationToken cancellationToken);
}

internal interface ITranslationProvider
{
    string Id { get; }

    string DisplayName { get; }

    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken);

    Task<ProviderHealth> TestAsync(CancellationToken cancellationToken);

    Task<ProviderLanguageCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken);
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
    private readonly LocalModelManager _localModels;
    private readonly ILocalTranslationEngine _localEngine;

    internal TranslationService(
        ISettingsService settings,
        ICredentialStore credentials,
        ILanguageDetector languageDetector,
        HttpClient httpClient,
        ITranslationProvider? overrideProvider = null,
        LocalModelManager? localModels = null,
        ILocalTranslationEngine? localEngine = null)
    {
        _settings = settings;
        _credentials = credentials;
        _languageDetector = languageDetector;
        _httpClient = httpClient;
        _overrideProvider = overrideProvider;
        _legacyCredentialProvider = settings.Current.TranslationProvider.Provider;
        _localModels = localModels ?? new LocalModelManager(httpClient);
        _localEngine = localEngine ?? new LocalTranslationEngine(_localModels, settings.Current.LocalTranslation);
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
        ValidateKnownTarget(request.TargetLanguage);
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

    public async Task<ProviderLanguageCapabilities> GetLanguageCapabilitiesAsync(
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var configuration = _settings.Current.TranslationProvider;
        var configurationKey = GetConfigurationKey(configuration);
        if (!forceRefresh &&
            configuration.Provider != TranslationProviderNames.Local &&
            _settings.Current.ProviderLanguageCache.TryGetValue(configuration.Provider, out var cached) &&
            cached.FetchedAt > DateTimeOffset.UtcNow.AddHours(-24) &&
            string.Equals(cached.ConfigurationKey, configurationKey, StringComparison.Ordinal))
        {
            return FromCache(configuration.Provider, cached);
        }

        var provider = _overrideProvider ?? await CreateConfiguredProviderAsync(cancellationToken);
        var capabilities = await provider.GetCapabilitiesAsync(cancellationToken);
        if (_overrideProvider is null && configuration.Provider != TranslationProviderNames.Local)
        {
            var updated = _settings.Current.Clone();
            updated.ProviderLanguageCache[configuration.Provider] = ToCache(capabilities, configurationKey);
            await _settings.SaveAsync(updated, cancellationToken);
        }

        return capabilities;
    }

    private async Task<ITranslationProvider> CreateConfiguredProviderAsync(CancellationToken cancellationToken)
    {
        var configuration = _settings.Current.TranslationProvider;
        var key = configuration.Provider == TranslationProviderNames.Local
            ? string.Empty
            : await GetApiKeyAsync(configuration.Provider, cancellationToken);
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
            TranslationProviderNames.TranslatePlus => new TranslatePlusProvider(
                _httpClient,
                key,
                _languageDetector),
            TranslationProviderNames.Langbly => new LangblyProvider(
                _httpClient,
                key,
                configuration.Region,
                configuration.Endpoint,
                _languageDetector),
            TranslationProviderNames.Local => new LocalTranslationProvider(
                _localModels,
                _localEngine,
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
        TranslationProviderNames.TranslatePlus => "translateplus-api-key",
        TranslationProviderNames.Langbly => "langbly-api-key",
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

    private void ValidateKnownTarget(Language target)
    {
        var configuration = _settings.Current.TranslationProvider;
        if (configuration.Provider == TranslationProviderNames.Local ||
            !_settings.Current.ProviderLanguageCache.TryGetValue(configuration.Provider, out var cached) ||
            !string.Equals(
                cached.ConfigurationKey,
                GetConfigurationKey(configuration),
                StringComparison.Ordinal))
        {
            return;
        }

        var targetCode = LanguageCatalog.NormalizeCode(target.Code);
        if (!cached.TargetLanguages.Any(language => string.Equals(
            LanguageCatalog.NormalizeCode(language.Code),
            targetCode,
            StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnsupportedProviderLanguageException(configuration.Provider, target.DisplayName);
        }
    }

    private static string GetConfigurationKey(ProviderConfiguration configuration) =>
        $"{configuration.Provider}|{configuration.Region.Trim()}|{configuration.Endpoint.Trim()}";

    private static ProviderLanguageCapabilities FromCache(
        string provider,
        ProviderLanguageCacheEntry cached) => new(
            provider,
            cached.SourceLanguages.Select(FromCachedLanguage).ToArray(),
            cached.TargetLanguages.Select(FromCachedLanguage).ToArray(),
            cached.FetchedAt);

    private static ProviderLanguageCacheEntry ToCache(
        ProviderLanguageCapabilities capabilities,
        string configurationKey) => new()
        {
            FetchedAt = capabilities.FetchedAt,
            ConfigurationKey = configurationKey,
            SourceLanguages = capabilities.SourceLanguages.Select(ToCachedLanguage).ToList(),
            TargetLanguages = capabilities.TargetLanguages.Select(ToCachedLanguage).ToList()
        };

    private static Language FromCachedLanguage(CachedLanguage language) => LanguageCatalog.Resolve(
        language.Code,
        language.DisplayName,
        language.NativeName);

    private static CachedLanguage ToCachedLanguage(Language language) => new()
    {
        Code = language.Code,
        DisplayName = language.DisplayName,
        NativeName = language.NativeName
    };
}

internal sealed class UnsupportedProviderLanguageException(string provider, string language) :
    InvalidOperationException($"{provider} does not support {language} for this request.");

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

    public Task<ProviderLanguageCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ProviderLanguageCapabilities.Fallback(Id));
    }
}
