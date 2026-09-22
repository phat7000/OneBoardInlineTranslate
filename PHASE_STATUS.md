# Phase Status

## Phase 0 — selection capture and safe test replacement

Status as of 2026-09-22: **PASS**.

The user manually executed the complete [TEST_MATRIX.md](TEST_MATRIX.md) against Teams Desktop, Outlook Desktop, Zalo Desktop, Chrome, and Edge and confirmed that every Phase 0 test passed. User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded.

## Implemented

- [x] Windows-only WPF application targeting `net10.0-windows`, x64
- [x] Global Alt+Q and Alt+E registration with no-repeat behavior
- [x] Foreground window, thread, process ID, and process-name snapshot
- [x] UI Automation `TextPattern` selected-text attempt
- [x] Single-flight UIA call with bounded wait so a stalled provider cannot accumulate worker calls
- [x] Clipboard capture fallback with eager multi-format snapshot, update-sequence wait, retries, and persisted restoration
- [x] Selected-text replacement through transactional Unicode clipboard plus Ctrl+V
- [x] Foreground-handle checks immediately before Ctrl+C and Ctrl+V
- [x] Non-activating, topmost, click-through overlay
- [x] Local metadata-only diagnostics; captured text is excluded by construction
- [x] Exact Phase 0 transform: `hello` → `[TEST] hello`
- [x] No Enter key declaration or input path
- [x] Browser manual-test fixture
- [x] Required documentation

## Verification completed

- [x] Debug x64 solution build: 0 warnings, 0 errors
- [x] Dependency-free default smoke checks: 9/9 passed
- [x] Opt-in multi-format clipboard integration check: 10/10 passed
- [x] Opt-in two-process global-hotkey integration check (UIA and forced Clipboard): 10/10 passed
- [x] Release x64 build: 0 warnings, 0 errors
- [x] `dotnet format --verify-no-changes` passed
- [x] Local diagnostic content audit found no captured/test text
- [x] No third-party NuGet package dependencies
- [x] Teams Desktop manual matrix - user-confirmed PASS
- [x] Outlook Desktop manual matrix - user-confirmed PASS
- [x] Zalo Desktop manual matrix - user-confirmed PASS
- [x] Google Chrome manual matrix - user-confirmed PASS
- [x] Microsoft Edge manual matrix - user-confirmed PASS

Qualification note: User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded.

## Definition-of-PASS gate

PASS requires all five target application categories to demonstrate:

1. short, multiline, Vietnamese, and Simplified Chinese capture;
2. selected-text replacement;
3. exact restoration of the pre-operation clipboard;
4. preservation of source focus;
5. no accidental send or submit.

Either UIA or Clipboard capture is acceptable. Any future safety failure reopens the Phase 0 qualification gate.

## Ongoing regression risks

- App updates can change accessibility trees and keyboard handling, so exact app versions must be recorded.
- Chromium/Electron, Office, and Zalo may expose selection differently by control type; clipboard fallback is the compatibility path.
- UIPI blocks synthetic input into a higher-integrity target.
- Third-party clipboard managers may ignore Windows history/monitor exclusion formats.
- Clipboard ownership is global Windows state; an unrelated program deliberately changing the clipboard during the sub-second transaction is a race that must be observed during soak testing.

## Next phase

Phase 1 production-shell implementation is active. Phase 0 remains the safety regression baseline.
