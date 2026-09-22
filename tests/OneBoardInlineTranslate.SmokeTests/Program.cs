using System.Runtime.InteropServices;
using System.Text.Json;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
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
        var record = DiagnosticRecord.Create("chrome", CaptureMethod.UIA, true, 17, null);
        using var document = JsonDocument.Parse(LocalDiagnosticLogger.Serialize(record));
        var propertyNames = document.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var expected = new[]
        {
            "captureMethod", "exceptionType", "latencyMs", "process", "success", "timestamp"
        };
        Equal(string.Join('|', expected), string.Join('|', propertyNames));
        return Task.CompletedTask;
    }

    private static Task TestDiagnosticRedactionAsync()
    {
        const string sensitiveText = "selected text must never reach a log";
        var record = DiagnosticRecord.Create(
            "ms-teams",
            CaptureMethod.Clipboard,
            false,
            21,
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
                DiagnosticRecord.Create("outlook", CaptureMethod.UIA, true, 8, null),
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

    private sealed class ClipboardOnlyTextBox : TextBox
    {
        protected override AutomationPeer? OnCreateAutomationPeer() => null;
    }
}
