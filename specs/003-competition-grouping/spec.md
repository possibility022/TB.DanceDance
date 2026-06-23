# Feature Specification: Competition Grouping & Combined Feedback

**Feature Branch**: `003-competition-grouping`

**Created**: 2026-06-23

**Status**: Draft

**Input**: User description: "convert you track item DD-74 into spec-plan. Don't mention youtrack
anywhere" — resolved to an existing backlog epic for grouping a dancer's private videos from a
single competition so a teacher can give feedback on them as a set.

**Source material**: a backlog epic (scope frozen) describing a new owner-owned grouping over an
owner's own private videos, sharing a whole group via one link, and combining feedback into one
thread per group, plus its breakdown into backend work (grouping entity + persistence, grouping
CRUD with ownership guards, sharing a group via one link, combined comment thread per group),
frontend work (manage groups from My Videos, a group detail view, a shared viewer rendering every
grouped video with one thread), and a local end-to-end verification pass. The epic's own stated
scope excludes mobile support, reusing a video across more than one group, merging a video's prior
individual feedback thread into a group's combined thread, and any converter/event/group(social)
changes — these are deferred follow-ups, not part of this feature's scope.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Group private videos for a single competition (Priority: P1)

A dancer who has uploaded several private videos from the same competition groups those videos
together under a single named competition, so they can be managed and shared as one set instead
of as separate, unrelated videos.

**Why this priority**: Without a way to form the group in the first place, there is nothing to
share or give combined feedback on — this is the foundation the rest of the feature builds on.

**Independent Test**: As the owner of two or more private videos, create a competition, add those
videos to it, and confirm they now appear together as a named group rather than as separate
standalone videos.

**Acceptance Scenarios**:

1. **Given** a user who owns two or more private videos, **When** they create a competition with a
   name and add those videos to it, **Then** the videos are grouped under that competition and no
   longer appear as standalone (ungrouped) videos.
2. **Given** a user attempting to add a video they do not own to a competition, **When** they
   submit the request, **Then** the action is rejected and the video's grouping is unchanged.
3. **Given** a video that is already grouped into one competition, **When** its owner attempts to
   add it to a different competition, **Then** the action is rejected — a video belongs to at most
   one competition at a time.
4. **Given** a competition the user owns, **When** they rename it, **Then** the new name is shown
   wherever the competition appears, without affecting which videos are grouped into it.
5. **Given** a competition with grouped videos, **When** the owner removes one specific video from
   it, **Then** that video becomes a standalone video again and the rest of the competition's
   videos are unaffected.
6. **Given** a competition the owner no longer wants, **When** they delete it, **Then** its videos
   become standalone videos again (none of the underlying videos are deleted).

---

### User Story 2 - Share a competition and collect one combined thread of feedback (Priority: P1)

The owner shares an entire competition with a teacher (or other reviewer) using a single link.
Opening that link, the teacher sees every video in the competition on one page and leaves feedback
in one shared comment thread that covers the competition as a whole, rather than having to repeat
or split feedback across a separate thread per video.

**Why this priority**: This combined-feedback experience is the entire point of grouping videos
into a competition — it is the value the feature exists to deliver, and is independently
verifiable once User Story 1 exists.

**Independent Test**: As the owner of a competition with two or more videos, create one share link
for the competition, open that link as the recipient, confirm every video in the competition is
visible and playable, post one comment, and confirm the owner sees that single comment associated
with the competition rather than with any one video.

**Acceptance Scenarios**:

1. **Given** a competition with two or more videos, **When** the owner creates a share link for
   the competition, **Then** a single link is produced that, when opened, shows every video
   currently grouped into that competition.
2. **Given** a teacher who opens a competition's share link, **When** they play any of the listed
   videos, **Then** that video plays using the same link without needing a separate link per video.
3. **Given** a teacher viewing a shared competition, **When** they submit feedback, **Then** their
   feedback is added to a single combined thread for the whole competition, not to a thread for
   any individual video.
4. **Given** an owner whose competition link permits comments, **When** a teacher (authenticated
   or anonymous, depending on the link's settings) posts feedback, **Then** the owner sees that
   feedback in the competition's combined thread.
5. **Given** a competition share link configured with a particular comment-visibility setting
   (e.g., visible only to the owner, to any authenticated visitor, or to everyone), **When**
   different visitors open the link, **Then** the combined thread's visibility behaves the same
   way that setting already behaves for a single shared video.
6. **Given** a standalone (ungrouped) video's existing share link, **When** it is opened, **Then**
   that video's own individual feedback thread continues to work exactly as before — sharing and
   combined feedback only apply to videos grouped into a competition.

---

### User Story 3 - Manage a competition's membership and lifecycle over time (Priority: P2)

After creating a competition and sharing it, the owner continues to manage it as their situation
changes — adding a newly uploaded video that belongs to the same competition, removing a video
that was grouped by mistake, or deleting the whole competition once it is no longer needed.

**Why this priority**: Competitions are not necessarily finalized at creation time, but the
feature is already useful without this ongoing management — it refines User Story 1's capability
rather than introducing new core value.

**Independent Test**: With an existing competition and share link already in place, add a newly
uploaded video to the competition and confirm it appears in the same share link without creating a
new link; remove a video from the competition and confirm it drops out of that same share link.

**Acceptance Scenarios**:

1. **Given** an existing competition that has already been shared, **When** the owner adds another
   of their own private videos to it, **Then** that video becomes visible through the
   competition's existing share link without the owner needing to create a new link.
2. **Given** a competition the owner manages from their video list, **When** they view their
   videos, **Then** they can tell which videos are grouped into a competition (and which one) and
   which remain standalone.
3. **Given** a competition that still has an active share link, **When** the owner deletes the
   competition, **Then** the share link no longer shows a competition (its videos, now standalone,
   keep their own individual sharing going forward).

### Edge Cases

- What happens when a video that already has its own individual feedback comments is added to a
  competition? Going forward, feedback for that video is collected at the competition level; the
  video's prior individual thread is not merged into the competition's combined thread (carrying
  old per-video feedback into a competition thread is explicitly out of scope for this feature).
- What happens when a video is removed from a competition or the competition is deleted? The video
  becomes a standalone video again and is no longer covered by the competition's combined thread;
  it does not retroactively regain whatever it accumulated while grouped.
- What happens when an owner tries to add a video that is already grouped into a different
  competition? The action is rejected; a video may belong to at most one competition.
- What happens when someone other than the competition's owner tries to rename, delete, add a
  video to, or remove a video from a competition? The action is rejected and the competition is
  unchanged.
- What happens when a competition has no videos in it yet? It can still be created and (if
  shared) opened via its link, simply showing no videos until at least one is added.
- Does this feature affect the mobile app? No — grouping, sharing, and combined feedback for
  competitions are web-only in this feature; mobile support is explicitly deferred.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow a user to create a named competition that they own.
- **FR-002**: System MUST allow a competition's owner to add their own private videos to it, and
  MUST reject adding a video that the requesting user does not own.
- **FR-003**: System MUST allow a video to be grouped into at most one competition at a time, and
  MUST reject adding a video to a competition while it is already grouped into a different one.
- **FR-004**: System MUST allow a competition's owner to rename it.
- **FR-005**: System MUST allow a competition's owner to remove a specific video from it, leaving
  that video as a standalone video without affecting the competition's other videos.
- **FR-006**: System MUST allow a competition's owner to delete the competition, and upon deletion
  MUST leave its videos intact as standalone videos rather than deleting them.
- **FR-007**: System MUST reject any attempt to create, rename, delete, or change the membership of
  a competition by a user other than its owner.
- **FR-008**: System MUST allow a competition's owner to view a list of their own competitions and
  the videos currently grouped into each one.
- **FR-009**: System MUST allow a competition's owner to produce a single share link for the whole
  competition, distinct from the existing per-video sharing already available for standalone
  videos.
- **FR-010**: System MUST, when a competition's share link is opened, show every video currently
  grouped into that competition and allow each one to be played using that same link.
- **FR-011**: System MUST collect feedback submitted through a competition's share link into one
  combined comment thread for the whole competition, rather than a separate thread per video.
- **FR-012**: System MUST apply the same comment-visibility behavior (who can see the thread: owner
  only, any authenticated visitor, or everyone) to a competition's combined thread that already
  applies to an individual shared video's thread.
- **FR-013**: System MUST apply the same allow-comments and allow-anonymous-comments controls to a
  competition's share link that already apply to an individual video's share link.
- **FR-014**: System MUST continue to support a standalone (ungrouped) video's existing individual
  share link and individual feedback thread unchanged.
- **FR-015**: System MUST NOT expose competition creation, management, sharing, or combined
  feedback in the mobile application in this feature (mobile support is a separate, deferred
  follow-up).

*Explicitly out of scope (per source requirements, not deferred for lack of a default):*

- Grouping a single video into more than one competition at the same time.
- Carrying a video's prior individual feedback thread into a competition's combined thread when
  the video is added.
- Any change to converter, event, or (social) group functionality as part of this feature.
- Mobile (MAUI) support for competitions.

### Key Entities

- **Competition**: An owner-owned named group of that owner's own private videos (e.g., "Regional
  Finals 2026"), with an optional date and/or location for display, and a comment-visibility
  setting controlling who may see its combined feedback thread. A competition may contain any
  number of videos, including zero.
- **Video (existing entity, extended)**: Gains an association to at most one competition. A video
  not associated with any competition remains a standalone video with its own existing individual
  sharing and feedback behavior.
- **Shared Link (existing entity, extended)**: A link that targets either one standalone video or
  one whole competition (never both), carrying the same expiration and comment-permission settings
  either way.
- **Comment Thread (existing concept, extended)**: A thread of feedback that belongs either to one
  standalone video or to one whole competition; a competition's thread is shared across every
  video grouped into that competition.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An owner can group two or more of their own private videos into a single named
  competition and confirm they are grouped, without needing to repeat any per-video setup steps.
- **SC-002**: An owner can produce exactly one share link for a competition and have that one link
  give a recipient access to every video currently in the competition.
- **SC-003**: A teacher reviewing a competition through its share link can give feedback covering
  the whole set of videos by writing into a single thread, instead of writing separate feedback
  once per video.
- **SC-004**: 100% of attempts to modify a competition (rename, delete, add video, remove video) by
  someone other than its owner are rejected; 0% succeed.
- **SC-005**: 100% of attempts to add a video to a competition while it is already grouped into a
  different competition are rejected; 0% succeed.
- **SC-006**: Deleting a competition never deletes any of the videos that were grouped into it —
  100% of those videos remain accessible to the owner as standalone videos afterward.

## Assumptions

- "Private videos" means videos the requesting user owns, consistent with how ownership already
  works for individual video sharing elsewhere in the product; competitions only ever group an
  owner's own videos.
- Reusing the existing comment-visibility setting (owner-only / authenticated / public) and the
  existing allow-comments / allow-anonymous-comments controls for a competition's combined thread,
  rather than introducing new visibility options, per the source material's stated intent to reuse
  these settings.
- A competition's optional date and/or location are display-only details and do not affect access
  control, sharing, or feedback behavior.
- "Web only for v1" means this feature's scope is the web application only; the mobile app is
  unaffected by this feature and gains no competition UI until a separate follow-up feature.
- Reusing a single video across more than one competition, merging a video's prior individual
  feedback thread into a competition thread, and any converter/event/(social) group changes are
  deliberately excluded from this feature's scope (tracked separately as potential future work),
  not deferred for lack of a default.
