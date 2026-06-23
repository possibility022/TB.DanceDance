# Quickstart: Validate Competition Grouping & Combined Feedback

Validates the end-to-end flow in `spec.md` (User Stories 1-3) against a running local stack, per
the `local-stack` skill and `CLAUDE.md`'s Local Development instructions.

## Prerequisites

- Docker stack rebuilt after the competition migrations (`AddCompetitions`,
  `AddCompetitionSharedLinks`, `AddCompetitionComments` — see `data-model.md`) and the local DB
  reset so they apply cleanly.
- A seeded user available: `testemail@email.com` / `1234` (per `CLAUDE.md`'s dev-only password
  grant), with at least 2 private videos already uploaded (seeded or uploaded manually).

## Setup

```bash
docker compose -f local_environment.dockercompose.yaml up
docker logs tbdanceInitializer   # wait for "Data initialized"
```

If validating against the API directly rather than through the web app, fetch a token via the
dev-only password grant described in `src/backend/Application/httpRequests/token.http`.

## Scenario A — Group videos into a competition (User Story 1)

1. Log in as the seeded user; open **My Videos**.
2. Create a competition (e.g., "Regional Finals 2026") and add two of the user's own private
   videos to it.
3. **Expected**: both videos now show as grouped under that competition and no longer appear as
   standalone videos (FR-001, FR-002, SC-001).
4. Attempt to add one of those same videos to a second, different competition.
5. **Expected**: rejected — a video belongs to at most one competition (FR-003, SC-005).

## Scenario B — Share the competition and collect one combined thread (User Story 2)

1. From the competition's detail view, create a share link (Share action).
2. **Expected**: exactly one link is produced (SC-002).
3. Open that link in a separate/incognito session (anonymous or authenticated, depending on the
   link's allow-comments settings).
4. **Expected**: every video grouped into the competition plays from this one link (FR-010).
5. Post one comment as feedback.
6. **Expected**: back as the owner, the competition's combined thread shows that one comment
   (FR-011, SC-003); no per-video thread was created for it.
7. Open an existing standalone video's own (pre-existing) share link.
8. **Expected**: it still works exactly as before — single video, single thread (FR-014).

## Scenario C — Manage a competition over time (User Story 3)

1. With the competition above still shared, upload (or use) a third private video belonging to
   the same competition and add it via the competition detail view.
2. **Expected**: reloading the *same* share link now also shows the third video — no new link was
   needed (FR-002).
3. Remove one video from the competition.
4. **Expected**: it drops out of the share link and reverts to being a standalone video (FR-005).
5. Delete the competition entirely.
6. **Expected**: the remaining grouped videos become standalone; none of them are deleted
   (FR-006, SC-006); the competition's share link no longer resolves to a competition.

## Authorization checks (cross-cutting, FR-007, SC-004)

- Repeat the rename/delete/add-video/remove-video actions above as a different seeded user
  (`testemail2@email.com`) against the first user's competition.
- **Expected**: every attempt is rejected and the competition is unchanged.

## Done when

All scenarios above pass against the running local stack, matching `spec.md`'s acceptance
scenarios and success criteria.
