# OneBoard Inline Translate — Phase 0

Windows-only technical proof of concept for capturing selected text and replacing that selection across desktop applications. Phase 0 deliberately performs no translation: selecting `hello` and pressing **Alt+E** produces `[TEST] hello`.

The implementation is complete and builds cleanly, but the cross-application PASS gate is not claimed until every manual case in [TEST_MATRIX.md](TEST_MATRIX.md) has been executed on the target machines and app versions.

## Scope

- C#, .NET 10 LTS, WPF, x64
- Global **Alt+Q** capture hotkey
- Global **Alt+E** test-replacement hotkey
- UI Automation first, transactional clipboard fallback second
- Non-activating, topmost overlay
- Local metadata-only JSON Lines diagnostics
- No database, telemetry, installer, network calls, translation APIs, AI, OCR, login, cloud backend, or auto-update

## Prerequisites

- Windows 10 or Windows 11, x64
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) to build and run from source
- Target application running at the same Windows integrity level as this process. An unelevated process cannot reliably inject input into an elevated application because of UIPI.

## Build and verify

From PowerShell at the repository root:

```powershell
.\scripts\Build-Phase0.ps1
```

The script creates a Release x64 build and runs the dependency-free smoke-check executable. The equivalent commands are:

```powershell
dotnet build .\OneBoardInlineTranslate.sln -c Release -p:Platform=x64
dotnet run --project .\tests\OneBoardInlineTranslate.SmokeTests\OneBoardInlineTranslate.SmokeTests.csproj -c Release -p:Platform=x64 --no-build
```

An opt-in integration check performs nested temporary clipboard writes on an STA dispatcher. It verifies Unicode text, RTF, a custom binary format, an originally empty clipboard, and restoration of the user's pre-test Unicode text. It does not run in the normal build because exercising global clipboard ownership should be explicit:

```powershell
dotnet run --project .\tests\OneBoardInlineTranslate.SmokeTests\OneBoardInlineTranslate.SmokeTests.csproj -c Release -p:Platform=x64 --no-build -- --clipboard-integration
```

A second opt-in check launches the real WPF executable and a separate editable WPF target. It exercises both UIA and a deliberately forced clipboard fallback, including the Alt+Q→Alt+E sequence, then verifies `[TEST] hello`, foreground/focus preservation, clipboard restoration, and zero Enter events:

```powershell
dotnet run --project .\tests\OneBoardInlineTranslate.SmokeTests\OneBoardInlineTranslate.SmokeTests.csproj -c Release -p:Platform=x64 --no-build -- --end-to-end
```

## Run

```powershell
.\scripts\Run-Phase0.ps1
```

The application runs without a taskbar window. Keep the launching PowerShell open; press **Ctrl+C** there to stop it. If either global hotkey is already registered by another program, startup fails with a visible error instead of silently running without that hotkey.

### Alt+Q — capture

1. Select non-empty text in the source application.
2. Press and release **Alt+Q**.
3. A six-second overlay shows the foreground process, captured text, `UIA` or `Clipboard`, and elapsed milliseconds.

### Alt+E — Phase 0 replacement

1. Select text in an editable field.
2. Press and release **Alt+E**.
3. The selected text is replaced with the same text prefixed by `[TEST] `.

The program waits for the Alt hotkey keys to be released before injecting input. It emits only Ctrl+C or Ctrl+V. It does not synthesize Enter and does not invoke application send/submit controls.

## Clipboard and focus safety

Before fallback capture or replacement, the program eagerly materializes every advertised clipboard format into an in-process snapshot. It temporarily supplies a sentinel or Unicode replacement value, performs Ctrl+C or Ctrl+V, then restores that snapshot with a persisted clipboard write. Clipboard access and restoration use bounded retries; restoration is attempted even when the operation fails or is cancelled. If any advertised format cannot be materialized safely, the operation aborts before touching the clipboard.

Temporary values carry Windows' [`ExcludeClipboardContentFromMonitorProcessing`](https://learn.microsoft.com/windows/win32/dataxchg/clipboard-formats#cloud-clipboard-and-clipboard-history-formats) registered format, which opts the whole temporary item out of clipboard history and cloud synchronization. The current clipboard content is restored, but third-party clipboard managers can choose to ignore that Windows convention; this is a manual-test item.

The foreground window handle is captured at hotkey time and rechecked immediately before every synthetic chord. If the user changes windows during the operation, the operation aborts rather than copying from or pasting into a different app. The overlay uses `WS_EX_NOACTIVATE`, `WS_EX_TOOLWINDOW`, `WS_EX_TRANSPARENT`, `ShowActivated=false`, and `SWP_NOACTIVATE` so it cannot take keyboard focus.

## Diagnostics and privacy

Logs stay on the local machine at:

```text
%LOCALAPPDATA%\OneBoardInlineTranslate\logs\phase0-YYYYMMDD.jsonl
```

Each record contains exactly:

- `timestamp`
- `process`
- `captureMethod`
- `success`
- `latencyMs`
- `exceptionType`

Captured text, transformed text, exception messages, and stack traces are never logged. There is no telemetry or network dependency.

## Validation

- Automated smoke checks cover transformation, Unicode/multiline preservation, diagnostic schema/redaction, local-only file output, x64 native input layout, and the no-Enter key invariant.
- Manual validation for Teams Desktop, Outlook Desktop, Zalo Desktop, Chrome, and Edge is defined in [TEST_MATRIX.md](TEST_MATRIX.md).
- Design details and safety invariants are in [ARCHITECTURE.md](ARCHITECTURE.md).
- Current completion state is in [PHASE_STATUS.md](PHASE_STATUS.md).

## Expected limitations of the proof of concept

- Password/secure fields commonly refuse selection exposure and copy by design.
- Elevated target apps require this proof of concept to run at the same integrity level; the application intentionally requests only `asInvoker` privileges.
- Enterprise policies or endpoint tools can block UI Automation, clipboard access, global hotkeys, or synthetic input.
- A target app can reserve Alt+Q or Alt+E before this application starts.
- Replacement uses clipboard paste because standard UI Automation text ranges are read-only and `ValuePattern.SetValue` would replace an entire control rather than the selected range.
- This phase has no tray UI or installer. Run it from PowerShell and stop it with Ctrl+C.

Phase 0 ends here. No later-phase translation or product functionality is included.
