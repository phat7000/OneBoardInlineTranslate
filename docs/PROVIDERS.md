# Translation Providers

## Shared behavior

Provider selection is independent from capture, replacement, overlay, reply, and OCR. Normal translation preserves text intent and does not invoke a free-form rewriting model. Source language is provider-detected when possible; the local detector supplies a safe fallback for Vietnamese, English, and Simplified Chinese.

Remote endpoints must use HTTPS. A loopback LibreTranslate service may use HTTP. API keys are DPAPI-protected for the current Windows user and never written to settings or logs.

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

**Test connection** makes a small translation request and reports only a safe health result. Automated tests use fake HTTP handlers and never call or consume quotas from real provider accounts.
