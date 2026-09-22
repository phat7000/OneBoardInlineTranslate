# Testing

## Release gate

```powershell
.\scripts\Build-Release.ps1
```

This restores dependencies, verifies formatting, builds Release x64, and runs the safe default suite.

## Default automated suite

```powershell
dotnet run --project .\tests\OneBoardInlineTranslate.SmokeTests\OneBoardInlineTranslate.SmokeTests.csproj -c Release -p:Platform=x64 --no-build
```

Coverage includes diagnostic allow-lists/redaction, no-Enter declaration, hotkeys, single instance, settings and corrupt recovery, DPAPI, language detection, fake translation, supported provider contracts, overlay placement, region coordinates, static OCR fixtures, and fake-provider stress.

## Clipboard integration

This opt-in test temporarily controls the global clipboard and restores the pre-test value:

```powershell
dotnet run --project .\tests\OneBoardInlineTranslate.SmokeTests\OneBoardInlineTranslate.SmokeTests.csproj -c Release -p:Platform=x64 --no-build -- --clipboard-integration
```

## Cross-process end to end

This opt-in test launches the real app in `--integration-test` mode with the test-only deterministic provider, drives UIA and forced clipboard-fallback paths, and verifies focus, replacement, restoration, and zero Enter events:

```powershell
dotnet run --project .\tests\OneBoardInlineTranslate.SmokeTests\OneBoardInlineTranslate.SmokeTests.csproj -c Release -p:Platform=x64 --no-build -- --end-to-end
```

Integration-test mode is selected only by an explicit command-line switch. The normal application never performs the `[TEST]` transform.

## Manual regression

The historical Phase 0 external-app qualification is recorded in [TEST_MATRIX.md](../TEST_MATRIX.md). Re-run the relevant target-app rows after materially changing UI Automation, clipboard transactions, input injection, overlay activation, or foreground validation.

For v1 workflows, use test conversations/drafts and verify: no automatic send; only the selection changes; clipboard formats restore; focus remains correct; long/multiline text scrolls; provider errors reveal no content; Reply Insert falls back to copy when the source closes; OCR Escape cancels and no screenshot file appears.
