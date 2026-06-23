# Research: Invite via Single-Use Link

No items in Technical Context were left as `NEEDS CLARIFICATION` — the spec's clarification session already resolved the two open product questions (link ownership/permissions, fixed vs. configurable expiration). This document instead records the technical decisions made while reconciling the spec with existing codebase patterns (`SharedLink`, `VideoTransfer`), each as Decision / Rationale / Alternatives considered.

## 1. Race-safe single redemption

**Decision**: Redeem by issuing a single conditional update — `UPDATE InviteLinks SET Status = Redeemed, RedeemedByUserId = @user, RedeemedAt = @now WHERE Id = @id AND Status = Active` (via EF Core's `ExecuteUpdateAsync` with a `Where(l => l.Status == InviteLinkStatus.Active)` predicate) — and only insert the `AssignedToGroup`/`AssignedToEvent` row if exactly one row was affected. If zero rows are affected, treat the attempt as already-used/revoked/expired (FR-005).

**Rationale**: This is the only place in the feature where two concurrent requests can legitimately race for the same row (spec edge case: "two people open the same unused invite link at nearly the same time"). A conditional `UPDATE ... WHERE Status = Active` is atomic at the database level regardless of isolation level, so exactly one of two concurrent callers gets `RowsAffected == 1`. Neither `SharedLink` nor `VideoTransfer` needs this — `SharedLink` allows unlimited redemptions until expiry, and `VideoTransfer.AcceptTransferAsync` only has a plausible single intended recipient, with no atomicity claim in its design.

**Alternatives considered**: A serializable transaction with a `SELECT ... FOR UPDATE`-equivalent (EF Core doesn't expose row locking directly without raw SQL) — rejected as unnecessary ceremony when a conditional `UPDATE` gives the same guarantee with no raw SQL and no explicit transaction management. An EF Core concurrency token (`[ConcurrencyCheck]`/`RowVersion`) on `Status` — works too, but throws `DbUpdateConcurrencyException` on the loser, requiring a catch block to translate that into "already used"; the conditional-update-and-check-rows-affected approach reads as a direct, intention-revealing check instead of exception-driven control flow, more in line with Principle III (no defensive scaffolding for cases that can be expressed directly).

## 2. Short link id generation

**Decision**: Reuse `Domain.Entities.ShortLinkGenerator.GenerateShortLinkId()` (8-char Base62) with the same generate-and-retry-on-collision loop already used in `TransferService.GenerateUniqueLinkIdAsync` (check `dbContext.InviteLinks.AnyAsync(l => l.Id == candidate)`, retry up to 5 times).

**Rationale**: Consistency — this is the third feature (`SharedLink`, `VideoTransfer`, now `InviteLink`) to need a short, URL-safe, unguessable id; there's already a shared generator and an established collision-retry idiom. No new abstraction needed (Principle III) since the existing static helper is already general-purpose.

**Alternatives considered**: A GUID-based id — rejected because the link must be short enough to be comfortably shared as a URL, which is exactly why `ShortLinkGenerator` exists.

## 3. Status modeling

**Decision**: Persist a `Status` enum (`Active`, `Redeemed`, `Revoked`) plus `ExpireAt`. Expiry is computed at read time (`Status == Active && ExpireAt <= DateTimeOffset.UtcNow` ⇒ treat as expired) rather than persisted as its own status — mirroring `TransferService.GetTransferAsync`, which treats an expired-but-still-`Pending` `VideoTransfer` as dead without a separate `Expired` enum value.

**Rationale**: Avoids a background job or trigger to flip rows to `Expired` on a timer; expiry is a pure function of `ExpireAt` and current time, so storing it would just be a derived value the system would have to keep in sync. Matches the existing `VideoTransfer` precedent exactly.

**Alternatives considered**: A persisted `Expired` status set by a scheduled job — rejected as unnecessary background infrastructure for a value that's computed for free on every read.

## 4. Authorization for create / list / revoke

**Decision**: For groups, reuse `IGroupService.IsGroupAdmin(groupId, userId, ct)` (existing method backing `GroupsAdmins`) for create, list, and revoke — i.e., the same check already used by `AddGroupAdminEndpoint` and friends. For events, reuse the existing inline check `event.Owner == userId` (events have no separate admin table — see Surprises below). Per the spec clarification, list/revoke deliberately do **not** also filter by "creator of this specific link" — any current admin passes the same single check used for create.

**Rationale**: Directly satisfies FR-007/FR-008 ("any current admin... not only the one who created it") using the permission checks that already exist for these resources, rather than inventing a parallel ACL just for invite links.

**Alternatives considered**: Storing a snapshot of "admins at creation time" on the link and checking membership in that set — rejected; it would let a since-removed admin keep managing the link and would deny a newly-added admin, the opposite of what FR-007 wants.

## 5. Event admin model gap (pre-existing, not introduced by this feature)

**Observation, not a decision**: Groups have a real `GroupAdmin` junction table (so "any current admin" is meaningfully plural), but `Event` only has a single `Owner` string property — there is no multi-admin concept for events anywhere in the current codebase. For this feature, "any current admin of the event" therefore degenerates to "the one owner," which already satisfies FR-007/FR-008 for events without any schema change. This plan does not attempt to retrofit multi-admin support for events — that would be unrelated, speculative scope.

## 6. Frontend: redemption route must not auto-redirect to login

**Decision**: The new `invite/:linkId` route carries **no** `canActivate` guard (it is a public route, unlike `transfer/:linkId`, which uses `autoLoginPartialRoutesGuard`). The `invite-landing` component itself reads the current auth state (via the existing OIDC auth service already used elsewhere in `core/auth/`) and renders either the redemption UI (if signed in) or an explanatory "you need to sign in to accept this invite" message with a manual sign-in action (if signed out) — never an automatic redirect.

**Rationale**: This is a direct, spec-mandated requirement (FR-012, from the clarification: "It should not be an automated redirect to login or sign in page"). It is the one deliberate divergence from the otherwise-identical `transfer/:linkId` precedent, and is called out explicitly so a future reviewer doesn't "fix" it back to consistency with `transfer/:linkId`.

**Alternatives considered**: Reusing `autoLoginPartialRoutesGuard` like `transfer/:linkId` — rejected outright; it's incompatible with the explicit clarification answer.

## 7. Fixed expiration window value

**Decision**: 7 days, as a constant on `InviteLink` (e.g. `InviteLink.ExpirationDays = 7`), not a request parameter.

**Rationale**: Per the clarification, the window is fixed and not creator-configurable. Seven days is shorter than `SharedLink`/`VideoTransfer`'s creator-chosen range (1–365 days) because an invite link is meant to be acted on promptly by one specific intended recipient, not held open as a long-lived sharing mechanism; it's long enough to survive a weekend without the admin needing to regenerate it.

**Alternatives considered**: Matching `SharedLink`'s default-if-unspecified (its endpoints require the caller to pass `expirationDays` explicitly, so there is no existing "default" constant to inherit) — there's nothing to reuse here, so a fresh, deliberately short constant was chosen instead.
