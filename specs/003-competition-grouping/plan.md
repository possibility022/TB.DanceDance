# Implementation Plan: Competition Grouping & Combined Feedback

**Branch**: `ai/spec-kit` | **Date**: 2026-06-23 | **Spec**: `specs/003-competition-grouping/spec.md`

**Input**: Feature specification from `specs/003-competition-grouping/spec.md`

## Summary

Let a video owner group several of their own private videos into a named **Competition** and
share the whole competition with one link. Opening that link shows every grouped video and a
single combined comment thread, so a teacher reviewing a competition gives feedback once instead
of once per video. Delivered as a new `Competition` aggregate plus nullable `CompetitionId` links
added to the existing `Video`, `SharedLink`, and `Comment` entities (exactly one of
video/competition set per link or comment), reusing the existing sharing and comment pipelines
end to end. Web only (`src/my-dance.web`); no mobile, converter, or (social) group/event changes.

## Technical Context

**Language/Version**: C# / .NET 10 (backend, auth server); TypeScript / Angular 22 (web frontend)

**Primary Dependencies**: FastEndpoints (API), EF Core/Npgsql (persistence), OpenIddict (auth,
unchanged by this feature), Angular standalone components + signals + `angular-auth-oidc-client`
(frontend)

**Storage**: PostgreSQL, `video` schema (new `Competition` table; extended `Video`, `SharedLink`,
`Comment` tables) — per `CLAUDE.md`'s 4-schema layout

**Testing**: xunit v3 + NSubstitute + Testcontainers (PostgreSQL + Azurite) for backend
integration tests (`src/tests/TB.DanceDance.Tests/Features/Competitions/`); Vitest for the Angular
frontend (co-located `.spec.ts` files)

**Target Platform**: Web (browser) only — `src/my-dance.web`; no MAUI/mobile surface

**Project Type**: Web application (Angular SPA + .NET API), within the existing monorepo layout

**Performance Goals**: No new performance targets beyond existing video listing/streaming and
comment endpoints already meet; a competition's video list reuses the same per-video metadata
shape already used by My Videos.

**Constraints**: Reuse existing `CommentVisibility` enum and `AllowComments` /
`AllowAnonymousComments` semantics unchanged (FR-012, FR-013); `SharedLink`/`Comment` must keep
exactly one of video/competition set (data integrity invariant, enforced in the application
layer); streaming endpoints must keep accepting the JWT via query parameter (existing
Constitution-documented exception for `<video>` elements), unchanged by this feature.

**Scale/Scope**: 3 EF Core migrations (`AddCompetitions`, `AddCompetitionSharedLinks`,
`AddCompetitionComments`); 1 new backend feature area (`Application/Features/Competitions`, 7
endpoints) plus extensions to 2 existing ones (Sharing, Comments); 1 new Angular feature area
(`features/competitions`, 2 routes) plus extensions to the existing My Videos and Sharing feature
areas.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Layered Architecture Discipline** — PASS. New `Competition` domain entity lives in
  `Domain/Entities`; `CompetitionService` (Application) is the only thing touching
  `IApplicationContext`; FastEndpoints endpoints depend on `ICompetitionService`/
  `ISharedLinkService`, never the `DbContext`, directly; new request/response DTOs live in
  `TB.DanceDance.API.Contracts.Features.Competitions`. The Angular feature module calls only
  `CompetitionsService` (in `core/api/`), never `HttpClient` directly.
- **II. Test-First & Integration Testing (NON-NEGOTIABLE)** — PASS. Backend changes are covered
  by Testcontainers-backed integration tests under
  `src/tests/TB.DanceDance.Tests/Features/Competitions/` (persistence, service, sharing,
  comments); no mocked database/blob storage. Frontend changes are covered by co-located Vitest
  specs. `.github/workflows/pr-gated.yaml` remains the merge gate, unmodified by this feature.
- **III. Simplicity & No Speculative Abstraction** — PASS. No new feature flag, no speculative
  many-to-many video/competition relationship, no comment-migration machinery for videos joining
  a competition — all deliberately excluded per the source material's stated v1 scope (see
  `research.md` §5-6). Sharing and comments extend the *existing* entities/services rather than
  introducing parallel ones (`research.md` §3-4).
- **IV. Security & Dev/Prod Boundary Guardrails** — PASS. No new CORS origins, OIDC redirects, or
  auth policies; new endpoints reuse the existing `tbdancedanceapi.read` scope/policy and the
  existing anonymous-streaming/anonymous-commenting exceptions, unchanged in shape.

No violations — Complexity Tracking is not needed.

## Project Structure

### Documentation (this feature)

```text
specs/003-competition-grouping/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md         # Phase 1 output
├── contracts/             # Phase 1 output
│   └── competitions-api.md
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
src/backend/Application/Domain/Entities/
├── Competition.cs                         # new
├── Video.cs                               # extended: CompetitionId, Competition nav
├── SharedLink.cs                          # extended: nullable VideoId, CompetitionId
└── Comment.cs                             # extended: nullable VideoId, CompetitionId

src/backend/Application/Features/Competitions/
├── CompetitionService.cs / ICompetitionService.cs
├── CompetitionsModule.cs                  # DI registration
└── Endpoints/
    ├── CreateCompetitionEndpoint.cs
    ├── RenameCompetitionEndpoint.cs
    ├── DeleteCompetitionEndpoint.cs
    ├── GetCompetitionEndpoint.cs
    ├── ListMyCompetitionsEndpoint.cs
    ├── AddVideoToCompetitionEndpoint.cs
    ├── RemoveVideoFromCompetitionEndpoint.cs
    └── CompetitionMapper.cs

src/backend/Application/Features/Sharing/Endpoints/
├── CreateCompetitionSharedLinkEndpoint.cs # new
├── StreamCompetitionVideoBySharedLinkEndpoint.cs # new
└── ShareMapper.cs                         # extended for competition responses

src/backend/Application/Features/Comments/
├── CommentService.cs                      # extended: competition-keyed threads
└── Endpoints/ListCommentsForCompetitionEndpoint.cs # new

src/backend/Application/ApiRoutes.cs        # extended: Competitions group, Share.CreateForCompetition/GetVideoStream, Comments.ListCommentsForCompetition

src/backend/TB.DanceDance.API.Contracts/Features/Competitions/CompetitionContracts.cs # new
src/backend/TB.DanceDance.API.Contracts/Features/Sharing/SharedLink.cs                # extended: SharedVideoItem, IsCompetition, Videos[]

src/backend/Infrastructure/Data/Migrations/
├── 20260618231129_AddCompetitions.cs
├── 20260618232718_AddCompetitionSharedLinks.cs
└── 20260619082404_AddCompetitionComments.cs

src/tests/TB.DanceDance.Tests/Features/Competitions/
├── CompetitionPersistenceTests.cs
├── CompetitionServiceTests.cs
├── CompetitionSharingTests.cs
└── CompetitionCommentsTests.cs

src/my-dance.web/src/app/core/api/
├── competitions.service.ts                # new
└── api-models.ts                          # extended: Competition*, SharedVideoItem, etc.

src/my-dance.web/src/app/features/competitions/
├── competitions.ts / .html / .spec.ts             # list view
├── competition-detail.ts / .html / .spec.ts       # detail view (rename/add/remove/delete/share)
└── competition-create-dialog.ts / .spec.ts

src/my-dance.web/src/app/features/videos/my-videos.ts   # extended: group-into-competition action
src/my-dance.web/src/app/features/sharing/
├── shared-link-viewer.ts / .html / .spec.ts       # extended: multi-video + single combined thread
└── share-dialog.ts                                 # extended: competition target

src/my-dance.web/src/app/app.routes.ts       # extended: /competitions, /competitions/:competitionId
```

**Structure Decision**: This is the existing web application structure (Angular SPA +
layered .NET backend) described in `CLAUDE.md` — no new top-level project. The feature is
implemented as one new backend feature area (`Competitions`) plus additive extensions to the
existing `Sharing` and `Comments` backend areas, and one new Angular feature area
(`features/competitions`) plus additive extensions to the existing `videos` and `sharing`
Angular feature areas. No new project, package, or service boundary was introduced.

## Complexity Tracking

*No Constitution Check violations — table intentionally omitted.*
