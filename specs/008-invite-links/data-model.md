# Data Model: Invite via Single-Use Link

## InviteLink (new entity)

Mirrors the shape of `SharedLink` / `VideoTransfer` (short id, creator, expiry, status), with the two fields a redemption record needs (`RedeemedByUserId`, `RedeemedAt`) and a `GroupId`/`EventId` XOR target, matching `SharedLink`'s `VideoId`/`CompetitionId` XOR pattern.

| Field | Type | Notes |
|---|---|---|
| `Id` | `string` | PK. 8-char Base62 via `ShortLinkGenerator.GenerateShortLinkId()`, generated with collision-retry (FR-001/FR-002). |
| `GroupId` | `Guid?` | Set when the link targets a group. Exactly one of `GroupId`/`EventId` is set (DB check constraint, like `SharedLink`'s `VideoId`/`CompetitionId`). |
| `EventId` | `Guid?` | Set when the link targets an event. |
| `CreatedBy` | `string` | User id of the admin who generated the link (FK → `User`). Retained for audit/display even though revoke/list permission does **not** key off this field (see Clarifications — any current admin manages it, not just the creator). |
| `CreatedAt` | `DateTimeOffset` | |
| `ExpireAt` | `DateTimeOffset` | `CreatedAt + InviteLink.ExpirationDays` (fixed constant, currently 7 — see research.md §7). Not creator-configurable. |
| `Status` | `InviteLinkStatus` | `Active` (default) → `Redeemed` \| `Revoked`. Expiry is not its own status; an `Active` link past `ExpireAt` is treated as expired at read/redemption time (research.md §3). |
| `RedeemedByUserId` | `string?` | User id who successfully redeemed it; null until redemption (FK → `User`, nullable). |
| `RedeemedAt` | `DateTimeOffset?` | Null until redemption. |

**Navigation properties**: `Group? Group`, `Event? Event`, `User CreatedByUser`, `User? RedeemedByUser`.

### State transitions

```
Active ──(redeem succeeds, first caller wins)──> Redeemed   [terminal]
Active ──(any current admin revokes)────────────> Revoked   [terminal]
Active ──(ExpireAt passes, unredeemed)───────────> (computed "expired"; Status stays Active, treated as dead on read — FR-006)
```

`Redeemed` and `Revoked` are terminal: revoking a `Redeemed` link is a documented no-op (User Story 4, scenario 3); redeeming a `Revoked` or expired-`Active` link is rejected (FR-005).

### Validation rules

- `GroupId` XOR `EventId` must be set (DB check constraint, mirrors `SharedLink`'s `CK_SharedLinks_VideoOrCompetition`).
- Creation requires the caller to pass `IsGroupAdmin(groupId, userId)` (groups) or `event.Owner == userId` (events) — FR-003.
- Redemption requires an authenticated caller (FastEndpoints `Policies(ApiScopes.Read)`) — FR-012. No additional input beyond the link id (FR-011: no per-invitee binding).
- Redemption when the caller already holds membership/access (an `AssignedToGroup`/`AssignedToEvent` row already exists for that user+target) is a no-op success that does **not** transition `Status` (FR-010) — i.e., the conditional update in research.md §1 is only attempted after this membership check short-circuits.
- List/Revoke require the caller to currently pass the same admin check as creation (`IsGroupAdmin`/`event.Owner == userId`) — independent of `CreatedBy` (FR-007/FR-008, per clarification).

### Relationship to existing entities

Redemption does **not** create a new kind of membership record — it inserts the same `AssignedToGroup` / `AssignedToEvent` row that `AccessManagementService.ApproveAccessRequest` already creates today, just without first creating-and-approving a `GroupAssigmentRequest`/`EventAssigmentRequest` (FR-009: "without requiring a separate manual approval step"). `GroupAssigmentRequest`/`EventAssigmentRequest` and the approval flow are untouched by this feature — they remain the path for unsolicited join requests; invite links are a second, parallel path straight to `AssignedToGroup`/`AssignedToEvent`.

```
InviteLink (Redeemed) ──inserts──> AssignedToGroup { GroupId, UserId, WhenJoined = RedeemedAt }
                       or          AssignedToEvent { EventId, UserId }
```

## InviteLinkStatus (new enum)

```csharp
public enum InviteLinkStatus
{
    Active = 0,
    Redeemed = 1,
    Revoked = 2,
}
```

## Schema placement

`access` schema (`DanceDbContext.Schemas.Access`) — same schema as `Group`, `Event`, `GroupAdmin`, `GroupAssigmentRequest`/`EventAssigmentRequest`, `AssignedToGroup`/`AssignedToEvent`. Unique index on `Id` (redundant with the PK but kept for parity with `SharedLink`'s explicit `HasIndex(r => r.Id).IsUnique()`, which exists there because `Id` isn't its EF-configured key in quite the same way — verify during implementation whether this index is actually redundant for `InviteLink` and drop it if so).
