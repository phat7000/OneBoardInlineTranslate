# Third-party notices

OneBoard Inline Translate has no third-party runtime NuGet package dependency.

The self-contained portable and installer artifacts include Microsoft .NET runtime components. Those components are distributed under their applicable Microsoft and open-source license terms. See the license files included in the published runtime and the [.NET license repository](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT).

The application uses Windows platform APIs, including WPF, Windows Forms notification-area integration, UI Automation, DPAPI, GDI screen capture, and Windows.Media.Ocr. No OCR model or language pack is bundled by this repository.

Inno Setup is used as a build tool for the installer and is not bundled as an installed application component.

## Optional Local Translation components

These components are not bundled in the base application. They are downloaded only when the user requests a local model and remain subject to their respective licenses:

- Python 3.12.10 embeddable runtime — Python Software Foundation License 2.0. Copyright Python Software Foundation and contributors.
- CTranslate2 4.8.2 — MIT License. Copyright OpenNMT/CTranslate2 contributors.
- SentencePiece 0.2.1 — Apache License 2.0. Copyright Google Inc. and contributors.
- setuptools 75.8.2 — MIT License. Copyright Python Packaging Authority and contributors.
- Argos-compatible OPUS-MT translation packages version 1.9 — package metadata identifies CC-BY-4.0. Original OPUS models are associated with Jörg Tiedemann, Santhosh Thottingal, and OPUS-MT contributors.

Relevant upstream projects and full license texts are available from:

- https://www.python.org/downloads/release/python-31210/
- https://github.com/OpenNMT/CTranslate2
- https://github.com/google/sentencepiece
- https://github.com/pypa/setuptools
- https://github.com/argosopentech/argos-translate
- https://github.com/Helsinki-NLP/OPUS-MT-train

Model use and redistribution must retain the applicable CC-BY-4.0 attribution. A common OPUS-MT citation is: Jörg Tiedemann and Santhosh Thottingal, “OPUS-MT — Building open translation services for the World,” EAMT 2020.
