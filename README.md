# Audiobook Generator

A Docker-runnable ebook-to-audiobook application. The foundation includes PostgreSQL-backed job persistence and a hosted worker; ebook ingestion, parsing, and text-to-speech are not yet integrated.

## Prerequisites

- .NET 10 SDK
- Docker Desktop with Docker Compose
- Node.js and npm

## Run locally

Start the API and PostgreSQL. PostgreSQL data is stored in the Compose named volume `postgres_data`.

```powershell
docker compose up --build
```

The API listens at `http://localhost:8080`; liveness is available at `/health` and database readiness at `/health/ready`. PostgreSQL is published on port `5432` for local development and EF migration commands. Job status is persisted and available through the API; conversion submission will be added with the real workflow. The hosted worker and deployment are single-instance, not a multi-replica queue.

During development, run Vite on the host in another terminal:

```powershell
Set-Location src\frontend
npm install
npm run dev
```

Open the Vite URL (normally `http://localhost:5173`). Vite proxies API and health requests to `http://localhost:8080`; use `VITE_API_BASE_URL` only when targeting another API. The `.NET API + Vite (local PostgreSQL)` debug profile runs the API on port `5014`, starts the PostgreSQL Compose service, and points Vite to that API.

To stop services, run `docker compose down`. This retains the PostgreSQL volume. To intentionally remove persisted data, run `docker compose down -v`.

## Repository layout

- `src/api/` is one ASP.NET Core project. Keep domain models, application services/contracts, and infrastructure implementations separated into folders within that project.
- `src/tests/` is the independent test project.
- `src/frontend/` is the React/Vite renderer.
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
dotnet tool run dotnet-ef migrations add <MigrationName> --project src\api\api.csproj --startup-project src\api\api.csproj --output-dir Infrastructure\Persistence\Migrations
```

## Tests

Tests are separate from the runtime API project and reference it:

```powershell
dotnet test src\tests\AbGen.Tests.csproj
```

## Job status

The API exposes `GET /api/jobs` and `GET /api/jobs/{id}` to inspect jobs persisted in PostgreSQL. Job submission and conversion are not available yet; the frontend reads backend-owned status and events and reloads them after a page refresh.

This milestone does not accept ebooks or write audio files. Those assets must later use persistent storage too; the database volume alone does not make container-local files durable.

## Future providers

C# provider contracts are in `src/api/Domain/Providers`. EF Core persistence and the hosted job worker are in `src/api/Infrastructure`. EPUB is the first planned importer, OpenRouter the initial parser candidate, and KokoroSharp (C#/ONNX CPU) the preferred future local TTS candidate. These providers are not yet integrated. Emotion metadata must not be described as synthesized unless the selected engine supports it.

Provider credentials remain server-side. The Compose PostgreSQL password is for local development only; replace it and use a secret manager before exposing the service.
