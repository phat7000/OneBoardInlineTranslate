# Security

## Controls

- Wrong-window injection: hotkey-time HWND/process authority and foreground revalidation before Ctrl+C/Ctrl+V.
- Clipboard loss: eager multi-format snapshot, bounded retry, persistent cancellation-independent restore, fail closed on an unsafe snapshot.
- Accidental send: no Enter virtual key, Send-button discovery, submit API, or automatic Reply insertion.
- Secret disclosure: provider-specific current-user DPAPI storage; no key in settings, URLs, source, logs, exception text, or diagnostics.
- Content disclosure: allow-listed metadata diagnostics and content-redacting domain `ToString()` implementations.
- Transport: HTTPS for remote endpoints; loopback-only HTTP exception for self-hosted LibreTranslate.
- Screenshot disclosure: selected pixels stay in memory and have no runtime disk/network path.
- Local supply chain: trusted manifest, HTTPS, random temporary paths, exact byte length and SHA-256 verification, zip traversal rejection, package direction/file validation, atomic activation, and failed-stage cleanup.
- Local isolation: no server port, installed Python dependency, shell interpolation, cloud fallback, or user-text process argument.

The app manifest is `asInvoker` with `uiAccess=false`; it does not elevate to bypass UIPI. Interaction with an elevated target may fail by design.

## Local component trust

Runtime/model URLs, versions, sizes, hashes, engine type, and licenses are compiled into `LocalModelManifest`. A payload is not extracted until its size/hash match and is not activated until its expected structure/direction validates. Archive entries must resolve inside a checked staging root. Cancellation removes temporary downloads/staging.

The persistent local worker returns only an error type on failure. It does not echo input, exception messages, or traceback content to the application. Standard error is drained but not stored.

## Release posture

Version 1.2.0 artifacts are unsigned unless a GitHub release explicitly says otherwise. No self-signed certificate is represented as trusted. SmartScreen may warn.

Report vulnerabilities through the repository's private security-reporting feature where available. Use synthetic content and omit credentials/provider bodies.
