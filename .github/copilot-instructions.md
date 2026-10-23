# Project Instructions

## Guiding principle

**Start simple and only add complexity as required.** Prefer the smallest design that meets the durable-conversion requirements; record decisions and revisit them when new evidence justifies change.

## Architecture

- Backend is one ASP.NET Core C# project under `src/api/`, with domain contracts/models, application services, and infrastructure implementations organized into separate folders in that project. Keep ebook import, parsing/script analysis, text-to-speech synthesis, voice/catalog management, audio storage, and job scheduling behind focused interfaces. Implementations must be replaceable through dependency injection without changing application workflows.
- PostgreSQL via EF Core is the durable source of truth for jobs, job events, scripts, and metadata. Container restarts, browser refreshes, and reconnects must not lose accepted work or its status. Store uploaded books and generated audio in persistent storage, never an ephemeral container filesystem.
- Start with an ASP.NET Core hosted background service in the API process and one active worker. Claim queued work transactionally and recover only stale in-progress attempts after a restart. Keep work idempotent/retry-aware. Add a separate worker, distributed queue, multiple active workers, or full lease-renewal protocol only when requirements justify them.
- The React/Vite frontend is a renderer of backend-owned state. It may collect input, issue API requests, and render responses; it must not own job lifecycle, parsing, voice assignment, retry, progress, or other business rules. Recover current status from the API after a page reload. Browser state or a live connection must never be the sole record of work/progress.
- Keep OpenRouter credentials and all provider secrets on the server. Never expose secrets to the frontend.
- EPUB is the first planned input format. Keep file-format-specific behavior inside an importer implementation.

## Script and voice processing

- Model script output explicitly: ordered segments with stable IDs/sequence, text, speaker/character identity, emotion/style metadata, and optional chapter/paragraph context. Persist parser output so synthesis can resume without reparsing.
- Keep speaker identity separate from a provider's engine-specific voice ID. Persist a stable project/character-to-voice mapping so the same character uses the selected voice across chapters and retries.
- Providers must declare capabilities. Preserve emotion/style metadata even if an engine cannot render it, and report unsupported features to users instead of silently claiming support or faking results.
- Report meaningful durable progress at workflow, chapter, segment, and provider boundaries. Persist user-facing progress/warning/error events and distinguish retryable provider failures from terminal validation/configuration errors.
- The initial parser candidate is OpenRouter with structured, schema-validated output. The preferred candidate for a future local CPU TTS adapter is KokoroSharp (C# / ONNX); verify package, model, and voice licenses and test its actual capabilities before adoption. Do not add either integration until scoped.

## Development and documentation

- Use dependency injection for interface implementations and options for configuration; validate required settings at startup or before accepting work.
- Add focused tests for state transitions, restart/retry behavior, progress/event persistence, provider capability reporting, and deterministic application logic. Avoid tests that require paid APIs or real model downloads by default; use fakes for those boundaries.
- Record significant architecture and provider decisions as concise Architecture Decision Records in `docs/adr/`, with status, context, decision, alternatives, and consequences. Update an ADR when a decision changes; do not use an ADR as a substitute for current implementation documentation.
- Keep README setup instructions aligned with actual Docker Compose and frontend commands. Never commit API keys, credentials, or secrets.
