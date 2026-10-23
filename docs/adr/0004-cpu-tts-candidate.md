# ADR 0004: Future local CPU TTS candidate

- Status: Accepted — candidate only; implementation deferred
- Date: 2026-10-20

## Context

The application needs a replaceable TTS boundary that can eventually support server-side cloud providers and local CPU/GPU engines. A useful local engine should support stable per-character voice assignment, segment-sized synthesis, and progress/reporting that can be surfaced to the user. Native C# integration is preferred where practical.

## Decision

Record KokoroSharp's CPU/ONNX package as the preferred initial local CPU prototype candidate for a later milestone, behind the application's own TTS and voice-catalog interfaces. Its documented C# surface includes synthesizer APIs, multiple voice IDs, and asynchronous synthesis/segment APIs. Keep each script segment/job outcome and progress in the application layer rather than treating a whole chapter as an opaque synthesis call. Store voice IDs as provider-specific mappings under a stable character identity. Preserve emotion/style metadata but report it unsupported unless an implementation can substantiate the requested capability. This decision does not commit to a package version or begin its integration.

## Alternatives considered

- Piper/PiperSharp: plausible local CPU option with a C# wrapper and clear per-text inference calls, but it wraps the Piper executable and offers less direct model/runtime integration than the preferred C# inference library.
- Kokoro Python pipeline: viable CPU option but adds a Python sidecar/runtime despite the preference to keep a C#-first backend.
- Select no candidate until a later benchmark: lower commitment but offers less direction for initial provider interfaces and architecture decisions.

## Consequences

Before adoption, benchmark representative book-length segment workloads on the target CPU, assess model load/cache behavior, cancellation, timestamps, memory/concurrency limits, supported languages, and voice consistency. Verify the NuGet package, native runtime assets, model weights, and voice licensing independently. Neither KokoroSharp nor Piper should be assumed to render arbitrary emotion prompts; test capabilities and surface limitations honestly. Retain the option to switch implementations without changing persisted character identities or application workflows.
