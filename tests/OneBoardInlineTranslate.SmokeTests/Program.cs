using System.Runtime.InteropServices;
using System.Text.Json;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using OneBoardInlineTranslate.Diagnostics;
using OneBoardInlineTranslate.Infrastructure;
using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Services;
using OneBoardInlineTranslate.OCR;
using OneBoardInlineTranslate.Providers;
using OneBoardInlineTranslate.Security;

namespace OneBoardInlineTranslate.SmokeTests;

internal static class Program
{
    private static readonly List<(string Name, Func<Task> Test)> Tests =
    [
        ("Phase 0 exact transform", TestExactTransformAsync),
        ("Phase 0 Unicode and multiline preservation", TestUnicodeTransformAsync),
        ("Phase 0 rejects null", TestNullTransformAsync),
        ("Diagnostics schema is allow-listed", TestDiagnosticSchemaAsync),
        ("Diagnostics omit exception messages", TestDiagnosticRedactionAsync),
        ("Diagnostics remain local", TestLocalLoggerAsync),
        ("Failure results never carry captured text", TestFailureResultAsync),
        ("SendInput layout is x64-correct", TestNativeInputLayoutAsync),
        ("No Enter virtual key is declared", TestNoEnterKeyAsync)
        ,("Default hotkeys parse and remain unique", TestHotkeyGesturesAsync)
        ,("Single-instance guard rejects a duplicate", TestSingleInstanceAsync)
        ,("Settings persist and recover from corruption", TestSettingsAsync)
        ,("Credentials are DPAPI protected", TestCredentialsAsync)
        ,("Provider credentials migrate without cross-provider reuse", TestProviderCredentialIsolationAsync)
        ,("Language detection covers v1 languages", TestLanguageDetectionAsync)
        ,("Translation models redact content", TestTranslationModelRedactionAsync)
        ,("Fake-provider translation preserves content", TestFakeTranslationAsync)
        ,("Provider HTTP contracts use supported APIs", TestProviderContractsAsync)
        ,("Google translates Vietnamese to English", TestGoogleVietnameseToEnglishAsync)
        ,("Google translates English to Vietnamese", TestGoogleEnglishToVietnameseAsync)
        ,("Google translates Simplified Chinese to Vietnamese", TestGoogleChineseToVietnameseAsync)
        ,("Google auto-detects source in one request", TestGoogleAutoDetectionAsync)
        ,("Google preserves Unicode and multiline text", TestGoogleUnicodeAndMultilineAsync)
        ,("Google sends the API key only in its header", TestGoogleApiKeyHeaderAsync)
        ,("Google maps an invalid API key", TestGoogleInvalidKeyAsync)
        ,("Google maps quota and rate limits", TestGoogleQuotaAndRateLimitAsync)
        ,("Google maps provider timeouts", TestGoogleTimeoutAsync)
        ,("Google honors cancellation", TestGoogleCancellationAsync)
        ,("Google maps provider 5xx failures", TestGoogleServerFailureAsync)
        ,("Google rejects malformed responses safely", TestGoogleMalformedResponseAsync)
        ,("Provider connection failures use safe messages", TestProviderFailureMessagesAsync)
        ,("Overlay placement remains inside work areas", TestOverlayPositioningAsync)
        ,("Region coordinates normalize safely", TestScreenRegionAsync)
        ,("OCR image fixtures decode and recognize where installed", TestOcrFixturesAsync)
        ,("Fake-provider stress remains bounded", TestTranslationStressAsync)
    ];

    [STAThread]
    private static async Task<int> Main(string[] args)
    {
        if (args.Contains("--clipboard-integration", StringComparer.OrdinalIgnoreCase))
        {
            Tests.Add(("Transactional clipboard round trip", TestClipboardRoundTripAsync));
        }

        if (args.Contains("--end-to-end", StringComparer.OrdinalIgnoreCase))
        {
            Tests.Add(("End-to-end hotkeys, focus, clipboard, and no Enter", TestEndToEndAsync));
        }

        var failures = 0;
        foreach (var (name, test) in Tests)
        {
            try
            {
                await test();
                Console.WriteLine($"PASS  {name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL  {name}: {exception.GetType().Name} — {exception.Message}");
            }
        }

        Console.WriteLine($"{Tests.Count - failures}/{Tests.Count} smoke checks passed.");
        return failures == 0 ? 0 : 1;
    }

    private static Task TestExactTransformAsync()
    {
        Equal("[TEST] hello", PhaseZeroTransformer.Transform("hello"));
        return Task.CompletedTask;
    }

    private static Task TestUnicodeTransformAsync()
    {
        const string selected = "Xin chào Việt Nam\r\n简体中文";
        Equal("[TEST] " + selected, PhaseZeroTransformer.Transform(selected));
        return Task.CompletedTask;
    }

    private static Task TestNullTransformAsync()
    {
        Throws<ArgumentNullException>(() => PhaseZeroTransformer.Transform(null!));
        return Task.CompletedTask;
    }

    private static Task TestDiagnosticSchemaAsync()
    {
        var record = DiagnosticRecord.Create(
            OperationType.Understand,
            "google-cloud-translation",
            "chrome",
            CaptureMethod.UIA,
            4,
            10,
            3,
            17,
            true,
            null);
        using var document = JsonDocument.Parse(LocalDiagnosticLogger.Serialize(record));
        var propertyNames = document.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var expected = new[]
        {
            "captureLatencyMs", "captureMethod", "exceptionType", "operation", "outputLatencyMs",
            "process", "provider", "providerLatencyMs", "success", "timestamp", "totalLatencyMs"
        };
        Equal(string.Join('|', expected), string.Join('|', propertyNames));
        return Task.CompletedTask;
    }

    private static Task TestDiagnosticRedactionAsync()
    {
        const string sensitiveText = "selected text must never reach a log";
        var record = DiagnosticRecord.Create(
            OperationType.TranslateToEnglish,
            "google-cloud-translation",
            "ms-teams",
            CaptureMethod.Clipboard,
            5,
            12,
            4,
            21,
            false,
            new InvalidOperationException(sensitiveText));
        var json = LocalDiagnosticLogger.Serialize(record);
        False(json.Contains(sensitiveText, StringComparison.Ordinal));
        True(json.Contains(typeof(InvalidOperationException).FullName!, StringComparison.Ordinal));
        return Task.CompletedTask;
    }

    private static async Task TestLocalLoggerAsync()
    {
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            "OneBoardInlineTranslate.SmokeTests",
            Guid.NewGuid().ToString("N"));
        try
        {
            var logger = new LocalDiagnosticLogger(testDirectory);
            var written = await logger.WriteAsync(
                DiagnosticRecord.Create(
                    OperationType.Understand,
                    "test-only",
                    "outlook",
                    CaptureMethod.UIA,
                    2,
                    4,
                    2,
                    8,
                    true,
                    null),
                CancellationToken.None);
            True(written);
            var files = Directory.GetFiles(logger.LogDirectory, "*.jsonl");
            Equal(1, files.Length);
            var line = await File.ReadAllTextAsync(files[0]);
            True(line.Contains("outlook", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    private static Task TestFailureResultAsync()
    {
        var result = CaptureResult.Failed(12, new SelectionUnavailableException());
        False(result.Success);
        Equal(string.Empty, result.Text);
        return Task.CompletedTask;
    }

    private static Task TestNativeInputLayoutAsync()
    {
        True(Environment.Is64BitProcess);
        Equal(40, Marshal.SizeOf<NativeMethods.Input>());
        Equal(24, Marshal.SizeOf<NativeMethods.KeyboardInput>());
        return Task.CompletedTask;
    }

    private static Task TestNoEnterKeyAsync()
    {
        var declaredVirtualKeys = typeof(NativeMethods)
            .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Where(field => field.Name.StartsWith("Vk", StringComparison.Ordinal))
            .Where(field => field.FieldType == typeof(int))
            .Select(field => (int)field.GetRawConstantValue()!)
            .ToArray();
        False(declaredVirtualKeys.Contains(0x0D));
        return Task.CompletedTask;
    }

    private static Task TestHotkeyGesturesAsync()
    {
        var settings = new HotkeySettings();
        var parsed = settings.Enumerate()
            .Select(item =>
            {
                True(HotkeyGesture.TryParse(item.Gesture, out var gesture));
                return gesture!;
            })
            .ToArray();
        Equal(5, parsed.Length);
        Equal(5, parsed.Select(item => (item.Modifiers, item.VirtualKey)).Distinct().Count());
        False(HotkeyGesture.TryParse("Q", out _));
        False(HotkeyGesture.TryParse("Alt+F1", out _));
        return Task.CompletedTask;
    }

    private static Task TestSingleInstanceAsync()
    {
        var name = "Local\\OneBoardInlineTranslate.Tests." + Guid.NewGuid().ToString("N");
        using var first = SingleInstanceGuard.Acquire(name);
        using var second = SingleInstanceGuard.Acquire(name);
        True(first.OwnsInstance);
        False(second.OwnsInstance);
        return Task.CompletedTask;
    }

    private static async Task TestSettingsAsync()
    {
        var directory = NewTestDirectory();
        try
        {
            var service = new SettingsService(directory);
            var settings = new AppSettings
            {
                PreferredLanguage = "zh-Hans",
                StartWithWindows = true,
                Hotkeys = new HotkeySettings { Understand = "Ctrl+Shift+Q" },
                TranslationProvider = new ProviderConfiguration
                {
                    Provider = "Azure Translator",
                    Endpoint = "https://example.invalid",
                    Region = "test-region"
                }
            };
            await service.SaveAsync(settings);
            var loaded = await new SettingsService(directory).LoadAsync();
            Equal("zh-Hans", loaded.PreferredLanguage);
            Equal("Ctrl+Shift+Q", loaded.Hotkeys.Understand);
            Equal("Azure Translator", loaded.TranslationProvider.Provider);
            var json = await File.ReadAllTextAsync(service.SettingsPath);
            False(json.Contains("apiKey", StringComparison.OrdinalIgnoreCase));

            await File.WriteAllTextAsync(service.SettingsPath, "{ corrupt");
            var recovered = await service.LoadAsync();
            Equal("vi", recovered.PreferredLanguage);
            Equal("Alt+Q", recovered.Hotkeys.Understand);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task TestCredentialsAsync()
    {
        var directory = NewTestDirectory();
        const string secret = "ob-test-secret-never-log";
        try
        {
            var store = new DpapiCredentialStore(directory);
            await store.SetAsync("provider", secret);
            Equal(secret, await store.GetAsync("provider"));
            var encrypted = await File.ReadAllBytesAsync(Path.Combine(directory, "credentials.dat"));
            False(Encoding.UTF8.GetString(encrypted).Contains(secret, StringComparison.Ordinal));
            await store.RemoveAsync("provider");
            Equal<string?>(null, await store.GetAsync("provider"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task TestProviderCredentialIsolationAsync()
    {
        const string legacySecret = "legacy-provider-secret";
        var azureDirectory = NewTestDirectory();
        var googleDirectory = NewTestDirectory();
        try
        {
            var azureSettings = new SettingsService(azureDirectory);
            await azureSettings.SaveAsync(new AppSettings
            {
                TranslationProvider = new ProviderConfiguration
                {
                    Provider = TranslationProviderNames.Azure
                }
            });
            var azureCredentials = new DpapiCredentialStore(azureDirectory);
            await azureCredentials.SetAsync(TranslationService.LegacyApiKeyCredentialName, legacySecret);
            var azureHandler = new StubHttpMessageHandler(request =>
            {
                Equal(legacySecret, request.Headers.GetValues("Ocp-Apim-Subscription-Key").Single());
                return Task.FromResult(JsonResponse(
                    "[{\"detectedLanguage\":{\"language\":\"en\",\"score\":1},\"translations\":[{\"text\":\"Xin chào\",\"to\":\"vi\"}]}]"));
            });
            var azureService = new TranslationService(
                azureSettings,
                azureCredentials,
                new LanguageDetector(),
                new HttpClient(azureHandler));
            await azureService.TranslateAsync(new TranslationRequest
            {
                Text = "Hello",
                TargetLanguage = Language.Vietnamese
            }, CancellationToken.None);
            Equal(
                legacySecret,
                await azureCredentials.GetAsync(TranslationService.GetCredentialName(TranslationProviderNames.Azure)));
            Equal<string?>(null, await azureCredentials.GetAsync(TranslationService.LegacyApiKeyCredentialName));

            var googleSettings = new SettingsService(googleDirectory);
            await googleSettings.SaveAsync(new AppSettings
            {
                TranslationProvider = new ProviderConfiguration
                {
                    Provider = TranslationProviderNames.GoogleCloud
                }
            });
            var googleCredentials = new DpapiCredentialStore(googleDirectory);
            await googleCredentials.SetAsync(TranslationService.LegacyApiKeyCredentialName, legacySecret);
            var requestCount = 0;
            var googleHandler = new StubHttpMessageHandler(_ =>
            {
                requestCount++;
                return Task.FromResult(GoogleResponse("Xin chào", "en"));
            });
            var googleService = new TranslationService(
                googleSettings,
                googleCredentials,
                new LanguageDetector(),
                new HttpClient(googleHandler));
            var health = await googleService.TestProviderAsync(CancellationToken.None);
            Equal("Invalid API key", health.Status);
            Equal(0, requestCount);
            Equal<string?>(
                null,
                await googleCredentials.GetAsync(TranslationService.GetCredentialName(TranslationProviderNames.GoogleCloud)));
        }
        finally
        {
            Directory.Delete(azureDirectory, recursive: true);
            Directory.Delete(googleDirectory, recursive: true);
        }
    }

    private static Task TestLanguageDetectionAsync()
    {
        var detector = new LanguageDetector();
        Equal("vi", detector.Detect("Tôi sẽ kiểm tra vào ngày mai.").Code);
        Equal("zh-Hans", detector.Detect("你好，世界。").Code);
        Equal("en", detector.Detect("Hello world").Code);
        return Task.CompletedTask;
    }

    private static Task TestTranslationModelRedactionAsync()
    {
        const string sensitive = "private message body";
        var request = new TranslationRequest { Text = sensitive, TargetLanguage = Language.English };
        var result = new TranslationResult
        {
            Text = sensitive,
            SourceLanguage = Language.Vietnamese,
            TargetLanguage = Language.English,
            ProviderId = "fake"
        };
        False(request.ToString().Contains(sensitive, StringComparison.Ordinal));
        False(result.ToString().Contains(sensitive, StringComparison.Ordinal));
        return Task.CompletedTask;
    }

    private static async Task TestFakeTranslationAsync()
    {
        var provider = new DeterministicTranslationProvider();
        var samples = new[]
        {
            "Xin chào Việt Nam.",
            "第一行\r\n第二行。",
            "Punctuation: !? — emoji 🙂 URL https://example.com number 10.25"
        };
        foreach (var sample in samples)
        {
            var result = await provider.TranslateAsync(new TranslationRequest
            {
                Text = sample,
                TargetLanguage = Language.English
            }, CancellationToken.None);
            Equal("[TEST] " + sample, result.Text);
        }
    }

    private static async Task TestProviderContractsAsync()
    {
        var detector = new LanguageDetector();
        var azureHandler = new StubHttpMessageHandler(async request =>
        {
            Equal(HttpMethod.Post, request.Method);
            True(request.RequestUri!.Host.Contains("cognitive.microsofttranslator.com", StringComparison.Ordinal));
            True(request.Headers.Contains("Ocp-Apim-Subscription-Key"));
            True((await request.Content!.ReadAsStringAsync()).Contains("Hello", StringComparison.Ordinal));
            return JsonResponse("[{\"detectedLanguage\":{\"language\":\"en\",\"score\":1},\"translations\":[{\"text\":\"Xin chào\",\"to\":\"vi\"}]}]");
        });
        var azure = new AzureTranslatorProvider(new HttpClient(azureHandler), "key", "region", string.Empty, detector);
        var azureResult = await azure.TranslateAsync(new TranslationRequest
        {
            Text = "Hello",
            TargetLanguage = Language.Vietnamese
        }, CancellationToken.None);
        Equal("Xin chào", azureResult.Text);

        var deepLHandler = new StubHttpMessageHandler(async request =>
        {
            True(request.Headers.Authorization?.Scheme == "DeepL-Auth-Key");
            True((await request.Content!.ReadAsStringAsync()).Contains("target_lang=ZH-HANS", StringComparison.Ordinal));
            return JsonResponse("{\"translations\":[{\"detected_source_language\":\"EN\",\"text\":\"你好\"}]}");
        });
        var deepL = new DeepLTranslationProvider(new HttpClient(deepLHandler), "key", string.Empty, detector);
        var deepLResult = await deepL.TranslateAsync(new TranslationRequest
        {
            Text = "Hello",
            TargetLanguage = Language.SimplifiedChinese
        }, CancellationToken.None);
        Equal("你好", deepLResult.Text);

        var libreHandler = new StubHttpMessageHandler(async request =>
        {
            True(request.RequestUri!.IsLoopback);
            True((await request.Content!.ReadAsStringAsync()).Contains("\"target\":\"en\"", StringComparison.Ordinal));
            return JsonResponse("{\"translatedText\":\"Hello\",\"detectedLanguage\":{\"language\":\"vi\"}}");
        });
        var libre = new LibreTranslateProvider(
            new HttpClient(libreHandler),
            string.Empty,
            "http://localhost:5000/translate",
            detector);
        var libreResult = await libre.TranslateAsync(new TranslationRequest
        {
            Text = "Xin chào",
            TargetLanguage = Language.English
        }, CancellationToken.None);
        Equal("Hello", libreResult.Text);
    }

    private static Task TestGoogleVietnameseToEnglishAsync() => TestGoogleLanguagePairAsync(
        "Xin chào Việt Nam.",
        Language.Vietnamese,
        Language.English,
        "Hello Vietnam.",
        "vi",
        "en");

    private static Task TestGoogleEnglishToVietnameseAsync() => TestGoogleLanguagePairAsync(
        "Hello Vietnam.",
        Language.English,
        Language.Vietnamese,
        "Xin chào Việt Nam.",
        "en",
        "vi");

    private static Task TestGoogleChineseToVietnameseAsync() => TestGoogleLanguagePairAsync(
        "你好，越南。",
        Language.SimplifiedChinese,
        Language.Vietnamese,
        "Xin chào Việt Nam.",
        "zh-CN",
        "vi");

    private static async Task TestGoogleAutoDetectionAsync()
    {
        var requestCount = 0;
        var handler = new StubHttpMessageHandler(async request =>
        {
            requestCount++;
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            False(document.RootElement.TryGetProperty("source", out _));
            Equal("vi", document.RootElement.GetProperty("target").GetString());
            return GoogleResponse("Xin chào", "en");
        });
        var provider = CreateGoogleProvider(handler);
        var result = await provider.TranslateAsync(new TranslationRequest
        {
            Text = "Hello",
            TargetLanguage = Language.Vietnamese
        }, CancellationToken.None);

        Equal(1, requestCount);
        Equal(Language.English, result.SourceLanguage);
        Equal("Xin chào", result.Text);
    }

    private static async Task TestGoogleUnicodeAndMultilineAsync()
    {
        const string source = "Dòng một 🙂\r\n第二行 — https://example.com?q=1&x=2";
        const string translated = "Line one 🙂\r\nSecond line — https://example.com?q=1&x=2";
        var handler = new StubHttpMessageHandler(async request =>
        {
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Equal(source, document.RootElement.GetProperty("q").GetString());
            return GoogleResponse(translated, "vi");
        });
        var result = await CreateGoogleProvider(handler).TranslateAsync(new TranslationRequest
        {
            Text = source,
            SourceLanguage = Language.Vietnamese,
            TargetLanguage = Language.English
        }, CancellationToken.None);

        Equal(translated, result.Text);
    }

    private static async Task TestGoogleApiKeyHeaderAsync()
    {
        const string apiKey = "google-test-key-never-log";
        var handler = new StubHttpMessageHandler(request =>
        {
            Equal(HttpMethod.Post, request.Method);
            Equal(GoogleCloudTranslationProvider.Endpoint, request.RequestUri!.AbsoluteUri);
            Equal(apiKey, request.Headers.GetValues("X-Goog-Api-Key").Single());
            False(request.RequestUri.Query.Contains(apiKey, StringComparison.Ordinal));
            return Task.FromResult(GoogleResponse("Xin chào", "en"));
        });
        var provider = new GoogleCloudTranslationProvider(
            new HttpClient(handler),
            apiKey,
            new LanguageDetector());

        await provider.TranslateAsync(new TranslationRequest
        {
            Text = "Hello",
            TargetLanguage = Language.Vietnamese
        }, CancellationToken.None);
    }

    private static async Task TestGoogleInvalidKeyAsync()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(JsonResponse(
            "{\"error\":{\"code\":400,\"status\":\"INVALID_ARGUMENT\",\"errors\":[{\"reason\":\"keyInvalid\"}]}}",
            HttpStatusCode.BadRequest)));
        var provider = CreateGoogleProvider(handler);
        await ThrowsProviderFailureAsync(
            () => TranslateWithGoogleAsync(provider, CancellationToken.None),
            ProviderFailure.InvalidApiKey);
    }

    private static async Task TestGoogleQuotaAndRateLimitAsync()
    {
        var quotaHandler = new StubHttpMessageHandler(_ => Task.FromResult(JsonResponse(
            "{\"error\":{\"code\":403,\"errors\":[{\"reason\":\"dailyLimitExceeded\"}]}}",
            HttpStatusCode.Forbidden)));
        await ThrowsProviderFailureAsync(
            () => TranslateWithGoogleAsync(CreateGoogleProvider(quotaHandler), CancellationToken.None),
            ProviderFailure.QuotaExceeded);

        var rateHandler = new StubHttpMessageHandler(_ => Task.FromResult(JsonResponse(
            "{\"error\":{\"code\":429,\"errors\":[{\"reason\":\"userRateLimitExceeded\"}]}}",
            HttpStatusCode.TooManyRequests)));
        await ThrowsProviderFailureAsync(
            () => TranslateWithGoogleAsync(CreateGoogleProvider(rateHandler), CancellationToken.None),
            ProviderFailure.RateLimited);
    }

    private static async Task TestGoogleTimeoutAsync()
    {
        var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return GoogleResponse("unreachable", "en");
        });
        using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(30) };
        var provider = new GoogleCloudTranslationProvider(httpClient, "key", new LanguageDetector());
        var service = CreateTranslationService(provider, httpClient);
        var health = await service.TestProviderAsync(CancellationToken.None);

        False(health.IsHealthy);
        Equal("Timeout", health.Status);
    }

    private static async Task TestGoogleCancellationAsync()
    {
        var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return GoogleResponse("unreachable", "en");
        });
        var provider = CreateGoogleProvider(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await ThrowsAsync<OperationCanceledException>(
            () => TranslateWithGoogleAsync(provider, cancellation.Token));
    }

    private static async Task TestGoogleServerFailureAsync()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(JsonResponse(
            "{\"error\":{\"code\":503,\"status\":\"UNAVAILABLE\"}}",
            HttpStatusCode.ServiceUnavailable)));
        var provider = CreateGoogleProvider(handler);
        await ThrowsProviderFailureAsync(
            () => TranslateWithGoogleAsync(provider, CancellationToken.None),
            ProviderFailure.ProviderUnavailable);
    }

    private static async Task TestGoogleMalformedResponseAsync()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(JsonResponse("{\"data\":{}")));
        var provider = CreateGoogleProvider(handler);
        await ThrowsProviderFailureAsync(
            () => TranslateWithGoogleAsync(provider, CancellationToken.None),
            ProviderFailure.ProviderUnavailable);
    }

    private static Task TestProviderFailureMessagesAsync()
    {
        Equal("Invalid API key", ProviderHttp.ToStatus(ProviderHttp.FromStatusCode(HttpStatusCode.Unauthorized)));
        Equal("Permission denied", ProviderHttp.ToStatus(ProviderHttp.FromStatusCode(HttpStatusCode.Forbidden)));
        Equal("Rate limited", ProviderHttp.ToStatus(ProviderHttp.FromStatusCode(HttpStatusCode.TooManyRequests)));
        Equal("Provider unavailable", ProviderHttp.ToStatus(ProviderHttp.FromStatusCode(HttpStatusCode.BadGateway)));
        Equal(
            "Network unavailable",
            ProviderHttp.ToStatus(ProviderHttp.FromException(
                new HttpRequestException("sensitive transport detail"),
                CancellationToken.None)));

        try
        {
            ProviderHttp.ValidateEndpoint("http://provider.example/translate", "https://example.invalid");
        }
        catch (ProviderException exception)
        {
            Equal(ProviderFailure.InvalidEndpoint, exception.Failure);
            Equal("Invalid endpoint", exception.Message);
            return Task.CompletedTask;
        }

        throw new InvalidOperationException("Expected an invalid endpoint failure.");
    }

    private static async Task TestGoogleLanguagePairAsync(
        string sourceText,
        Language sourceLanguage,
        Language targetLanguage,
        string translatedText,
        string expectedSourceCode,
        string expectedTargetCode)
    {
        var handler = new StubHttpMessageHandler(async request =>
        {
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Equal(sourceText, document.RootElement.GetProperty("q").GetString());
            Equal(expectedSourceCode, document.RootElement.GetProperty("source").GetString());
            Equal(expectedTargetCode, document.RootElement.GetProperty("target").GetString());
            return GoogleResponse(translatedText, expectedSourceCode);
        });
        var result = await CreateGoogleProvider(handler).TranslateAsync(new TranslationRequest
        {
            Text = sourceText,
            SourceLanguage = sourceLanguage,
            TargetLanguage = targetLanguage
        }, CancellationToken.None);

        Equal(translatedText, result.Text);
        Equal(sourceLanguage, result.SourceLanguage);
        Equal(targetLanguage, result.TargetLanguage);
    }

    private static GoogleCloudTranslationProvider CreateGoogleProvider(HttpMessageHandler handler) =>
        new(new HttpClient(handler), "key", new LanguageDetector());

    private static Task<TranslationResult> TranslateWithGoogleAsync(
        GoogleCloudTranslationProvider provider,
        CancellationToken cancellationToken) =>
        provider.TranslateAsync(new TranslationRequest
        {
            Text = "Hello",
            TargetLanguage = Language.Vietnamese
        }, cancellationToken);

    private static TranslationService CreateTranslationService(
        ITranslationProvider provider,
        HttpClient httpClient) =>
        new(
            new SettingsService(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))),
            new StubCredentialStore(),
            new LanguageDetector(),
            httpClient,
            provider);

    private static async Task ThrowsProviderFailureAsync(Func<Task> action, ProviderFailure expected)
    {
        try
        {
            await action();
        }
        catch (ProviderException exception)
        {
            Equal(expected, exception.Failure);
            return;
        }

        throw new InvalidOperationException($"Expected provider failure '{expected}'.");
    }

    private static Task TestOverlayPositioningAsync()
    {
        var work = new PixelRect(-1920, 0, 0, 1080);
        var source = new PixelRect(-1900, 20, -100, 1000);
        var placed = OverlayPositioner.Place(source, work, 480, 360);
        True(placed.X >= work.Left + 16);
        True(placed.X + 480 <= work.Right - 16);
        True(placed.Y >= work.Top + 16);
        True(placed.Y + 360 <= work.Bottom - 16);
        return Task.CompletedTask;
    }

    private static Task TestScreenRegionAsync()
    {
        var region = ScreenRegion.FromPoints(500, 400, -100, 50);
        Equal(-100, region.X);
        Equal(50, region.Y);
        Equal(600, region.Width);
        Equal(350, region.Height);
        True(region.IsUsable);
        return Task.CompletedTask;
    }

    private static async Task TestOcrFixturesAsync()
    {
        var service = new WindowsOcrService();
        var fixtures = new[]
        {
            (Name: "english", Tag: "en", Expected: "oneboardocr2026"),
            (Name: "vietnamese", Tag: "vi", Expected: "xinchào"),
            (Name: "chinese", Tag: "zh-Hans", Expected: "你好")
        };

        foreach (var fixture in fixtures)
        {
            var image = LoadFixture(fixture.Name);
            True(image.PixelWidth > 0 && image.PixelHeight > 0);
            try
            {
                var text = await service.RecognizeAsync(image, fixture.Tag, CancellationToken.None);
                var normalized = new string(text.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
                True(normalized.Contains(fixture.Expected, StringComparison.OrdinalIgnoreCase),
                    $"OCR fixture '{fixture.Name}' produced an unexpected result.");
            }
            catch (OcrLanguageUnavailableException)
            {
                // Missing optional Windows language packs are an explicitly supported state.
            }
        }
    }

    private static async Task TestTranslationStressAsync()
    {
        var provider = new DeterministicTranslationProvider();
        var tasks = Enumerable.Range(0, 500).Select(index => provider.TranslateAsync(
            new TranslationRequest
            {
                Text = $"message-{index}",
                TargetLanguage = Language.SimplifiedChinese
            },
            CancellationToken.None));
        var results = await Task.WhenAll(tasks);
        Equal(500, results.Length);
        Equal(500, results.Select(result => result.Text).Distinct(StringComparer.Ordinal).Count());
    }

    private static System.Windows.Media.Imaging.BitmapSource LoadFixture(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", name + ".png.b64");
        var bytes = Convert.FromBase64String(File.ReadAllText(path).Trim());
        using var stream = new MemoryStream(bytes);
        var image = new System.Windows.Media.Imaging.BitmapImage();
        image.BeginInit();
        image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static HttpResponseMessage GoogleResponse(string translatedText, string detectedSourceLanguage) =>
        JsonResponse(JsonSerializer.Serialize(new
        {
            data = new
            {
                translations = new[]
                {
                    new { translatedText, detectedSourceLanguage }
                }
            }
        }));

    private static HttpResponseMessage JsonResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK) => new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static string NewTestDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "OneBoardInlineTranslate.SmokeTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static Task TestClipboardRoundTripAsync() => RunOnStaDispatcherAsync(async () =>
    {
        var beforeHadText = Clipboard.ContainsText(TextDataFormat.UnicodeText);
        var beforeText = beforeHadText
            ? Clipboard.GetText(TextDataFormat.UnicodeText)
            : null;

        const string controlledOriginal = "OB controlled original — Tiếng Việt — 中文";
        const string temporaryValue = "OB temporary value — không được giữ lại — 临时";
        const string controlledRtf = @"{\rtf1\ansi OneBoard controlled RTF}";
        const string controlledFormat = "OneBoardInlineTranslate.SmokeTests.Custom";

        await ClipboardTransaction.RunAsync(
            async outerTransaction =>
            {
                var controlledData = new DataObject();
                controlledData.SetText(controlledOriginal, TextDataFormat.UnicodeText);
                controlledData.SetData(DataFormats.Rtf, controlledRtf, autoConvert: false);
                controlledData.SetData(controlledFormat, new byte[] { 7, 11, 13 }, autoConvert: false);
                await outerTransaction.PutDataObjectAsync(controlledData, CancellationToken.None);
                True(
                    Clipboard.ContainsText(TextDataFormat.UnicodeText),
                    "Controlled original text format was not available.");
                True(string.Equals(
                    controlledOriginal,
                    Clipboard.GetText(TextDataFormat.UnicodeText),
                    StringComparison.Ordinal),
                    "Controlled original value was not installed.");

                await ClipboardTransaction.RunAsync(
                    async innerTransaction =>
                    {
                        await innerTransaction.PutUnicodeTextAsync(temporaryValue, CancellationToken.None);
                        True(string.Equals(
                            temporaryValue,
                            Clipboard.GetText(TextDataFormat.UnicodeText),
                            StringComparison.Ordinal),
                            "Inner temporary value was not installed.");
                        return true;
                    },
                    CancellationToken.None);

                True(string.Equals(
                    controlledOriginal,
                    Clipboard.GetText(TextDataFormat.UnicodeText),
                    StringComparison.Ordinal),
                    "Inner transaction did not restore the controlled original.");
                var restoredData = Clipboard.GetDataObject()
                    ?? throw new InvalidOperationException("Restored clipboard data object was unavailable.");
                True(
                    restoredData.GetDataPresent(DataFormats.Rtf, autoConvert: false),
                    "Inner transaction did not restore the RTF format.");
                True(
                    string.Equals(
                        restoredData.GetData(DataFormats.Rtf, autoConvert: false) as string,
                        controlledRtf,
                        StringComparison.Ordinal),
                    "Inner transaction did not restore the RTF value.");
                True(
                    restoredData.GetDataPresent(controlledFormat, autoConvert: false),
                    "Inner transaction did not restore the custom format.");
                True(
                    ClipboardValueMatchesBytes(
                        restoredData.GetData(controlledFormat, autoConvert: false),
                        new byte[] { 7, 11, 13 }),
                    "Inner transaction did not restore the custom-format value.");

                Clipboard.Clear();
                await ClipboardTransaction.RunAsync(
                    async emptyClipboardTransaction =>
                    {
                        await emptyClipboardTransaction.PutUnicodeTextAsync(
                            temporaryValue,
                            CancellationToken.None);
                        return true;
                    },
                    CancellationToken.None);
                var emptyDataObject = Clipboard.GetDataObject();
                True(
                    emptyDataObject is null ||
                    emptyDataObject.GetFormats(autoConvert: false).Length == 0,
                    "An originally empty clipboard was not restored to empty.");
                return true;
            },
            CancellationToken.None);

        var afterHadText = Clipboard.ContainsText(TextDataFormat.UnicodeText);
        True(
            beforeHadText == afterHadText,
            "Outer transaction changed Unicode-text format availability.");
        if (beforeHadText)
        {
            True(string.Equals(
                beforeText,
                Clipboard.GetText(TextDataFormat.UnicodeText),
                StringComparison.Ordinal),
                "Outer transaction did not restore the pre-test Unicode text.");
        }
    });

    private static Task TestEndToEndAsync() => RunOnStaDispatcherAsync(async () =>
    {
        var appPath = Path.Combine(AppContext.BaseDirectory, "OneBoardInlineTranslate.exe");
        True(File.Exists(appPath), "The OneBoard executable was not copied beside the smoke runner.");

        var editor = new TextBox
        {
            Text = "hello",
            FontSize = 24,
            Margin = new Thickness(24),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        var enterCount = 0;
        editor.PreviewKeyDown += (_, eventArgs) =>
        {
            if (eventArgs.Key is Key.Enter or Key.Return)
            {
                enterCount++;
            }
        };

        var targetWindow = new Window
        {
            Title = "OneBoard Phase 0 integration target",
            Width = 620,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = editor
        };
        Process? appProcess = null;

        try
        {
            targetWindow.Show();
            targetWindow.Activate();
            editor.Focus();
            Keyboard.Focus(editor);
            editor.SelectAll();
            await Task.Delay(100);

            appProcess = Process.Start(new ProcessStartInfo
            {
                FileName = appPath,
                Arguments = "--integration-test",
                UseShellExecute = false,
                WorkingDirectory = AppContext.BaseDirectory,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("The OneBoard process did not start.");
            await Task.Delay(700);
            True(!appProcess.HasExited, "The OneBoard process exited during startup.");

            const string clipboardCanary = "OB end-to-end clipboard canary — 中文 — Tiếng Việt";
            const string clipboardCanaryFormat = "OneBoardInlineTranslate.EndToEnd.Canary";
            await ClipboardTransaction.RunAsync(
                async outerTransaction =>
                {
                    var clipboardCanaryData = new DataObject();
                    clipboardCanaryData.SetText(clipboardCanary, TextDataFormat.UnicodeText);
                    clipboardCanaryData.SetData(
                        clipboardCanaryFormat,
                        new byte[] { 2, 3, 5, 7 },
                        autoConvert: false);
                    await outerTransaction.PutDataObjectAsync(
                        clipboardCanaryData,
                        CancellationToken.None);

                    await ActivateAndSelectAllAsync(targetWindow, editor, "before Alt+E");
                    var directUiaContext = new ForegroundWindowService().GetCurrent();
                    var directUiaText = await Task.Run(
                        () => new UiaSelectionReader().TryRead(directUiaContext));
                    Equal("hello", directUiaText);
                    SendAltHotkey(NativeMethods.VkE);
                    var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                    while (!string.Equals(editor.Text, "[TEST] hello", StringComparison.Ordinal) &&
                           DateTime.UtcNow < deadline)
                    {
                        await Task.Delay(25);
                    }

                    Equal("[TEST] hello", editor.Text);
                    VerifyTargetFocus(targetWindow, editor, "after Alt+E");
                    Equal(0, enterCount);
                    var clipboardRestored = await WaitForClipboardTextAsync(
                        clipboardCanary,
                        TimeSpan.FromSeconds(10));
                    var clipboardState = Clipboard.ContainsText(TextDataFormat.UnicodeText)
                        ? Clipboard.GetText(TextDataFormat.UnicodeText) switch
                        {
                            "[TEST] hello" => "temporary replacement",
                            "hello" => "captured selection",
                            _ => "other Unicode text"
                        }
                        : "no Unicode text";
                    var canaryFormatPresent = Clipboard.GetDataObject()?
                        .GetDataPresent(clipboardCanaryFormat, autoConvert: false) == true;
                    var canaryDebug = canaryFormatPresent &&
                                      Clipboard.ContainsText(TextDataFormat.UnicodeText)
                        ? $" expectedUtf16={Convert.ToHexString(System.Text.Encoding.Unicode.GetBytes(clipboardCanary))};" +
                          $" actualUtf16={Convert.ToHexString(System.Text.Encoding.Unicode.GetBytes(Clipboard.GetText(TextDataFormat.UnicodeText)))};"
                        : string.Empty;
                    True(
                        clipboardRestored,
                        $"Alt+E did not restore the clipboard canary; clipboard contains {clipboardState}; " +
                        $"canary format present={canaryFormatPresent}; app exited={appProcess.HasExited}, " +
                        $"responding={!appProcess.HasExited && appProcess.Responding};{canaryDebug}");

                    await ActivateAndSelectAllAsync(targetWindow, editor, "before Alt+Q");
                    SendAltHotkey(NativeMethods.VkQ);
                    await Task.Delay(2_500);

                    Equal("[TEST] hello", editor.Text);
                    VerifyTargetFocus(targetWindow, editor, "after Alt+Q");
                    True(
                        string.Equals(
                            clipboardCanary,
                            Clipboard.GetText(TextDataFormat.UnicodeText),
                            StringComparison.Ordinal),
                        "Alt+Q did not restore the clipboard canary.");

                    // Exercise the user-facing sequence independently: capture first, then replace.
                    editor.Text = "hello";
                    await ActivateAndSelectAllAsync(targetWindow, editor, "before sequential Alt+Q");
                    SendAltHotkey(NativeMethods.VkQ);
                    await Task.Delay(2_500);
                    Equal("hello", editor.Text);
                    VerifyTargetFocus(targetWindow, editor, "after sequential Alt+Q");

                    await ActivateAndSelectAllAsync(targetWindow, editor, "before sequential Alt+E");
                    SendAltHotkey(NativeMethods.VkE);
                    deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                    while (!string.Equals(editor.Text, "[TEST] hello", StringComparison.Ordinal) &&
                           DateTime.UtcNow < deadline)
                    {
                        await Task.Delay(25);
                    }

                    Equal("[TEST] hello", editor.Text);
                    VerifyTargetFocus(targetWindow, editor, "after sequential Alt+E");
                    True(
                        await WaitForClipboardTextAsync(clipboardCanary, TimeSpan.FromSeconds(5)),
                        "Sequential Alt+Q/Alt+E did not restore the clipboard canary.");
                    Equal(0, enterCount);

                    // Force the production clipboard fallback by focusing an editable control that
                    // deliberately exposes no UI Automation peer.
                    var clipboardOnlyEditor = new ClipboardOnlyTextBox
                    {
                        Text = "hello",
                        FontSize = 24,
                        Margin = new Thickness(24),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    clipboardOnlyEditor.PreviewKeyDown += (_, eventArgs) =>
                    {
                        if (eventArgs.Key is Key.Enter or Key.Return)
                        {
                            enterCount++;
                        }
                    };
                    targetWindow.Content = clipboardOnlyEditor;
                    targetWindow.UpdateLayout();
                    await Dispatcher.Yield(DispatcherPriority.Loaded);
                    await ActivateAndSelectAllAsync(
                        targetWindow,
                        clipboardOnlyEditor,
                        "before clipboard-fallback Alt+Q");
                    var fallbackContext = new ForegroundWindowService().GetCurrent();
                    var unavailableUiaText = await Task.Run(
                        () => new UiaSelectionReader().TryRead(fallbackContext));
                    True(
                        unavailableUiaText is null,
                        "The clipboard-only integration control unexpectedly exposed a UIA selection.");

                    SendAltHotkey(NativeMethods.VkQ);
                    await Task.Delay(2_500);
                    Equal("hello", clipboardOnlyEditor.Text);
                    VerifyTargetFocus(
                        targetWindow,
                        clipboardOnlyEditor,
                        "after clipboard-fallback Alt+Q");
                    True(
                        await WaitForClipboardTextAsync(clipboardCanary, TimeSpan.FromSeconds(5)),
                        "Clipboard-fallback Alt+Q did not restore the clipboard canary.");

                    await ActivateAndSelectAllAsync(
                        targetWindow,
                        clipboardOnlyEditor,
                        "before clipboard-fallback Alt+E");
                    SendAltHotkey(NativeMethods.VkE);
                    deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                    while (!string.Equals(
                               clipboardOnlyEditor.Text,
                               "[TEST] hello",
                               StringComparison.Ordinal) &&
                           DateTime.UtcNow < deadline)
                    {
                        await Task.Delay(25);
                    }

                    Equal("[TEST] hello", clipboardOnlyEditor.Text);
                    VerifyTargetFocus(
                        targetWindow,
                        clipboardOnlyEditor,
                        "after clipboard-fallback Alt+E");
                    True(
                        await WaitForClipboardTextAsync(clipboardCanary, TimeSpan.FromSeconds(5)),
                        "Clipboard-fallback Alt+E did not restore the clipboard canary.");
                    Equal(0, enterCount);
                    return true;
                },
                CancellationToken.None);
        }
        finally
        {
            targetWindow.Close();
            if (appProcess is not null)
            {
                try
                {
                    if (!appProcess.HasExited)
                    {
                        appProcess.Kill(entireProcessTree: true);
                        appProcess.WaitForExit(3_000);
                    }
                }
                finally
                {
                    appProcess.Dispose();
                }
            }
        }
    });

    private static async Task ActivateAndSelectAllAsync(
        Window targetWindow,
        TextBox editor,
        string stage)
    {
        var targetHandle = new WindowInteropHelper(targetWindow).Handle;
        targetWindow.Activate();
        ForceForegroundForTest(targetHandle);
        editor.Focus();
        Keyboard.Focus(editor);
        editor.SelectAll();
        await Task.Delay(100);
        VerifyTargetFocus(targetWindow, editor, stage);
    }

    private static void VerifyTargetFocus(Window targetWindow, TextBox editor, string stage)
    {
        var targetHandle = new WindowInteropHelper(targetWindow).Handle;
        True(targetWindow.IsActive, $"The integration target window is not active ({stage}).");
        True(editor.IsKeyboardFocusWithin, $"The integration editor lost keyboard focus ({stage}).");
        True(
            NativeMethods.GetForegroundWindow() == targetHandle,
            $"The integration target is not the foreground window ({stage}).");
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint windowHandle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BringWindowToTop(nint windowHandle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(uint attachThreadId, uint attachToThreadId, bool attach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    private static void ForceForegroundForTest(nint targetHandle)
    {
        var foregroundHandle = NativeMethods.GetForegroundWindow();
        var foregroundThread = foregroundHandle == nint.Zero
            ? 0
            : NativeMethods.GetWindowThreadProcessId(foregroundHandle, out _);
        var currentThread = GetCurrentThreadId();
        var attached = foregroundThread != 0 && foregroundThread != currentThread &&
            AttachThreadInput(currentThread, foregroundThread, attach: true);

        try
        {
            BringWindowToTop(targetHandle);
            SetForegroundWindow(targetHandle);
        }
        finally
        {
            if (attached)
            {
                AttachThreadInput(currentThread, foregroundThread, attach: false);
            }
        }
    }

    private static void SendAltHotkey(int virtualKey)
    {
        var inputs = new[]
        {
            NativeKey(NativeMethods.VkMenu, keyUp: false),
            NativeKey(virtualKey, keyUp: false),
            NativeKey(virtualKey, keyUp: true),
            NativeKey(NativeMethods.VkMenu, keyUp: true)
        };
        var sent = NativeMethods.SendInput(
            checked((uint)inputs.Length),
            inputs,
            Marshal.SizeOf<NativeMethods.Input>());
        if (sent != inputs.Length)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "The test hotkey was not injected.");
        }
    }

    private static NativeMethods.Input NativeKey(int virtualKey, bool keyUp) => new()
    {
        Type = NativeMethods.InputKeyboard,
        Data = new NativeMethods.InputUnion
        {
            Keyboard = new NativeMethods.KeyboardInput
            {
                VirtualKey = checked((ushort)virtualKey),
                Flags = keyUp ? NativeMethods.KeyeventfKeyup : 0
            }
        }
    };

    private static async Task<bool> WaitForClipboardTextAsync(string expected, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (Clipboard.ContainsText(TextDataFormat.UnicodeText) &&
                string.Equals(
                    expected,
                    Clipboard.GetText(TextDataFormat.UnicodeText),
                    StringComparison.Ordinal))
            {
                return true;
            }

            await Task.Delay(25);
        }

        return false;
    }

    private static Task RunOnStaDispatcherAsync(Func<Task> action)
    {
        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(dispatcher));
            dispatcher.BeginInvoke(async () =>
            {
                try
                {
                    await action();
                    completion.SetResult();
                }
                catch (Exception exception)
                {
                    completion.SetException(exception);
                }
                finally
                {
                    dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                }
            });
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "OneBoard clipboard integration test"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    private static bool ClipboardValueMatchesBytes(object? value, byte[] expected) => value switch
    {
        byte[] bytes => bytes.SequenceEqual(expected),
        MemoryStream memoryStream => memoryStream.ToArray().SequenceEqual(expected),
        _ => false
    };

    private static void True(bool condition, string message = "Expected true.")
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void False(bool condition) => True(!condition);

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }

    private static void Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private static async Task ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private sealed class ClipboardOnlyTextBox : TextBox
    {
        protected override AutomationPeer? OnCreateAutomationPeer() => null;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responseFactory;

        internal StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        {
            _responseFactory = (request, _) => responseFactory(request);
        }

        internal StubHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => _responseFactory(request, cancellationToken);
    }

    private sealed class StubCredentialStore : ICredentialStore
    {
        public Task<string?> GetAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task SetAsync(string name, string secret, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveAsync(string name, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
