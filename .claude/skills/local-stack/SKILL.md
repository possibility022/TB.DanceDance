---
name: local-stack
description: >-
  Run, debug, and inspect the TB.DanceDance app locally via Docker Compose
  (local_environment.dockercompose.yaml). Use for building/starting/stopping/restarting
  the whole stack or a single container, tailing logs, checking status, resetting state
  (DB + blob volumes), waiting for seed data, fetching a user/converter access token
  by HTTP for hitting the API, and listing/inspecting/uploading/replacing/deleting blobs
  in the local Azurite emulator. Triggers: "start the app locally", "restart the api
  container", "reset the local db", "get me a token", "why is the converter failing",
  "tail the auth server logs", "list blobs in thumbnails", "delete that blob from
  azurite", "replace/upload a video blob locally".
---

# Local stack (Docker) — run & debug TB.DanceDance

The local environment is defined entirely in `local_environment.dockercompose.yaml` at the
repo root. **Every compose command must pass `-f local_environment.dockercompose.yaml`** —
there is no default `docker-compose.yml`. Run commands from the repo root
(`C:\workspace\TB.DanceDance`).

> Shorthand used below: `DC = docker compose -f local_environment.dockercompose.yaml`

## Services, containers, and reachable URLs

| Service        | Container name      | Reachable at (host)                        | Role |
|----------------|---------------------|--------------------------------------------|------|
| `api`          | `tbdancedanceapi`   | `https://localhost:7068` (+ `:8080` http)  | ASP.NET Core REST API |
| `authserver`   | `tbauthserver`      | `https://localhost:7259` (+ `:5296` http)  | OpenIddict OIDC / token endpoint |
| `frontendspa`  | `tbdancefrontend`   | `http://localhost:3000`                     | Angular SPA |
| `converter`    | `tbdanceconverter`  | —                                           | Polls API, runs FFmpeg |
| `postgresDb`   | (compose-generated) | `localhost:5432`                            | PostgreSQL (db `dancedance`, `tbauthwebdb`) |
| `azuriteStorage`| (compose-generated)| `localhost:10000-10002`                     | Azure Blob emulator |
| `initializator`| `tbdanceInitializer`| — (one-shot)                                | Seeds DB + blobs, then exits |

The auth server is HTTPS-only for browser flows; HTTP `:5296` 307-redirects to HTTPS.

## Prerequisites (one-time)

The API and auth server mount a dev cert and read an env file. If `up` fails on a missing
cert/file, generate them first:

```powershell
# Dev TLS cert mounted by api + authserver (volume: $USERPROFILE\.aspnet\https\aspnetapp.pfx)
dotnet dev-certs https --clean
dotnet dev-certs https -ep $env:USERPROFILE\.aspnet\https\aspnetapp.pfx -p 2918379123298173918273861

tools\generateAuthSigningCert.ps1   # writes .env.authserver-certs (used by authserver)
```

## Lifecycle commands

### Bring the whole stack up
```powershell
docker compose -f local_environment.dockercompose.yaml up -d --build   # build images + start
docker compose -f local_environment.dockercompose.yaml up -d           # start without rebuild
```
Then **wait for seed data** before using the app — the initializer is one-shot:
```powershell
docker logs -f tbdanceInitializer    # wait for the line: Data initialized
```

### Status / logs
```powershell
docker compose -f local_environment.dockercompose.yaml ps              # what's running
docker compose -f local_environment.dockercompose.yaml logs -f api     # tail one service
docker compose -f local_environment.dockercompose.yaml logs --tail=200 authserver converter
```

### Single container — start / stop / restart / rebuild
```powershell
docker compose -f local_environment.dockercompose.yaml up -d api               # start/ensure one
docker compose -f local_environment.dockercompose.yaml restart api             # restart one
docker compose -f local_environment.dockercompose.yaml stop converter          # stop one
docker compose -f local_environment.dockercompose.yaml up -d --build api       # rebuild + restart one
```
After changing backend C# code, rebuild the affected image (`api`, `authserver`, or
`converter`) with `up -d --build <service>` — a plain `restart` reuses the old image.

### Stop / tear down
```powershell
docker compose -f local_environment.dockercompose.yaml stop            # stop, keep containers
docker compose -f local_environment.dockercompose.yaml down            # remove containers, KEEP volumes
docker compose -f local_environment.dockercompose.yaml down -v         # FULL RESET: also wipe DB + blob volumes
```
`down -v` deletes `postgresDance` and `azuriteStorageDance` — all seeded users, videos,
and blobs are lost and the initializer must re-run on next `up`. Use it when state is
corrupt; confirm with the user first since it is destructive.

### Frontend hot-reload (skip the SPA container)
```powershell
docker compose -f local_environment.dockercompose.yaml up -d --scale frontendspa=0
cd src/my-dance.web; npm start        # serves on http://localhost:3000
```

## Getting an access token (HTTP)

Local dev enables the OAuth2 **password grant** (`AuthServer:AllowWeakPasswords=true`), so a
single REST call returns a JWT. Two seeded users, both password `1234`:
`testemail@email.com` (user 1) and `testemail2@email.com` (user 2).

Use the bundled helper (works in PowerShell 5.1 and 7; uses `curl.exe -k` for the dev cert):

```powershell
# token metadata -> stderr, raw JWT -> stdout
.\.claude\skills\local-stack\scripts\get-token.ps1                 # user 1, read scope
.\.claude\skills\local-stack\scripts\get-token.ps1 -User 2         # second user
$t = .\.claude\skills\local-stack\scripts\get-token.ps1 -Raw       # capture only the JWT
.\.claude\skills\local-stack\scripts\get-token.ps1 -Convert        # converter (client_credentials)
```

Then call the API:
```powershell
curl.exe -k -H "Authorization: Bearer $t" https://localhost:7068/<endpoint>
```
Video streaming endpoints take the JWT as a query param instead (`?token=...`) because
`<video>` elements can't send an Authorization header.

Raw request (what the helper sends), also in `src/backend/Application/httpRequests/token.http`:
```
POST https://localhost:7259/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password&client_id=tbdancedancehttpclient&username=testemail@email.com&password=1234&scope=openid profile tbdancedanceapi.read
```

## Inspecting / managing blobs (Azurite)

Azurite has no built-in CLI, and `az`/`azcopy`/`Az.Storage` aren't installed. Use the
bundled file-based C# app instead — it references `Azure.Storage.Blobs` (the same SDK
and connection string the API uses, see `ConnectionStrings:Blob` in the compose file)
and runs directly with `dotnet run`, no `.csproj` needed (.NET 10 "file-based programs"):

```powershell
cd .claude/skills/local-stack/scripts

dotnet run azurite-blob.cs containers                                          # list containers
dotnet run azurite-blob.cs list thumbnails                                     # list blobs
dotnet run azurite-blob.cs list videos <prefix>                                # filter by name prefix
dotnet run azurite-blob.cs get thumbnails <blobName> ./out.jpg                 # download
dotnet run azurite-blob.cs put thumbnails <blobName> ./new.jpg image/jpeg      # upload / replace (overwrites)
dotnet run azurite-blob.cs delete thumbnails <blobName>                        # delete one (dry run by default)
dotnet run azurite-blob.cs delete videos --prefix stale- --force               # delete all matching a prefix
```
Known containers (from `BlobDataServiceFactory.cs`): `videos`, `videostoconvert`,
`thumbnails`. `delete` always prints what it *would* remove first — pass `--force` to
actually delete; confirm with the user before doing so since it's destructive.

## Inspecting the databases

Two databases on `localhost:5432` (user `postgres`, password `rgFraWIuyxONqWCQ71wh`):
`dancedance` (app — schemas `access`, `video`, `comments`, default/users) and `tbauthwebdb`
(auth server). The `postgres-dancedance` and `postgres-auth` MCP `query` tools are wired to
these; prefer them for read-only SQL, or use `psql` via the `postgres-cli-tools` skill.

## Debugging playbook

- **App won't authenticate / 401s** — confirm `tbauthserver` is up and discovery works:
  `curl.exe -k https://localhost:7259/.well-known/openid-configuration`. Issuer must be
  `https://localhost:7259/`.
- **API up but no data** — the initializer hasn't finished or failed:
  `docker logs tbdanceInitializer` (look for `Data initialized`).
- **Converter not processing** — `docker compose -f local_environment.dockercompose.yaml logs -f converter`;
  it polls every `DelayInMinutes=1` and authenticates via client credentials (`-Convert` token).
- **Code change not taking effect** — rebuild the image (`up -d --build <service>`), don't just `restart`.
- **State looks corrupt** — `down -v` then `up -d --build` for a clean reseed (destructive; confirm first).
