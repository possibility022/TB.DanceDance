# Research: Competition Grouping & Combined Feedback

**Input**: `specs/003-competition-grouping/spec.md`

This feature's source material already included concrete technical proposals (entity shape, route
layout, layering), so research here resolves how those proposals map onto this repository's
existing conventions rather than evaluating unrelated alternatives.

## 1. Where the grouping concept lives in the domain model

- **Decision**: Add a new `Competition` aggregate (`Domain/Entities/Competition.cs`) rather than
  reusing the existing (social) `Group` or `Event` entities.
- **Rationale**: `Group` and `Event` model multi-user membership/administration (see
  `CLAUDE.md`'s description of group administrators/members); a competition is a single owner's
  private grouping with no membership concept of its own. Reusing `Group`/`Event` would either
  drag in irrelevant admin/membership semantics or require weakening them, both of which the
  source material explicitly ruled out ("converter/event/group changes" are out of scope).
- **Alternatives considered**: Tagging videos with a free-text "competition name" string instead
  of a real entity — rejected because it cannot carry its own `CommentVisibility`, cannot be the
  target of a `SharedLink`, and cannot be renamed/deleted as one thing.

## 2. How a video joins exactly one competition

- **Decision**: A single nullable `CompetitionId` (Guid?) foreign key + `Competition?` navigation
  on `Video`, defaulting to `null` (standalone). Enforced at the application layer in
  `CompetitionService.AddVideoAsync`, not via a database constraint.
- **Rationale**: A nullable FK is the natural, smallest-diff representation of "at most one"; a
  join table would be needed only if a video could belong to many competitions, which is
  explicitly excluded. Enforcing "not already in another competition" in the service (returning a
  domain-level rejection) keeps the rule visible and testable without a database trigger.
- **Alternatives considered**: A many-to-many join table (`CompetitionVideo`) — rejected as
  unjustified complexity (Constitution Principle III) for a relationship that is 0-or-1 by design.

## 3. How sharing extends to a multi-video target

- **Decision**: Extend the existing `SharedLink` entity with a nullable `CompetitionId` alongside
  the existing nullable `VideoId`, with exactly one of the two set per link, instead of creating a
  parallel `CompetitionSharedLink` entity.
- **Rationale**: Every downstream concern a link already has — expiration, revocation,
  `AllowComments`/`AllowAnonymousComments`, the `{linkId}` URL shape used by `/shared/:linkId` —
  applies identically whether the target is one video or a whole competition. Duplicating the
  entity would duplicate all of that and the endpoints that resolve a link.
- **Alternatives considered**: A separate sharing entity/table for competitions — rejected; it
  would have forced either two divergent `/shared/:linkId` resolution paths or an early
  unification anyway, with no benefit.

## 4. How combined feedback is represented

- **Decision**: Extend `Comment` with a nullable `CompetitionId` alongside the existing nullable
  `VideoId` (exactly one set), and key the comment-create/list flow off whichever the link target
  resolves to, sourcing owner identity and `CommentVisibility` from the `Competition` instead of
  the `Video` when the link targets a competition.
- **Rationale**: This reuses the entire existing comment pipeline (create/list/hide/unhide/report,
  the three `CommentVisibility` levels, anonymous authorship via `ShaOfAnonymousId`) for "one
  thread covering several videos" by changing only *what the thread is keyed on*, not how a thread
  behaves. This is what makes the combined thread "feel" identical to an existing single-video
  thread to both the teacher and the owner.
- **Alternatives considered**: Posting one comment per video automatically duplicated across all
  of a competition's videos — rejected; that is N threads with synced content, not one thread, and
  does not satisfy "writes feedback into one combined comment thread" (spec User Story 2).

## 5. Whether to migrate a video's prior individual comments when it joins a competition

- **Decision**: Do not migrate or merge a video's existing per-video comments into the
  competition's combined thread when the video is added.
- **Rationale**: The source material explicitly lists "merging existing per-video threads into a
  competition thread" as out of scope for v1. Implementing a migration would be speculative work
  beyond the stated scope (Constitution Principle III).
- **Alternatives considered**: Automatic migration on add — rejected per explicit scope exclusion.

## 6. Mobile scope

- **Decision**: No MAUI changes; competitions are web-only (`src/my-dance.web`) in this feature.
- **Rationale**: Explicit in the source material ("Web only for v1"); the mobile app's `Contracts`
  dependency is unaffected since no mobile UI consumes the new endpoints.
- **Alternatives considered**: None — explicitly out of scope, not a genuine alternative to weigh.

## Outcome

No `NEEDS CLARIFICATION` markers remain. All decisions above are already reflected in the shipped
implementation (entities, migrations, endpoints, and the Angular feature module); this research
records the rationale for that shape rather than proposing a different one.
