# Implementation Plan: Invite via Single-Use Link

**Branch**: `008-invite-links` | **Date**: 2026-06-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/008-invite-links/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Group admins and event owners can mint a short, single-use link scoped to one group or one event. Anyone who opens it and is (or becomes) signed in is granted membership/access on first redemption; every subsequent open is rejected. Links also expire automatically after a fixed system-wide window and can be revoked early by any current admin of the target group/event — not just the link's creator. This follows the existing `SharedLink` / `VideoTransfer` short-link patterns already in the codebase (short Base62 id, expiry, status lifecycle), but adds the single-redemption race-safety guarantee neither existing feature needs.

## Technical Context

**Language/Version**: C# / .NET 10 (backend), TypeScript / Angular 22 (frontend, `src/my-dance.web`)

**Primary Dependencies**: FastEndpoints (API endpoints), EF Core / Npgsql (persistence), `angular-auth-oidc-client` (frontend auth state), existing `ShortLinkGenerator` (Base62 link ids)

**Storage**: PostgreSQL, `access` schema (same schema as `Group`, `Event`, `GroupAdmin`, `GroupAssigmentRequest`/`EventAssigmentRequest`, `AssignedToGroup`/`AssignedToEvent`)

**Testing**: xUnit v3 + Testcontainers (PostgreSQL) for backend integration tests in `src/tests/TB.DanceDance.Tests/`; Vitest for frontend unit tests

**Target Platform**: Existing Angular SPA (`src/my-dance.web`) + ASP.NET Core API. Mobile (MAUI) is out of scope — the spec describes a web-shared link flow and the mobile app has no equivalent group/event admin surface today.

**Project Type**: Web application (extends the existing Groups/Events features in an established monorepo)

**Performance Goals**: None beyond the spec's SC-001 (link creation <10s) and SC-004 (revoke takes effect within seconds) — both satisfied trivially by a single synchronous DB write; no special performance work needed.

**Constraints**: Redemption must be race-safe — under concurrent opens of the same link, exactly one MUST succeed (SC-002/SC-003, edge case in spec). No existing feature in this codebase has this exact requirement (`SharedLink` is multi-use-until-expiry; `VideoTransfer` is single-recipient but has no concurrent-redeemer race because only the intended recipient can plausibly hold the link in practice, and it doesn't promise atomicity under a race — invite links explicitly must, since they're not bound to one identity per FR-011).

**Scale/Scope**: Same order of magnitude as existing groups/events (small number of admins per group/event, infrequent link creation) — no scale-specific design needed.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Assessment |
|---|---|
| I. Layered Architecture Discipline | **PASS.** New `InviteLink` entity in `Domain`; `InviteLinkService` in `Application/Features/Invites/`; FastEndpoints under the same feature folder; `Infrastructure` only gains DbContext configuration + a migration. Controllers/endpoints reach the DB only through the service, matching `SharedLinkService`/`TransferService`. Frontend HTTP access goes through a new `invite-links.service.ts` in `src/app/core/api/`, never `HttpClient` directly from components. |
| II. Test-First & Integration Testing (NON-NEGOTIABLE) | **PASS, with an explicit new test requirement.** Integration tests (Testcontainers-backed, real PostgreSQL) MUST cover: create (admin-only), redeem-once-succeeds, redeem-twice-fails, **concurrent redemption resolves to exactly one winner**, revoke-by-any-admin, revoke-after-redeem-is-no-op, expiry. The concurrency test is the one genuinely new testing concern this feature introduces. |
| III. Simplicity & No Speculative Abstraction | **PASS.** No configurable expiration (confirmed via clarification — fixed constant, mirroring `VideoTransfer.RollbackWindowDays`-style constants, not a request parameter). No per-invitee binding (confirmed via clarification — no email-target field). Reuses `IGroupService.IsGroupAdmin` and `Event.Owner` equality rather than inventing a new permission concept. |
| IV. Security & Dev/Prod Boundary Guardrails | **PASS / N/A.** No dev-only flags involved. One deliberate, spec-mandated deviation from the existing `transfer/:linkId` route pattern: the redemption route must NOT use `autoLoginPartialRoutesGuard` (which auto-redirects to OIDC login), because FR-012 requires showing an explicit "please sign in" message instead of an automatic redirect. This is called out explicitly here so it isn't mistaken for an oversight in review. |

No violations — Complexity Tracking section is not needed.

## Project Structure

### Documentation (this feature)

```text
specs/008-invite-links/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── invite-links.http
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/backend/
├── Application/
│   ├── Domain/Entities/
│   │   ├── InviteLink.cs                # new
│   │   └── InviteLinkStatus.cs          # new (enum: Active, Redeemed, Revoked)
│   ├── Features/Invites/                # new feature folder
│   │   ├── IInviteLinkService.cs
│   │   ├── InviteLinkService.cs
│   │   ├── InvitesModule.cs             # DI registration, mirrors SharingModule.cs
│   │   └── Endpoints/
│   │       ├── CreateGroupInviteLinkEndpoint.cs
│   │       ├── CreateEventInviteLinkEndpoint.cs
│   │       ├── ListGroupInviteLinksEndpoint.cs
│   │       ├── ListEventInviteLinksEndpoint.cs
│   │       ├── GetInviteLinkInfoEndpoint.cs    # AllowAnonymous — preview before login
│   │       ├── RedeemInviteLinkEndpoint.cs     # Policies(ApiScopes.Read) — requires auth
│   │       └── RevokeInviteLinkEndpoint.cs
│   └── ApiRoutes.cs                      # add InviteLinks routes; extend Groups/Events
├── TB.DanceDance.API.Contracts/Features/Invites/
│   ├── InviteLinkResponse.cs
│   └── InviteLinkInfoResponse.cs
└── Infrastructure/Data/
    ├── DanceDbContext.cs                 # add DbSet<InviteLink>, OnModelCreating config
    └── Migrations/                      # new migration (via the generate-migration tooling)

src/my-dance.web/src/app/
├── core/api/
│   ├── invite-links.service.ts          # new, mirrors sharing.service.ts/transfers.service.ts
│   └── api-models.ts                    # add InviteLinkModel, InviteLinkInfoModel
└── features/
    ├── invites/
    │   ├── invite-landing.ts            # new — public-ish redemption page, mirrors transfer-landing.ts
    │   └── invite-landing.html
    ├── groups/
    │   ├── group-management.ts          # extend — add "Invite Links" panel
    │   └── group-management.html
    └── events/
        ├── event-management.ts          # new — no per-event admin screen exists yet; minimal
        └── event-management.html        #   scope: invite-link management only, not full membership UI
```

**Structure Decision**: Follow the existing `Features/<Capability>/{Service, Endpoints/}` backend layout used by `Sharing` and `Transfers`, and the existing `core/api/<capability>.service.ts` + `features/<capability>/` frontend layout. The one structural addition beyond "more of the same" is `event-management.ts`: today there is no per-event admin screen at all (events only have `Owner`, no member-management UI), so a minimal one is added — scoped strictly to invite-link management, not a broader event-membership feature, per Principle III.

## Complexity Tracking

*No constitution violations — section intentionally left without entries.*
