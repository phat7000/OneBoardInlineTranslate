# OneBoard Inline Translate

OneBoard Inline Translate 1.2.0 is a compact Windows utility for translating selected text without leaving the app you are using. It preserves the validated selected-range capture/replacement workflow and never presses Enter, clicks Send, or submits a form.

> Builds are currently unsigned, so Windows SmartScreen may show a warning.

## Highlights

- Searchable, provider-aware language catalog with Vietnamese, English, and Chinese Simplified first, recent choices next, then the remaining languages alphabetically.
- Configurable Quick Target 1 (`Alt+E` by default) and Quick Target 2 (`Alt+C` by default).
- Google Cloud Translation, Azure Translator, DeepL, LibreTranslate, TranslatePlus v2, Langbly Global/EU, and optional offline Local Translation.
- On-demand, SHA-256-verified local OPUS-MT models powered by a private CTranslate2 runtime—no Ollama, LLM, Python installation, or user-managed server.
- Popup, reusable Pinned, and Hidden result modes. Hidden suppresses successful quick-replace notices while Understand still shows a small result.
- Modern compact Settings, Reply Mode, result surface, tray, executable, and installer using the OneBoard icon.
- UI Automation-first selection capture, transactional multi-format clipboard fallback/restoration, foreground validation, and no-send guarantees.
- Reply Mode with explicit preview and Insert/Copy; local in-memory Windows OCR; DPAPI-protected credentials; metadata-only diagnostics.

## Default hotkeys

| Hotkey | Action |
|---|---|
| `Alt+Q` | Understand selected text in the preferred language |
| `Alt+E` | Translate and replace with Quick Target 1 (default: English) |
| `Alt+C` | Translate and replace with Quick Target 2 (default: Chinese Simplified) |
| `Alt+R` | Open Reply Mode |
| `Alt+Shift+Q` | Select a screen region, OCR locally, then translate |

Hotkeys and both quick target languages are configurable. Existing saved Alt+E/Alt+C settings migrate without semantic changes.

## Install

- Portable: extract `OneBoardInlineTranslate-1.2.0-win-x64.zip`, then run `OneBoardInlineTranslate.exe`.
- Installer: run `OneBoardInlineTranslate-Setup-1.2.0-win-x64.exe`. It installs per user without administrator rights and offers an optional desktop shortcut.

Both artifacts are self-contained. Settings and downloaded local models live beneath `%LOCALAPPDATA%\OneBoardInlineTranslate` and remain separate from the app package.

## Configure translation

Open **Settings → Providers**, choose a provider, enter only the fields shown, and use **Test connection**. A successful check reports `Connected · Provider · latency ms`. Real keys are protected with DPAPI and never enter `settings.json`, source, diagnostics, or URLs.

- Google Cloud Translation: fixed official Basic v2 endpoint; `X-Goog-Api-Key`.
- Azure Translator: API key, optional resource region, and optional advanced endpoint.
- DeepL: API key; Free/Pro endpoint inferred from the key unless overridden.
- LibreTranslate: complete `/translate` endpoint; key optional where supported. Plain HTTP is accepted only for loopback.
- TranslatePlus: fixed official v2 endpoint; `X-API-KEY`.
- Langbly: managed Global or EU endpoint; `X-API-Key`; Custom exposes a base endpoint.
- Local Translation: no key and no cloud fallback. Install only the directions you need in **Settings → Local Translation**.

Provider language metadata is fetched from official capability endpoints where available and cached for 24 hours. **Refresh languages** reloads it. Known unsupported target requests are blocked before translation.

See [provider setup](docs/PROVIDERS.md), [language behavior](docs/LANGUAGES.md), and [local engine evaluation](docs/LOCAL_TRANSLATION_EVALUATION.md).

## Local Translation

The first model download also installs a private runtime under `%LOCALAPPDATA%\OneBoardInlineTranslate\models\_runtime`. Downloads use HTTPS, temporary files, exact size and SHA-256 verification, safe archive extraction, and atomic activation.

Initial directions:

- Vietnamese → English
- English → Vietnamese
- English → Chinese Simplified
- Chinese Simplified → English
- Vietnamese ↔ Chinese Simplified through English when both required legs are installed

The base application contains no translation models. Local mode never silently calls a cloud provider. See the measured sizes, latency, memory, quality samples, licenses, and tradeoffs in [docs/LOCAL_TRANSLATION_EVALUATION.md](docs/LOCAL_TRANSLATION_EVALUATION.md).

## Result modes

- Popup: compact non-activating result with Auto/Small/Medium/Large/Custom sizes and monitor-clamped placement.
- Pinned: one movable/resizable panel reused for every result; size, position, and monitor are remembered and recovered if a monitor disappears.
- Hidden: successful quick replacement is silent. Errors and unsafe replacement results remain visible; Understand/OCR still show the smallest result surface.

Details are in [docs/UX.md](docs/UX.md).

## Privacy and safety

- Cloud mode sends selected or OCR-extracted text only to the configured provider after an explicit action.
- Local Translation keeps text on-device; only requested runtime/model downloads use the network.
- Screenshots are processed locally in memory and are not saved.
- Translation history and telemetry are not implemented.
- Diagnostics contain timing and operation metadata, never message, reply, OCR, clipboard, response-body, endpoint-secret, or credential content.
- Replacement revalidates the original foreground window, replaces only the selected range, restores the clipboard, and never sends.

Read [Privacy](docs/PRIVACY.md) and [Security](docs/SECURITY.md).

## Requirements and limitations

- Windows 10 build 19041 or later, or Windows 11; x64.
- OneBoard and the target application must normally run at the same integrity level.
- Windows OCR requires the corresponding installed Windows language pack.
- Provider availability, pricing, quotas, quality, retention, and supported languages remain provider-controlled.
- Local model quality is suitable for short practical communication but is below the best cloud systems, especially for pivot translation.
- No automatic updater or trusted code signature is included in 1.2.0.

## Build and test

Prerequisites: Windows x64, .NET 10 SDK, and Inno Setup 6 for the installer.

```powershell
.\scripts\Build-Release.ps1
.\scripts\Package-Release.ps1
```

The release gate verifies formatting, Release x64 compilation, provider contracts, settings migration, language capabilities, local model security/routing, result modes, privacy invariants, icon integration, clipboard integration, and cross-process replacement. See [docs/TESTING.md](docs/TESTING.md).

OneBoard Inline Translate is MIT-licensed. Local runtime/model components retain their own licenses and notices in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
