# Privacy

## Stored locally

OneBoard stores non-sensitive settings, provider language metadata, local model/runtime files explicitly downloaded by the user, current-user DPAPI-encrypted credentials, and metadata-only diagnostics beneath `%LOCALAPPDATA%\OneBoardInlineTranslate`. Translation history and telemetry are not implemented.

## Cloud Translation

When the user invokes translation with Google Cloud Translation, Azure Translator, DeepL, LibreTranslate, TranslatePlus, or Langbly selected, the selected text—or locally extracted OCR text—is sent to that configured provider. The provider's privacy, retention, account, region, and jurisdiction policies apply. Merely running OneBoard causes no content request.

## Local Translation

Translation text remains on-device. It passes from the WPF process to a private local worker through anonymous standard-input/output pipes. It is not written to disk or placed in process arguments. The only Local-mode network operations are explicit downloads of public runtime/model payloads; downloads contain no user text.

## Clipboard and OCR

Fallback capture/replacement temporarily uses the Windows clipboard after creating a fail-closed multi-format snapshot. Temporary content is marked for Windows Clipboard History/Cloud Clipboard exclusion where supported, then the previous clipboard is restored. Third-party clipboard managers may ignore the convention. An explicit Copy action intentionally leaves output on the clipboard.

Only the selected screen rectangle is captured. Pixels stay in memory, are recognized with Windows OCR, and are neither saved nor transmitted. Only the extracted text may be translated afterward.

## Diagnostics

Local diagnostic fields are allow-listed: timestamp, process name, operation, provider, capture method, stage timings, success, and exception type. They exclude selected/translated/reply/OCR/clipboard text, screenshots, credentials, tokens, response bodies, exception messages, and stack traces.

## Sending

OneBoard never presses Enter or Ctrl+Enter, clicks Send, calls a target-app submit API, or submits a form. The user always performs the final send action.
