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
