# ADR 0001: Architecture and application boundaries

- Status: Accepted
- Date: 2026-10-20

## Context

The application accepts long-running ebook-to-audiobook requests. Its UI must render backend-owned state, operations should survive browser refreshes and container restarts, and parsing/TTS engines may change. The initial deployment should remain easy to run and understand.

## Decision

Use a React/Vite renderer and one ASP.NET Core API project in `src/api/`. Organize domain/use-case contracts and infrastructure implementations as folders/namespaces within that project rather than separate production projects. Keep the automated test project separate under `src/tests/`. Use PostgreSQL with EF Core as the system of record. Host one background worker in the API process and run API plus PostgreSQL in Docker Compose; run Vite on the host in development. Keep provider seams for ebook ingestion, script parsing, TTS, voice mapping/catalog, and file/audio storage. Add job submission with the first real conversion workflow.

## Alternatives considered

- Separate production projects for core and infrastructure: stronger compile-time isolation, but the current scale does not yet justify the extra project/tooling overhead. Keep boundaries as namespaces and folders in one API project; split them only if real build/runtime or team needs arise.
- A separate worker container and full database-backed leases: clearer resource/failure isolation but adds deployment, claim, lease-renewal, and coordination concerns before the conversion workflow requires them. Begin with one hosted worker when real conversion processing is introduced; revisit separation if scaling or isolation needs justify it.
- A browser-owned conversion/progress workflow: rejected because refresh and browser lifetime must not determine job lifetime or state.
- Tightly coupling application use cases to a particular TTS or parser SDK: rejected because providers are expected to be replaceable.

## Consequences

The API and worker share CPU, process lifecycle, and deployment. PostgreSQL-backed job state can be recovered after restart; the UI reloads authoritative state from API endpoints. The single-process design is not a multi-instance queue: before running multiple API replicas, add a concurrency-safe claim/lease strategy or extract a worker. Uploaded ebooks and generated audio require separate persistent file storage when those features are introduced; PostgreSQL job rows alone do not make container-local files durable.
