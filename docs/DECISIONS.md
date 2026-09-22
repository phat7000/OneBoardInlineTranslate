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
