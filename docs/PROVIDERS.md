# Translation Providers

## Shared behavior

Provider selection is independent from capture, replacement, overlay, reply, and OCR. Normal translation preserves text intent and does not invoke a free-form rewriting model. Source language is provider-detected when possible; the local detector supplies a safe fallback for Vietnamese, English, and Simplified Chinese.

Remote endpoints must use HTTPS. A loopback LibreTranslate service may use HTTP. API keys are DPAPI-protected for the current Windows user and never written to settings or logs.

## Google Cloud Translation

Select **Google Cloud Translation** and enter an API key for a Google Cloud project with the **Cloud Translation API** enabled. Billing and quotas are managed in Google Cloud. Restrict the key to the Cloud Translation API and apply appropriate application restrictions where practical.

OneBoard uses Cloud Translation Basic API v2 at:

`https://translation.googleapis.com/language/translate/v2`

The API key is sent in the `X-Goog-Api-Key` HTTP header and is never placed in the request URL. Vietnamese maps to `vi`, English to `en`, and Simplified Chinese to Google `zh-CN` and back to OneBoard `zh-Hans`.

When OneBoard already knows the source language, it supplies `source`. Otherwise it omits `source` and reads `detectedSourceLanguage` from the translation response. It does not make a separate detect-language request, so a normal translation uses one provider request.

## Azure Translator

Select **Azure Translator**, enter the subscription key and region required by the Azure resource, and leave Endpoint blank for `https://api.cognitive.microsofttranslator.com`. A custom HTTPS resource endpoint is supported.

OneBoard uses the documented Translator v3 `translate` operation with subscription-key and optional subscription-region headers.

## DeepL

Select **DeepL** and enter an authentication key. OneBoard selects `api-free.deepl.com` for Free keys ending in `:fx` and `api.deepl.com` otherwise. An explicit supported HTTPS endpoint overrides this choice.

OneBoard uses DeepL's supported `v2/translate` form API and maps Simplified Chinese to `ZH-HANS`.

## LibreTranslate

Select **LibreTranslate**, enter the complete `/translate` endpoint, and enter an API key only if the deployment requires one. Self-hosted loopback endpoints such as `http://localhost:5000/translate` are supported.

LibreTranslate deployments vary. Administrators are responsible for server security, retention, authentication, capacity, and supported languages.

## Testing

**Test connection** makes one small translation request and reports `Connected · Provider name · latency ms` on success. Common failures are reduced to safe messages: Invalid API key, Permission denied, Quota exceeded, Rate limited, Timeout, Network unavailable, Provider unavailable, or Invalid endpoint. Provider response bodies and secrets are not displayed or logged.

Automated provider tests use fake HTTP handlers and never call or consume quotas from real provider accounts. Google coverage includes Vietnamese/English/Simplified Chinese direction mapping, auto-detection, Unicode, multiline text, header authentication, invalid keys, quota and rate limits, timeout, cancellation, 5xx, and malformed responses.
