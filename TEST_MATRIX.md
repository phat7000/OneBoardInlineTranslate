# Phase 0 Manual Test Matrix

This matrix is the acceptance record for OneBoard Inline Translate Phase 0. All rows are manual because the behavior crosses process, accessibility-provider, focus, keyboard-input, and global-clipboard boundaries.

The user reports completing this entire matrix successfully against all five target applications. Every row is recorded as **PASS** based on that report.

## Qualification record

The detailed environment metadata was not captured during the completed qualification run:

| Field | Value |
|---|---|
| Tester | User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| Date/time and time zone | User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| Windows edition/build | User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| Display/DPI configuration | User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| Keyboard layout | User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| OneBoard commit/build | User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| Teams version | User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| Outlook version and Classic/New | User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| Zalo version | User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| Chrome version | User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| Edge version | User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| Clipboard manager/history state | User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |

Allowed status values were `NOT RUN`, `PASS`, `FAIL`, `BLOCKED`; this completed record uses `PASS`. For `FAIL` or `BLOCKED`, add a reproducible note and the local diagnostic line; never paste selected text into a bug report unless the test data is explicitly non-sensitive.

## Common setup

1. Build and start the x64 Release app with `./scripts/Build-Phase0.ps1`, then `./scripts/Run-Phase0.ps1`.
2. Confirm no startup error reports a hotkey collision.
3. Use only test accounts, a self-chat, or unsent drafts for messaging/mail applications. Never aim the no-send test at a real recipient.
4. Before every clipboard-restoration check, copy the exact canary `OB-CLIPBOARD-CANARY-中文-Tiếng-Việt-01` from a separate scratch document.
5. Immediately after each hotkey, visually verify that the target window remains foreground before clicking elsewhere.
6. For Alt+Q, record the overlay's process, method (`UIA` or `Clipboard`), and latency.
7. For Alt+E, confirm only the selected range changed and the expected value is `[TEST] ` followed by the original selection.
8. Verify clipboard restoration by moving to a scratch editor only after the focus check and pasting once. The result must exactly equal the canary, with no OneBoard sentinel or test replacement text.
9. Check `%LOCALAPPDATA%\OneBoardInlineTranslate\logs` after the run. Records must contain only the six documented fields and none of the sample strings.

Use these canonical strings:

| Case | Test text |
|---|---|
| Short | `hello` |
| Multiline | `first line` + line break + `second line` + line break + `third line` |
| Vietnamese | `Xin chào Việt Nam — tiếng Việt có dấu.` |
| Simplified Chinese | `你好，世界。这是简体中文。` |

Line-ending normalization between CRLF and LF is acceptable. Missing, added, or reordered visible characters are not.

## Microsoft Teams Desktop

Use a test tenant/self-chat. Read-only capture may use message history; editable tests use an unsent compose draft.

| ID | Required test | Manual procedure | Expected result | Method / latency | Status / notes |
|---|---|---|---|---|---|
| TMS-01 | Capture short text | Select `hello` in a message or compose box; press Alt+Q. | Overlay shows `ms-teams`/Teams process, exact text, method, and timing. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| TMS-02 | Capture multiline text | Select all three canonical lines; press Alt+Q. | Overlay preserves all characters and three logical lines. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| TMS-03 | Unicode Vietnamese | Select the canonical Vietnamese string; press Alt+Q. | Overlay preserves every diacritic and punctuation mark. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| TMS-04 | Chinese Simplified | Select the canonical Chinese string; press Alt+Q. | Overlay preserves every Han character and punctuation mark. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| TMS-05 | Replace selected text | In an unsent compose draft containing `hello`, select only `hello`; press Alt+E. | Draft becomes `[TEST] hello`; surrounding content is unchanged. | _Record capture method / total latency_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| TMS-06 | Clipboard restored | Set the canary, perform one Alt+Q and one Alt+E test, checking after each in a scratch editor. | Canary is restored exactly after both operations. | _Record any manager/history behavior_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| TMS-07 | Focus preserved | Keep the compose box focused and selected; run each hotkey. Before clicking, type a harmless character and undo it. | Teams stays foreground; input still goes to the same compose control; overlay never activates. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| TMS-08 | No accidental send | In a self-chat/test channel draft, run Alt+Q and Alt+E and wait 10 seconds. | No message is posted, no send action occurs, and draft remains editable. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |

## Microsoft Outlook Desktop

Record whether the test uses Classic Outlook or New Outlook. Use received test mail for read-only capture and a recipient-free unsent draft for editing.

| ID | Required test | Manual procedure | Expected result | Method / latency | Status / notes |
|---|---|---|---|---|---|
| OUT-01 | Capture short text | Select `hello` in a received mail body or draft; press Alt+Q. | Overlay shows the Outlook process, exact text, method, and timing. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| OUT-02 | Capture multiline text | Select all three canonical lines in a mail body; press Alt+Q. | Overlay preserves all characters and three logical lines. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| OUT-03 | Unicode Vietnamese | Select the canonical Vietnamese string; press Alt+Q. | Overlay preserves every diacritic and punctuation mark. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| OUT-04 | Chinese Simplified | Select the canonical Chinese string; press Alt+Q. | Overlay preserves every Han character and punctuation mark. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| OUT-05 | Replace selected text | In a recipient-free draft containing `hello`, select only `hello`; press Alt+E. | Draft becomes `[TEST] hello`; formatting and surrounding content are unchanged. | _Record capture method / total latency_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| OUT-06 | Clipboard restored | Set the canary, perform one Alt+Q and one Alt+E test, checking after each in a scratch editor. | Canary is restored exactly after both operations, including when Office owns the clipboard. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| OUT-07 | Focus preserved | Keep the draft body focused; run each hotkey. Before clicking, type a harmless character and undo it. | Outlook stays foreground; the same draft body retains keyboard focus; overlay never activates. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| OUT-08 | No accidental send | Leave To/Cc empty, run both hotkeys in the draft, and wait 10 seconds. | Draft is not sent; no send dialog/action occurs. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |

## Zalo Desktop

Use a self-chat or dedicated test conversation. Do not use a conversation with a real recipient for the no-send row.

| ID | Required test | Manual procedure | Expected result | Method / latency | Status / notes |
|---|---|---|---|---|---|
| ZAL-01 | Capture short text | Select `hello` in history or the compose box; press Alt+Q. | Overlay shows the Zalo process, exact text, method, and timing. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| ZAL-02 | Capture multiline text | Select all three canonical lines; press Alt+Q. | Overlay preserves all characters and three logical lines. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| ZAL-03 | Unicode Vietnamese | Select the canonical Vietnamese string; press Alt+Q. | Overlay preserves every Vietnamese diacritic and punctuation mark. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| ZAL-04 | Chinese Simplified | Select the canonical Chinese string; press Alt+Q. | Overlay preserves every Han character and punctuation mark. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| ZAL-05 | Replace selected text | In an unsent self-chat draft containing `hello`, select only `hello`; press Alt+E. | Draft becomes `[TEST] hello`; surrounding content is unchanged. | _Record capture method / total latency_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| ZAL-06 | Clipboard restored | Set the canary, perform one Alt+Q and one Alt+E test, checking after each in a scratch editor. | Canary is restored exactly after both operations. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| ZAL-07 | Focus preserved | Keep the Zalo compose box focused; run each hotkey. Before clicking, type a harmless character and undo it. | Zalo stays foreground; input remains in the same compose box; overlay never activates. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| ZAL-08 | No accidental send | In self-chat/test conversation, run both hotkeys and wait 10 seconds. | No chat message is sent; transformed text remains only in the compose draft. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |

## Google Chrome

Open [tests/manual/selection-fixture.html](tests/manual/selection-fixture.html) directly in Chrome. This fixture is local and makes no network requests.

| ID | Required test | Manual procedure | Expected result | Method / latency | Status / notes |
|---|---|---|---|---|---|
| CHR-01 | Capture short text | Select `hello` in the read-only sample; press Alt+Q. | Overlay shows `chrome`, exact text, method, and timing. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| CHR-02 | Capture multiline text | Select all three read-only canonical lines; press Alt+Q. | Overlay preserves all characters and three logical lines. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| CHR-03 | Unicode Vietnamese | Select the Vietnamese sample; press Alt+Q. | Overlay preserves every diacritic and punctuation mark. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| CHR-04 | Chinese Simplified | Select the Chinese sample; press Alt+Q. | Overlay preserves every Han character and punctuation mark. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| CHR-05 | Replace selected text | In both the textarea and contenteditable sample, select `hello`; press Alt+E. | Each selected range becomes `[TEST] hello`; surrounding content is unchanged. | _Record both control types_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| CHR-06 | Clipboard restored | Set the canary, perform Alt+Q and Alt+E on the fixture, checking after each in a scratch editor. | Canary is restored exactly after both operations. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| CHR-07 | Focus preserved | Keep the textarea focused and run each hotkey. Before clicking, type a harmless character and undo it. | Chrome stays foreground; the same textarea retains keyboard focus; overlay never activates. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| CHR-08 | No accidental send | Select `hello` in the fixture's single-line submit sentinel; press Alt+E; wait 10 seconds. | Value becomes `[TEST] hello` and submit count remains `0`. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |

## Microsoft Edge

Open [tests/manual/selection-fixture.html](tests/manual/selection-fixture.html) directly in Edge. Do not reuse the Chrome result; Edge must be executed independently.

| ID | Required test | Manual procedure | Expected result | Method / latency | Status / notes |
|---|---|---|---|---|---|
| EDG-01 | Capture short text | Select `hello` in the read-only sample; press Alt+Q. | Overlay shows `msedge`, exact text, method, and timing. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| EDG-02 | Capture multiline text | Select all three read-only canonical lines; press Alt+Q. | Overlay preserves all characters and three logical lines. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| EDG-03 | Unicode Vietnamese | Select the Vietnamese sample; press Alt+Q. | Overlay preserves every diacritic and punctuation mark. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| EDG-04 | Chinese Simplified | Select the Chinese sample; press Alt+Q. | Overlay preserves every Han character and punctuation mark. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| EDG-05 | Replace selected text | In both the textarea and contenteditable sample, select `hello`; press Alt+E. | Each selected range becomes `[TEST] hello`; surrounding content is unchanged. | _Record both control types_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| EDG-06 | Clipboard restored | Set the canary, perform Alt+Q and Alt+E on the fixture, checking after each in a scratch editor. | Canary is restored exactly after both operations. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| EDG-07 | Focus preserved | Keep the textarea focused and run each hotkey. Before clicking, type a harmless character and undo it. | Edge stays foreground; the same textarea retains keyboard focus; overlay never activates. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |
| EDG-08 | No accidental send | Select `hello` in the fixture's single-line submit sentinel; press Alt+E; wait 10 seconds. | Value becomes `[TEST] hello` and submit count remains `0`. | _Record_ | **PASS** — User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded. |

## Acceptance summary

Fill only after completing the detailed rows:

| Application | Short | Multiline | Vietnamese | Chinese | Replace | Clipboard | Focus | No send | Overall |
|---|---|---|---|---|---|---|---|---|---|
| Teams Desktop | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Outlook Desktop | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Zalo Desktop | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Chrome | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Edge | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |

Phase 0 is **PASS**. The user confirmed every detailed test and application summary as passing on 2026-09-22. Detailed qualification metadata was not recorded. Either `UIA` or `Clipboard` remains an acceptable capture method; safety failures remain unacceptable regardless of method.

