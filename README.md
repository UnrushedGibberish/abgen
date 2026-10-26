# Audiobook Generator

A Docker-runnable ebook-to-audiobook application. PostgreSQL-backed projects are the top-level container for future ebook uploads, conversion settings, scripts, and generated audiobook assets. This milestone supports project metadata only; ebook ingestion, parsing, and text-to-speech are not yet integrated.

## Prerequisites

- .NET 10 SDK
- Docker Desktop with Docker Compose
- Node.js and npm

## Run locally

Start the API and PostgreSQL. PostgreSQL data is stored in the Compose named volume `postgres_data`.

```powershell
docker compose up --build
```

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

The API uses `ConnectionStrings__Postgres` or the local development connection configured in `src/api/appsettings.json`.

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
