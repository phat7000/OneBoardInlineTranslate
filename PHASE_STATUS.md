# Phase Status

Status date: 2026-09-22

## Focused post-v1 Phase 2

| Phase | Status | Evidence |
|---|---|---|
| Phase 2 - Google Cloud, provider configuration, fast path, and latency | PASS | Official Cloud Translation Basic v2 provider; `X-Goog-Api-Key` header; one-request auto-detection; provider-specific compact fields and DPAPI credentials; safe latency-bearing connection result; stage timing metadata; fake-handler success/error coverage; complete v1 regressions retained. |

The v1.0.0 capture, clipboard, focus, replacement, and no-send core was not materially changed, so the historical external Teams/Outlook/Zalo/Chrome/Edge matrix was not repeated.

## Historical v1.0.0 phases

| Phase | Status | Evidence |
|---|---|---|
| Phase 0 - safe selection capture/replacement proof | PASS | User-confirmed complete manual matrix for Teams Desktop, Outlook Desktop, Zalo Desktop, Chrome, and Edge. User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. Automated regression coverage retained. |
| Phase 1 - shell, settings, lifecycle | PASS | Single-instance guard, tray lifecycle, per-user startup, pause/resume, configurable collision-tolerant hotkeys, Settings/About UI, persistence/corruption tests. |
| Phase 2 - production capture/replacement | PASS | Phase 0 safety core retained; production action routing replaces runtime test transform; test-only deterministic provider retained; capture/replacement integration passes. |
| Phase 3 - translation engine | PASS | Provider-neutral domain/service, Azure Translator, DeepL, LibreTranslate-compatible provider, DPAPI credentials, contract tests, safe endpoint validation. |
| Phase 4 - translation overlay | PASS | Modern non-activating overlay, copy/close controls, adaptive scroll bounds, work-area positioning tests. |
| Phase 5 - translate and replace | PASS | Alt+E English and Alt+C Simplified Chinese routing, pre-paste HWND validation, clipboard restoration, Unicode/multiline/punctuation/emoji/URL/number fake-provider coverage. |
| Phase 6 - Reply Mode | PASS | Incoming understanding, detected-language response target, override, preview, explicit Insert/Copy, destination revalidation, fallback-to-copy, no-send architecture. |
| Phase 7 - OCR region translate | PASS | PerMonitorV2 region selector, Escape cancellation, in-memory selected-region capture, Windows OCR, installed-language handling, static English/Vietnamese/Chinese fixtures. |
| Phase 8 - security, packaging, release | PASS | Security/no-send/secret audits passed; Release x64 and all suites passed; portable and installer artifacts built and launch-tested; silent install/uninstall passed; CI and tag-release workflows are ready. |

## Current v1.1.0 automated baseline

- Release x64 build: 0 warnings, 0 errors.
- Default automated suite: 35/35 passed.
- Clipboard integration suite: 36/36 passed.
- Cross-process end-to-end suite: 36/36 passed.
- Formatting verification: final `--verify-no-changes` passed.
- Portable launch smoke: passed with ProductVersion 1.1.0.
- Portable ZIP and installer x64 builds: passed; v1.0.0 artifacts were not overwritten.

## External qualification facts

- No Google credential is configured locally; Google behavior is validated with official contract shapes and fake HTTP handlers rather than live billable calls.
- Installed Windows OCR languages vary by machine; matching fixture recognition is asserted for available packs and missing packs produce a supported setup condition.
- No trusted code-signing certificate is available in the repository. The release will remain unsigned and must not be represented as trusted-signed.
