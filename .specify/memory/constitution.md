<!--
Sync Impact Report
- Version change: (template, unratified) → 1.0.0
- Modified principles: none (initial ratification — template placeholders filled for the first time)
- Added sections:
  - Core Principles: I. Layered Architecture Discipline, II. Test-First & Integration Testing
    (NON-NEGOTIABLE), III. Simplicity & No Speculative Abstraction, IV. Security & Dev/Prod
    Boundary Guardrails
  - Technology & Platform Constraints
  - Development Workflow & Quality Gates
  - Governance
- Removed sections: none (template's optional 5th principle slot intentionally not used —
  4 principles were selected as sufficient for current project scope)
- Templates requiring updates:
  - ✅ .specify/templates/plan-template.md — Constitution Check section is generic and already
    compatible; no edit required.
  - ✅ .specify/templates/spec-template.md — no constitution-specific references; no edit required.
  - ✅ .specify/templates/tasks-template.md — task categories (setup/tests/core/integration/polish)
    already align with Principle II (tests precede implementation); no edit required.
  - ✅ .specify/templates/checklist-template.md — no constitution-specific references; no edit
    required.
  - ⚠ No command files found under .specify/templates/commands/ — nothing to propagate there.
- Follow-up TODOs: none.
-->

# TB.DanceDance Constitution

## Core Principles

### I. Layered Architecture Discipline
Backend code MUST respect the layer dependency direction documented in CLAUDE.md:
`TB.DanceDance.API` → `Application` → `Domain`, with `Infrastructure` implementing
interfaces defined by `Domain`/`Application`, and `TB.DanceDance.API.Contracts` as the
only project shared with the mobile app. Controllers MUST NOT access the EF Core
`DanceDbContext` or blob storage directly; that belongs in `Infrastructure`, reached
through `Application` services. On the Angular frontend, all HTTP access MUST go
through the typed API layer in `src/app/core/api/` (`ApiClient` plus per-capability
services); components and other services MUST NOT call `HttpClient` directly.

**Rationale**: The project spans an API, an auth server, two frontends (one being
retired), a converter daemon, and a mobile app. Without an enforced layering rule, the
data-access and HTTP-calling conventions already established in CLAUDE.md erode as the
codebase grows, producing duplicated and inconsistent integration logic.

### II. Test-First & Integration Testing (NON-NEGOTIABLE)
Backend integration tests MUST exercise real PostgreSQL and Azure Blob Storage
(Azurite) via Testcontainers; mocking the database or blob storage in integration tests
is prohibited. Any change to the API, Application, or Infrastructure layers MUST ship
with integration test coverage in `src/tests/TB.DanceDance.Tests/` (or the mobile
equivalent) before it is considered done. `.github/workflows/pr-gated.yaml`, which
validates the API, frontend, mobile, converter, and initializer, is the canonical merge
gate — it MUST pass on every PR and MUST NOT be bypassed (no skipped checks, no
`--no-verify`).

**Rationale**: Mocked persistence layers can pass while the real schema/migration or
blob-storage interaction is broken in production. Testcontainers-backed integration
tests catch that divergence; the CI gate is what makes the rule enforceable rather than
aspirational.

### III. Simplicity & No Speculative Abstraction
Implementations MUST solve only the stated task. Do not introduce abstractions,
configuration knobs, feature flags, or backwards-compatibility shims for needs that do
not yet exist. Error handling and input validation MUST be added only at system
boundaries (user input, external HTTP calls, third-party services) — not for internal
states already guaranteed by the type system or calling code. When a pattern repeats
three times without a clear, present need for variation, a shared abstraction MAY be
introduced; until then, duplication is preferred over premature generalization.

**Rationale**: With five active platforms (API, auth server, Angular SPA, legacy React
SPA being retired, MAUI mobile app, converter daemon) sharing a small team's attention,
speculative abstractions and defensive code for impossible states are a recurring
source of wasted review time and silent half-finished features.

### IV. Security & Dev/Prod Boundary Guardrails
Local-development-only conveniences — most notably
`AuthServer:AllowWeakPasswords` and the OAuth2 password grant it enables at
`/connect/token` — MUST remain explicitly flag-gated and MUST be unreachable whenever
that flag is false or unset. Signing certificates and TLS certificates MUST be
generated and rotated only through the documented tooling
(`tools/generateAuthSigningCert.ps1`, `tools/generateCertificate.ps1`); certificates,
keys, and other secrets MUST NOT be committed to the repository. Any change that
widens what is reachable outside local development — CORS origins, OIDC redirect URIs,
auth policies, or newly exposed endpoints — MUST be called out explicitly in the pull
request description so it receives deliberate review.

**Rationale**: The architecture already depends on a hard dev/prod boundary (see the
"Local dev only" callout in CLAUDE.md). Making that boundary an explicit, reviewable
rule — instead of an implicit convention — prevents a dev-only convenience from
silently becoming reachable in a deployed environment.

## Technology & Platform Constraints

The backend targets .NET 10 with an OpenIddict-based auth server and EF Core/Npgsql
against PostgreSQL (schemas: `access`, `video`, `comments`, and the default schema for
users); EF Core migrations MUST be generated through the project's documented tooling
rather than hand-edited. The frontend of record is the Angular 22 SPA in
`src/my-dance.web` (standalone components, signals, zoneless change detection,
`OnPush`); the legacy React SPA in `src/frontend` is being retired and MUST NOT receive
new features — only the fixes necessary to keep it functional until retirement.
Mobile is a .NET MAUI app sharing `TB.DanceDance.API.Contracts` with the backend. Video
streaming endpoints MUST accept the JWT via a query parameter (in addition to the
Authorization header) because `<video>` elements cannot send custom headers — this is a
deliberate exception to standard bearer-token handling, not an oversight.

## Development Workflow & Quality Gates

The Docker Compose stack (`local_environment.dockercompose.yaml`) is the standard way
to validate end-to-end behavior across services before a change is considered verified;
UI changes MUST be exercised against a running stack, not just unit-tested. Code
review MUST verify compliance with the layering (Principle I) and testing (Principle
II) rules above; a PR that adds backend logic without corresponding integration test
coverage, or that bypasses the typed API layer on the frontend, MUST be rejected or
sent back for revision. Complexity that appears to violate Principle III (e.g., a new
abstraction layer, a new feature flag) MUST be justified in the PR description with the
concrete present need it serves.

## Governance

This constitution supersedes ad-hoc team practice when the two conflict. Amendments
are made by editing this file directly: update the affected principle or section,
recompute the version per the policy below, update `**Last Amended**`, and prepend a
refreshed Sync Impact Report comment summarizing what changed. Versioning follows
semantic versioning: MAJOR for backward-incompatible governance/principle
redefinitions or removals, MINOR for adding a new principle or materially expanding
existing guidance, PATCH for wording/clarification fixes with no semantic change. Every
PR description MUST note any deliberate deviation from these principles and its
justification; reviewers MUST treat an unjustified deviation as grounds to block the
PR. For day-to-day technical conventions not covered here, defer to CLAUDE.md and the
per-project `CLAUDE.md`/`README.md` files (e.g., `src/my-dance.web/.claude/CLAUDE.md`).

**Version**: 1.0.0 | **Ratified**: 2026-06-23 | **Last Amended**: 2026-06-23
