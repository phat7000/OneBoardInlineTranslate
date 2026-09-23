using System.Windows;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Providers;
using OneBoardInlineTranslate.Security;
using OneBoardInlineTranslate.Services;
using AppLanguage = OneBoardInlineTranslate.Models.Language;

namespace OneBoardInlineTranslate.Views;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settings;
    private readonly ICredentialStore _credentials;
    private readonly ITranslationService _translation;
    private readonly StartupService _startup;
    private readonly string _legacyCredentialProvider;
    private string _displayedProvider = TranslationProviderNames.None;

    internal SettingsWindow(
        ISettingsService settings,
        ICredentialStore credentials,
        ITranslationService translation,
        StartupService startup)
    {
        InitializeComponent();
        _settings = settings;
        _credentials = credentials;
        _translation = translation;
        _startup = startup;
        _legacyCredentialProvider = settings.Current.TranslationProvider.Provider;
        PreferredLanguageCombo.ItemsSource = AppLanguage.Supported;
        ProviderCombo.ItemsSource = TranslationProviderNames.All;
        LoadValues(_settings.Current);
    }

    internal event EventHandler? SettingsSaved;

    private void LoadValues(AppSettings settings)
    {
        PreferredLanguageCombo.SelectedValue = settings.PreferredLanguage;
        StartWithWindowsCheckBox.IsChecked = settings.StartWithWindows;
        PausedCheckBox.IsChecked = settings.IsPaused;
        UnderstandHotkeyBox.Text = settings.Hotkeys.Understand;
        EnglishHotkeyBox.Text = settings.Hotkeys.TranslateToEnglish;
        ChineseHotkeyBox.Text = settings.Hotkeys.TranslateToChinese;
        ReplyHotkeyBox.Text = settings.Hotkeys.Reply;
        OcrHotkeyBox.Text = settings.Hotkeys.OcrTranslate;
        ProviderCombo.SelectedItem = settings.TranslationProvider.Provider;
        EndpointBox.Text = settings.TranslationProvider.Endpoint;
        RegionBox.Text = settings.TranslationProvider.Region;
        ApiKeyBox.Password = string.Empty;
        _displayedProvider = settings.TranslationProvider.Provider;
        UpdateProviderFields();
        _ = UpdateCredentialHintAsync();
    }

    private async void Save_Click(object sender, RoutedEventArgs eventArgs)
    {
        try
        {
            await SaveValuesAsync();
            SaveStatusText.Text = "Settings saved.";
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            SaveStatusText.Text = exception.Message;
        }
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs eventArgs)
    {
        ProviderStatusText.Text = "Testing...";
        TestConnectionButton.IsEnabled = false;
        try
        {
            await SaveValuesAsync();
            var health = await _translation.TestProviderAsync(CancellationToken.None);
            ProviderStatusText.Text = health.IsHealthy
                ? $"Connected · {health.ProviderName} · {health.LatencyMilliseconds} ms"
                : health.Status;
        }
        catch (Exception exception)
        {
            ProviderStatusText.Text = ProviderHttp.ToStatus(
                ProviderHttp.FromException(exception, CancellationToken.None));
        }
        finally
        {
            UpdateProviderFields();
        }
    }

    private void ProviderCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs eventArgs)
    {
        if (!IsInitialized)
        {
            return;
        }

        if (IsLoaded)
        {
            ApiKeyBox.Password = string.Empty;
            var selectedProvider = ProviderCombo.SelectedItem as string ?? TranslationProviderNames.None;
            if (!string.Equals(_displayedProvider, selectedProvider, StringComparison.Ordinal))
            {
                EndpointBox.Clear();
                RegionBox.Clear();
            }
        }

        _displayedProvider = ProviderCombo.SelectedItem as string ?? TranslationProviderNames.None;
        ProviderStatusText.Text = string.Empty;
        UpdateProviderFields();
        _ = UpdateCredentialHintAsync();
    }

    private void Cancel_Click(object sender, RoutedEventArgs eventArgs) => Close();

    private async Task SaveValuesAsync()
    {
        var hotkeys = new HotkeySettings
        {
            Understand = UnderstandHotkeyBox.Text.Trim(),
            TranslateToEnglish = EnglishHotkeyBox.Text.Trim(),
            TranslateToChinese = ChineseHotkeyBox.Text.Trim(),
            Reply = ReplyHotkeyBox.Text.Trim(),
            OcrTranslate = OcrHotkeyBox.Text.Trim()
        };
        ValidateHotkeys(hotkeys);

        var settings = new AppSettings
        {
            PreferredLanguage = PreferredLanguageCombo.SelectedValue as string ?? AppLanguage.Vietnamese.Code,
            StartWithWindows = StartWithWindowsCheckBox.IsChecked == true,
            IsPaused = PausedCheckBox.IsChecked == true,
            Hotkeys = hotkeys,
            TranslationProvider = new ProviderConfiguration
            {
                Provider = ProviderCombo.SelectedItem as string ?? TranslationProviderNames.None,
                Endpoint = EndpointBox.Text.Trim(),
                Region = RegionBox.Text.Trim()
            }
        };

        if (!string.IsNullOrEmpty(ApiKeyBox.Password))
        {
            var provider = ProviderCombo.SelectedItem as string ?? TranslationProviderNames.None;
            await _credentials.SetAsync(
                TranslationService.GetCredentialName(provider),
                ApiKeyBox.Password);
            ApiKeyBox.Password = string.Empty;
        }

        _startup.SetEnabled(settings.StartWithWindows);
        await _settings.SaveAsync(settings);
        await UpdateCredentialHintAsync();
    }

    private void UpdateProviderFields()
    {
        var provider = ProviderCombo.SelectedItem as string ?? TranslationProviderNames.None;
        var showEndpoint = provider is TranslationProviderNames.Azure or
            TranslationProviderNames.DeepL or TranslationProviderNames.LibreTranslate;
        var showRegion = provider == TranslationProviderNames.Azure;
        var showApiKey = provider != TranslationProviderNames.None;

        EndpointLabel.Visibility = showEndpoint ? Visibility.Visible : Visibility.Collapsed;
        EndpointBox.Visibility = showEndpoint ? Visibility.Visible : Visibility.Collapsed;
        RegionLabel.Visibility = showRegion ? Visibility.Visible : Visibility.Collapsed;
        RegionBox.Visibility = showRegion ? Visibility.Visible : Visibility.Collapsed;
        ApiKeyLabel.Visibility = showApiKey ? Visibility.Visible : Visibility.Collapsed;
        ApiKeyBox.Visibility = showApiKey ? Visibility.Visible : Visibility.Collapsed;
        ApiKeyHintText.Visibility = showApiKey ? Visibility.Visible : Visibility.Collapsed;
        TestConnectionPanel.Visibility = showApiKey ? Visibility.Visible : Visibility.Collapsed;
        TestConnectionButton.IsEnabled = showApiKey;

        EndpointLabel.Text = provider switch
        {
            TranslationProviderNames.Azure => "Endpoint (advanced)",
            TranslationProviderNames.DeepL => "Endpoint (optional)",
            TranslationProviderNames.LibreTranslate => "Endpoint",
            _ => "Endpoint"
        };
        ApiKeyLabel.Text = provider == TranslationProviderNames.LibreTranslate
            ? "API key (optional)"
            : "API key";
        ProviderDescriptionText.Text = provider switch
        {
            TranslationProviderNames.GoogleCloud => "Cloud Translation Basic v2. The supported Google endpoint is fixed; only an API key is required.",
            TranslationProviderNames.Azure => "Region is required only for Azure resources that use it. The public endpoint is used unless an advanced endpoint is entered.",
            TranslationProviderNames.DeepL => "The Free or Pro endpoint is selected from the key unless a custom supported endpoint is entered.",
            TranslationProviderNames.LibreTranslate => "Enter the complete /translate endpoint. An API key is optional for deployments that do not require one.",
            _ => "Choose a provider to configure translation."
        };
    }

    private async Task UpdateCredentialHintAsync()
    {
        var provider = ProviderCombo.SelectedItem as string ?? TranslationProviderNames.None;
        if (provider == TranslationProviderNames.None)
        {
            ApiKeyHintText.Text = string.Empty;
            return;
        }

        try
        {
            var credential = await _credentials.GetAsync(TranslationService.GetCredentialName(provider));
            if (string.IsNullOrEmpty(credential) &&
                provider == _legacyCredentialProvider &&
                provider != TranslationProviderNames.GoogleCloud)
            {
                credential = await _credentials.GetAsync(TranslationService.LegacyApiKeyCredentialName);
            }

            if (string.Equals(provider, ProviderCombo.SelectedItem as string, StringComparison.Ordinal))
            {
                ApiKeyHintText.Text = string.IsNullOrEmpty(credential)
                    ? "No saved key."
                    : "A protected key is saved. Leave this blank to keep it.";
            }
        }
        catch
        {
            ApiKeyHintText.Text = "The protected key store is unavailable.";
        }
    }

    private static void ValidateHotkeys(HotkeySettings settings)
    {
        var seen = new HashSet<(uint Modifiers, int Key)>();
        foreach (var (operation, text) in settings.Enumerate())
        {
            if (!HotkeyGesture.TryParse(text, out var gesture) || gesture is null)
            {
                throw new InvalidOperationException($"{operation} has an invalid hotkey.");
            }

            if (!seen.Add((gesture.Modifiers, gesture.VirtualKey)))
            {
                throw new InvalidOperationException($"{gesture.DisplayText} is assigned more than once.");
            }
        }
    }
}
