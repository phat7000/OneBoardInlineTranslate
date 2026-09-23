using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OneBoardInlineTranslate.Local;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Providers;
using OneBoardInlineTranslate.Security;
using OneBoardInlineTranslate.Services;
using AppLanguage = OneBoardInlineTranslate.Models.Language;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace OneBoardInlineTranslate.Views;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settings;
    private readonly ICredentialStore _credentials;
    private readonly ITranslationService _translation;
    private readonly StartupService _startup;
    private readonly LocalModelManager _localModels;
    private readonly string _legacyCredentialProvider;
    private IReadOnlyList<AppLanguage> _availableTargets = LanguageCatalog.All;
    private CancellationTokenSource? _modelOperationCancellation;
    private bool _loading = true;
    private string _displayedProvider = TranslationProviderNames.None;

    internal SettingsWindow(
        ISettingsService settings,
        ICredentialStore credentials,
        ITranslationService translation,
        StartupService startup,
        LocalModelManager localModels)
    {
        InitializeComponent();
        _settings = settings;
        _credentials = credentials;
        _translation = translation;
        _startup = startup;
        _localModels = localModels;
        _legacyCredentialProvider = settings.Current.TranslationProvider.Provider;
        ProviderCombo.ItemsSource = TranslationProviderNames.All;
        LangblyRegionCombo.ItemsSource = new[] { "Global", "EU", "Custom" };
        ResultModeCombo.ItemsSource = Choices(
            (ResultWindowMode.Popup, "Popup"),
            (ResultWindowMode.Pinned, "Pinned"),
            (ResultWindowMode.Hidden, "Hidden"));
        PopupSizeCombo.ItemsSource = Choices(
            (PopupSizePreset.Auto, "Auto size"),
            (PopupSizePreset.Small, "Small"),
            (PopupSizePreset.Medium, "Medium"),
            (PopupSizePreset.Large, "Large"),
            (PopupSizePreset.Custom, "Custom"));
        PopupPositionCombo.ItemsSource = Choices(
            (PopupPositionMode.AutoNearSelection, "Auto near selection"),
            (PopupPositionMode.TopRight, "Top right"),
            (PopupPositionMode.BottomRight, "Bottom right"),
            (PopupPositionMode.TopLeft, "Top left"),
            (PopupPositionMode.BottomLeft, "Bottom left"),
            (PopupPositionMode.Custom, "Custom remembered position"));
        ConfigureChoiceCombo(ResultModeCombo);
        ConfigureChoiceCombo(PopupSizeCombo);
        ConfigureChoiceCombo(PopupPositionCombo);
        ApplyCachedCapabilities(settings.Current);
        LoadValues(settings.Current);
        _loading = false;
        ShowSection("General");
        RefreshModelCards();
        _ = UpdateCredentialHintAsync();
        _ = RefreshCapabilitiesAsync(forceRefresh: false, saveFirst: false);
    }

    internal event EventHandler? SettingsSaved;

    protected override void OnClosed(EventArgs eventArgs)
    {
        _modelOperationCancellation?.Cancel();
        _modelOperationCancellation?.Dispose();
        base.OnClosed(eventArgs);
    }

    private static IReadOnlyList<KeyValuePair<T, string>> Choices<T>(params (T Value, string Label)[] values)
        where T : notnull => values.Select(value =>
            new KeyValuePair<T, string>(value.Value, value.Label)).ToArray();

    private static void ConfigureChoiceCombo(ComboBox combo)
    {
        combo.DisplayMemberPath = "Value";
        combo.SelectedValuePath = "Key";
    }

    private void LoadValues(AppSettings settings)
    {
        SetLanguageItems(PreferredLanguageCombo, settings.PreferredLanguage);
        SetLanguageItems(QuickTarget1Combo, settings.QuickTarget1);
        SetLanguageItems(QuickTarget2Combo, settings.QuickTarget2);
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
        LangblyRegionCombo.SelectedItem = settings.TranslationProvider.Region is "EU" or "Custom"
            ? settings.TranslationProvider.Region
            : "Global";
        ApiKeyBox.Password = string.Empty;
        _displayedProvider = settings.TranslationProvider.Provider;
        ResultModeCombo.SelectedValue = settings.ResultWindowMode;
        PopupSizeCombo.SelectedValue = settings.PopupSizePreset;
        PopupPositionCombo.SelectedValue = settings.PopupPositionMode;
        PopupWidthBox.Text = settings.PopupWidth.ToString("0", CultureInfo.CurrentCulture);
        PopupHeightBox.Text = settings.PopupHeight.ToString("0", CultureInfo.CurrentCulture);
        PopupLeftBox.Text = settings.PopupCustomLeft?.ToString("0", CultureInfo.CurrentCulture) ?? string.Empty;
        PopupTopBox.Text = settings.PopupCustomTop?.ToString("0", CultureInfo.CurrentCulture) ?? string.Empty;
        UpdateProviderFields();
        UpdateResultFields();
        UpdateHotkeyLabels();
    }

    private void Navigation_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is Button { Tag: string section })
        {
            ShowSection(section);
        }
    }

    private void ShowSection(string section)
    {
        foreach (var item in new[]
                 {
                     GeneralSection, LanguagesSection, HotkeysSection, ProvidersSection,
                     LocalSection, ResultSection, PrivacySection, AboutSection
                 })
        {
            item.Visibility = Visibility.Collapsed;
        }

        var selected = section switch
        {
            "Languages" => LanguagesSection,
            "Hotkeys" => HotkeysSection,
            "Providers" => ProvidersSection,
            "Local" => LocalSection,
            "Result" => ResultSection,
            "Privacy" => PrivacySection,
            "About" => AboutSection,
            _ => GeneralSection
        };
        selected.Visibility = Visibility.Visible;
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
        ProviderStatusText.Text = "Testing…";
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
            TestConnectionButton.IsEnabled = true;
            UpdateProviderFields();
        }
    }

    private async void RefreshLanguages_Click(object sender, RoutedEventArgs eventArgs) =>
        await RefreshCapabilitiesAsync(forceRefresh: true, saveFirst: true);

    private async Task RefreshCapabilitiesAsync(bool forceRefresh, bool saveFirst)
    {
        var provider = ProviderCombo.SelectedItem as string ?? TranslationProviderNames.None;
        if (provider == TranslationProviderNames.None)
        {
            ApplyLanguageTargets(LanguageCatalog.All);
            LanguageStatusText.Text = "Choose a provider to load its supported languages.";
            return;
        }

        try
        {
            LanguageStatusText.Text = "Loading provider languages…";
            if (saveFirst)
            {
                await SaveValuesAsync();
            }
            else if (!string.Equals(provider, _settings.Current.TranslationProvider.Provider, StringComparison.Ordinal))
            {
                LanguageStatusText.Text = "Save or refresh to load this provider's current language list.";
                return;
            }

            var capabilities = await _translation.GetLanguageCapabilitiesAsync(forceRefresh, CancellationToken.None);
            ApplyLanguageTargets(
                capabilities.TargetLanguages,
                allowEmpty: provider == TranslationProviderNames.Local);
            LanguageStatusText.Text = $"{_availableTargets.Count} target languages · {provider}";
        }
        catch (Exception exception)
        {
            ApplyCachedCapabilities(_settings.Current);
            LanguageStatusText.Text = $"Language refresh unavailable: {ProviderHttp.ToStatus(ProviderHttp.FromException(exception, CancellationToken.None))}";
        }
    }

    private void ApplyCachedCapabilities(AppSettings settings)
    {
        var provider = ProviderCombo.SelectedItem as string ?? settings.TranslationProvider.Provider;
        if (provider == TranslationProviderNames.Local)
        {
            var targets = _localModels.InstalledModels.Select(model => LanguageCatalog.Resolve(model.TargetLanguage));
            ApplyLanguageTargets(targets, allowEmpty: true);
            return;
        }

        if (settings.ProviderLanguageCache.TryGetValue(provider, out var cache) && cache.TargetLanguages.Count > 0)
        {
            ApplyLanguageTargets(cache.TargetLanguages.Select(language =>
                LanguageCatalog.Resolve(language.Code, language.DisplayName, language.NativeName)));
            return;
        }

        ApplyLanguageTargets(LanguageCatalog.All);
    }

    private void ApplyLanguageTargets(IEnumerable<AppLanguage> languages, bool allowEmpty = false)
    {
        var previous = new[]
        {
            GetLanguageCode(PreferredLanguageCombo, _settings.Current.PreferredLanguage),
            GetLanguageCode(QuickTarget1Combo, _settings.Current.QuickTarget1),
            GetLanguageCode(QuickTarget2Combo, _settings.Current.QuickTarget2)
        };
        var materialized = languages
            .GroupBy(language => LanguageCatalog.NormalizeCode(language.Code), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
        _availableTargets = materialized.Length == 0 && !allowEmpty ? LanguageCatalog.All : materialized;
        SetLanguageItems(PreferredLanguageCombo, previous[0]);
        SetLanguageItems(QuickTarget1Combo, previous[1]);
        SetLanguageItems(QuickTarget2Combo, previous[2]);
        UpdateHotkeyLabels();
    }

    private void SetLanguageItems(ComboBox combo, string code)
    {
        combo.ItemsSource = LanguageCatalog.OrderForPicker(_availableTargets, _settings.Current.RecentLanguages);
        combo.SelectedValue = code;
        if (combo.SelectedItem is null && combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }
    }

    private void LanguageCombo_KeyUp(object sender, KeyEventArgs eventArgs)
    {
        if (sender is not ComboBox combo || eventArgs.Key is Key.Up or Key.Down or Key.Enter or Key.Escape or Key.Tab)
        {
            return;
        }

        var query = combo.Text;
        combo.ItemsSource = LanguageCatalog.Search(_availableTargets, query, _settings.Current.RecentLanguages);
        combo.IsDropDownOpen = true;
        combo.Text = query;
    }

    private void LanguageCombo_DropDownOpened(object sender, EventArgs eventArgs)
    {
        if (sender is ComboBox combo && combo.SelectedItem is AppLanguage selected)
        {
            SetLanguageItems(combo, selected.Code);
        }
    }

    private void QuickLanguage_SelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
    {
        if (!_loading)
        {
            UpdateHotkeyLabels();
        }
    }

    private void UpdateHotkeyLabels()
    {
        Quick1HotkeyLabel.Text = $"Quick Target 1 — {GetSelectedLanguage(QuickTarget1Combo, AppLanguage.English).DisplayName}";
        Quick2HotkeyLabel.Text = $"Quick Target 2 — {GetSelectedLanguage(QuickTarget2Combo, AppLanguage.SimplifiedChinese).DisplayName}";
    }

    private void ProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
    {
        if (!IsInitialized)
        {
            return;
        }

        var selectedProvider = ProviderCombo.SelectedItem as string ?? TranslationProviderNames.None;
        if (!_loading && !string.Equals(_displayedProvider, selectedProvider, StringComparison.Ordinal))
        {
            ApiKeyBox.Password = string.Empty;
            EndpointBox.Clear();
            RegionBox.Clear();
            LangblyRegionCombo.SelectedItem = "Global";
        }

        _displayedProvider = selectedProvider;
        ProviderStatusText.Text = string.Empty;
        UpdateProviderFields();
        ApplyCachedCapabilities(_settings.Current);
        _ = UpdateCredentialHintAsync();
    }

    private void LangblyRegionCombo_SelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
    {
        if (!_loading)
        {
            UpdateProviderFields();
        }
    }

    private void UpdateProviderFields()
    {
        var provider = ProviderCombo.SelectedItem as string ?? TranslationProviderNames.None;
        var isAzure = provider == TranslationProviderNames.Azure;
        var isLangbly = provider == TranslationProviderNames.Langbly;
        var isLibre = provider == TranslationProviderNames.LibreTranslate;
        var isLocal = provider == TranslationProviderNames.Local;
        var hasProvider = provider != TranslationProviderNames.None;
        var customLangbly = isLangbly && string.Equals(
            LangblyRegionCombo.SelectedItem as string,
            "Custom",
            StringComparison.Ordinal);
        var showEndpoint = isAzure || provider == TranslationProviderNames.DeepL || isLibre || customLangbly;

        RegionLabel.Visibility = isAzure ? Visibility.Visible : Visibility.Collapsed;
        RegionBox.Visibility = isAzure ? Visibility.Visible : Visibility.Collapsed;
        LangblyRegionCombo.Visibility = isLangbly ? Visibility.Visible : Visibility.Collapsed;
        EndpointLabel.Visibility = showEndpoint ? Visibility.Visible : Visibility.Collapsed;
        EndpointBox.Visibility = showEndpoint ? Visibility.Visible : Visibility.Collapsed;
        ApiKeyLabel.Visibility = hasProvider && !isLocal ? Visibility.Visible : Visibility.Collapsed;
        ApiKeyBox.Visibility = hasProvider && !isLocal ? Visibility.Visible : Visibility.Collapsed;
        ApiKeyHintText.Visibility = hasProvider && !isLocal ? Visibility.Visible : Visibility.Collapsed;
        TestConnectionPanel.Visibility = hasProvider ? Visibility.Visible : Visibility.Collapsed;

        EndpointLabel.Text = customLangbly ? "Custom base endpoint" : isLibre ? "Translate endpoint" : "Endpoint (advanced)";
        ApiKeyLabel.Text = isLibre ? "API key (optional)" : "API key";
        ProviderDescriptionText.Text = provider switch
        {
            TranslationProviderNames.GoogleCloud => "Cloud Translation Basic v2. The official endpoint is fixed; only an API key is required.",
            TranslationProviderNames.Azure => "Azure Translator. Region is needed only when required by the Azure resource.",
            TranslationProviderNames.DeepL => "DeepL Free or Pro is selected from the key unless an advanced endpoint is supplied.",
            TranslationProviderNames.LibreTranslate => "A self-hosted or hosted LibreTranslate /translate endpoint.",
            TranslationProviderNames.TranslatePlus => "TranslatePlus v2 with its fixed official endpoint and X-API-KEY authentication.",
            TranslationProviderNames.Langbly => "Choose the managed Global or EU region. A URL is needed only for Custom.",
            TranslationProviderNames.Local => "Offline CTranslate2 translation using only models installed on this device.",
            _ => "Choose a translation provider."
        };
    }

    private void ResultModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs eventArgs) => UpdateResultFields();

    private void PopupSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs eventArgs) => UpdateResultFields();

    private void PopupPositionCombo_SelectionChanged(object sender, SelectionChangedEventArgs eventArgs) => UpdateResultFields();

    private void UpdateResultFields()
    {
        if (!IsInitialized)
        {
            return;
        }

        var mode = ResultModeCombo.SelectedValue is ResultWindowMode value ? value : ResultWindowMode.Popup;
        PopupOptionsPanel.Visibility = mode == ResultWindowMode.Pinned ? Visibility.Collapsed : Visibility.Visible;
        CustomSizePanel.Visibility = PopupSizeCombo.SelectedValue is PopupSizePreset.Custom
            ? Visibility.Visible
            : Visibility.Collapsed;
        CustomPositionPanel.Visibility = PopupPositionCombo.SelectedValue is PopupPositionMode.Custom
            ? Visibility.Visible
            : Visibility.Collapsed;
        ResultModeDescription.Text = mode switch
        {
            ResultWindowMode.Pinned => "One persistent, movable panel is updated for every translation.",
            ResultWindowMode.Hidden => "Successful quick replacements stay silent. Understand still shows a compact result.",
            _ => "A compact, non-activating result appears and hides automatically."
        };
    }

    private async void ModelAction_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is not Button { Tag: LocalModelManifestEntry model })
        {
            return;
        }

        _modelOperationCancellation = new CancellationTokenSource();
        SetModelBusy(true);
        try
        {
            if (_localModels.IsInstalled(model))
            {
                ModelStatusText.Text = $"Removing {model.DisplayName}…";
                await _localModels.RemoveAsync(model, _modelOperationCancellation.Token);
            }
            else
            {
                var progress = new Progress<ModelDownloadProgress>(value =>
                {
                    ModelProgressBar.Value = value.Percentage;
                    ModelStatusText.Text = $"Downloading {value.ItemName} · {value.Percentage:0}%";
                });
                await _localModels.InstallAsync(model, progress, _modelOperationCancellation.Token);
            }

            ModelStatusText.Text = $"{model.DisplayName} updated.";
        }
        catch (OperationCanceledException)
        {
            ModelStatusText.Text = "Download cancelled. Temporary files were cleaned.";
        }
        catch (Exception exception)
        {
            ModelStatusText.Text = $"Model operation failed ({exception.GetType().Name}).";
        }
        finally
        {
            _modelOperationCancellation.Dispose();
            _modelOperationCancellation = null;
            SetModelBusy(false);
            RefreshModelCards();
            if (ProviderCombo.SelectedItem as string == TranslationProviderNames.Local)
            {
                ApplyCachedCapabilities(_settings.Current);
            }
        }
    }

    private void CancelDownload_Click(object sender, RoutedEventArgs eventArgs) => _modelOperationCancellation?.Cancel();

    private void SetModelBusy(bool busy)
    {
        foreach (var button in ModelsPanel.Children.OfType<Border>()
                     .Select(border => border.Child)
                     .OfType<Grid>()
                     .SelectMany(grid => grid.Children.OfType<Button>()))
        {
            button.IsEnabled = !busy;
        }

        CancelDownloadButton.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        ModelProgressBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        if (!busy)
        {
            ModelProgressBar.Value = 0;
        }
    }

    private void RefreshModelCards()
    {
        ModelsPanel.Children.Clear();
        foreach (var model in LocalModelManifest.Models)
        {
            var installed = _localModels.IsInstalled(model);
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var details = new StackPanel();
            details.Children.Add(new TextBlock { Text = model.DisplayName, FontWeight = FontWeights.SemiBold });
            details.Children.Add(new TextBlock
            {
                Text = installed ? "Installed" : $"Not installed · {ToMegabytes(model.DownloadSize):0.0} MB",
                Foreground = (System.Windows.Media.Brush)FindResource("MutedBrush"),
                Margin = new Thickness(0, 3, 0, 0)
            });
            var action = new Button
            {
                Content = installed ? "Remove" : "Download",
                Tag = model,
                MinWidth = 86
            };
            action.Click += ModelAction_Click;
            Grid.SetColumn(action, 1);
            grid.Children.Add(details);
            grid.Children.Add(action);
            ModelsPanel.Children.Add(new Border
            {
                BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrush"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 8, 0, 8),
                Child = grid
            });
        }

        ModelStatusText.Text = $"Storage used: {ToMegabytes(_localModels.GetStorageSize()):0.0} MB";
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

        var preferred = GetSelectedLanguage(PreferredLanguageCombo, AppLanguage.Vietnamese);
        var quick1 = GetSelectedLanguage(QuickTarget1Combo, AppLanguage.English);
        var quick2 = GetSelectedLanguage(QuickTarget2Combo, AppLanguage.SimplifiedChinese);
        var provider = ProviderCombo.SelectedItem as string ?? TranslationProviderNames.None;
        var updated = _settings.Current.Clone();
        updated.PreferredLanguage = preferred.Code;
        updated.QuickTarget1 = quick1.Code;
        updated.QuickTarget2 = quick2.Code;
        updated.RecentLanguages = new[] { preferred.Code, quick1.Code, quick2.Code }
            .Concat(updated.RecentLanguages)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
        updated.StartWithWindows = StartWithWindowsCheckBox.IsChecked == true;
        updated.IsPaused = PausedCheckBox.IsChecked == true;
        updated.Hotkeys = hotkeys;
        updated.TranslationProvider = new ProviderConfiguration
        {
            Provider = provider,
            Endpoint = EndpointBox.Text.Trim(),
            Region = provider == TranslationProviderNames.Langbly
                ? LangblyRegionCombo.SelectedItem as string ?? "Global"
                : RegionBox.Text.Trim()
        };
        updated.ResultWindowMode = ResultModeCombo.SelectedValue is ResultWindowMode mode
            ? mode
            : ResultWindowMode.Popup;
        updated.PopupSizePreset = PopupSizeCombo.SelectedValue is PopupSizePreset preset
            ? preset
            : PopupSizePreset.Auto;
        updated.PopupPositionMode = PopupPositionCombo.SelectedValue is PopupPositionMode position
            ? position
            : PopupPositionMode.AutoNearSelection;
        updated.PopupWidth = ParseDimension(PopupWidthBox.Text, "Popup width", 300, 900);
        updated.PopupHeight = ParseDimension(PopupHeightBox.Text, "Popup height", 180, 720);
        updated.PopupCustomLeft = ParseOptionalNumber(PopupLeftBox.Text, "Popup left");
        updated.PopupCustomTop = ParseOptionalNumber(PopupTopBox.Text, "Popup top");

        if (!string.IsNullOrEmpty(ApiKeyBox.Password) && provider is not TranslationProviderNames.None and not TranslationProviderNames.Local)
        {
            await _credentials.SetAsync(TranslationService.GetCredentialName(provider), ApiKeyBox.Password);
            ApiKeyBox.Password = string.Empty;
        }

        _startup.SetEnabled(updated.StartWithWindows);
        await _settings.SaveAsync(updated);
        await UpdateCredentialHintAsync();
    }

    private async Task UpdateCredentialHintAsync()
    {
        var provider = ProviderCombo.SelectedItem as string ?? TranslationProviderNames.None;
        if (provider is TranslationProviderNames.None or TranslationProviderNames.Local)
        {
            ApiKeyHintText.Text = string.Empty;
            return;
        }

        try
        {
            var credential = await _credentials.GetAsync(TranslationService.GetCredentialName(provider));
            if (string.IsNullOrEmpty(credential) && provider == _legacyCredentialProvider && provider != TranslationProviderNames.GoogleCloud)
            {
                credential = await _credentials.GetAsync(TranslationService.LegacyApiKeyCredentialName);
            }

            if (string.Equals(provider, ProviderCombo.SelectedItem as string, StringComparison.Ordinal))
            {
                ApiKeyHintText.Text = string.IsNullOrEmpty(credential)
                    ? "No saved key."
                    : "A protected key is saved. Leave blank to keep it.";
            }
        }
        catch
        {
            ApiKeyHintText.Text = "The protected key store is unavailable.";
        }
    }

    private static AppLanguage GetSelectedLanguage(ComboBox combo, AppLanguage fallback)
    {
        if (combo.SelectedItem is AppLanguage selected)
        {
            return selected;
        }

        var query = combo.Text.Trim();
        var match = LanguageCatalog.All.FirstOrDefault(language =>
            string.Equals(language.Code, query, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(language.DisplayName, query, StringComparison.CurrentCultureIgnoreCase) ||
            string.Equals(language.PickerLabel, query, StringComparison.CurrentCultureIgnoreCase));
        return match ?? fallback;
    }

    private static string GetLanguageCode(ComboBox combo, string fallback) =>
        combo.SelectedItem is AppLanguage selected ? selected.Code : fallback;

    private static double ParseDimension(string value, string field, double minimum, double maximum)
    {
        if ((!double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsed) &&
             !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)) ||
            parsed < minimum || parsed > maximum)
        {
            throw new InvalidOperationException($"{field} must be between {minimum:0} and {maximum:0}.");
        }

        return parsed;
    }

    private static double? ParseOptionalNumber(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsed) ||
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"{field} must be a number or left blank.");
    }

    private static double ToMegabytes(long bytes) => bytes / 1024d / 1024d;

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
