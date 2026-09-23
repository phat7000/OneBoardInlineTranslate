using System.Windows;
using System.Windows.Input;
using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Services;
using AppLanguage = OneBoardInlineTranslate.Models.Language;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace OneBoardInlineTranslate.Views;

public partial class ReplyWindow : Window
{
    private readonly ForegroundContext _sourceContext;
    private readonly ITranslationService _translation;
    private readonly IClipboardService _clipboard;
    private readonly TextReplacementService _replacement;
    private readonly Language _preferredLanguage;
    private readonly ISettingsService _settings;
    private IReadOnlyList<Language> _availableTargets = LanguageCatalog.All;

    internal ReplyWindow(
        ForegroundContext sourceContext,
        string original,
        TranslationResult understanding,
        Language preferredLanguage,
        ITranslationService translation,
        IClipboardService clipboard,
        TextReplacementService replacement,
        ISettingsService settings)
    {
        InitializeComponent();
        _sourceContext = sourceContext;
        _preferredLanguage = preferredLanguage;
        _translation = translation;
        _clipboard = clipboard;
        _replacement = replacement;
        _settings = settings;
        OriginalText.Text = original;
        UnderstandingText.Text = understanding.Text;
        TargetLanguageCombo.ItemsSource = LanguageCatalog.OrderForPicker(_availableTargets, settings.Current.RecentLanguages);
        TargetLanguageCombo.SelectedValue = understanding.SourceLanguage.Code;
        Loaded += async (_, _) =>
        {
            await LoadCapabilitiesAsync(understanding.SourceLanguage.Code);
            ReplyInput.Focus();
        };
    }

    private async void Translate_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (string.IsNullOrWhiteSpace(ReplyInput.Text))
        {
            StatusText.Text = "Write a reply before translating.";
            return;
        }

        try
        {
            SetBusy(true, "Translating reply...");
            var target = TargetLanguageCombo.SelectedItem as Language ??
                LanguageCatalog.Find(TargetLanguageCombo.SelectedValue as string) ??
                understandingFallback();
            var result = await _translation.TranslateAsync(new TranslationRequest
            {
                Text = ReplyInput.Text,
                SourceLanguage = _preferredLanguage,
                TargetLanguage = target
            }, CancellationToken.None);
            TranslatedReply.Text = result.Text;
            await RememberLanguageAsync(target.Code);
            SetBusy(false, "Review the translation, then choose Insert or Copy.");
        }
        catch (Exception exception)
        {
            SetBusy(false, exception is UnsupportedProviderLanguageException
                ? exception.Message
                : $"Translation failed ({exception.GetType().Name}).");
        }

        Language understandingFallback() => _availableTargets.FirstOrDefault() ?? AppLanguage.English;
    }

    private async void Insert_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (string.IsNullOrWhiteSpace(TranslatedReply.Text))
        {
            StatusText.Text = "Translate the reply and review it before inserting.";
            return;
        }

        try
        {
            SetBusy(true, "Returning to the original window...");
            if (!await ForegroundWindowService.ActivateOriginalAsync(_sourceContext, CancellationToken.None))
            {
                await CopyFallbackAsync();
                return;
            }

            var result = await _replacement.ReplaceAsync(
                _sourceContext,
                TranslatedReply.Text,
                CancellationToken.None);
            if (!result.Success)
            {
                await CopyFallbackAsync();
                return;
            }

            Close();
        }
        catch
        {
            await CopyFallbackAsync();
        }
    }

    private async void Copy_Click(object sender, RoutedEventArgs eventArgs)
    {
        var text = string.IsNullOrWhiteSpace(TranslatedReply.Text) ? ReplyInput.Text : TranslatedReply.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            StatusText.Text = "Nothing to copy.";
            return;
        }

        try
        {
            await _clipboard.CopyTextAsync(text, CancellationToken.None);
            StatusText.Text = "Copied. OneBoard did not send anything.";
        }
        catch
        {
            StatusText.Text = "The clipboard is unavailable.";
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs eventArgs) => Close();

    private async Task CopyFallbackAsync()
    {
        try
        {
            await _clipboard.CopyTextAsync(TranslatedReply.Text, CancellationToken.None);
            SetBusy(false, "The original destination could not be verified. The reply was copied instead and was not inserted or sent.");
        }
        catch
        {
            SetBusy(false, "The destination and clipboard are unavailable. Nothing was inserted or sent.");
        }
    }

    private void SetBusy(bool busy, string status)
    {
        ReplyInput.IsEnabled = !busy;
        TargetLanguageCombo.IsEnabled = !busy;
        StatusText.Text = status;
    }

    private async Task LoadCapabilitiesAsync(string selectedCode)
    {
        try
        {
            var capabilities = await _translation.GetLanguageCapabilitiesAsync(false, CancellationToken.None);
            _availableTargets = capabilities.TargetLanguages.Count > 0
                ? capabilities.TargetLanguages
                : LanguageCatalog.All;
        }
        catch
        {
            _availableTargets = LanguageCatalog.All;
        }

        SetLanguageItems(selectedCode);
        if (TargetLanguageCombo.SelectedItem is null)
        {
            StatusText.Text = "The detected incoming language is unavailable with the current provider. Choose another target.";
        }
    }

    private void TargetLanguageCombo_KeyUp(object sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key is Key.Up or Key.Down or Key.Enter or Key.Escape or Key.Tab)
        {
            return;
        }

        var query = TargetLanguageCombo.Text;
        TargetLanguageCombo.ItemsSource = LanguageCatalog.Search(
            _availableTargets,
            query,
            _settings.Current.RecentLanguages);
        TargetLanguageCombo.IsDropDownOpen = true;
        TargetLanguageCombo.Text = query;
    }

    private void TargetLanguageCombo_DropDownOpened(object sender, EventArgs eventArgs)
    {
        if (TargetLanguageCombo.SelectedItem is Language selected)
        {
            SetLanguageItems(selected.Code);
        }
    }

    private void SetLanguageItems(string selectedCode)
    {
        TargetLanguageCombo.ItemsSource = LanguageCatalog.OrderForPicker(
            _availableTargets,
            _settings.Current.RecentLanguages);
        TargetLanguageCombo.SelectedValue = selectedCode;
    }

    private async Task RememberLanguageAsync(string code)
    {
        var updated = _settings.Current.Clone();
        updated.RecentLanguages = new[] { LanguageCatalog.NormalizeCode(code) }
            .Concat(updated.RecentLanguages)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
        await _settings.SaveAsync(updated);
    }
}
