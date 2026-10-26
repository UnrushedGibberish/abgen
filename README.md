# Audiobook Generator

A Docker-runnable ebook-to-audiobook application. PostgreSQL-backed projects are the top-level container for future ebook uploads, conversion settings, scripts, and generated audiobook assets. This milestone supports project metadata only; ebook ingestion, parsing, and text-to-speech are not yet integrated.

## Prerequisites

- .NET 10 SDK
- Docker Desktop with Docker Compose
- Node.js and npm

## Run locally

The VS Code debug profile is the recommended per-worktree workflow. Open the worktree in VS Code and start **API + Vite (local PostgreSQL)** from Run and Debug. Its pre-launch task builds the API, creates a worktree-specific `.env` and `.vscode/settings.json`, and starts that worktree's PostgreSQL service. Install frontend dependencies once with `npm install` in `src/frontend`. The setup script selects an available API, Vite, and PostgreSQL port set from ranges `5000-5999`, `6000-6999`, and `15000-15999`; the generated `.env` and workspace settings are ignored by Git. Re-run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/setup-local-dev.ps1` to refresh the ignored VS Code port settings and `.env` values. For custom ports, create `.env` from `.env.example` and set `ABGEN_API_PORT`, `ABGEN_FRONTEND_PORT`, and `ABGEN_POSTGRES_PORT` before running the setup script.

To use Compose for the full stack, run the setup script first, then `docker compose up --build`. PostgreSQL data is stored in a Compose named volume unique to each worktree. The API is available at `http://localhost:<ABGEN_API_PORT>`; liveness is available at `/health` and database readiness at `/health/ready`. PostgreSQL is published on `127.0.0.1:<ABGEN_POSTGRES_PORT>` for local development and EF migration commands. Create and list projects through `POST /api/projects` and `GET /api/projects`; project details are available from `GET /api/projects/{id}`.

To stop a worktree's services, run `docker compose down` from that worktree. This retains its PostgreSQL volume. To intentionally remove that worktree's persisted data, run `docker compose down -v`.
The API listens at `http://localhost:8080`; liveness is available at `/health` and database readiness at `/health/ready`. PostgreSQL is published on port `5432` for local development and EF migration commands. Create and list projects through `POST /api/projects` and `GET /api/projects`; project details are available from `GET /api/projects/{id}`.

During development, run Vite on the host in another terminal:

```powershell
Set-Location src\frontend
npm install
npm run dev
```

The .NET API types and endpoints generate the OpenAPI schema at `src/api/obj/openapi/api.json`. TypeScript types are generated from that schema before each frontend build. After changing API contracts, refresh the generated frontend types with `npm run generate:api`; run `npm run check:api` to verify checked-in generated types match the current backend schema.

Open the Vite URL (normally `http://localhost:5173`). Vite proxies API and health requests to `http://localhost:8080`; use `VITE_API_BASE_URL` only when targeting another API. The `.NET API + Vite (local PostgreSQL)` debug profile runs the API on port `5014`, starts the PostgreSQL Compose service, and points Vite to that API.

To stop services, run `docker compose down`. This retains the PostgreSQL volume. To intentionally remove persisted data, run `docker compose down -v`.

## Repository layout

- `src/api/` is one ASP.NET Core project. `Controllers/` contains HTTP endpoints, `Services/` contains application services and provider contracts, and `Data/` contains EF Core persistence, entities, repositories, health checks, and migrations.
- `src/tests/` is the independent test project and references the API.
- `src/frontend/` is the React/Vite project renderer.
- `docs/adr/` contains architecture decisions; `.github/copilot-instructions.md` contains project-specific guidance.

## Local backend development

Start only PostgreSQL in Compose, then launch the API project directly:

```powershell
docker compose up -d postgres
dotnet run --project src\api\api.csproj
```

The API uses `ConnectionStrings__Postgres` or the default local connection in `src/api/appsettings.json`. The VS Code setup stores its connection string in the ignored `.env` file. The EF design-time factory honors `ConnectionStrings__Postgres` and `ABGEN_POSTGRES_PORT`; load the worktree's `.env` before running EF tooling from a terminal.

After changing the EF model, regenerate a migration and apply it on API startup:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations add <MigrationName> --project src\api\api.csproj --startup-project src\api\api.csproj --output-dir Data\Migrations
```

## Tests

Tests are separate from the runtime API project and reference it:

```powershell
dotnet test src\tests\AbGen.Tests.csproj
```

## Current scope

Projects currently store a name and creation/update timestamps. Ebook uploads, parsing, script editing, voice selection, and audio generation will be added inside the project workflow over time. Project-owned uploads and generated audio must use persistent storage; the database volume alone does not make container-local files durable.

## Future providers

Provider contracts are in `src/api/Services/Providers`. Project EF Core persistence is in `src/api/Data`. EPUB is the first planned importer, OpenRouter the initial parser candidate, and KokoroSharp (C#/ONNX CPU) the preferred future local TTS candidate. These providers are not yet integrated. Emotion metadata must not be described as synthesized unless the selected engine supports it.

Provider credentials remain server-side. The Compose PostgreSQL password is for local development only; replace it and use a secret manager before exposing the service.
