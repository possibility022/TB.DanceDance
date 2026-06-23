---

description: "Task list for Invite via Single-Use Link"
---

# Tasks: Invite via Single-Use Link

**Input**: Design documents from `/specs/012-invite-links/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/invite-links.http](./contracts/invite-links.http), [quickstart.md](./quickstart.md)

**Tests**: Included — Constitution Principle II (Test-First & Integration Testing) is NON-NEGOTIABLE for this project, and plan.md's Constitution Check explicitly names the required integration test scenarios (create, redeem-once, redeem-twice-fails, concurrent redemption, revoke-by-any-admin, revoke-after-redeem-no-op, expiry). Tests use xUnit v3 + Testcontainers (real PostgreSQL), following the existing `Features/Transfers/` / `Features/Sharing/` test layout.

**Organization**: Tasks are grouped by user story (US1–US4 from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- File paths are exact and repo-relative

## Path Conventions

- Backend: `src/backend/Application/`, `src/backend/Infrastructure/`, `src/backend/TB.DanceDance.API.Contracts/`
- Backend tests: `src/tests/TB.DanceDance.Tests/Features/Invites/`
- Frontend: `src/my-dance.web/src/app/`

---

## Phase 1: Setup

**Purpose**: Scaffold the new feature's folders (the surrounding monorepo, tooling, and DI container already exist — no project init needed)

- [X] T001 Create the backend feature folder `src/backend/Application/Features/Invites/Endpoints/` (empty, ready for Phase 2+ files)
- [X] T002 [P] Create the frontend feature folder `src/my-dance.web/src/app/features/invites/` (empty, ready for US2's `invite-landing.ts`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The `InviteLink` entity, persistence, service contract, routes, and frontend models that every user story builds on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 [P] Create `InviteLinkStatus` enum (`Active = 0, Redeemed = 1, Revoked = 2`) in `src/backend/Application/Domain/Entities/InviteLinkStatus.cs`
- [X] T004 Create `InviteLink` entity (`Id`, `GroupId`, `EventId`, `CreatedBy`, `CreatedAt`, `ExpireAt`, `Status`, `RedeemedByUserId`, `RedeemedAt`, navigation properties) per data-model.md, in `src/backend/Application/Domain/Entities/InviteLink.cs` (depends on T003)
- [X] T005 Configure `InviteLink` in `DanceDbContext` — `DbSet<InviteLink>`, `access` schema, unique index on `Id`, DB check constraint `GroupId XOR EventId` (mirroring `SharedLink`'s `CK_SharedLinks_VideoOrCompetition`), FK navigations to `Group`/`Event`/`User` — in `src/backend/Infrastructure/Data/DanceDbContext.cs` (depends on T004)
- [X] T006 Generate and review the EF Core migration for the new `InviteLinks` table in `src/backend/Infrastructure/Data/Migrations/` (depends on T005)
- [X] T007 [P] Create `InviteLinkResponse` and `InviteLinkInfoResponse` contracts per contracts/invite-links.http response shapes, in `src/backend/TB.DanceDance.API.Contracts/Features/Invites/InviteLinkResponse.cs` and `InviteLinkInfoResponse.cs`
- [X] T008 Create `IInviteLinkService` interface (`CreateForGroupAsync`, `CreateForEventAsync`, `GetInfoAsync`, `RedeemAsync`, `ListForGroupAsync`, `ListForEventAsync`, `RevokeAsync`) in `src/backend/Application/Features/Invites/IInviteLinkService.cs` (depends on T004)
- [X] T009 Implement `InviteLinkService` skeleton with short-id generation — reuse `ShortLinkGenerator.GenerateShortLinkId()` with the collision-retry loop pattern from `TransferService.GenerateUniqueLinkIdAsync` (max 5 retries) — in `src/backend/Application/Features/Invites/InviteLinkService.cs` (depends on T008)
- [X] T010 [P] Register `InviteLinkService`/`IInviteLinkService` in DI via `InvitesModule.cs`, mirroring `SharingModule.cs`, in `src/backend/Application/Features/Invites/InvitesModule.cs` (depends on T009)
- [X] T011 [P] Add `InviteLinks` route constants for groups and events to `src/backend/Application/ApiRoutes.cs`
- [X] T012 [P] Add `InviteLinkModel` and `InviteLinkInfoModel` to `src/my-dance.web/src/app/core/api/api-models.ts`
- [X] T013 [P] Create `invite-links.service.ts` with typed methods for all 6 endpoints, mirroring `sharing.service.ts`/`transfers.service.ts`, in `src/my-dance.web/src/app/core/api/invite-links.service.ts` (depends on T012)

**Checkpoint**: Foundation ready — `InviteLink` persists, the service contract and DI exist, and the frontend has typed models. User story implementation can now begin.

---

## Phase 3: User Story 1 - Generate and share an invite link (Priority: P1) 🎯 MVP (part 1 of 3)

**Goal**: A group admin or event owner can generate a unique, shareable invite link for their group/event; non-admins are denied.

**Independent Test**: An administrator generates an invite link for a group or event and a shareable URL is produced — no redemption flow needed yet.

### Tests for User Story 1

- [X] T014 [US1] Integration tests: `InviteLinkService.CreateForGroupAsync`/`CreateForEventAsync` succeed for an admin/owner and throw/deny for a non-admin (group + event variants), in `src/tests/TB.DanceDance.Tests/Features/Invites/InviteLinkServiceTests.cs`
- [X] T015 [P] [US1] Endpoint tests: `POST /api/groups/{id}/invite-links` and `POST /api/events/{id}/invite-links` return `200 OK` for an admin/owner and `403 Forbidden` for a non-admin, in `src/tests/TB.DanceDance.Tests/Features/Invites/InviteLinkEndpointTests.cs`

### Implementation for User Story 1

- [X] T016 [US1] Implement `InviteLinkService.CreateForGroupAsync`/`CreateForEventAsync` — admin check via `IGroupService.IsGroupAdmin` / `event.Owner == userId` (FR-003), `ExpireAt = CreatedAt + InviteLink.ExpirationDays` (7, research.md §7) — in `src/backend/Application/Features/Invites/InviteLinkService.cs` (depends on T009, T014 failing first)
- [X] T017 [P] [US1] Implement `CreateGroupInviteLinkEndpoint.cs` (`POST`, `Policies(ApiScopes.Read)`) in `src/backend/Application/Features/Invites/Endpoints/CreateGroupInviteLinkEndpoint.cs` (depends on T016)
- [X] T018 [P] [US1] Implement `CreateEventInviteLinkEndpoint.cs` (`POST`, `Policies(ApiScopes.Read)`) in `src/backend/Application/Features/Invites/Endpoints/CreateEventInviteLinkEndpoint.cs` (depends on T016)
- [X] T019 [US1] Add an "Invite Links" panel with a "Generate Invite Link" action that displays the created URL, in `src/my-dance.web/src/app/features/groups/group-management.ts` and `group-management.html` (depends on T013, T017)
- [X] T020 [US1] Create a new, minimally-scoped `event-management.ts`/`event-management.html` component with the same "Generate Invite Link" action (no per-event admin screen exists today — see plan.md Structure Decision), in `src/my-dance.web/src/app/features/events/event-management.ts` (depends on T013, T018)
- [X] T021 [US1] Register the route for the new event-management component, mirroring the existing group-management route, in `src/my-dance.web/src/app/app.routes.ts` (depends on T020)

**Checkpoint**: User Story 1 is fully functional and independently testable — admins can create a link and see its URL.

---

## Phase 4: User Story 2 - Redeem an invite link to join (Priority: P1) 🎯 MVP (part 2 of 3)

**Goal**: A person who opens a valid, unused invite link is granted membership/access; already-members are a no-op; signed-out users see a message instead of an auto-redirect.

**Independent Test**: Take a freshly generated, unused invite link, open it as a different signed-in person, and confirm that person becomes a member.

### Tests for User Story 2

- [X] T022 [P] [US2] Integration tests: `InviteLinkService.RedeemAsync` grants `AssignedToGroup`/`AssignedToEvent` on first redemption (FR-009) and is a no-op for an already-existing member (FR-010), in `InviteLinkServiceTests.cs`
- [X] T023 [P] [US2] Endpoint tests: `GET /api/invite-links/{id}` (anonymous) returns the public preview; `POST /api/invite-links/{id}/redeem` returns `401 Unauthorized` when signed out and `200 OK` when signed in, in `InviteLinkEndpointTests.cs`

### Implementation for User Story 2

- [X] T024 [US2] Implement `InviteLinkService.GetInfoAsync` — public preview (`targetType`, `targetName`, `isRedeemable`) for use before sign-in (FR-005/FR-012) — in `InviteLinkService.cs` (depends on T009)
- [X] T025 [US2] Implement `InviteLinkService.RedeemAsync` (initial version): already-member short-circuit (FR-010) before any state change, then insert `AssignedToGroup`/`AssignedToEvent` on success (FR-009) — single-use race-safety hardening happens in US3 — in `InviteLinkService.cs` (depends on T009)
- [X] T026 [P] [US2] Implement `GetInviteLinkInfoEndpoint.cs` (`GET`, `AllowAnonymous()`) in `src/backend/Application/Features/Invites/Endpoints/GetInviteLinkInfoEndpoint.cs` (depends on T024)
- [X] T027 [P] [US2] Implement `RedeemInviteLinkEndpoint.cs` (`POST`, `Policies(ApiScopes.Read)`) in `src/backend/Application/Features/Invites/Endpoints/RedeemInviteLinkEndpoint.cs` (depends on T025)
- [X] T028 [US2] Create `invite-landing.ts`/`invite-landing.html`: calls the info endpoint on load, shows target name; if signed out, shows a "please sign in" message with a manual sign-in action and **no automatic redirect** (FR-012); if signed in, triggers redemption — in `src/my-dance.web/src/app/features/invites/invite-landing.ts` (depends on T013, T026, T027)
- [X] T029 [US2] Register the `invite/:linkId` route with **no** `canActivate` guard (deliberately unlike `transfer/:linkId`'s `autoLoginPartialRoutesGuard` — research.md §6) in `src/my-dance.web/src/app/app.routes.ts` (depends on T028)
- [X] T030 [US2] After signing in from the landing page's message, automatically complete redemption of the originally-opened link without requiring the user to re-open it (FR-013), in `invite-landing.ts` (depends on T028, T029)

**Checkpoint**: User Stories 1 AND 2 both work independently — a link can be created and redeemed end-to-end.

---

## Phase 5: User Story 3 - Invite link becomes unusable after one redemption (Priority: P1) 🎯 MVP (part 3 of 3)

**Goal**: Exactly one redemption ever succeeds per link, even under concurrent attempts; every later attempt is clearly rejected.

**Independent Test**: Redeem a link once, then attempt to redeem the same link again and confirm the second attempt is rejected with a clear explanation.

### Tests for User Story 3

- [X] T031 [P] [US3] Integration test: a second redemption attempt by a different user after a successful first redemption is rejected (FR-004), in `InviteLinkServiceTests.cs`
- [X] T032 [P] [US3] Integration test: two concurrent `RedeemAsync` calls for the same link (e.g. two parallel `Task`s) resolve to exactly one success and one rejection (edge case; research.md §1 — the one genuinely new testing concern this feature introduces), in `InviteLinkServiceTests.cs`

### Implementation for User Story 3

- [X] T033 [US3] Harden `InviteLinkService.RedeemAsync` with the atomic conditional update — `ExecuteUpdateAsync` setting `Status = Redeemed` with `Where(l => l.Status == InviteLinkStatus.Active)`, proceeding to the membership insert only if exactly one row was affected (research.md §1) — replacing the read-then-write version from T025, in `InviteLinkService.cs` (depends on T025, T031, T032 failing first)
- [X] T034 [US3] Surface the "already used" rejection as a clear, non-technical message (FR-005) from `RedeemInviteLinkEndpoint.cs` and in `invite-landing.ts` (depends on T027, T028, T033)

**Checkpoint**: All three P1 stories (US1–US3) are complete — this is the MVP.

---

## Phase 6: User Story 4 - Manage outstanding invite links (Priority: P2)

**Goal**: Any current admin of a group/event can view all its invite links (regardless of creator) and revoke an unredeemed one.

**Independent Test**: Create an invite link, view it in the list of outstanding links, revoke it, and confirm it can no longer be redeemed.

### Tests for User Story 4

- [X] T035 [P] [US4] Integration test: any current admin — not just the link's creator — can list a group's/event's invite links with correct status, in `InviteLinkServiceTests.cs`
- [X] T036 [P] [US4] Integration test: any current admin can revoke an unredeemed link (blocking future redemption); revoking an already-redeemed link is a no-op (User Story 4 scenario 3); a non-admin's revoke attempt is denied, in `InviteLinkServiceTests.cs`

### Implementation for User Story 4

- [X] T037 [US4] Implement `InviteLinkService.ListForGroupAsync`/`ListForEventAsync` — same admin check as creation, independent of `CreatedBy` (FR-007/FR-008), computing the read-time expired status (research.md §3) — in `InviteLinkService.cs` (depends on T009)
- [X] T038 [US4] Implement `InviteLinkService.RevokeAsync` — same admin check, no-op if `Status == Redeemed` — in `InviteLinkService.cs` (depends on T009)
- [X] T039 [P] [US4] Implement `ListGroupInviteLinksEndpoint.cs` and `ListEventInviteLinksEndpoint.cs` in `src/backend/Application/Features/Invites/Endpoints/` (depends on T037)
- [X] T040 [P] [US4] Implement `RevokeInviteLinkEndpoint.cs` (`DELETE`) in `src/backend/Application/Features/Invites/Endpoints/RevokeInviteLinkEndpoint.cs` (depends on T038)
- [X] T041 [US4] Extend the "Invite Links" panel in `group-management.ts`/`.html` with a list of outstanding links (status, who redeemed/when) and a revoke action (depends on T019, T039, T040)
- [X] T042 [US4] Extend `event-management.ts`/`.html` with the same list-and-revoke UI (depends on T020, T039, T040)

**Checkpoint**: All four user stories are independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Cross-cutting FRs that don't belong to exactly one story, plus final validation

- [X] T043 [P] Integration test: an unused invite link automatically becomes unredeemable once `ExpireAt` passes (FR-006), in `InviteLinkServiceTests.cs`
- [X] T044 [P] Integration test: a malformed/unrecognized invite-link id returns `404 Not Found` from `GetInviteLinkInfoEndpoint` (edge case), in `InviteLinkEndpointTests.cs`
- [X] T045 Verify whether the explicit unique index on `InviteLink.Id` (data-model.md "Schema placement") is redundant given the EF-configured key, and drop it if so, in `DanceDbContext.cs` — `Id` is the EF-configured primary key by convention (same as `SharedLink`/`VideoTransfer`), so no separate unique index was ever added; confirmed redundant and intentionally omitted.
- [X] T046 Run the quickstart.md validation scenarios end-to-end against the local docker-compose stack — Scenarios 1 and 2 verified live via `curl` against the running stack (create → anonymous info preview → redeem → DB-verified `AssingedToGroups` row → repeat redeem 409; create → list visible to a non-creator admin → revoke by that admin 204 → redeem of revoked link 409). SPA routes for `/invite/:linkId` and `/groups/:id/manage` confirmed served (200); a full visual/browser check of the new UI panels was not done — no headless-browser driver (`chromium-cli`) is installed in this environment.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **User Stories (Phase 3–6)**: All depend on Foundational completion
  - US1, US2, US3 are all P1 and form the MVP together (US2 depends on US1 existing for a link to redeem; US3 depends on US2's redeem endpoint existing to harden)
  - US4 (P2) can start once Foundational is done, but is most naturally built after US1–US3 since it reuses the same service/admin-check plumbing
- **Polish (Phase 7)**: Depends on all four user stories being complete

### User Story Dependencies

- **US1 (P1)**: Foundational only. No dependency on other stories.
- **US2 (P1)**: Foundational + needs a link to exist to redeem (practically sequenced after US1, though its service/endpoint code is independent).
- **US3 (P1)**: Builds directly on US2's `RedeemAsync`/`RedeemInviteLinkEndpoint` (hardens the same code path) — sequenced after US2.
- **US4 (P2)**: Foundational only for its own list/revoke logic; independently testable once US1 has produced a link to manage.

### Parallel Opportunities

- T003, T007, T010, T011, T012 (Phase 2) can run in parallel — different files, no inter-dependencies
- T013 depends on T012 but is otherwise independent of the backend foundational tasks
- Within each story's "Tests" subsection, the listed test tasks marked [P] target different files and can run in parallel
- T017/T018, T026/T027, T039/T040 (endpoint pairs within a story) are different files and can run in parallel
- T043/T044 (Polish tests) can run in parallel

---

## Parallel Example: User Story 1

```bash
# Tests (different files):
Task: "Integration tests for CreateForGroupAsync/CreateForEventAsync in InviteLinkServiceTests.cs"
Task: "Endpoint tests for create invite-link routes in InviteLinkEndpointTests.cs"

# Endpoints (different files, after T016):
Task: "Implement CreateGroupInviteLinkEndpoint.cs"
Task: "Implement CreateEventInviteLinkEndpoint.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1–3 only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3 (US1) → Phase 4 (US2) → Phase 5 (US3) in order
4. **STOP and VALIDATE**: run quickstart.md Scenarios 1 and 3 — create, redeem, single-use, concurrency
5. This is the MVP: a working, race-safe, single-use invite link, end-to-end

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. US1 → admins can generate links (testable, but not yet redeemable)
3. US2 → links are redeemable (MVP core loop complete)
4. US3 → single-use guarantee hardened and proven under concurrency (MVP complete)
5. US4 → admins gain visibility and revocation control (incremental, P2)
6. Polish → expiry, 404 handling, schema cleanup, full quickstart pass

---

## Notes

- [P] tasks touch different files with no unmet dependencies
- [Story] labels map every story-phase task back to spec.md's US1–US4 for traceability
- Tests are written before their corresponding implementation task within each story (per Constitution Principle II)
- Commit after each task or logical group
- Stop at any checkpoint to validate a story independently before moving on
