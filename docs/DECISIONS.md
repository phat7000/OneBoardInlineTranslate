# Architecture and UX Decisions

## 2026-09-22 - Preserve the validated Phase 0 safety core

Decision: Evolve the existing UI Automation-first capture and transactional clipboard replacement services instead of rewriting them.

Alternatives considered: Replace controls through UI Automation ValuePattern; build a new input-injection layer; discard the proof and start over.

Reason: The existing implementation passed automated checks and the full user-run external-application matrix. ValuePattern would replace whole controls and can destroy surrounding content.

Security/privacy impact: Preserves fail-closed clipboard snapshots, foreground-window authority, clipboard-history exclusion, metadata-only diagnostics, and the no-auto-send invariant.

## 2026-09-22 - Keep v1 local-first and provider-neutral

Decision: Use a provider-neutral translation layer with supported HTTPS APIs, OS-protected credentials, no backend, no analytics, and no translation history.

Alternatives considered: A mandatory cloud account, an embedded history database, unofficial translation scraping, or an LLM-only translation architecture.

Reason: A small provider-neutral client is easier to audit, deploy, and operate across personal and enterprise settings.

Security/privacy impact: Text leaves the device only for a user-selected cloud translation request; secrets do not enter settings or logs.

## 2026-09-22 - Use per-user DPAPI for provider credentials

Decision: Store the small credential dictionary in a separate DPAPI-protected file scoped to the current Windows user.

Alternatives considered: Windows Credential Manager integration, plaintext JSON, environment variables only, or a third-party vault SDK.

Reason: DPAPI is a supported OS protection mechanism, needs no service or added package, works for portable/per-user installation, and keeps secrets out of settings.

Security/privacy impact: An attacker reading the file without the same Windows user context cannot recover the secret through the application format. Secrets and decrypted values are never logged.

## 2026-09-22 - Support three official provider contracts

Decision: Ship Azure Translator, DeepL, and LibreTranslate-compatible implementations behind `ITranslationProvider`; do not add unofficial Google endpoints or LLM-dependent normal translation.

Alternatives considered: One hardcoded vendor, browser scraping, Google Cloud authentication in v1, and an OpenAI-compatible rewriting provider.

Reason: The selected set covers enterprise, commercial, and self-hosted operation with small auditable HTTP clients. Optional rewriting would increase scope without being required for reliable translation/reply.

Security/privacy impact: Remote endpoints require HTTPS except explicit loopback HTTP. Content is sent only on invoked operations to the configured provider.

## 2026-09-22 - Use Windows OCR and in-memory GDI capture

Decision: Capture only the selected pixel rectangle with GDI, convert it in memory, and recognize it with installed Windows OCR engines.

Alternatives considered: Cloud OCR, bundling an OCR engine/model, full-screen persistence, or temporary image files.

Reason: Windows OCR keeps screenshots local, avoids model redistribution and large dependencies, and exposes a clear missing-language-pack state.

Security/privacy impact: Screenshots have no normal-runtime disk path and are never transmitted. Only extracted text may reach the configured translation provider.

## 2026-09-22 - Ship self-contained portable and per-user Inno packages

Decision: Publish self-contained `win-x64`, zip the publish output, and create a lowest-privilege Inno Setup installer under the current user's local programs directory.

Alternatives considered: Framework-dependent deployment, MSI tooling, MSIX, or an administrator-wide installer.

Reason: This gives a small operational surface, no prerequisite .NET install, no admin requirement, and a familiar uninstall path.

Security/privacy impact: The application remains `asInvoker`; user settings are not removed or silently uploaded. Builds remain honestly documented as unsigned without a trusted certificate.

## 2026-09-22 - Defer one-action selected-text replacement enhancement

Decision: Preserve the intended quick-action behavior for a future release: when the user selects only a sentence or text range and invokes a configured translate/replace action, OneBoard translates that exact selected range and replaces only that range in place. The action must never press Enter, trigger Send, submit a form, or automatically transmit the message.

Alternatives considered: Opening Reply Mode for every outbound translation; replacing the full compose control; automatically sending after translation.

Reason: The fast workflow should remain minimal: select text, invoke the action, and get an in-place translation without extra confirmation steps. Reply Mode remains a separate workflow for understanding an incoming message and composing a response. This enhancement is intentionally deferred until after more real-world usage of v1.x.

Security/privacy impact: Reuse the existing foreground validation, transactional clipboard restoration, selected-range-only replacement, and no-auto-send invariants. If the original destination cannot be validated safely, abort or fall back to showing/copying the translated text rather than injecting into an uncertain target.

## 2026-09-22 - Add Google Cloud Translation as a focused v1.1 provider

Decision: Add the official Cloud Translation Basic API v2 contract behind the existing `ITranslationProvider` abstraction. Authenticate with `X-Goog-Api-Key`, keep the endpoint fixed, and omit the source field when provider-native detection is needed.

This supersedes only the v1.0 decision to stop at three production provider contracts; the supported-API, no-scraping, provider-neutral, and no-LLM constraints remain in force.

Alternatives considered: Google service-account/OAuth integration, a separate detect-language request, an unofficial Google Translate endpoint, browser scraping, or an LLM translation path.

Reason: Basic v2 provides a small supported one-request translation contract that fits the existing provider architecture and fast hotkey path without new dependencies or AI behavior.

Security/privacy impact: The key remains DPAPI-protected per Windows user, never enters a URL/settings/log, and text is transmitted only for an invoked translation. Error payloads are reduced to allow-listed health categories.

## 2026-09-22 - Keep performance diagnostics local and metadata-only

Decision: Record capture, provider, output, and total operation latency in the existing local JSON Lines diagnostics, together with operation/provider/capture method/success/exception type.

Alternatives considered: Telemetry, a persistent performance dashboard, content-correlated traces, or no stage timing.

Reason: Stage timing makes real-world latency diagnosable without adding UI weight or a remote analytics system.

Security/privacy impact: Records contain no captured, translated, reply, OCR, or clipboard text; no endpoint, key, exception message, response body, or stack trace is stored.

## 2026-09-23 - Use provider capabilities with a canonical catalog

Decision: Keep canonical BCP-47-like IDs internally, normalize provider aliases at boundaries, query official capability APIs where available, and cache non-sensitive metadata for 24 hours.

Reason: A fixed three-language list was inaccurate, while assuming one universal provider list would allow preventable failures. A broad fallback keeps setup usable when metadata is temporarily unavailable.

Security/privacy impact: Capability calls carry no translation text. Known unsupported targets are rejected before a content request.

## 2026-09-23 - Select CTranslate2 with on-demand OPUS-MT direction packages

Decision: Use a private Python embeddable runtime containing CTranslate2 and SentencePiece, plus verified Argos-compatible OPUS-MT packages downloaded per direction. Do not add Ollama, a general LLM, or a user-configured server.

Alternatives considered: Bergamot/Marian native distribution, ONNX Runtime conversion/generation, full Argos dependency stack, and local LLM runtimes.

Reason: It produced the smallest maintainable Windows path that met required VI/EN/ZH coverage and measured interactive CPU latency while keeping the base application model-free.

Security/privacy impact: Local text remains on-device; downloads are manifest-pinned by size/hash and safely activated; Local never falls back to cloud.

## 2026-09-23 - Reuse one result window across Popup, Pinned, and Hidden modes

Decision: Adapt the existing overlay rather than create separate result-window implementations. Popup remains non-activating/transient; Pinned changes the same native window to a persistent movable panel; Hidden suppresses only successful quick-replace confirmation.

Reason: One rendering path reduces visual inconsistency and resource churn. Explicit result operations remain visible even in Hidden mode.

Security/privacy impact: Hidden does not hide errors or unsafe replacement; non-activating Popup preserves source focus; bounds recovery prevents an inaccessible off-screen panel.

## 2026-09-23 - Keep WPF and build a compact native design system

Decision: Add small ResourceDictionary-style application resources and a left-navigation Settings layout without a third-party UI framework.

Reason: Native WPF controls preserve keyboard/accessibility behavior and avoid package/startup overhead while materially improving the most frequently seen surfaces.

Security/privacy impact: No web view, remote UI asset, analytics dependency, or expanded runtime trust boundary was introduced.
