using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using Windows.Globalization;

namespace OneBoardInlineTranslate.OCR;

internal interface IOcrService
{
    Task<string> RecognizeAsync(BitmapSource image, CancellationToken cancellationToken);
}

internal sealed class WindowsOcrService : IOcrService
{
    public async Task<string> RecognizeAsync(BitmapSource image, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(image);
        cancellationToken.ThrowIfCancellationRequested();
        var engines = new List<OcrEngine>();
        var profileEngine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (profileEngine is not null)
        {
            engines.Add(profileEngine);
        }

        foreach (var tag in new[] { "en", "vi", "zh-Hans" })
        {
            var available = OcrEngine.AvailableRecognizerLanguages.FirstOrDefault(
                language => language.LanguageTag.StartsWith(tag, StringComparison.OrdinalIgnoreCase));
            if (available is not null)
            {
                var engine = OcrEngine.TryCreateFromLanguage(available);
                if (engine is not null && engines.All(existing => existing.RecognizerLanguage.LanguageTag != engine.RecognizerLanguage.LanguageTag))
                {
                    engines.Add(engine);
                }
            }
        }

        if (engines.Count == 0)
        {
            throw new InvalidOperationException(
                "Windows OCR is unavailable. Install an English, Vietnamese, or Simplified Chinese OCR language pack in Windows Settings.");
        }

        using var randomAccessStream = new InMemoryRandomAccessStream();
        using var output = randomAccessStream.AsStreamForWrite();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(FitForOcr(image)));
        encoder.Save(output);
        await output.FlushAsync(cancellationToken);

        randomAccessStream.Seek(0);
        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(randomAccessStream);
        using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);
        cancellationToken.ThrowIfCancellationRequested();
        var candidates = new List<string>();
        foreach (var engine in engines)
        {
            var result = await engine.RecognizeAsync(softwareBitmap);
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                candidates.Add(result.Text.Trim());
            }
        }

        var text = candidates.OrderByDescending(candidate => candidate.Count(char.IsLetterOrDigit)).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Windows OCR did not find readable text in the selected region.");
        }

        return text;
    }

    internal async Task<string> RecognizeAsync(
        BitmapSource image,
        string languageTag,
        CancellationToken cancellationToken)
    {
        var language = OcrEngine.AvailableRecognizerLanguages.FirstOrDefault(
            candidate => candidate.LanguageTag.StartsWith(languageTag, StringComparison.OrdinalIgnoreCase));
        var engine = language is null ? null : OcrEngine.TryCreateFromLanguage(language);
        if (engine is null)
        {
            throw new OcrLanguageUnavailableException(languageTag);
        }

        using var softwareBitmap = await ToSoftwareBitmapAsync(image, cancellationToken);
        var result = await engine.RecognizeAsync(softwareBitmap);
        return result.Text?.Trim() ?? string.Empty;
    }

    private static async Task<SoftwareBitmap> ToSoftwareBitmapAsync(
        BitmapSource image,
        CancellationToken cancellationToken)
    {
        using var randomAccessStream = new InMemoryRandomAccessStream();
        using var output = randomAccessStream.AsStreamForWrite();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(FitForOcr(image)));
        encoder.Save(output);
        await output.FlushAsync(cancellationToken);

        randomAccessStream.Seek(0);
        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(randomAccessStream);
        return await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    }

    private static BitmapSource FitForOcr(BitmapSource image)
    {
        var longestSide = Math.Max(image.PixelWidth, image.PixelHeight);
        if (longestSide <= OcrEngine.MaxImageDimension)
        {
            return image;
        }

        var scale = (double)OcrEngine.MaxImageDimension / longestSide;
        var transformed = new TransformedBitmap(image, new ScaleTransform(scale, scale));
        transformed.Freeze();
        return transformed;
    }
}

internal sealed class OcrLanguageUnavailableException(string languageTag) : Exception(
    $"The Windows OCR language pack '{languageTag}' is not installed.");
