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

    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken);

    Task<ProviderHealth> TestAsync(CancellationToken cancellationToken);
}

internal sealed class TranslationService : ITranslationService
{
    internal const string ApiKeyCredentialName = "translation-api-key";
    private readonly ISettingsService _settings;
    private readonly ICredentialStore _credentials;
    private readonly ILanguageDetector _languageDetector;
    private readonly HttpClient _httpClient;
    private readonly ITranslationProvider? _overrideProvider;

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
        return await provider.TranslateAsync(request, cancellationToken);
    }

    public async Task<ProviderHealth> TestProviderAsync(CancellationToken cancellationToken)
    {
        try
        {
            var provider = _overrideProvider ?? await CreateConfiguredProviderAsync(cancellationToken);
            return await provider.TestAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new ProviderHealth(false, _settings.Current.TranslationProvider.Provider, exception.GetType().Name);
        }
    }

    private async Task<ITranslationProvider> CreateConfiguredProviderAsync(CancellationToken cancellationToken)
    {
        var configuration = _settings.Current.TranslationProvider;
        var key = await _credentials.GetAsync(ApiKeyCredentialName, cancellationToken) ?? string.Empty;
        return configuration.Provider switch
        {
            "Azure Translator" => new AzureTranslatorProvider(
                _httpClient,
                key,
                configuration.Region,
                configuration.Endpoint,
                _languageDetector),
            "DeepL" => new DeepLTranslationProvider(
                _httpClient,
                key,
                configuration.Endpoint,
                _languageDetector),
            "LibreTranslate" => new LibreTranslateProvider(
                _httpClient,
                key,
                configuration.Endpoint,
                _languageDetector),
            _ => throw new InvalidOperationException("Configure a translation provider in Settings before translating.")
        };
    }
}

internal sealed class DeterministicTranslationProvider : ITranslationProvider
{
    private readonly ILanguageDetector _detector = new LanguageDetector();

    public string Id => "test-only";

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
        Task.FromResult(new ProviderHealth(true, Id, "Ready"));
}
