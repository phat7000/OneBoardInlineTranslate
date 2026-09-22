using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Views;

namespace OneBoardInlineTranslate.Services;

internal sealed class ReplyService(
    ITranslationService translation,
    IClipboardService clipboard,
    TextReplacementService replacement)
{
    internal void Open(
        ForegroundContext context,
        string original,
        TranslationResult understanding,
        Language preferredLanguage)
    {
        var window = new ReplyWindow(
            context,
            original,
            understanding,
            preferredLanguage,
            translation,
            clipboard,
            replacement);
        window.Show();
        window.Activate();
    }
}
