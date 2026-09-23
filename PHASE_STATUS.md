# Phase Status

Status date: 2026-09-23

| Area | Status | Evidence |
|---|---|---|
| Existing capture/replacement core | Preserved / PASS | UIA-first capture, transactional clipboard fallback/restoration, foreground authority, selected-range replacement, and no-Enter invariant unchanged. |
| TranslatePlus | PASS | Official v2 translate/language endpoints, `X-API-KEY`, one-request `source=auto`, safe failures, fake-handler tests. |
| Langbly | PASS | Google-v2-style contract, omitted source detection, `X-API-Key`, Global/EU/Custom routing, language endpoint, fake-handler tests. |
| Dynamic languages | PASS | Broad fallback catalog, provider capability APIs/cache, search, normalization, recents, filtering, migration, full Reply target list. |
| Local Translation | PASS | Verified runtime/model manifest, on-demand/cancellable install, safe extraction, hash validation, bounded persistent model cache, direct and English-pivot routing, no cloud fallback. |
| Result modes | PASS | Popup, single reusable Pinned panel, Hidden quick-success suppression, explicit-result visibility, bounds/size persistence and monitor clamping. |
| UX and icon | PASS | Compact left-rail Settings, modern Reply/result surfaces, provider-specific fields, model cards/progress, supplied OneBoard icon throughout app/package definitions. |
| Regression/security | PASS in development | 49/49 default smoke checks; final Release, clipboard, cross-process, format, scan, and packaging results are added to the execution log. |

The historical external Teams/Outlook/Zalo/Chrome/Edge matrix is not repeated because the proven capture/replacement core was not materially rewritten.
