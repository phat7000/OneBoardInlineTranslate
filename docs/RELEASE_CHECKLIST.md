# v1.0.0 Release Checklist

## Product and safety

- [x] Phase 0 user-confirmed external-app matrix recorded factually.
- [x] Production runtime contains no Phase 0 test transform path.
- [x] No Enter virtual key or auto-send integration.
- [x] Clipboard restoration and foreground authority retained.
- [x] Translation history and telemetry absent.
- [x] Provider secrets protected with per-user DPAPI.
- [x] OCR screenshots remain in memory and local.

## Quality

- [x] Final Release x64 build has 0 errors and 0 warnings.
- [x] Default automated suite passes.
- [x] Clipboard integration suite passes.
- [x] Cross-process end-to-end suite passes.
- [x] `dotnet format --verify-no-changes` passes.
- [x] Repository secret/content-log audit passes.
- [x] Published app launch smoke passes.

## Artifacts

- [x] `artifacts/OneBoardInlineTranslate-1.0.0-win-x64.zip` created and inspected.
- [x] `artifacts/OneBoardInlineTranslate-Setup-1.0.0-win-x64.exe` created and inspected.
- [x] Installer is per-user, x64, self-contained, and has clean uninstall metadata.
- [x] Unsigned status documented; no fake certificate.

## GitHub

- [x] Windows CI workflow added.
- [x] `v*` release workflow added with `contents: write`.
- [x] Clean final source tree committed and pushed to `main`.
- [x] Annotated `v1.0.0` tag created and pushed.
- [ ] GitHub Actions release result and attached artifacts verified.
