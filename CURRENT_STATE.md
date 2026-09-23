# Current State

Status date: 2026-09-23

- Product version: 1.2.0
- Branch: `main`, tracking `origin/main`
- Upgrade status: implementation and local release gate complete.
- Preserved baseline: the validated UIA/clipboard/foreground/replacement core and existing Google Cloud work were retained.
- Providers: Google Cloud Translation, Azure Translator, DeepL, LibreTranslate, TranslatePlus v2, Langbly, and Local Translation.
- Languages: broad built-in fallback plus provider capability discovery/cache, searchable name/native-name/code selection, recent ordering, configurable preferred/quick targets, and unsupported-target prevention.
- Local engine: on-demand private Python 3.12 + CTranslate2 4.8.2 + SentencePiece 0.2.1 runtime with four Argos-compatible OPUS-MT packages. No Ollama, LLM, installed Python, or configured server.
- Result UX: Popup, reusable Pinned, and Hidden modes; saved size/position/monitor with work-area recovery.
- Build/test: format verification PASS; Release x64 0 warnings/errors; default 49/49; clipboard 50/50; final isolated cross-process UIA/clipboard/no-Enter run 50/50.
- Packaging: portable `artifacts/OneBoardInlineTranslate-1.2.0-win-x64.zip` — 83,064,726 bytes, SHA-256 `AA4DA83FB944B99B6E8E085880A21485CE2AB8B879147EE314A7610D493D9D45`; installer `artifacts/OneBoardInlineTranslate-Setup-1.2.0-win-x64.exe` — 60,910,970 bytes, SHA-256 `F54F1C11AEF515672915E052D8435A6FD97C42F06914C159C2EDAB76D84C8BCF`.
- Packaging smoke: ProductVersion 1.2.0, executable and installer icon extraction PASS, worker included, no source/PDB in ZIP, portable launch PASS. Installer compilation passed; a live install was not run over the user's currently running older installed instance.
- External provider testing: automated cloud tests use fake HTTP handlers. No live paid-provider credential was used.
- Signing: no trusted certificate is present; artifacts are unsigned.
- GitHub status: implementation commit `d466d76` is tagged `v1.2.0`; `main` includes the follow-up release-workflow filename correction. CI and Release workflows passed. The public non-draft/non-prerelease GitHub Release contains the verified portable ZIP and installer. Existing release tags remain immutable.
- Immutable history: the published `v1.0.0` tag remains unchanged. No existing tag is moved or overwritten.

Resume by reading this file, `PHASE_STATUS.md`, `docs/DECISIONS.md`, and `docs/EXECUTION_LOG.md`, then inspect `git status`, `git log`, and `origin/main`.
