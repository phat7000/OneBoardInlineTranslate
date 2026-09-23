# UX

## Settings

The compact 820×650 target uses a native WPF left rail: General, Languages, Hotkeys, Providers, Local Translation, Result Window, Privacy, and About. Content uses quiet white surfaces, neutral separators, restrained OneBoard blue, Segoe UI, keyboard-native controls, and explicit automation names on sensitive actions.

Provider configuration shows only relevant controls: fixed-endpoint providers do not show endpoint fields; Langbly shows Global/EU and reveals a URL only for Custom; Local shows no key. Model cards show direction, install state, download size, progress/cancel, and Remove.

## Searchable languages

Editable dropdowns filter by language/native name/code. Important languages remain first, then recents, then alphabetical provider-supported results. Quick Target hotkey labels update to show their current language.

## Result surface

One reusable `OverlayWindow` renders quiet language chips, original text, translation, provider/latency, Copy, and Close. Long content scrolls instead of growing beyond the working area.

### Popup

Non-activating and automatically hidden after 12 seconds. Auto size is default; Small, Medium, Large, and Custom are available. Position can be near the current selection-pointer proxy, any monitor corner, or saved custom coordinates. Native placement is clamped inside the pointer's monitor work area.

### Pinned

The same window becomes activating, movable, and resizable. Every new result updates it; a translation does not create another panel. Physical position, DIP size, and monitor device name are saved. If that monitor disappears, the panel uses the source/nearest screen and clamps into its working area. Unpin changes the configured mode back to Popup.

### Hidden

A successful Quick Target replacement produces no confirmation popup. Errors, provider failure, source-window change, and unsafe replacement still show UI. Understand and OCR are explicit result actions, so Hidden temporarily shows the Small result surface; it never performs an invisible result-only action.

## Reply Mode

Reply retains the established workflow: selected incoming text → preferred-language understanding → write → translate → review → explicit Insert or Copy. Insert revalidates the original window and never sends. The surface now shares the compact styling and complete searchable provider language list.

## Accessibility and motion

Controls retain standard WPF keyboard/focus behavior and automation names. Content is shown immediately; there is no animation that delays results. The design currently uses no required motion, so reduced-motion users receive the same immediate behavior.
