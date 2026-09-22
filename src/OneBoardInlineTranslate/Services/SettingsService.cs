using System.Text.Json;
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
        PropertyNameCaseInsensitive = true
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
        settings.SchemaVersion = 1;
        settings.PreferredLanguage = Language.FromCode(settings.PreferredLanguage).Code;
        settings.Hotkeys ??= new HotkeySettings();
        settings.TranslationProvider ??= new ProviderConfiguration();
        return settings;
    }
}
