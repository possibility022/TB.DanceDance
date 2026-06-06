# my-dance.web

Angular 22 web app for TB.DanceDance (replaces the React app in `src/frontend`).

## Prerequisites

- **Node** `^22.22.3 || ^24.15.0 || >=26` (the repo is developed on Node 24).
- **Docker** (for the backend stack: API, auth server, PostgreSQL, Azurite, converter).
- **Trusted ASP.NET dev cert** — the API (`https://localhost:7068`) and auth server
  (`https://localhost:7259`) use a dev cert. Trust it once so the browser accepts it:
  ```bash
  dotnet dev-certs https --trust
  ```

## Port 3000 matters

The app **must** be served on `http://localhost:3000` locally, because:

- the auth server has `http://localhost:3000/callback` registered as the OIDC redirect, and
- the API and auth server only allow CORS from `http://localhost:3000`.

Both `npm start` (dev server) and the Docker image are configured for port 3000.

## Running

### Frontend development (hot reload) — recommended

Start the backend **without** the web container, then run the dev server:

```bash
# from the repo root
docker compose -f local_environment.dockercompose.yaml up --scale frontendspa=0
docker logs tbdanceInitializer   # wait for "Data initialized"

# in another terminal
cd src/my-dance.web
npm install
npm start                         # http://localhost:3000  (hot reload)
```

### Full stack (production-like, Dockerized frontend)

```bash
docker compose -f local_environment.dockercompose.yaml up
# app served at http://localhost:3000 by nginx
```

## Configuration

Runtime config is loaded from `config.json` at startup (no rebuild per environment):

| Key | Local default | Source |
|-----|---------------|--------|
| `apiBaseUrl` | `https://localhost:7068` | `public/config.json` (dev) / entrypoint env (Docker) |
| `authUrl` | `https://localhost:7259` | same |
| `redirectUri` | `window.origin + /callback` | computed when blank |

- **Dev server** serves `public/config.json` as-is (defaults already point at localhost).
- **Docker image** generates `config.json` at container start from `API_BASE_URL` /
  `AUTH_URL` / `REDIRECT_URI` env vars (see `docker-entrypoint.sh`).

## Commands

```bash
npm start            # dev server on :3000
npm run build        # production build -> dist/my-dance.web/browser
npm test             # unit tests (Vitest)
```

## Project layout

```
src/app/
  core/      config, auth (OIDC), API services + models
  shared/    reusable UI (video-card/list), formatting pipes, enums
  layout/    navbar, cookie-consent
  features/  home, videos, comments, sharing, events, access, upload, auth pages
```
