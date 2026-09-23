using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Services;

internal interface ISettingsService
{
    AppSettings Current { get; }

    string SettingsPath { get; }

    event EventHandler? Changed;

    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}

internal sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    internal SettingsService(string? directory = null)
    {
        var settingsDirectory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneBoardInlineTranslate");
        SettingsPath = Path.Combine(settingsDirectory, "settings.json");
    }

    public AppSettings Current { get; private set; } = new();

    public string SettingsPath { get; }

    public event EventHandler? Changed;

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(SettingsPath))
            {
                Current = new AppSettings();
                return Current.Clone();
            }

            try
            {
                await using var stream = File.OpenRead(SettingsPath);
                var settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                    stream,
                    JsonOptions,
                    cancellationToken);
                Current = Normalize(settings ?? new AppSettings());
            }
            catch (JsonException)
            {
                Current = new AppSettings();
            }
            catch (IOException)
            {
                Current = new AppSettings();
            }

            return Current.Clone();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var normalized = Normalize(settings.Clone());
            var directory = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(directory);
            var temporaryPath = SettingsPath + ".tmp";
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, normalized, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, SettingsPath, overwrite: true);
            Current = normalized;
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        settings.SchemaVersion = 2;
        settings.PreferredLanguage = NormalizePersistedLanguage(
            settings.PreferredLanguage,
            Language.Vietnamese.Code);
        settings.Hotkeys ??= new HotkeySettings();
        settings.TranslationProvider ??= new ProviderConfiguration();
        settings.QuickTarget1 = NormalizePersistedLanguage(settings.QuickTarget1, Language.English.Code);
        settings.QuickTarget2 = NormalizePersistedLanguage(
            settings.QuickTarget2,
            Language.SimplifiedChinese.Code);
        settings.RecentLanguages = (settings.RecentLanguages ?? [])
            .Select(LanguageCatalog.NormalizeCode)
            .Where(IsPlausibleLanguageCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
        settings.PopupWidth = Math.Clamp(settings.PopupWidth, 300, 900);
        settings.PopupHeight = Math.Clamp(settings.PopupHeight, 180, 720);
        settings.PinnedBounds ??= new WindowBoundsSettings();
        settings.PinnedBounds.Width = Math.Clamp(settings.PinnedBounds.Width, 340, 1400);
        settings.PinnedBounds.Height = Math.Clamp(settings.PinnedBounds.Height, 220, 1000);
        settings.ProviderLanguageCache = new Dictionary<string, ProviderLanguageCacheEntry>(
            settings.ProviderLanguageCache ?? [],
            StringComparer.OrdinalIgnoreCase);
        settings.LocalTranslation ??= new LocalTranslationSettings();
        settings.LocalTranslation.MaximumLoadedModels = Math.Clamp(
            settings.LocalTranslation.MaximumLoadedModels,
            1,
            3);
        return settings;
    }

    private static string NormalizePersistedLanguage(string? code, string fallback)
    {
        var normalized = LanguageCatalog.NormalizeCode(code);
        return IsPlausibleLanguageCode(normalized) ? normalized : fallback;
    }

    private static bool IsPlausibleLanguageCode(string code) =>
        code.Length is >= 2 and <= 35 &&
        !string.Equals(code, "und", StringComparison.OrdinalIgnoreCase) &&
        code.All(character => char.IsAsciiLetterOrDigit(character) || character == '-');
}
