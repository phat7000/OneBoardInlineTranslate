# Security

## Threat model and controls

- Clipboard loss: fail-closed eager snapshot, bounded contention retry, persistent restoration, and cancellation-independent cleanup.
- Wrong-window injection: hotkey-time HWND authority and immediate foreground validation before every synthetic chord.
- Secret disclosure: per-user DPAPI protection, no settings serialization, no logging, and no hardcoded production credential.
- Content disclosure: allow-listed metadata diagnostics and content-redacting domain string representations.
- Untrusted transport: HTTPS required for remote provider endpoints; HTTP allowed only for loopback LibreTranslate. Google Cloud credentials are sent only in `X-Goog-Api-Key`, never in a URL.
- Screenshot disclosure: region pixels remain in memory and are never sent or persisted.
- Accidental send: only Ctrl+C and Ctrl+V are synthesized; no Enter key path or target-app submit integration exists.

## Process integrity

The application manifest requests `asInvoker` with `uiAccess=false`. OneBoard does not request administrator privileges to bypass Windows User Interface Privilege Isolation. Interaction with elevated applications may fail and should be treated as a platform safety boundary.

## Network behavior

The app has no backend and performs no translation network request before a user configures a provider and invokes an operation. Provider requests use supported JSON/form APIs and a bounded timeout. Google support uses only the official Cloud Translation Basic v2 API; unofficial Google Translate endpoints and scraping remain prohibited.

## Reporting a vulnerability

Use the repository's private security reporting feature if available. Do not attach real message content, clipboard data, credentials, or provider responses. A useful report includes the app version, operation type, target process, reproduction steps with synthetic data, and exception type.

## Release signing

Version 1.1.0 is unsigned unless the GitHub release explicitly states otherwise. No self-signed certificate is presented as trusted. Windows SmartScreen may warn when the executable or installer is first downloaded.
