using System.Diagnostics;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Providers;
using OneBoardInlineTranslate.Services;

namespace OneBoardInlineTranslate.Local;

internal sealed class LocalTranslationProvider(
    LocalModelManager modelManager,
    ILocalTranslationEngine engine,
    ILanguageDetector detector) : ITranslationProvider
{
    public string Id => "local-ctranslate2";

    public string DisplayName => TranslationProviderNames.Local;

    public async Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken)
    {
        var source = request.SourceLanguage ?? detector.Detect(request.Text);
        var sourceCode = LanguageCatalog.NormalizeCode(source.Code);
        var targetCode = LanguageCatalog.NormalizeCode(request.TargetLanguage.Code);
        if (string.Equals(sourceCode, targetCode, StringComparison.OrdinalIgnoreCase))
        {
            return new TranslationResult
            {
                Text = request.Text,
                SourceLanguage = source,
                TargetLanguage = request.TargetLanguage,
                ProviderId = Id,
                Latency = TimeSpan.Zero
            };
        }

        var stopwatch = Stopwatch.StartNew();
        var direct = LocalModelManifest.Find(sourceCode, targetCode);
        if (direct is not null && modelManager.IsInstalled(direct))
        {
            var text = await engine.TranslateAsync(direct, request.Text, cancellationToken);
            return CreateResult(text, source, request.TargetLanguage, stopwatch.Elapsed, usedPivot: false);
        }

        var first = LocalModelManifest.Find(sourceCode, Language.English.Code);
        var second = LocalModelManifest.Find(Language.English.Code, targetCode);
        if (sourceCode != Language.English.Code &&
            targetCode != Language.English.Code &&
            first is not null && second is not null &&
            modelManager.IsInstalled(first) && modelManager.IsInstalled(second))
        {
            var english = await engine.TranslateAsync(first, request.Text, cancellationToken);
            var text = await engine.TranslateAsync(second, english, cancellationToken);
            return CreateResult(text, source, request.TargetLanguage, stopwatch.Elapsed, usedPivot: true);
        }

        throw new LocalModelUnavailableException($"{sourceCode}-{targetCode}");
    }

    public Task<ProviderHealth> TestAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var healthy = modelManager.IsRuntimeInstalled && modelManager.InstalledModels.Count > 0;
        return Task.FromResult(new ProviderHealth(
            healthy,
            DisplayName,
            healthy ? "Connected" : "Local model not installed",
            0));
    }

    public Task<ProviderLanguageCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var installed = modelManager.InstalledModels;
        var sources = ProviderHttp.DistinctLanguages(installed.Select(model =>
            LanguageCatalog.Resolve(model.SourceLanguage)));
        var targets = ProviderHttp.DistinctLanguages(installed.Select(model =>
            LanguageCatalog.Resolve(model.TargetLanguage)));
        return Task.FromResult(new ProviderLanguageCapabilities(
            Id,
            sources,
            targets,
            DateTimeOffset.UtcNow));
    }

    private TranslationResult CreateResult(
        string text,
        Language source,
        Language target,
        TimeSpan latency,
        bool usedPivot) => new()
        {
            Text = text,
            SourceLanguage = source,
            TargetLanguage = target,
            ProviderId = Id,
            Latency = latency,
            UsedPivot = usedPivot
        };
}
