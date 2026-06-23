# Data Model: Competition Grouping & Combined Feedback

**Input**: `specs/003-competition-grouping/spec.md`, `research.md`

All entities live in the `video` schema (matching `Video`/`SharedWith`), consistent with
`CLAUDE.md`'s schema layout (`access`, `video`, `comments`, default).

## Competition (new entity)

Owner-owned grouping of the owner's own private videos.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Primary key. |
| `Name` | `string` (required) | Display name; trimmed on write. |
| `OwnerUserId` | `string` (required) | Mirrors `Video.OwnerUserId`. Only this user may mutate the competition. |
| `Date` | `DateTime?` | Optional, display-only; stored UTC. |
| `Location` | `string?` | Optional, display-only; trimmed on write, empty/whitespace stored as `null`. |
| `CommentVisibility` | `CommentVisibility` enum | Default `OwnerOnly`. Governs the combined thread's visibility — same enum and same three values (`AuthenticatedOnly` = 0, `OwnerOnly` = 1, `Public` = 2) already used by `Video.CommentVisibility`. |
| `CreatedDateTime` | `DateTime` | Set at creation (UTC). |
| `Videos` | `ICollection<Video>` | Navigation; the videos currently grouped into this competition. May be empty. |

**Validation rules** (FR-001, FR-004):
- `Name` must not be empty/whitespace (enforced in `CompetitionService`, not a DB constraint).
- Rename follows the same non-empty rule.

**Relationships**:
- One `Competition` has many `Video` (via `Video.CompetitionId`).
- One `Competition` has at most one `SharedLink` pointed at it at a time in practice (not a hard
  constraint — see `SharedLink` below), and many `Comment`s (the combined thread).

**Lifecycle / state transitions**:
- Create → has zero videos.
- Add video (FR-002, FR-003) → video's `CompetitionId` set, only if the requester owns the video
  and the video has no existing `CompetitionId`.
- Remove video (FR-005) → video's `CompetitionId` cleared; video becomes standalone again.
- Delete (FR-006) → every grouped video's `CompetitionId` is cleared (detach), then the
  `Competition` row itself is removed. Videos are never cascade-deleted.

## Video (existing entity, extended)

| Field (new) | Type | Notes |
|---|---|---|
| `CompetitionId` | `Guid?` | `null` = standalone (default for all existing and newly uploaded videos). FK to `Competition`, delete behavior **detach** (set null), never cascade. |
| `Competition` | `Competition?` | Navigation for the FK above. |

`Video.CommentVisibility` is retained and continues to govern a *standalone* video's own thread;
it is not consulted once a video is grouped (the competition's `CommentVisibility` governs the
combined thread instead — see Comment below).

## SharedLink (existing entity, extended)

| Field (new/changed) | Type | Notes |
|---|---|---|
| `VideoId` | `Guid?` (was required) | Now nullable. Exactly one of `VideoId` / `CompetitionId` is set per link. |
| `CompetitionId` | `Guid?` | New. The competition this link targets, if any. |
| `Competition` | `Competition?` | New navigation. |

All other fields (`SharedBy`, `CreatedAt`, `ExpireAt`, `IsRevoked`, `AllowComments`,
`AllowAnonymousComments`) are unchanged and apply identically to a video-targeted or a
competition-targeted link (FR-013).

**Invariant**: exactly one of `VideoId` / `CompetitionId` is non-null per `SharedLink` row.

## Comment (existing entity, extended)

| Field (new/changed) | Type | Notes |
|---|---|---|
| `VideoId` | `Guid?` (was required) | Now nullable. Exactly one of `VideoId` / `CompetitionId` is set per comment. |
| `CompetitionId` | `Guid?` | New. Set when the comment belongs to a competition's combined thread. |
| `Competition` | `Competition?` | New navigation. |

All other fields (`UserId`, `SharedLinkId`, `Content`, `PostedAsAnonymous`, `AnonymousName`,
`ShaOfAnonymousId`, `CreatedAt`, `UpdatedAt`, `IsHidden`, `IsReported`, `ReportedReason`) are
unchanged. When a comment's `CompetitionId` is set, ownership/visibility decisions (who may
hide/unhide/report/edit/delete, and who may see the thread per FR-012) resolve against
`Competition.OwnerUserId` / `Competition.CommentVisibility` instead of the video's; this is a
behavioral switch in the comment service, not a new field.

**Invariant**: exactly one of `VideoId` / `CompetitionId` is non-null per `Comment` row.

## Summary of schema changes (already applied via EF Core migrations)

1. `AddCompetitions` — creates the `Competition` table; adds `Video.CompetitionId` (nullable FK,
   detach-on-delete).
2. `AddCompetitionSharedLinks` — makes `SharedLink.VideoId` nullable; adds `SharedLink.CompetitionId`.
3. `AddCompetitionComments` — makes `Comment.VideoId` nullable; adds `Comment.CompetitionId`.
