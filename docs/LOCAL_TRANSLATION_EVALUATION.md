# Local Translation Evaluation

Evaluation date: 2026-09-23

## Decision

Use Argos-compatible OPUS-MT direction packages with CTranslate2 and SentencePiece in a private, on-demand Python embeddable runtime. This is neural machine translation, not a general-purpose LLM. The application starts the worker itself over anonymous pipes; users install no Python and configure no server.

## Options considered

| Option | Strengths | Blocking tradeoff for this release |
|---|---|---|
| CTranslate2 + OPUS-MT/Argos packages | Mature CPU inference, int8, fast warm path, compact direction packages, usable VI/EN/ZH packages, clear Windows wheels | Private runtime is larger than a single native DLL; packages are directional; quality trails top cloud systems |
| Bergamot/Marian native | Native/offline and potentially smaller runtime | Current dependable Windows redistribution/model pipeline and required VI/EN/ZH coverage were not as straightforward; available artifacts/coverage were less maintainable for this release |
| ONNX Runtime converted Marian | Strong .NET/Windows runtime and no Python worker | Requires maintaining model conversion, tokenizer parity, generation/beam-search logic, and separate validated artifacts; total engineering/supply-chain surface was larger |
| Full Argos Translate runtime | Existing package ecosystem and routing | Pulls substantially more Python/NLP dependencies than inference needs; OneBoard uses only compatible packages, CTranslate2, and SentencePiece |
| Ollama/general LLM | Broad generative capability | Rejected: multi-GB footprint, slower CPU path, nondeterministic rewriting risk, and explicitly outside the translation-only release |

The selected design provides the best practical balance of measured latency, deployment cleanliness, direction availability, license clarity, and maintenance without adding a NuGet/runtime dependency to the base package.

## Trusted runtime

Downloaded only with the first requested model:

| Component | Version | Download |
|---|---:|---:|
| Python embeddable x64 | 3.12.10 | 10.6 MiB |
| CTranslate2 Windows x64 | 4.8.2 | 18.3 MiB |
| SentencePiece Windows x64 | 0.2.1 | 1.0 MiB |
| setuptools support files | 75.8.2 | 1.2 MiB |
| Total | | 31.1 MiB |

Measured installed runtime footprint: 87.4 MiB (91,636,356 bytes). Runtime size is not part of the base portable/installer artifact.

## Models

| Direction | Package version | Download | Measured installed |
|---|---:|---:|---:|
| Vietnamese → English | 1.9 | 62.8 MiB | 76.0 MiB |
| English → Vietnamese | 1.9 | 64.6 MiB | 75.7 MiB |
| English → Chinese Simplified | 1.9 | 67.5 MiB | 81.7 MiB |
| Chinese Simplified → English | 1.9 | 71.0 MiB | 82.1 MiB |

Download bytes and SHA-256 values are fixed in `LocalModelManifest`; installation rejects any mismatch. Model packages identify the original OPUS-MT work and use CC-BY-4.0. See `THIRD_PARTY_NOTICES.md`.

Vietnamese↔Chinese uses English pivot only when the two required direction packages are installed and no direct verified package is available. The result is marked as pivot and may lose more nuance.

## Measured performance

Machine: 12th Gen Intel Core i5-1245U (10 cores/12 logical processors), 31.7 GiB RAM, Windows build 26200, CPU int8, short one-sentence communication text, beam size 4. Numbers are observations on this machine, not universal guarantees.

Product worker measurement for Vietnamese → English: about 313 ms from new worker/model to first response, 28 ms warm, approximately 120.1 MB worker working set.

The following isolated per-direction run measured model initialization plus first translation after the interpreter/imports were ready; warm is the median of seven translations:

| Direction | Cold model + first | Warm median | Working set | Observed output |
|---|---:|---:|---:|---|
| VI → EN | 167.0 ms | 29.6 ms | 119.7 MB | “Please confirm this today.” |
| EN → VI | 233.2 ms | 59.9 ms | 119.4 MB | “Anh có thể xác nhận điều này hôm nay không?” |
| EN → ZH-Hans | 192.4 ms | 31.9 ms | 126.5 MB | “你今天能确认一下吗?” |
| ZH-Hans → EN | 202.3 ms | 37.6 ms | 127.3 MB | “Can you confirm this today?” |

Two loaded models:

| Pivot | Cold load + first | Warm median | Working set |
|---|---:|---:|---:|
| VI → EN → ZH-Hans | 415.6 ms | 59.7 ms | 215.0 MB |
| ZH-Hans → EN → VI | 475.9 ms | 90.6 ms | 215.9 MB |

These samples confirm interactive warm behavior and basic short-message quality in all required directions. They are not a linguistic benchmark. Proper nouns, domain terminology, long context, ambiguity, and pivot translation remain known quality limits.

## Runtime behavior

- One worker is retained across translations and models are loaded lazily.
- An LRU cache keeps at most 1–3 models (default 2), preventing every installed model from remaining in RAM.
- Direct direction is preferred. Pivot uses exactly two local inferences and is explicitly reported.
- Cancellation terminates the worker to avoid an indeterminate outstanding request.
- Local mode has no cloud fallback. Missing directions produce “Local model not installed” with a path to Settings → Local.
- Text is read from stdin and returned on stdout; errors return only an exception type. No translation text is logged or persisted.

## Sources

- [CTranslate2 documentation](https://opennmt.net/CTranslate2/)
- [CTranslate2 repository/license](https://github.com/OpenNMT/CTranslate2)
- [SentencePiece repository/license](https://github.com/google/sentencepiece)
- [Argos package index](https://argos-net.com/)
- [OPUS-MT models](https://github.com/Helsinki-NLP/OPUS-MT-train)
