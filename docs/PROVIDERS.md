# Translation Providers

OneBoard calls only the provider selected in Settings. Cloud text is sent after an explicit translation action; Local Translation never calls a cloud provider. Credentials use provider-specific current-user DPAPI entries and never appear in URLs, `settings.json`, logs, or diagnostics.

| Provider | Translate contract | Authentication | Capability source |
|---|---|---|---|
| Google Cloud Translation | `POST https://translation.googleapis.com/language/translate/v2` | `X-Goog-Api-Key` | Basic v2 `/languages?target=en` |
| Azure Translator | `POST /translate?api-version=3.0` | `Ocp-Apim-Subscription-Key`, optional region | public `GET /languages?api-version=3.0&scope=translation` |
| DeepL | `POST /v2/translate` | `DeepL-Auth-Key` | `GET /v2/languages?type=source|target` |
| LibreTranslate | configured `/translate` | optional `api_key` in JSON | sibling `/languages` |
| TranslatePlus | `POST https://api.translateplus.io/v2/translate` | `X-API-KEY` | `GET /v2/supported-languages` |
| Langbly | `POST /language/translate/v2` | `X-API-Key` | `GET /language/translate/v2/languages` |
| Local Translation | on-device CTranslate2 | none | installed verified model directions |

Google, Langbly, and other providers that support inline detection omit the source field when it is unknown. TranslatePlus sends `source: "auto"`. No separate detection request is added to the normal fast path.

## Provider-specific setup

- Google: enable Cloud Translation API, create/restrict an API key, then paste it in Settings. The endpoint is fixed.
- Azure: enter the resource key. Enter a region only when the Azure resource requires it. The advanced endpoint is optional.
- DeepL: enter the key. Keys ending in `:fx` use the Free endpoint; others use Pro unless explicitly overridden.
- LibreTranslate: enter a full `/translate` endpoint. HTTPS is required except for a loopback HTTP service. Deployment security/retention remain the operator's responsibility.
- TranslatePlus: enter its API key. OneBoard uses only the official v2 endpoint and actual `supported_languages` object response.
- Langbly: choose Global (`https://api.langbly.com`) or EU (`https://eu.langbly.com`). Only Custom shows a base endpoint field.
- Local: install directions under **Settings → Local Translation**. No API key or network translation is used.

## Languages and cache

Provider capabilities are normalized into OneBoard's canonical catalog and cached for 24 hours. Endpoint/region is part of the cache key. **Refresh languages** bypasses the cache. DeepL keeps distinct source/target lists; other providers use their documented shapes. LibreTranslate derives both sides from each returned language and its targets.

When reliable cached metadata says a target is unavailable, OneBoard throws a compatibility error before content leaves the process. If capability metadata is unavailable, the built-in fallback catalog remains available for cloud providers; the provider still has final authority. Local choices are derived only from installed models.

## Health and errors

**Test connection** makes a small translation and reports `Connected · Provider · latency ms`. Failures are reduced to: Invalid API key, Permission denied, Quota exceeded, Rate limited, Timeout, Network unavailable, Provider unavailable, or Invalid endpoint. Response bodies and secrets are never surfaced or logged.

Automated cloud coverage uses fake HTTP handlers, not live credentials or quota. No live provider was claimed tested for 1.2.0.
