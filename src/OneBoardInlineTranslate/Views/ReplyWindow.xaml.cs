using System.Windows;
using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Services;
using AppLanguage = OneBoardInlineTranslate.Models.Language;

namespace OneBoardInlineTranslate.Views;

public partial class ReplyWindow : Window
{
    private readonly ForegroundContext _sourceContext;
    private readonly ITranslationService _translation;
    private readonly IClipboardService _clipboard;
    private readonly TextReplacementService _replacement;
    private readonly Language _preferredLanguage;

    internal ReplyWindow(
        ForegroundContext sourceContext,
        string original,
        TranslationResult understanding,
        Language preferredLanguage,
        ITranslationService translation,
        IClipboardService clipboard,
        TextReplacementService replacement)
    {
        InitializeComponent();
        _sourceContext = sourceContext;
        _preferredLanguage = preferredLanguage;
        _translation = translation;
        _clipboard = clipboard;
        _replacement = replacement;
        OriginalText.Text = original;
        UnderstandingText.Text = understanding.Text;
        TargetLanguageCombo.ItemsSource = AppLanguage.Supported;
        TargetLanguageCombo.SelectedValue = understanding.SourceLanguage.Code;
        Loaded += (_, _) => ReplyInput.Focus();
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
            var target = AppLanguage.FromCode(TargetLanguageCombo.SelectedValue as string);
            var result = await _translation.TranslateAsync(new TranslationRequest
            {
                Text = ReplyInput.Text,
                SourceLanguage = _preferredLanguage,
                TargetLanguage = target
            }, CancellationToken.None);
            TranslatedReply.Text = result.Text;
            SetBusy(false, "Review the translation, then choose Insert or Copy.");
        }
        catch (Exception exception)
        {
            SetBusy(false, $"Translation failed ({exception.GetType().Name}).");
        }
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
}
