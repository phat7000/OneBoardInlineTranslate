# OneBoard Inline Translate

OneBoard Inline Translate is a lightweight Windows utility for understanding and composing multilingual text without leaving the application you are using. Select text in Teams, Outlook, Zalo, a browser, or another Windows app, then use a global hotkey to translate, replace, reply, or capture an on-screen region.

Version 1.1.0 supports Vietnamese, English, and Simplified Chinese. The human always performs the final send action: OneBoard never presses Enter, clicks Send, or submits a form.

> Unsigned build - Windows SmartScreen may show a warning.

## Key features

- UI Automation-first selected-text capture with a transactional clipboard fallback.
- Exact clipboard restoration, including multiple clipboard formats.
- Cloud Clipboard and Clipboard History exclusion for temporary clipboard data.
- Non-activating compact translation overlay.
- One-action translation and selected-text replacement.
- Reply Mode with translation preview and explicit Insert/Copy controls.
- Local Windows OCR for text that cannot be selected.
- Google Cloud Translation, Azure Translator, DeepL, and LibreTranslate-compatible providers.
- API keys protected for the current Windows user with DPAPI.
- Metadata-only capture, provider, output, and total latency diagnostics.
- No account, backend, analytics, telemetry, or translation-history database.

## Default hotkeys

| Hotkey | Action |
|---|---|
| `Alt+Q` | Understand selected text in your preferred language |
| `Alt+E` | Translate selected text to English and replace the selection |
| `Alt+C` | Translate selected text to Simplified Chinese and replace the selection |
| `Alt+R` | Open Reply Mode for the selected incoming message |
| `Alt+Shift+Q` | Select a screen region, OCR it locally, then translate the extracted text |

Hotkeys are configurable in Settings. A collision disables only the unavailable binding; the rest of the app continues running.

## Screenshots

Screenshots will be added to the GitHub release page. The app uses a compact white Windows 11-inspired overlay and a small tabbed Settings window rather than a dashboard.

## Install or run portable

### Portable

1. Download `OneBoardInlineTranslate-1.1.0-win-x64.zip` from Releases.
2. Extract the archive to a folder you control.
3. Run `OneBoardInlineTranslate.exe`.
4. Open the notification-area icon and choose **Open Settings**.

The portable build is self-contained and does not require a separate .NET installation.

### Installer

Run `OneBoardInlineTranslate-Setup-1.1.0-win-x64.exe`. Installation is per-user, needs no administrator rights, creates a Start Menu shortcut, and offers an optional desktop shortcut. User settings under `%LOCALAPPDATA%\OneBoardInlineTranslate` are preserved when the program is uninstalled.

## Configure a translation provider

Open **Settings → Providers**, choose a provider, enter only the fields shown for that provider, and choose **Test connection**, then **Save**.

- **Google Cloud Translation:** enable the Cloud Translation API in a billed Google Cloud project, create an API key, and restrict that key to the Cloud Translation API. OneBoard uses the official Cloud Translation Basic v2 endpoint and sends the key in `X-Goog-Api-Key`, never in the URL.
- **Azure Translator:** API key, optional Azure region, and optionally a custom resource endpoint. The public Translator endpoint is used when Endpoint is blank.
- **DeepL:** API key and optionally a custom supported API endpoint. The Free endpoint is chosen automatically for keys ending in `:fx`.
- **LibreTranslate:** HTTPS endpoint and optional API key. Plain HTTP is accepted only for a loopback service such as `http://localhost:5000/translate`.

OneBoard uses supported HTTP APIs and does not scrape translation websites. API keys are protected with Windows DPAPI for the current user and are not written to `settings.json`. A successful provider check reports `Connected · Provider name · latency ms`. See [Provider setup](docs/PROVIDERS.md) for request, quota, and privacy details.

## Workflows

### Understand selected text

Select text and press `Alt+Q`. OneBoard detects the source language, sends the text to your configured translation provider, and displays the original and translation in a non-activating overlay.

### Translate and replace

Select editable text and press `Alt+E` or `Alt+C`. OneBoard captures and translates the selection, revalidates the original foreground window, pastes only the translated selection, restores your clipboard, and does not send.

### Reply Mode

Select an incoming message and press `Alt+R`. Review the preferred-language understanding, write your reply, translate it back to the detected incoming language, and inspect the final result. **Insert** revalidates and returns to the original window; when that cannot be done safely, the reply is copied instead. Nothing is sent automatically.

### OCR region translation

Press `Alt+Shift+Q`, drag around on-screen text, and release. Escape cancels. The selected pixels are captured in memory, recognized by Windows OCR locally, disposed without being written to disk, and only the extracted text is sent to the configured translation provider.

## Privacy design

- Translation history: off and not stored.
- Telemetry and analytics: not implemented.
- Diagnostic logs: local, metadata-only, and exclude captured, translated, OCR, reply, clipboard, and credential content.
- Temporary clipboard values: excluded from Windows clipboard monitoring/history where supported, then the previous multi-format clipboard is restored.
- Cloud translation: selected or OCR-extracted text is transmitted to the provider you configure when you invoke a translation workflow.
- Screenshots: processed locally and never sent to a provider or saved during normal operation.
- Auto-send: never.

Read [Privacy](docs/PRIVACY.md) and [Security](docs/SECURITY.md) for the full model.

## System requirements

- Windows 10 version 2004 (build 19041) or newer, or Windows 11.
- x64 processor and operating system.
- Target application at the same Windows integrity level as OneBoard.
- A configured translation provider for production translation.
- Installed Windows OCR language packs for the languages you want to recognize.

OneBoard intentionally runs with `asInvoker`. Windows UIPI can prevent an unelevated OneBoard process from interacting with an elevated target application.

## Build from source

Prerequisites: Windows x64, .NET 10 SDK, and optionally Inno Setup 6 for the installer.

```powershell
.\scripts\Build-Release.ps1
```

To build both release artifacts:

```powershell
.\scripts\Package-Release.ps1
```

Development details are in [Architecture](ARCHITECTURE.md) and [Testing](docs/TESTING.md).

## Troubleshooting

- **A hotkey does nothing:** open the tray menu and Settings. Another application may own that binding; choose a different combination and save.
- **No selected text:** some secure/password controls intentionally deny UI Automation and copy access. Select ordinary text and retry.
- **Clipboard is busy:** retry after closing software that continuously owns the clipboard. OneBoard aborts rather than overwriting a clipboard it cannot snapshot safely.
- **Provider unavailable:** use **Test connection** and verify endpoint, key, region, network, and provider subscription status.
- **OCR language unavailable:** install the corresponding Windows language/OCR feature in Windows Settings. OneBoard does not silently download language packs.
- **Elevated application:** run both applications at the same integrity level. OneBoard does not request elevation by default.
- **SmartScreen warning:** release builds are unsigned unless the release page explicitly states otherwise.

## Limitations

- Input injection is restricted by Windows UIPI and enterprise endpoint policies.
- Selection fidelity depends on each target application's UI Automation and clipboard behavior.
- Third-party clipboard managers may ignore the Windows exclusion format.
- Cloud-provider availability, quotas, pricing, language quality, and retention policies are controlled by the selected provider.
- Windows OCR quality depends on the installed language pack, font, scaling, and source-image quality.
- No automatic updater is included in v1.1.0.

## License

OneBoard Inline Translate is available under the [MIT License](LICENSE). See [Third-party notices](THIRD_PARTY_NOTICES.md).

Repository: https://github.com/phat7000/OneBoardInlineTranslate
