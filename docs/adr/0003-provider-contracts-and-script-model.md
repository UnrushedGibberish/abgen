# ADR 0003: Provider contracts and script model

- Status: Accepted
- Date: 2026-10-20

## Context

Several distinct external or local engines will participate in converting ebooks into audio. Script parsing needs to identify speakers and emotional/style cues. User-facing progress and actionable errors matter, and providers may support different features.

## Decision

Keep provider implementations behind focused C# interfaces injected into application workflows. Define script output as ordered, persistable segments with stable identifiers, text, speaker/character identity, emotion/style metadata, and optional chapter context. Keep a project's stable character-to-voice mapping separate from any engine-specific voice ID. Providers expose capabilities and typed failures/progress; preserve requested metadata and clearly report unsupported capabilities rather than silently inventing support. The first parser candidate is an OpenRouter chat-completions adapter using schema-constrained/validated structured output. Implement no remote parser or provider key in the foundation milestone.

## Alternatives considered

- One unified provider interface with engine conditionals: rejected because unrelated provider capabilities become tangled.
- Store only raw LLM text and parse it later: rejected because downstream processing requires validated, stable script segments.
- Let the model choose an ephemeral voice ID per segment: rejected because a character's sound must remain stable across chapters and retries.

## Consequences

Abstractions and data contracts precede provider implementation and need fake-backed tests. OpenRouter structured-output/model endpoint support must be verified at provider implementation time. Credentials remain server-side. Human review/editing of parser output is a future workflow, not in this foundation milestone.
