# ADR 0002: Durable jobs and progress

- Status: Accepted
- Date: 2026-10-20

## Context

An accepted conversion request must not be lost on page refresh or an API/container restart. Users need useful progress, warnings, and errors. The initial deployment should remain straightforward without introducing a dedicated queue service.

## Decision

Persist each job's identity, type, status, timestamps, attempt count, and durable ordered user-facing events in PostgreSQL through EF Core. The API transactionally accepts and records work before returning its ID. An ASP.NET Core hosted background service transactionally claims queued jobs within a single API instance and updates progress and terminal state in the database. Recover in-progress attempts only after a conservative 30-second stale-attempt threshold. Expose list/get endpoints; the UI re-fetches persisted state after a reload. Begin with one active hosted worker and minimal polling/refresh behavior.

## Alternatives considered

- Store job state in React memory, browser storage, or a live connection: rejected because the browser must not own accepted work or the only copy of progress.
- Deploy a distributed queue and separate worker immediately: deferred until scaling/resource isolation requires it.
- Promise exactly-once execution after a crash: rejected; process death can occur between an external side effect and database commit. Real steps must be idempotent or checkpointed, and retries must be visible.

## Consequences

The database is authoritative, so progress can be reloaded after refresh/reconnect. Database transactions cover job state/events, but real file artifacts need a persistent file/object-storage boundary and idempotent naming. Initial single-instance concurrency assumptions must be revisited before enabling multiple API replicas. The initial stale-attempt threshold is a recovery heuristic, not a renewable lease or proof that a prior side effect did not occur; real providers must be idempotent/checkpointed before automatic reprocessing. A change-feed/SignalR/SSE channel may later improve update latency but cannot replace persisted events/status.