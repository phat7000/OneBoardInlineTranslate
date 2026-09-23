# Testing

## Release gate

```powershell
.\scripts\Build-Release.ps1
```

This restores, verifies formatting, builds Release x64, and runs the default suite.

```powershell
dotnet run --project .\tests\OneBoardInlineTranslate.SmokeTests\OneBoardInlineTranslate.SmokeTests.csproj -c Release -p:Platform=x64 --no-build
```

The 1.2 suite covers:

- capture/clipboard/no-Enter/security regressions and metadata redaction;
- settings persistence, corruption recovery, schema-1 migration, quick targets, and result bounds;
- Google, Azure, DeepL, LibreTranslate, TranslatePlus, and Langbly contracts using fake HTTP handlers;
- TranslatePlus/Langbly authentication, detection shape, language APIs, endpoint choice, invalid key, 429, timeout, cancellation, 5xx, and malformed bodies;
- language normalization, search, feature/recent ordering, capability filtering, and known-unsupported prevention;
- Popup/Pinned/Hidden policy, custom/persistent bounds, missing-work-area recovery, and long-content clamping;
- local manifest integrity, HTTPS/hash/size checks, corruption rejection, safe paths/extraction, cancellation, install marker/remove, capability discovery, unavailable direction, and direct/pivot routing without cloud fallback;
- icon embedded in the executable, OCR fixtures, and bounded translation stress.

Provider tests use no live keys or quota. Routing tests use a fake inference engine; the real engine/model benchmark and output samples are separately recorded in `LOCAL_TRANSLATION_EVALUATION.md`.

## Clipboard integration

```powershell
dotnet run --project .\tests\OneBoardInlineTranslate.SmokeTests\OneBoardInlineTranslate.SmokeTests.csproj -c Release -p:Platform=x64 --no-build -- --clipboard-integration
```

This temporarily controls and then restores the real clipboard.

## Cross-process end to end

```powershell
dotnet run --project .\tests\OneBoardInlineTranslate.SmokeTests\OneBoardInlineTranslate.SmokeTests.csproj -c Release -p:Platform=x64 --no-build -- --end-to-end
```

This launches the production executable in explicit `--integration-test` mode, exercises UIA and forced clipboard-fallback paths, and verifies focus, selected-range replacement, restoration, and zero Enter events. Normal operation never selects the deterministic `[TEST]` provider.

## Packaging checks

After `scripts\Package-Release.ps1`, verify ProductVersion 1.2.0, embedded executable/installer icons, no source/PDB files in the ZIP, a clean portable launch, and silent per-user install/launch/uninstall. Record artifact size and SHA-256 in `CURRENT_STATE.md` and `docs/EXECUTION_LOG.md`.

The historical external Teams/Outlook/Zalo/Chrome/Edge matrix in `TEST_MATRIX.md` does not need repetition because 1.2 did not materially rewrite the validated capture/replacement core.
