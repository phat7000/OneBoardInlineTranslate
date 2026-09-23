using System.Net;
using System.Net.Http;
using System.Text.Json;
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
            throw new ProviderException(ProviderFailure.InvalidEndpoint);
        }

        var localHttp = uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;
        if (uri.Scheme != Uri.UriSchemeHttps && !localHttp)
        {
            throw new ProviderException(ProviderFailure.InvalidEndpoint);
        }

        return uri;
    }

    internal static Language ResolveLanguage(string? code, ILanguageDetector detector, string originalText) =>
        string.IsNullOrWhiteSpace(code)
            ? detector.Detect(originalText)
            : LanguageCatalog.Resolve(code);

    internal static IReadOnlyList<Language> DistinctLanguages(IEnumerable<Language> languages) => languages
        .Where(language => language.Code != "und" && language.Code != "auto")
        .GroupBy(language => language.Code, StringComparer.OrdinalIgnoreCase)
        .Select(group => group.First())
        .OrderBy(language => language.DisplayName, StringComparer.CurrentCultureIgnoreCase)
        .ToArray();

    internal static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        bool inspectGoogleError,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var failure = inspectGoogleError
            ? await ResolveGoogleFailureAsync(response, cancellationToken)
            : FromStatusCode(response.StatusCode);
        throw new ProviderException(failure);
    }

    internal static ProviderFailure FromStatusCode(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized => ProviderFailure.InvalidApiKey,
        HttpStatusCode.Forbidden => ProviderFailure.PermissionDenied,
        HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => ProviderFailure.Timeout,
        HttpStatusCode.TooManyRequests => ProviderFailure.RateLimited,
        HttpStatusCode.NotFound => ProviderFailure.InvalidEndpoint,
        >= HttpStatusCode.InternalServerError => ProviderFailure.ProviderUnavailable,
        _ => ProviderFailure.ProviderUnavailable
    };

    internal static ProviderFailure FromException(Exception exception, CancellationToken cancellationToken) =>
        exception switch
        {
            ProviderException providerException => providerException.Failure,
            OperationCanceledException when !cancellationToken.IsCancellationRequested => ProviderFailure.Timeout,
            HttpRequestException { StatusCode: { } statusCode } => FromStatusCode(statusCode),
            HttpRequestException => ProviderFailure.NetworkUnavailable,
            JsonException or KeyNotFoundException => ProviderFailure.ProviderUnavailable,
            _ => ProviderFailure.ProviderUnavailable
        };

    internal static string ToStatus(ProviderFailure failure) => failure switch
    {
        ProviderFailure.InvalidApiKey => "Invalid API key",
        ProviderFailure.PermissionDenied => "Permission denied",
        ProviderFailure.QuotaExceeded => "Quota exceeded",
        ProviderFailure.RateLimited => "Rate limited",
        ProviderFailure.Timeout => "Timeout",
        ProviderFailure.NetworkUnavailable => "Network unavailable",
        ProviderFailure.InvalidEndpoint => "Invalid endpoint",
        _ => "Provider unavailable"
    };

    private static async Task<ProviderFailure> ResolveGoogleFailureAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document.RootElement.TryGetProperty("error", out var error))
            {
                var signals = new List<string>();
                if (error.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String)
                {
                    signals.Add(status.GetString() ?? string.Empty);
                }

                if (error.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in errors.EnumerateArray())
                    {
                        if (item.TryGetProperty("reason", out var reason) && reason.ValueKind == JsonValueKind.String)
                        {
                            signals.Add(reason.GetString() ?? string.Empty);
                        }
                    }
                }

                var signal = string.Join('|', signals);
                if (signal.Contains("keyInvalid", StringComparison.OrdinalIgnoreCase) ||
                    signal.Contains("API_KEY_INVALID", StringComparison.OrdinalIgnoreCase))
                {
                    return ProviderFailure.InvalidApiKey;
                }

                if (signal.Contains("rate", StringComparison.OrdinalIgnoreCase))
                {
                    return ProviderFailure.RateLimited;
                }

                if (signal.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                    signal.Contains("dailyLimit", StringComparison.OrdinalIgnoreCase))
                {
                    return ProviderFailure.QuotaExceeded;
                }
            }
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            // Error bodies are never surfaced. Status-only mapping remains the safe fallback.
        }

        return FromStatusCode(response.StatusCode);
    }
}

internal enum ProviderFailure
{
    InvalidApiKey,
    PermissionDenied,
    QuotaExceeded,
    RateLimited,
    Timeout,
    NetworkUnavailable,
    ProviderUnavailable,
    InvalidEndpoint
}

internal sealed class ProviderException(ProviderFailure failure) : Exception(ProviderHttp.ToStatus(failure))
{
    internal ProviderFailure Failure { get; } = failure;
}
