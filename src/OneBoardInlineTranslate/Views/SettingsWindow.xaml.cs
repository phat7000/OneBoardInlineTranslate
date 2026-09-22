using System.Windows;
using OneBoardInlineTranslate.Models;
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
        PreferredLanguageCombo.ItemsSource = AppLanguage.Supported;
        ProviderCombo.ItemsSource = new[] { "None", "Azure Translator", "DeepL", "LibreTranslate" };
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
        try
        {
            await SaveValuesAsync();
            var health = await _translation.TestProviderAsync(CancellationToken.None);
            ProviderStatusText.Text = health.IsHealthy ? "Connected" : $"Unavailable ({health.Status})";
        }
        catch (Exception exception)
        {
            ProviderStatusText.Text = $"Unavailable ({exception.GetType().Name})";
        }
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
                Provider = ProviderCombo.SelectedItem as string ?? "None",
                Endpoint = EndpointBox.Text.Trim(),
                Region = RegionBox.Text.Trim()
            }
        };

        if (!string.IsNullOrEmpty(ApiKeyBox.Password))
        {
            await _credentials.SetAsync(TranslationService.ApiKeyCredentialName, ApiKeyBox.Password);
            ApiKeyBox.Password = string.Empty;
        }

        _startup.SetEnabled(settings.StartWithWindows);
        await _settings.SaveAsync(settings);
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
