using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Services;

namespace OneBoardInlineTranslate.Providers;

internal static class ProviderHttp
{
    internal static Uri ValidateEndpoint(string endpoint, string defaultEndpoint)
    {
        var value = string.IsNullOrWhiteSpace(endpoint) ? defaultEndpoint : endpoint.Trim();
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("The configured provider endpoint is not a valid absolute URL.");
        }

        var localHttp = uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;
        if (uri.Scheme != Uri.UriSchemeHttps && !localHttp)
        {
            throw new InvalidOperationException("Provider endpoints must use HTTPS; HTTP is allowed only for loopback LibreTranslate instances.");
        }

        return uri;
    }

    internal static Language ResolveLanguage(string? code, ILanguageDetector detector, string originalText) =>
        string.IsNullOrWhiteSpace(code)
            ? detector.Detect(originalText)
            : code.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? Language.SimplifiedChinese
                : Language.FromCode(code);
}
