# Privacy

## Data stored locally

OneBoard stores non-sensitive settings, DPAPI-encrypted provider credentials, and metadata-only diagnostic logs under `%LOCALAPPDATA%\OneBoardInlineTranslate`. Translation history is not implemented and text content is not persisted.

## Data sent to a provider

When the user invokes a translation workflow, the selected text or locally extracted OCR text is sent over HTTPS to the configured Google Cloud Translation, Azure Translator, DeepL, or LibreTranslate-compatible endpoint. Provider policies, retention, jurisdiction, and account controls apply. OneBoard makes no provider request simply because it is running.

## Clipboard

Fallback capture and replacement temporarily use the Windows clipboard. OneBoard first creates a fail-closed multi-format snapshot, excludes temporary data from Windows Clipboard History and Cloud Clipboard monitoring where supported, and restores the original clipboard. Third-party clipboard managers may ignore the Windows exclusion convention.

An explicit **Copy** action intentionally leaves the requested output on the clipboard, while still applying the Windows monitoring exclusion format.

## OCR

Only the selected screen rectangle is captured. The bitmap remains in memory, is processed by Windows OCR locally, is never transmitted, and is never written to disk during normal operation. Only extracted text is eligible for translation after the OCR step.

## Logs and telemetry

There is no telemetry or analytics. Local diagnostics contain timestamp, process name, operation, provider, capture method, capture/provider/output/total latency, success/failure, and exception type. They exclude selected, translated, OCR, reply, and clipboard text; secrets; tokens; endpoints with embedded credentials; exception messages; and provider response bodies.

## Auto-send

OneBoard never sends a message or email. It never presses Enter or Ctrl+Enter, clicks Send, invokes a submit API, or submits a form. The user performs the final send action.
