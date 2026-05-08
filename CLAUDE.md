# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TB.DanceDance is a multi-platform video management and sharing application focused on dance instruction/practice videos. It consists of a .NET 10 backend API, an OpenIddict-based auth server, a React SPA, a video converter daemon, a .NET MAUI mobile app, and a PostgreSQL database.

## Commands

### Local Development (Docker)
```bash
# Start the full local stack (PostgreSQL, Azurite, authserver, API, frontend, converter)
docker compose -f local_environment.dockercompose.yaml up

# Wait for seed data before using the app
docker logs tbdanceInitializer   # wait for "Data initialized"
```

### Backend (.NET)
```bash
dotnet restore
dotnet build
dotnet test                                                   # all backend tests
dotnet test src/tests/TB.DanceDance.Tests/TB.DanceDance.Tests.csproj   # integration tests only
dotnet test src/tests/TB.DanceDance.Mobile.Tests/TB.DanceDance.Mobile.Tests.csproj
```

### Frontend (React)
```bash
cd src/frontend
npm install
npm start        # dev server
npm run build
npm test
```

### Mobile (MAUI)
```bash
dotnet build ./src/mobile/TB.DanceDance.Mobile/ -c Release -f net10.0-android --no-restore
```

### Certificates (local HTTPS)
```powershell
tools/generateAuthSigningCert.ps1   # auth server signing cert
tools/generateCertificate.ps1       # API TLS cert
```

## Architecture

### Components and Interactions

```
Browser/Mobile
    │  OIDC login redirect
    ▼
Auth Server (src/authserver)          OpenIddict-based OIDC/OAuth2 server
    │  JWT tokens
    ▼
API (src/backend/TB.DanceDance.API)   ASP.NET Core 10, validates JWT, exposes REST
    │  EF Core / Npgsql
    ▼
PostgreSQL                            4 schemas: access, video, comments, default(users)
    │
    └── Azure Blob Storage (Azurite locally)   video files

Converter Daemon (src/backend/...Converter.Deamon)
    │  polls API for pending jobs, authenticates via OAuth2 client credentials
    └── FFMpegCore  →  Azure Blob Storage
```

### Backend Layer Structure

The backend follows a strict layered architecture under `src/backend/`:

| Layer | Project | Responsibility |
|-------|---------|----------------|
| API | `TB.DanceDance.API` | Controllers, DI wiring, HTTP concerns, auth policies |
| Application | `Application/` | Business use cases, services, DTOs |
| Domain | `Domain/` | Entities, value objects, domain services, exceptions |
| Infrastructure | `Infrastructure/` | EF Core DbContext, migrations, blob storage |
| Contracts | `TB.DanceDance.API.Contracts` | Shared request/response models (also used by mobile) |

### Authentication Flow

- Frontend (and MAUI app) redirects users to the auth server for login.
- The auth server issues JWT tokens. Two OIDC scopes matter: `tbdancedanceapi.read` (users) and `tbdancedanceapi.convert` (converter daemon).
- The API validates tokens via JWT Bearer middleware; issuer discovery uses the HTTP endpoint of the auth server (`:8080`).
- Video streaming endpoints accept the JWT in a query parameter (`token=...`) because `<video>` elements cannot send Authorization headers.
- The converter daemon uses OAuth2 client credentials flow (not user login).

### Database

- Single `DanceDbContext` with 4 PostgreSQL schemas: `access`, `video`, `comments`, and the default schema for users.
- EF Core migrations live in `src/backend/Infrastructure/Data/Migrations/`.
- Integration tests use **Testcontainers** (spins up real PostgreSQL and Azurite containers).

### Frontend

- OIDC authentication is bootstrapped in `src/frontend/src/providers/AuthProvider.ts` using `oidc-client-ts`.
- `src/frontend/src/services/AppClient.ts` is the central API client; individual service files (VideoInfoService, CommentsService, etc.) wrap it.
- Routing is defined in `src/frontend/src/App.tsx`.

### Video Processing Pipeline

1. Client uploads directly to Azure Blob Storage (SAS URL provided by API).
2. API records a pending conversion job in the database.
3. Converter daemon polls the API, downloads the blob, runs FFmpeg, uploads the result, and marks the job complete.

## Key Configuration Files

- `local_environment.dockercompose.yaml` — defines all local services and their environment variables.
- `src/backend/TB.DanceDance.API/appsettings.json` + `appsettings.Local.json` — API config.
- `src/authserver/appsettings.Local.json` — auth server config (clients, scopes, signing keys).
- `src/frontend/src/constants/` — frontend API base URLs and OIDC settings per environment.

## Testing Notes

Backend integration tests require Docker (Testcontainers pulls PostgreSQL and Azurite images on first run). Tests are in `src/tests/TB.DanceDance.Tests/` and use xunit v3, NSubstitute, and WireMock.Net for HTTP mocking.

CI runs via `.github/workflows/pr-gated.yaml` and validates API, frontend, mobile, converter, and initializer on every PR.
