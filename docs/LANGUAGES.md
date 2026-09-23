# Languages

## Canonical model

OneBoard stores canonical language IDs and maps them only at provider boundaries. Examples: `zh`, `zh-CN`, and `zh_CHS` normalize to `zh-Hans`; `zh-TW`/`zh-HK` normalize to `zh-Hant`; legacy `iw`, `in`, `tl`, and `no` normalize to `he`, `id`, `fil`, and `nb`.

The built-in fallback covers mainstream languages used by major translation providers. It is not a claim that every provider supports every language or direction.

## Provider capabilities

Where available, OneBoard calls the selected provider's documented language endpoint. Results are normalized, stored without secrets/content, and cached for 24 hours using provider/region/endpoint as the cache key. DeepL source and target lists remain distinct. Local capabilities come only from installed models.

Known unsupported targets are blocked before text is sent. If a cloud capability endpoint is temporarily unavailable and no cache exists, the broad fallback remains selectable and the provider has final authority. Local mode never uses that fallback: no installed target means no supported local target.

## Picker behavior

Searchable pickers match display name, native name, and code. Ordering is:

1. Vietnamese
2. English
3. Chinese Simplified
4. recently used supported languages
5. remaining supported languages alphabetically

Examples: `jap` and `ja` find Japanese. Provider capability changes refresh Preferred Language, both Quick Targets, and Reply Mode choices.

## Preferred and quick targets

Preferred Language may be any current-provider target and defaults to Vietnamese. Quick Target 1 defaults to English/Alt+E; Quick Target 2 defaults to Chinese Simplified/Alt+C. Changing a target does not change its hotkey automatically, preserving old user bindings and muscle memory.

Schema-1 settings migrate to schema 2 without discarding preferred language, hotkeys, provider, endpoint/region, startup preference, or credential references.

## Reply Mode

The detected incoming language is selected when supported. The user can search and override it from the same provider-filtered catalog. If the current provider cannot target it, Reply Mode shows a clear compatibility message instead of issuing a known-invalid request.

## Local directions

The initial local catalog is intentionally limited to installed VI→EN, EN→VI, EN→ZH-Hans, and ZH-Hans→EN packages. VI↔ZH-Hans becomes available by English pivot only when both legs are installed. Local never broadens the list by silently using cloud translation.
