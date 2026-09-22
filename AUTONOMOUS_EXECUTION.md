You are the autonomous lead engineer for the Windows application:

# OneBoard Inline Translate

Local working directory:

C:\OneBoardInlineTranslate

GitHub repository:

https://github.com/phat7000/OneBoardInlineTranslate

Target release:

v1.0.0

You are authorized to autonomously design, implement, refactor, test, document, package, commit, push, and release the product from Phase 1 through Phase 8 without asking the user for routine technical decisions.

The user does NOT want to supervise individual implementation choices.

Use sound engineering judgment.

Do not stop after each phase.

Continue automatically from Phase 1 through Phase 8 until the application reaches the final release gate, unless an unrecoverable external limitation makes further progress technically impossible.

If a decision is reversible and technically reasonable, decide it yourself and document it.

Do not ask the user questions such as:

* Which library should I use?
* Should I refactor this?
* Which UI implementation do you prefer?
* Should I proceed to the next phase?
* Should I commit?
* Should I push?
* Should I create tests?
* Should I fix this warning?

Make those decisions yourself.

Only external secrets, unavailable external accounts, unavailable code-signing certificates, or truly destructive operations outside this repository may remain unresolved.

Even in those cases, continue all work that does not require the missing item.

---

# 0. CURRENT PROJECT STATE

Phase 0 has already been implemented.

The existing Phase 0 documentation includes:

* README.md
* ARCHITECTURE.md
* PHASE_STATUS.md
* TEST_MATRIX.md

Phase 0 technical implementation already includes:

* C#
* .NET 10
* WPF
* Windows x64
* global Alt+Q
* global Alt+E
* selected text capture
* UI Automation first
* transactional clipboard fallback
* selected text replacement
* foreground-window safety checks
* multi-format clipboard snapshot and restoration
* Cloud Clipboard / Clipboard History exclusion for temporary content
* non-activating topmost overlay
* metadata-only local diagnostics
* no Enter key injection
* no automatic message sending
* automated smoke tests
* clipboard integration tests
* cross-process end-to-end tests

The user has now manually executed the COMPLETE Phase 0 TEST_MATRIX against:

* Microsoft Teams Desktop
* Microsoft Outlook Desktop
* Zalo Desktop
* Google Chrome
* Microsoft Edge

The user reports that ALL Phase 0 manual tests passed.

Therefore:

PHASE 0 SHALL NOW BE TREATED AS PASS.

Update PHASE_STATUS.md and TEST_MATRIX.md accordingly.

Do NOT invent application versions, latency measurements, Windows build numbers, tester metadata, or other values that were not actually recorded.

Where metadata is unavailable, explicitly record:

"User-confirmed PASS on 2026-09-22; detailed qualification metadata not recorded."

Do not misrepresent unknown data.

---

# 1. FIRST ACTION: CREATE PERSISTENT PROJECT MEMORY

Before implementing Phase 1, create the following persistent project files.

## AUTONOMOUS_EXECUTION.md

Save the complete operational requirements from this prompt into:

AUTONOMOUS_EXECUTION.md

This becomes the long-term execution contract.

If context is lost in a future Codex session, this file must contain enough information to resume the entire project.

## CURRENT_STATE.md

Create:

CURRENT_STATE.md

Use this structure:

# OneBoard Inline Translate - Current State

Overall status:
Current phase:
Last completed phase:
Next action:
Current version:
Current branch:
Latest commit:
Build status:
Test status:
Packaging status:
GitHub status:
Known blockers:
Important decisions:
Resume instructions:

Update this file continuously.

At minimum update it:

* before beginning a phase
* after completing a phase
* after a significant architecture change
* before committing
* after committing
* after packaging
* after tagging/releasing

The purpose is crash/context recovery.

## DECISIONS.md

Create:

docs/DECISIONS.md

For every meaningful architecture or UX decision record:

* date
* decision
* alternatives considered
* reason
* security/privacy impact if relevant

Do not record trivial coding details.

## EXECUTION_LOG.md

Create:

docs/EXECUTION_LOG.md

Append short milestone entries.

Do not turn this into verbose chain-of-thought.

Record only useful engineering history such as:

* phase started
* phase completed
* important bug discovered
* architecture migration
* build/test result
* commit SHA
* release event

---

# 2. GIT AND GITHUB INITIALIZATION

The GitHub repository currently exists but is empty:

https://github.com/phat7000/OneBoardInlineTranslate

Inspect the local directory first.

Do NOT overwrite or recreate the existing Phase 0 project.

Preserve its working implementation.

If Git is not initialized locally:

1. initialize Git
2. use main as the primary branch
3. create an appropriate .gitignore
4. add the GitHub remote
5. commit the Phase 0 baseline
6. push it to main

Suggested baseline commit:

phase 0: validated selection capture and safe replacement

If Git is already initialized, inspect its history and remote configuration and adapt safely.

Never force-push unless absolutely required to recover a repository created solely by this autonomous run.

Prefer normal fast-forward history.

After every completed phase:

* build
* test
* update documentation
* update CURRENT_STATE.md
* commit
* push to main if authentication is already available

Use meaningful commits such as:

phase 1: add production shell and settings
phase 2: harden capture and replacement services
phase 3: add translation provider architecture
phase 4: implement modern translation overlay
phase 5: add translate and replace workflows
phase 6: implement reply mode
phase 7: add region OCR translation
phase 8: harden package and prepare v1.0.0

Do not ask the user for GitHub credentials.

Use existing Git/GitHub authentication if available.

If push authentication is unavailable, continue local development and record the condition in CURRENT_STATE.md.

Do not stop implementation merely because remote push is temporarily unavailable.

---

# 3. NON-NEGOTIABLE SAFETY INVARIANTS

The Phase 0 safety model is mandatory.

Do not regress it.

These invariants apply throughout Phase 1-8.

## Never automatically send

OneBoard Inline Translate must NEVER:

* synthesize Enter to send
* trigger Ctrl+Enter to send
* click a Send button
* invoke a submit API
* submit a form automatically
* send Teams messages automatically
* send Outlook email automatically
* send Zalo messages automatically

The application may:

* capture
* translate
* rewrite
* copy
* replace selected text
* insert text into a compose field after explicit user action

The human always performs the final send action.

## Clipboard safety

Preserve the existing transactional clipboard mechanism.

Temporary clipboard contents must continue to be excluded from Windows Clipboard History / Cloud Clipboard when supported.

Always restore the user's previous clipboard contents.

If the clipboard snapshot cannot be safely created:

ABORT THE OPERATION.

Do not destroy existing clipboard contents.

## Focus safety

Normal quick translation operations must not unexpectedly steal focus.

If source HWND changes before an automated capture/paste action:

ABORT.

Reply Mode may temporarily activate its own compose overlay because the user explicitly writes inside it.

When the user explicitly chooses Insert:

* verify the original source window still exists
* restore the target only as necessary
* revalidate destination
* insert text
* never auto-send

If safe insertion cannot be guaranteed:

copy the generated response to clipboard and show a non-intrusive message instead of injecting into an uncertain destination.

## Logging privacy

Never log:

* captured text
* translated text
* OCR text
* reply text
* clipboard contents
* API keys
* provider secrets
* access tokens
* email/chat content

Diagnostic logging may include metadata such as:

* timestamp
* process
* operation type
* provider
* success/failure
* latency
* exception type

Exception message text should not be persisted if it can contain user content.

## Data persistence

Translation history is OFF by default.

Version 1.0.0 should not require storing translation history at all.

Prefer transient in-memory processing.

---

# 4. PRODUCT VISION

This is NOT another full chat assistant.

This is a lightweight Windows communication utility.

Primary user pain:

A user communicates across multiple languages in:

* Microsoft Teams
* Microsoft Outlook
* Zalo
* Chrome
* Edge
* other Windows apps

Today they repeatedly:

select text
copy it
open a translator
translate
switch back
write a response
translate the response
copy it
switch back
paste it

OneBoard Inline Translate should collapse this workflow into global hotkeys and a tiny overlay.

Primary languages for v1:

* Vietnamese
* English
* Simplified Chinese

Architecture should remain extensible for more languages.

Default user/native language:

Vietnamese

Configurable through Settings.

---

# 5. UX DESIGN DIRECTION

The user explicitly wants:

* modern
* refined
* compact
* clean
* mostly white
* optionally translucent
* no excessive colors
* no visual clutter

Design language:

Windows 11 / Fluent-inspired.

Do not build a large dashboard.

Do not create a colorful consumer-style translator.

Visual direction:

* white or subtly translucent surfaces
* soft neutral borders
* subtle shadows
* rounded corners
* compact spacing
* clear typography
* restrained icons
* very limited accent color
* smooth but subtle animation
* excellent DPI handling
* excellent multi-monitor handling

Support light mode first.

Dark mode may be implemented if low complexity, but do not compromise Phase 1-8 completion for it.

Use the native Segoe UI family available in Windows.

Do not distribute custom font files.

Where Windows 11 backdrop effects such as Mica/Acrylic are available and stable, they may be used conservatively.

Provide an elegant solid-white fallback for Windows versions where backdrop effects are unavailable or unreliable.

Do not introduce a large UI framework merely for visual effects.

Prefer native WPF and small reusable styles.

Keep runtime footprint small.

---

# 6. ARCHITECTURE PRINCIPLES

Continue using:

C#
.NET 10
WPF
Windows x64

Keep the solution modular.

Refactor the existing Phase 0 project where useful, but do not rewrite working safety-critical code without justification.

Recommended logical architecture:

src/
OneBoardInlineTranslate/
Application/
Infrastructure/
Models/
Services/
ViewModels/
Views/
Providers/
Security/
Diagnostics/
OCR/

tests/
OneBoardInlineTranslate.Tests/
OneBoardInlineTranslate.IntegrationTests/
fixtures/

scripts/

packaging/

docs/

Exact folders may be adapted if the existing layout suggests a cleaner alternative.

Prefer abstractions such as:

ITextCaptureService
ITextReplacementService
IClipboardService
IHotkeyService
ITranslationService
ITranslationProvider
ILanguageDetector
IOverlayService
IReplyService
IOcrService
ICredentialStore
ISettingsService

Do not create abstractions without practical value.

Avoid architecture astronautics.

No database is required for v1.0.0.

No backend server is required.

No authentication/login system is required.

No mandatory cloud account is required to launch the app.

---

# 7. DEPENDENCY POLICY

Phase 0 intentionally used no third-party NuGet packages.

Post-Phase-0 packages MAY be introduced when they provide significant value.

Rules:

* minimize dependencies
* prefer Microsoft / first-party platform APIs
* prefer actively maintained libraries
* avoid obscure packages
* avoid packages requiring invasive runtime services
* audit licenses
* document third-party dependencies
* no GPL dependency that would unexpectedly alter product distribution obligations unless explicitly justified and compatible
* do not use unofficial Google Translate scraping endpoints
* do not scrape websites to translate text
* do not bundle unauthorized proprietary models or services

If functionality is straightforward with platform APIs, prefer platform APIs.

---

# 8. PHASE EXECUTION RULES

For each Phase 1-8:

1. update CURRENT_STATE.md to mark phase ACTIVE
2. inspect existing implementation
3. design the smallest robust implementation
4. implement
5. add/update automated tests
6. run Release x64 build
7. run relevant automated test suites
8. run dotnet format verification
9. inspect logs for content leakage where relevant
10. update documentation
11. update PHASE_STATUS.md
12. update CURRENT_STATE.md
13. commit
14. push if available
15. automatically continue to the next phase

Never pause merely to report success.

Proceed.

If tests fail:

diagnose
fix
rerun

Do not skip failing tests just to advance phases.

A phase is complete only when its automated acceptance gate passes.

For capabilities that fundamentally require a human external application test, create a manual regression item, but do not stop autonomous execution unless the failure invalidates the architecture.

The previously user-confirmed Phase 0 external application qualification remains valid unless you materially modify its capture/replacement safety behavior.

---

# PHASE 1 - PRODUCT SHELL, SETTINGS, LIFECYCLE, MODERN UX FOUNDATION

Goal:

Turn the Phase 0 technical proof into a proper Windows background application.

Implement:

## Single instance

Only one OneBoard Inline Translate process may run per user session.

Use a safe single-instance mechanism.

If another instance exists, do not create duplicate hotkey registrations.

## System tray

Add a system tray icon.

Tray menu should include at minimum:

OneBoard Inline Translate
Status: Running

Open Settings
Pause Translation
Resume Translation
About
Exit

Keep it compact.

## Settings window

Create a refined compact Settings window.

Suggested sections:

General
Languages
Hotkeys
Translation Providers
Privacy
About

Do not overcrowd the interface.

## Persisted non-sensitive settings

Store settings under an appropriate per-user location such as:

%LOCALAPPDATA%\OneBoardInlineTranslate\

Use versioned JSON or another simple local configuration format.

Handle corrupt settings gracefully by falling back to safe defaults.

Never store secrets in plain JSON.

## Startup with Windows

Add optional:

Start OneBoard Inline Translate with Windows

Use a per-user mechanism requiring no administrator privileges.

Default may be OFF unless there is a compelling reason otherwise.

## Hotkeys

Production hotkeys:

Alt+Q
Understand selected text / translate to user's language

Alt+E
Translate selected text to English and replace selection

Alt+C
Translate selected text to Simplified Chinese and replace selection

Alt+R
Reply Mode

Alt+Shift+Q
OCR region translate

Allow the user to modify hotkeys in Settings.

Detect collisions.

If one hotkey fails to register:

do not crash the entire app.

Show which hotkey is unavailable.

## Pause mode

When paused:

* keep tray icon alive
* unregister or ignore translation hotkeys
* do not capture user content

## About

Show:

OneBoard Inline Translate
Version
GitHub repository
Privacy statement summary

No analytics SDK.

### Phase 1 acceptance

Release x64 builds cleanly.

No regressions in Phase 0 automated tests.

Single-instance behavior passes.

Settings persistence tests pass.

Hotkey registration/unregistration tests pass where automation is practical.

Tray lifecycle is stable.

Commit Phase 1 and continue automatically.

---

# PHASE 2 - PRODUCTION CAPTURE / REPLACEMENT HARDENING

Goal:

Promote Phase 0 selection technology into production services without breaking its safety guarantees.

Refactor only where useful.

Preserve:

* UIA-first capture
* clipboard fallback
* single-flight UIA behavior
* bounded UIA wait
* foreground HWND authority
* keyboard release checks
* multi-format clipboard snapshot
* clipboard restoration
* no-send invariant

Replace the Phase 0 test transformer with production action routing.

Remove `[TEST]` behavior from normal runtime.

Keep a test-only deterministic transform for automated integration tests.

Add production operation types such as:

Understand
TranslateToEnglish
TranslateToChinese
Reply
OCRTranslate

Ensure services can receive cancellation tokens where appropriate.

Improve resilience around:

* clipboard contention
* slow accessibility providers
* process shutdown
* app pause/resume
* hotkey collisions
* multi-monitor environments
* DPI changes

Do not introduce unsafe ValuePattern whole-control replacement merely to avoid clipboard paste.

Preserve surrounding formatting/content where practical.

### Phase 2 acceptance

All previous Phase 0 automated checks still pass or have equivalent renamed production tests.

The real app no longer performs `[TEST]` replacement.

Test-only harness still verifies capture/replacement behavior.

No path sends Enter.

No log contains captured text.

Commit and continue.

---

# PHASE 3 - TRANSLATION ENGINE

Goal:

Implement a production-quality provider-neutral translation layer.

## Domain model

Create clear models for:

TranslationRequest
TranslationResult
Language
ProviderHealth
ProviderConfiguration

Support:

vi
en
zh-Hans

Design language mapping to allow more languages later.

## Provider abstraction

Implement:

ITranslationProvider

Provider switching must not require changing capture/overlay/reply code.

Do not tightly couple UI to a single vendor.

## Production providers

Implement a sensible subset of providers using official APIs / supported endpoints.

Prioritize practical enterprise-friendly providers such as:

* Microsoft Azure Translator
* DeepL API
* LibreTranslate-compatible API endpoint

A generic OpenAI-compatible provider MAY be added for optional rewriting/reply tone functionality, but normal translation must not depend on an LLM.

Google Cloud Translation may be added if the official authentication model can be implemented cleanly without bloating v1.

Do NOT use unofficial Google Translate endpoints or browser scraping.

## Optional reuse audit

If this local directory exists:

C:\OneBoardCaptionTranslate\LiveCaptions-Translator

or another existing OneBoard Caption Translate source directory can be safely identified:

inspect its translation-provider implementation.

You may reuse concepts or compatible code where beneficial.

Do not modify the external project.

Before copying code:

* inspect its license
* inspect dependencies
* verify architectural suitability
* record the decision in DECISIONS.md

Do not blindly copy old code.

## Credentials

Never store API secrets in settings JSON.

Prefer Windows Credential Manager or an equivalent OS-protected per-user secret store.

If Credential Manager implementation proves disproportionately complex, Windows DPAPI may be used with clear justification.

Secrets must never appear in logs.

## Provider settings

Translation Providers settings should allow:

* provider selection
* endpoint if relevant
* API key / secret
* optional region
* Test Connection
* health/status display

Do not require a provider to be configured merely to launch the application.

If no provider is configured:

show a concise setup message when translation is requested.

Do not crash.

## Provider testing

Create deterministic fake providers for automated tests.

Live-provider tests must be optional and environment-variable driven.

Do not require real API credentials for CI.

## Language detection

Prefer provider-native language detection when available.

Normalize language identifiers across providers.

Handle mixed/unknown content gracefully.

Avoid unnecessary double API calls when a provider can detect and translate in a single request.

## Network behavior

Use HttpClient correctly.

Reuse clients/handlers.

Apply reasonable timeouts.

Support cancellation.

Handle:

* 401/403
* rate limiting
* 429
* service unavailable
* timeout
* malformed response
* unsupported language

Expose simple user-friendly errors without leaking sensitive payloads.

### Phase 3 acceptance

Translation service contract tests pass.

Fake-provider tests cover VI/EN/ZH.

Credentials never appear in settings or logs.

Provider failure does not crash the app.

Application works normally when no provider is configured.

Commit and continue.

---

# PHASE 4 - MODERN FLOATING TRANSLATION OVERLAY

Goal:

Replace the Phase 0 diagnostic overlay with the actual product experience.

## Understand workflow

User:

selects text in any supported application
presses Alt+Q

App:

captures selected text
detects source language
translates to user's preferred language
shows a compact floating overlay

Default preferred language:

Vietnamese

## Overlay appearance

Modern
minimal
white / lightly translucent
rounded
subtle shadow
small footprint

Place it intelligently:

* near selection/cursor when reliable
* otherwise near active window
* never off-screen
* respect monitor working area
* respect DPI
* support multiple monitors

Avoid covering the selected text if practical.

## Overlay content

Show:

source language
original text
translated text

Do not show developer diagnostics by default.

Developer diagnostic information may be available only through a debug view.

Useful controls:

Copy translation
Pin/unpin if low complexity
Close

A language override control may be included if elegant.

Do not clutter the overlay.

## Focus

The normal Alt+Q result overlay should not steal keyboard focus from the source application.

If clickable actions are implemented, preserve the source app as much as Windows permits.

Never cause the user to accidentally type into the wrong application.

## Size

Use adaptive limits.

Short translations produce a tiny popup.

Long translations should use a sensible maximum size with scroll behavior.

Do not create a giant window for long emails.

### Phase 4 acceptance

Overlay tests / view-model tests pass.

DPI and multi-monitor positioning logic has automated tests where feasible.

Alt+Q correctly routes:

capture -> translation -> overlay

Fake provider end-to-end test succeeds.

Focus/no-send invariants remain intact.

Commit and continue.

---

# PHASE 5 - TRANSLATE AND REPLACE

Goal:

Provide the fastest outbound communication workflow.

## Alt+E

Select Vietnamese, Chinese, or other supported text.

Press Alt+E.

Translate to natural English.

Replace only the selected text.

Do not send.

## Alt+C

Select text.

Press Alt+C.

Translate to Simplified Chinese.

Replace only the selected text.

Do not send.

## Behavior

Capture the original selection.

Translate.

Before replacement:

revalidate original destination.

If the source target changed:

do NOT paste into the new window.

Instead show the translated result safely.

Preserve clipboard contents.

Preserve surrounding content.

Do not automatically modify more than the selected range.

## Feedback

For successful quick replacement:

use minimal visual feedback.

Avoid unnecessary modal dialogs.

For slow translation:

a small non-blocking progress indicator is acceptable.

For failure:

show a compact error notification with retry/copy option where appropriate.

## Natural translation

Normal translation should preserve meaning rather than perform free-form rewriting.

Do not let an LLM unexpectedly change names, numbers, URLs, dates, technical terms, or intent.

Optional rewriting belongs to Reply/Tone features, not standard Translate & Replace.

### Phase 5 acceptance

Automated end-to-end harness verifies:

capture
fake translate
replace
clipboard restore
foreground validation
no Enter
no send

Test Vietnamese diacritics.

Test Simplified Chinese.

Test multiline content.

Test punctuation.

Test emoji.

Test URLs.

Test numbers.

Commit and continue.

---

# PHASE 6 - REPLY MODE

Goal:

Turn translation into a cross-language communication workflow.

Hotkey:

Alt+R

## Incoming message workflow

User selects an incoming foreign-language message.

Presses Alt+R.

App captures it.

App detects its language.

App translates it to the user's preferred language.

Open a compact Reply panel.

## Reply panel

Display:

Original
Vietnamese understanding/translation

Then:

Reply in Vietnamese:
[text box]

Actions:

Translate Reply
Insert
Copy
Cancel

Do not auto-send.

## Response language

Default response target should be the detected language of the incoming selection.

Example:

incoming Chinese -> reply output Chinese

incoming English -> reply output English

Allow the user to manually override target language.

## Translate Reply

User writes:

"Ok, tôi sẽ kiểm tra với team và phản hồi anh trước 10 giờ sáng mai."

App translates it into the target language.

Show final result before insertion.

The user must be able to inspect the translation before inserting it.

## Insert

Only on explicit Insert action:

* ensure the original target still exists
* return to the original target safely if reasonable
* verify destination
* insert translated text
* do NOT press Enter
* do NOT trigger Send

If exact destination safety is uncertain:

copy the translated reply to clipboard
notify the user
do not inject blindly

## Tone

If a generic LLM/OpenAI-compatible provider is configured, optionally support:

Natural
Business
Friendly
Formal

This is OPTIONAL enhancement.

Do not make Phase 6 depend on an AI provider.

Without AI, Reply Mode must still work perfectly as:

incoming translate
Vietnamese compose
translate back
insert

If AI rewriting is implemented:

* clearly distinguish Translate from Rewrite
* never silently change meaning
* preserve names, numbers, dates, URLs, identifiers
* let user review output before insertion

### Phase 6 acceptance

Automated Reply Mode tests cover:

English incoming
Chinese incoming
Vietnamese reply
translation back
target-language selection
safe insert
fallback-to-copy
no Enter/send

Commit and continue.

---

# PHASE 7 - OCR REGION TRANSLATE

Goal:

Support text that cannot be selected.

Hotkey:

Alt+Shift+Q

Workflow:

hotkey
region selection overlay
user drags a rectangle
screenshot only selected region
OCR locally
translate OCR output
display normal translation overlay

## OCR requirements

Prefer Windows-native OCR capabilities where practical.

Avoid cloud OCR for v1 unless no safe local Windows solution is viable.

Do not send screenshots to translation providers.

Only extracted text may be translated, subject to provider configuration.

Do not persist screenshots.

Dispose of screenshot bitmaps promptly after OCR.

Do not write screenshots to disk unless a test fixture explicitly requires a temporary test file.

## Region selector

Create a clean translucent full-screen or per-monitor selection overlay.

Requirements:

* Escape cancels
* drag to select
* visible but restrained selection border
* accurate coordinates under DPI scaling
* multi-monitor aware

Do not make the selector visually flashy.

## OCR language

Support installed Windows OCR languages where available.

At minimum handle practical extraction for:

English
Vietnamese where the Windows OCR environment supports it
Simplified Chinese where installed/available

If an OCR language pack is unavailable:

show a clear setup message.

Do not silently download large language packs.

## OCR tests

Use static local image fixtures containing:

English
Vietnamese
Simplified Chinese
mixed punctuation
numbers

Test OCR service separately from live screen selection.

### Phase 7 acceptance

OCR fixtures pass within reasonable recognition tolerance.

No screenshot is persisted in normal runtime.

OCR text is not logged.

Region selection handles Escape.

Translation overlay receives extracted text.

Commit and continue.

---

# PHASE 8 - SECURITY, QUALITY, PACKAGING, CI/CD, RELEASE V1.0.0

Goal:

Produce a distributable public v1.0.0.

## Security audit

Audit:

clipboard handling
credential storage
logging
temporary files
network requests
OCR buffers
settings
exception handling
process integrity behavior
auto-start
dependency licenses

Search the repository for accidental secrets.

Search logs/tests for sensitive content leakage.

No hardcoded API key.

No committed real credential.

No telemetry.

No analytics.

## Process integrity

Continue using least privilege.

Do not request administrator privileges merely to interact with elevated apps.

Document that Windows UIPI can prevent interaction with higher-integrity applications.

Use asInvoker unless an exceptionally strong reason exists.

## Settings privacy

Provide a Privacy section with at least:

Translation history: Off / not stored
Telemetry: Off / not implemented
Clipboard restoration: On
Auto-send: Never

If provider content is transmitted to a cloud provider, make that fact clear.

Do not claim text is fully local when a cloud translation provider is selected.

## UI polish

Conduct a final UX pass.

Focus on:

* spacing
* typography
* clipping
* DPI
* keyboard navigation
* accessibility names
* error states
* empty provider states
* long text
* multiline text
* high DPI
* multi-monitor
* light theme consistency

Keep visual design restrained.

No unnecessary gradients or bright colors.

## Accessibility

Use appropriate automation names for interactive controls.

Respect standard keyboard behavior.

Avoid tiny hit targets.

Do not depend solely on color to indicate state.

## Version

Set product version:

1.0.0

Product name:

OneBoard Inline Translate

Company/brand:

OneBoard

Repository:

https://github.com/phat7000/OneBoardInlineTranslate

## README

Rewrite README.md into public product documentation.

Include:

* what the product does
* screenshots placeholders or actual screenshots if automatically capturable
* key features
* hotkeys
* supported workflows
* privacy design
* supported Windows versions
* translation provider setup
* OCR requirements
* portable usage
* installer usage
* build from source
* troubleshooting
* limitations
* license
* GitHub repository

Do not retain Phase 0 proof-of-concept wording as the main README.

Move historical Phase 0 details into docs if useful.

## Documentation

Final docs should include at minimum:

README.md
ARCHITECTURE.md
PHASE_STATUS.md
CURRENT_STATE.md
AUTONOMOUS_EXECUTION.md
docs/DECISIONS.md
docs/EXECUTION_LOG.md
docs/PRIVACY.md
docs/SECURITY.md
docs/PROVIDERS.md
docs/TESTING.md
docs/RELEASE_CHECKLIST.md

Keep documentation factual.

## Automated test suite

Run all:

unit tests
smoke tests
clipboard integration
cross-process end-to-end
translation provider contract tests
fake-provider tests
settings tests
security/redaction tests
OCR fixture tests

Run:

dotnet format --verify-no-changes

Release build must have:

0 errors

Aim for:

0 warnings

Fix warnings unless they are documented toolchain noise with a clear justification.

## Soak / reliability testing

Create an automated stress or repeat test for safe operations where possible.

Exercise capture/replace transactions repeatedly.

Look for:

clipboard restoration failures
resource leaks
stuck UIA calls
unbounded tasks
window leaks
race conditions

Do not hammer real cloud APIs.

Use fake providers.

## Portable build

Produce a Windows x64 self-contained portable release.

Target artifact naming:

OneBoardInlineTranslate-1.0.0-win-x64.zip

The user should be able to:

download
extract
run

without installing .NET separately if technically practical.

Prefer self-contained publish.

Do not include source files or development artifacts in the portable package.

## Installer

Produce:

OneBoardInlineTranslate-Setup-1.0.0-win-x64.exe

Use Inno Setup or another mature lightweight installer.

Installer requirements:

per-user installation where practical
no admin requirement unless unavoidable
Start Menu shortcut
optional desktop shortcut
clean uninstall
preserve or clearly handle user settings
no bundled adware
no bundled runtime downloaded during install if using self-contained publish

Do not install unrelated components.

## Code signing

If no trusted code-signing certificate is available:

DO NOT create a fake public certificate.

DO NOT self-sign and pretend it is trusted.

Ship unsigned and document:

"Unsigned build - Windows SmartScreen may show a warning."

Lack of a signing certificate must NOT block v1.0.0.

## GitHub Actions

Create CI workflow for pushes / pull requests.

Use Windows runner.

CI should at least:

restore
build Release x64
run safe automated tests
verify formatting where practical

Create release workflow triggered by tags matching:

v*

The release workflow should build the distributable artifacts and create a GitHub Release.

Set appropriate workflow permission:

contents: write

Attach:

OneBoardInlineTranslate-1.0.0-win-x64.zip
OneBoardInlineTranslate-Setup-1.0.0-win-x64.exe

If local Inno Setup is unavailable, the GitHub Actions release runner may install/use Inno Setup in the workflow.

Do not rely on a manually configured build machine.

## License

Inspect reused code and dependencies first.

If there is no incompatible requirement and no existing repository license:

use a simple permissive MIT License for this public OneBoard Free App.

Document third-party license obligations if any.

Do not use code whose license is incompatible with the intended repository distribution.

## Final repository cleanup

Before release:

remove bin/
remove obj/
remove temp/
remove local logs
remove private settings
remove API credentials
remove development-only screenshots containing private content
remove unnecessary generated files

Ensure .gitignore is correct.

## Final Git process

After all tests pass:

commit final release preparation.

Push main.

Create annotated tag:

v1.0.0

Push tag.

If GitHub Actions release workflow is configured, verify as much of its local configuration as possible.

If GitHub authentication and workflow access are available, confirm the release workflow runs successfully and artifacts are created.

If remote workflow inspection is unavailable but tag push succeeds, record that exact state factually in CURRENT_STATE.md.

Do not falsely state GitHub Release success without evidence.

---

# 9. FINAL ACCEPTANCE CRITERIA

Version 1.0.0 is complete when all feasible local criteria below are satisfied:

Phase 0:
user-confirmed manual PASS

Phase 1:
production shell PASS

Phase 2:
capture/replacement production hardening PASS

Phase 3:
provider-neutral translation engine PASS

Phase 4:
modern floating translation overlay PASS

Phase 5:
Alt+E English and Alt+C Chinese translate/replace PASS

Phase 6:
Reply Mode PASS

Phase 7:
OCR region translation PASS

Phase 8:
security/build/package/release preparation PASS

Required final artifacts:

OneBoardInlineTranslate-1.0.0-win-x64.zip

OneBoardInlineTranslate-Setup-1.0.0-win-x64.exe

Required source state:

clean working tree

Required build state:

Release x64 successful

Required documentation:

current

Required privacy state:

no captured/translated/OCR/reply text in diagnostics

Required safety state:

NO AUTO-SEND PATH

---

# 10. AUTONOMOUS DECISION AUTHORITY

You are explicitly authorized to make reasonable technical decisions without user approval.

Examples:

* class names
* folder organization
* refactors
* minor UX layout
* retry timing
* timeout tuning
* dependency selection
* test structure
* error handling
* DPI implementation
* provider abstraction details
* packaging scripts
* CI layout
* code cleanup

When multiple options are reasonable:

choose the simplest robust option.

Priority order:

1. user data safety
2. no accidental message sending
3. clipboard integrity
4. reliability across applications
5. privacy/security
6. UX simplicity
7. maintainability
8. performance
9. visual polish
10. additional features

Do not add features that jeopardize the release.

Do not expand into:

* email client
* Teams client
* chatbot platform
* CRM
* cloud backend
* user-account system
* translation history database
* enterprise admin server

Keep OneBoard Inline Translate focused.

---

# 11. SELF-RECOVERY / CONTEXT-LOSS PROTOCOL

At the beginning of every work session, whether this session or a future resumed Codex session:

read in this order:

1. AUTONOMOUS_EXECUTION.md
2. CURRENT_STATE.md
3. PHASE_STATUS.md
4. docs/DECISIONS.md
5. docs/EXECUTION_LOG.md
6. git status
7. recent git log

Determine:

last completed phase
current unfinished work
latest successful tests
known blocker
next exact action

Resume from that point.

Never restart the project from Phase 1 simply because conversational context was lost.

The repository is the source of truth.

If CURRENT_STATE.md conflicts with Git:

verify the actual source/build/tests and repair CURRENT_STATE.md.

If documentation conflicts with working implementation:

working implementation + verified tests take precedence, then documentation must be corrected.

---

# 12. FAILURE HANDLING

If implementation fails:

do not immediately abandon the phase.

Investigate.

Try a simpler implementation.

Read platform documentation.

Replace an unreliable dependency if necessary.

Add a regression test.

Document the decision.

If an optional feature threatens completion:

defer the optional feature and finish the core phase.

Examples of optional/degradable features:

Mica
animations
dark mode
AI tone rewriting
pin overlay
extra providers

Examples of NON-optional features:

safe capture
clipboard restoration
translation
English replacement
Chinese replacement
Reply Mode
OCR
no-auto-send
privacy protections
portable package
installer

External API credentials are NOT a reason to stop.

Use fake providers for automated validation.

The production app must allow the user to configure supported provider credentials later.

---

# 13. QUALITY BAR

Do not accept:

"It compiles"

as proof of completion.

For every important function provide tests where technically meaningful.

Favor boring reliable engineering over clever code.

Do not suppress exceptions silently.

Do not expose raw exception details to ordinary users.

Do not swallow clipboard restoration errors.

Do not write user text into logs for debugging convenience.

Do not add telemetry.

Do not add advertisements.

Do not add account creation.

Do not make network calls when the user has not configured a provider or invoked a function requiring one.

---

# 14. FINAL COMPLETION REPORT

Only after Phase 8 is complete, produce a concise final report containing:

Overall status

Phase 0 through Phase 8 status

Git commit SHA

GitHub repository status

v1.0.0 tag status

Release build result

Automated test result

Portable ZIP path

Installer EXE path

GitHub Release status if verified

Implemented translation providers

OCR implementation

Known limitations

Any remaining external-only requirement such as code signing certificate

Do not output a huge narrative.

Do not claim success for anything that was not actually verified.

---

# EXECUTION COMMAND

Begin now.

First inspect the current repository and existing Phase 0 source.

Create the persistent project-memory files.

Update Phase 0 to user-confirmed PASS without fabricating missing qualification metadata.

Initialize/synchronize GitHub.

Then execute Phase 1, Phase 2, Phase 3, Phase 4, Phase 5, Phase 6, Phase 7, and Phase 8 sequentially.

Do not stop after a phase.

Do not ask for permission to continue.

Continue until v1.0.0 is complete or until the only remaining blockers are genuinely external and cannot be solved by code.
