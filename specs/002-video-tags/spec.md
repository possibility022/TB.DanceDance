# Feature Specification: Video Tags

**Feature Branch**: `002-video-tags`

**Created**: 2026-06-23

**Status**: Draft

**Input**: User description: "take another item that is not specified yet" — resolved to an
existing backlog epic for video tagging, not yet implemented and not yet covered by a spec.

**Source material**: a backlog epic for video tagging (scope frozen) and its breakdown into
backend work (tag/video-tag data model + migration, seed curated suggestions, tag service with
normalization + authorization, REST endpoints, suggestions endpoint, single-tag listing filter),
contracts (DTOs), frontend work (tag chip component + detail display, tag editor restricted to
owner/admin, suggestions/autocomplete in the editor, chips on listing cards, click-to-filter),
integration tests, and a feature flag + rollout note. The epic's own suggested build order
excludes mobile parity and two explicitly-future items (multi-tag filtering; admin tooling to
rename/merge/hide tags) — these three are deferred follow-ups, not part of this feature's scope.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Tag a video (Priority: P1)

A video's owner, or an administrator of the group or event the video belongs to, attaches one
or more free-form text tags to the video so it can later be found and categorized by topic
(e.g., "salsa", "footwork", "competition-2026").

**Why this priority**: Tagging is the foundational capability — chips, listing display, and
filtering all depend on tags existing on at least some videos. Without this, nothing else in the
epic has data to show.

**Independent Test**: As a video's owner, open the video's detail page, add a tag, and confirm
it is saved and visible on that video going forward (including after a page reload).

**Acceptance Scenarios**:

1. **Given** a video's owner viewing the video detail page, **When** they add a new tag with
   valid text, **Then** the tag is saved and immediately appears attached to the video.
2. **Given** an administrator of the group (or event) a video belongs to, but who does not own
   the video, **When** they add or remove a tag on that video, **Then** the action succeeds the
   same way it would for the owner.
3. **Given** a user who neither owns the video nor administers its group/event, **When** they
   attempt to add or remove a tag on that video, **Then** the action is rejected and the video's
   tags are unchanged.
4. **Given** a video that already has 20 tags attached, **When** its owner or an admin attempts
   to add a 21st tag, **Then** the action is rejected and the video keeps its existing 20 tags.
5. **Given** a video's owner, **When** they attempt to add a tag that is empty, whitespace-only,
   or longer than 30 characters, **Then** the action is rejected and no tag is added.
6. **Given** a video that already has a tag "Salsa" attached, **When** its owner adds "salsa" (
   different casing) or "  salsa  " (surrounding whitespace), **Then** no duplicate tag is
   created — the existing tag is recognized as the same one.

---

### User Story 2 - See tags on a video (Priority: P1)

Anyone viewing a video's detail page or a listing of videos (e.g., My Videos, a group's videos)
sees that video's tags rendered as chips, so they can tell at a glance what a video is about.

**Why this priority**: Tagging has no visible value to most users until tags are actually shown
somewhere; this is the other half of the MVP alongside User Story 1.

**Independent Test**: Open the detail page of a tagged video and confirm its tags render as
chips; open a listing containing that same video and confirm the same tags render on its card.

**Acceptance Scenarios**:

1. **Given** a video with one or more tags, **When** a user opens that video's detail page,
   **Then** every tag is displayed as a chip in the video's display casing (not necessarily the
   casing first used to create it, but a consistent display form).
2. **Given** a video with one or more tags, **When** a user views a listing that includes that
   video's card, **Then** the same tags are displayed as chips on the card.
3. **Given** a video with no tags, **When** a user views its detail page or card, **Then** no tag
   chips are shown and no error occurs.

---

### User Story 3 - Filter a listing by clicking a tag (Priority: P2)

A user viewing a listing of videos clicks a tag chip on any video card and the listing narrows
to only videos that carry that exact tag.

**Why this priority**: This is the main payoff of tagging beyond visual labeling, but it is only
useful once tags exist and are visible (User Stories 1 and 2), so it follows them.

**Independent Test**: In a listing containing both tagged and untagged videos, click a tag chip
on one video's card and confirm the listing now shows only videos carrying that tag; clear the
filter and confirm the full listing returns.

**Acceptance Scenarios**:

1. **Given** a listing showing videos with mixed tags, **When** a user clicks a tag chip on one
   video's card, **Then** the listing updates to show only videos that have that exact tag.
2. **Given** a listing currently filtered by a tag, **When** the user clears the filter, **Then**
   the listing returns to showing all videos it would normally show.
3. **Given** a listing filtered by a tag with no matching videos (e.g., the last tagged video
   was just untagged), **When** the filter is applied, **Then** the listing shows an empty result
   rather than an error.

---

### User Story 4 - Get tag suggestions while tagging (Priority: P2)

While adding a tag to a video, the person tagging it sees suggestions drawn from a curated set
of tags and from tags already popular across the system, so they can reuse existing wording
instead of guessing or introducing near-duplicate tags.

**Why this priority**: Improves the quality and consistency of tagging (fewer near-duplicate
tags like "salsa" / "Salsa " / "salsa dance"), but tagging is functional without it — it's a
quality-of-life layer on top of User Story 1.

**Independent Test**: Start typing a tag that partially matches a curated or already-popular tag
and confirm it appears in the suggestion list; select it and confirm it is added without having
to type the full value.

**Acceptance Scenarios**:

1. **Given** a user adding a tag, **When** they type a few characters matching a curated or
   popular existing tag, **Then** that tag appears in a suggestion list they can pick from.
2. **Given** a user adding a tag, **When** they select a suggested tag rather than typing it out
   fully, **Then** the selected tag is added to the video exactly as suggested.
3. **Given** a user typing a tag with no matching curated or popular tag, **When** they finish
   typing, **Then** they can still submit it as a new free-form tag (suggestions are optional,
   not a restriction on allowed values).

### Edge Cases

- What happens when a video's last tag is removed entirely from the system (no video carries it
  anymore)? The underlying tag record is kept (not deleted) so its popularity history and
  suggestion ranking are preserved; only the association to that video is removed.
- What happens when a tag is added to a video that belongs to an event rather than a group? The
  same owner-or-admin rule applies, using the event's administrators in place of group
  administrators.
- What happens when the same tag (after normalization) is suggested and also already attached to
  a video? It is not shown as a suggestion for that video, or is visually marked as already
  applied, so the user does not try to add a duplicate.
- What happens when tagging is attempted on a video while the tagging feature is disabled (flag
  off)? Tag editing, suggestions, chip display, and click-to-filter are all unavailable, and any
  existing tag data already stored is neither shown nor lost.
- How does the system behave on the mobile app? Tags are out of scope for the mobile app in this
  feature; mobile parity is explicitly deferred to a follow-up.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow a video's owner to attach a free-form text tag to that video.
- **FR-002**: System MUST allow an administrator of the group or event a video belongs to attach
  or remove tags on that video, with the same effect as if the owner had done so.
- **FR-003**: System MUST reject any attempt to add or remove a tag on a video by a user who is
  neither that video's owner nor an administrator of its group/event.
- **FR-004**: System MUST reject a tag value that is empty, whitespace-only, or longer than 30
  characters.
- **FR-005**: System MUST normalize tags for matching (case-insensitive, trimmed of surrounding
  whitespace) so that adding a tag that already exists on a video (after normalization) does not
  create a duplicate, while preserving a display casing for rendering.
- **FR-006**: System MUST reject adding a tag to a video that already has 20 tags attached.
- **FR-007**: System MUST allow a video's owner or an authorized administrator to remove a tag
  from a video.
- **FR-008**: System MUST retain a tag's underlying record after it is removed from the last
  video that carried it (no cascading deletion of the tag itself).
- **FR-009**: System MUST display a video's tags as chips on that video's detail page.
- **FR-010**: System MUST display a video's tags as chips on that video's card wherever it
  appears in a listing (e.g., My Videos, group videos, event videos).
- **FR-011**: System MUST allow a user viewing a listing to click a tag chip on a video card and
  have the listing filtered to show only videos carrying that exact tag.
- **FR-012**: System MUST allow the user to clear an active tag filter and return to the
  listing's normal (unfiltered) results.
- **FR-013**: System MUST offer tag suggestions, drawn from a curated set and from tags already
  in use across the system, while a user is entering a tag.
- **FR-014**: System MUST allow a user to submit a tag that does not match any suggestion, as
  long as it satisfies the validation rules in FR-004 and FR-006.
- **FR-015**: System MUST be controllable by a feature flag such that, when disabled, no tag
  editing, suggestions, chip display, or tag-based filtering is available to users, without
  deleting any previously stored tag data.
- **FR-016**: System MUST NOT expose tag viewing or editing in the mobile application in this
  feature (mobile parity is a separate, deferred follow-up).

*Explicitly out of scope (per source requirements, not deferred for lack of a default):*

- Filtering a listing by more than one tag at a time (AND/OR multi-tag filtering) or a dedicated
  tag-explorer page.
- Per-user private tags or bookmarks.
- Profanity filtering or other moderation tooling for tag text.
- Administrative tooling to rename, merge, or hide tags.
- Any mobile (MAUI) UI for viewing or editing tags.

### Key Entities

- **Tag**: A normalized, reusable text label (e.g., "salsa"). Has a canonical display casing and
  a popularity signal derived from how many videos currently carry it. Persists even when no
  video currently carries it.
- **Video Tag**: An association between a Tag and a Video, representing that the tag is
  currently attached to that video. A video may have at most 20 active tags.
- **Tag Suggestion**: A curated, administrator-provided tag intended to appear in suggestions
  even before it has been used on any video, alongside tags that are already popular from actual
  usage.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A video owner or authorized administrator can attach a tag to a video and see it
  rendered as a chip on that video's detail page in the same session, with no page reload
  required to see their own change.
- **SC-002**: 100% of tag-edit attempts by users who are neither the video's owner nor an
  administrator of its group/event are rejected; 0% succeed.
- **SC-003**: A user can narrow a video listing to a single topic by clicking one tag chip,
  without navigating to a separate search or filter page.
- **SC-004**: At least one relevant suggestion appears for a user typing a tag that matches an
  existing curated or popular tag, reducing the chance of near-duplicate tag text for the same
  concept.
- **SC-005**: Disabling the feature flag removes all tagging UI and filtering from view without
  any loss of previously stored tag data, verified by re-enabling the flag and confirming prior
  tags reappear unchanged.

## Assumptions

- Event videos follow the same authorization rule as group videos: the video's owner or any
  administrator of the event it belongs to may edit its tags (resolved using the source
  material's own stated proposal for this open question).
- Maximum tags per video is 20, and maximum tag length is 30 characters, normalized
  case-insensitively and trimmed for matching while preserving display casing — both per the
  source material's stated proposals.
- Removing the last association between a tag and any video does not delete the tag record
  itself; it is kept for popularity history and future suggestions, per the source material's
  stated proposal.
- "Web v1; mobile parity in a follow-up" means this feature's scope is the Angular web app only;
  the mobile (MAUI) app is unaffected by this feature and gains no tag UI until a separate
  follow-up feature.
- Multi-tag filtering, a dedicated tag-explorer page, and tag rename/merge/hide administrative
  tooling are deliberately excluded from this feature's scope (tracked separately as future
  work), not deferred for lack of a default.
- The feature flag mentioned in the source breakdown gates the entire user-facing feature (editing,
  display, suggestions, filtering) as a single on/off switch, consistent with how rollout flags
  are typically used elsewhere in this codebase.
