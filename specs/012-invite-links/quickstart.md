# Quickstart: Invite via Single-Use Link

Validates the feature end-to-end against a running local stack. See [contracts/invite-links.http](./contracts/invite-links.http) for exact request/response shapes and [data-model.md](./data-model.md) for the underlying entity.

## Prerequisites

- Local stack running: `docker compose -f local_environment.dockercompose.yaml up`, and `docker logs tbdanceInitializer` shows `Data initialized`.
- Two seeded users available for the dev-only password grant at `/connect/token`: `testemail@email.com` (admin of at least one seeded group) and `testemail2@email.com` (the redeemer), both password `1234`.
- A `groupId` for a group `testemail@email.com` administers (list via `GET /api/groups/my`, or use the seeded group from `src/backend/Application/httpRequests/sharedLinks.http`).

## Scenario 1 — Create and redeem (User Stories 1–3)

1. Get a token for the admin (`testemail@email.com`) via the password grant described in `src/backend/Application/httpRequests/token.http`.
2. `POST /api/groups/{groupId}/invite-links` with the admin's token → expect `200 OK` with `status: "Active"` and a `url` like `https://localhost:3000/invite/<id>`.
3. Get a token for the redeemer (`testemail2@email.com`).
4. `GET /api/invite-links/{id}` with no auth header → expect `200 OK`, `isRedeemable: true`, and the group's name in `targetName` (proves the public preview works signed-out — FR-005/FR-012's "show info before requiring sign-in").
5. `POST /api/invite-links/{id}/redeem` with the redeemer's token → expect `200 OK`, `alreadyMember: false`.
6. `GET /api/groups/my` with the redeemer's token → the group from step 1 now appears (proves FR-009: membership granted without a separate approval step).
7. Repeat step 5 (same redeemer, same link) → expect `409 Conflict` (FR-004: single use).
8. Repeat step 5 with a third user's token → expect `409 Conflict` (FR-011: not bound to one invitee, but still only ever redeemable once total).

## Scenario 2 — Revoke by a non-creator admin (User Story 4, clarification on link ownership)

1. As the admin from Scenario 1, add a second admin to the same group (`POST /api/groups/{groupId}/admins`, if not already seeded with one).
2. As the *first* admin, create a new invite link (`POST /api/groups/{groupId}/invite-links`).
3. As the *second* admin (not the creator), `GET /api/groups/{groupId}/invite-links` → the link from step 2 appears (FR-008: visible to any admin, not just its creator).
4. As the *second* admin, `DELETE /api/invite-links/{id}` → expect `204 No Content` (FR-007: revocable by any current admin).
5. Have a third user attempt `POST /api/invite-links/{id}/redeem` → expect `409 Conflict`.

## Scenario 3 — Concurrent redemption resolves to exactly one winner (edge case, research.md §1)

This is primarily covered by an automated integration test (see Testing below), but can be sanity-checked manually:

1. Create a fresh invite link as in Scenario 1.
2. Fire two `POST /api/invite-links/{id}/redeem` requests back-to-back with two different users' tokens (e.g. two terminal tabs, near-simultaneously).
3. Exactly one returns `200 OK` with `alreadyMember: false`; the other returns `409 Conflict`. Confirm via `GET /api/groups/{groupId}/members` that only the winner was added.

## Scenario 4 — Already-a-member redemption is a no-op (FR-010)

1. As a user already a member of `groupId`, `POST /api/invite-links/{id}/redeem` against a fresh, valid link for that same group → expect `200 OK`, `alreadyMember: true`, and the link's `status` remains `Active` (confirm via `GET /api/groups/{groupId}/invite-links` as an admin — a second, different user can still redeem the same link afterward).

## Frontend check (manual)

1. With the web app running (`npm start` in `src/my-dance.web`, backend stack up per `local_environment.dockercompose.yaml --scale frontendspa=0`), open `http://localhost:3000/invite/<id>` for a fresh link **while signed out**.
2. Expect: a message explaining sign-in is required, with a manual "sign in" action — **not** an automatic redirect to the OIDC login page (FR-012; this is the one deliberate divergence from `transfer/:linkId`'s behavior — see research.md §6).
3. Sign in from that page, then confirm redemption completes without needing to re-open the link (FR-013) and the user lands somewhere showing their new group/event access.

## Testing

Automated coverage lives in `src/tests/TB.DanceDance.Tests/` (Testcontainers-backed integration tests) per the Constitution Check in [plan.md](./plan.md) — in particular a concurrency test exercising Scenario 3 via two parallel `Task`s hitting `RedeemInviteLinkAsync` for the same link id, asserting exactly one `Accepted`-equivalent result.
