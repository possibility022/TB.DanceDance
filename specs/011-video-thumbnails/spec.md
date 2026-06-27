# Feature Specification: Automatic Video Thumbnails

**Feature Branch**: `011-video-thumbnails`

**Created**: 2026-06-23

**Status**: Implemented (retroactively documented)

**Input**: User description: "Use the issue tracker and existing code to document features we
have now" — scoped to automatic thumbnail generation and its display on the web app (already
shipped on `master`; mobile display and storage-link delivery are covered by separate, related
specs).

**Source material**: a tracked epic for generating a preview image per video, automatically
extracted from the source video, shown on web video cards and the video detail/player page, with
a single backfill mechanism that covers both newly uploaded videos and videos that existed
before the feature shipped; cross-checked against
`src/backend/TB.DanceDance.Services.Converter.Deamon/Deamon.cs`,
`src/backend/TB.DanceDance.Services.Converter.Deamon/FFmpegClient/FFmpegClientConverter.cs`,
`src/backend/Application/Domain/Entities/Video.cs`,
`src/my-dance.web/src/app/shared/ui/video-card/video-card.ts`, and
`src/my-dance.web/src/app/features/videos/video-player.ts` in the current codebase.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A preview image is generated automatically for every video (Priority: P1)

Once a video has been uploaded and converted, a preview image is automatically extracted from
the video itself — no one has to upload, pick, or crop an image by hand.

**Why this priority**: Nothing else in this feature has anything to display without a generated
image existing first; this is the foundation the rest builds on.

**Independent Test**: Upload a new video and, after conversion finishes, confirm a preview image
now exists for that video without any manual step to create one.

**Acceptance Scenarios**:

1. **Given** a video has finished converting, **When** the next thumbnail-generation pass runs,
   **Then** a single preview image is extracted from that video and associated with it.
2. **Given** a video whose source is shorter than the extraction point used to pick a frame,
   **When** a preview image is generated for it, **Then** a usable image is still produced rather
   than the process failing outright.

---

### User Story 2 - Videos that existed before this feature also get a preview image (Priority: P1)

A video that was already in the system before automatic thumbnail generation existed gets a
preview image through the same mechanism used for new videos, with no separate one-off job and
no difference in how it eventually appears once generated.

**Why this priority**: Without covering pre-existing videos, every video uploaded before this
feature shipped would be permanently missing a preview, which is just as important as covering
new uploads going forward — hence the same priority as User Story 1.

**Independent Test**: Identify a video that existed before this feature shipped and had no
preview image; confirm that, after the generation process has had a chance to run, it now has
one, generated the same way as a newly uploaded video's.

**Acceptance Scenarios**:

1. **Given** a video that existed before automatic thumbnail generation and currently has no
   preview image, **When** the generation process next considers it, **Then** a preview image is
   generated and associated with it, indistinguishable in kind from one generated for a new
   upload.
2. **Given** a video that already has a preview image, **When** the generation process runs,
   **Then** that video is not selected again and its existing preview image is left unchanged.

---

### User Story 3 - See a video's preview image on listing cards (Priority: P1)

A user browsing a list of videos (their own, a group's, or an event's) sees each video's preview
image on its card, so they can recognize a video without opening it.

**Why this priority**: This is the most frequent place a preview image is seen and the primary
payoff of generating one at all.

**Independent Test**: Open any video listing and confirm that a video with a generated preview
shows that image on its card.

**Acceptance Scenarios**:

1. **Given** a video with a generated preview image, **When** its card appears in any listing,
   **Then** the preview image is shown on that card.
2. **Given** a video with no preview image yet (e.g., generation has not run for it), **When**
   its card appears in a listing, **Then** a placeholder treatment is shown instead, with no
   broken image.

---

### User Story 4 - See a video's preview image before playback starts (Priority: P2)

A user opening a single video's detail/player page sees that video's preview image as the
player's poster — the image shown before the user presses play — instead of a blank player area.

**Why this priority**: This refines the experience on the detail page specifically; the listing
cards (User Story 3) already deliver most of the feature's value, so the player poster is a
secondary, slightly lower-priority placement of the same underlying image.

**Independent Test**: Open the detail/player page of a video that has a generated preview image,
before pressing play, and confirm that image is shown in the player area.

**Acceptance Scenarios**:

1. **Given** a video with a generated preview image, **When** its detail/player page loads,
   **Then** that image is shown in the player area before playback starts.
2. **Given** a video with no preview image, **When** its detail/player page loads, **Then** the
   player area shows its normal empty/loading state rather than a broken image.

### Edge Cases

- What happens if generating a preview image for a particular video fails (e.g., a corrupted
  source file)? That video is left without a preview image rather than blocking the generation
  process from moving on to other videos.
- What happens to a video's preview image if the video itself is later re-converted or replaced?
  This feature does not define regeneration behavior beyond initial generation; it is covered by
  whatever video-replacement behavior already exists elsewhere in the system.
- What happens when many videos are missing a preview image at once (e.g., right after this
  feature first shipped)? They are worked through one at a time by the same ongoing process that
  also generates previews for new uploads, rather than all at once.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST automatically generate a single preview image for a video by extracting
  a frame from that video's own content, with no manual upload or selection step.
- **FR-002**: System MUST generate a preview image for a video that existed before this feature
  shipped using the same mechanism used for newly converted videos, requiring no separate one-off
  job.
- **FR-003**: System MUST NOT regenerate or duplicate a preview image for a video that already
  has one.
- **FR-004**: System MUST still produce a usable preview image for a video whose source is too
  short to contain the normally-used extraction point.
- **FR-005**: System MUST display a video's preview image on its card wherever that video appears
  in a listing (e.g., own videos, group videos, event videos).
- **FR-006**: System MUST show a placeholder treatment, not a broken image, for a video's card
  when no preview image exists for it yet.
- **FR-007**: System MUST display a video's preview image as the player's poster on that video's
  detail/player page before playback starts.
- **FR-008**: System MUST leave a video usable for playback and listing display even if preview
  generation has not yet run or has failed for it.

*Explicitly out of scope (per source requirements, not deferred for lack of a default):*

- Letting a video's owner upload a custom preview image or pick among multiple candidate frames.
- Generating more than one preview image, or more than one size/resolution, per video.
- Mobile display of preview images (covered by a separate, dependent feature).
- How the preview image is delivered to clients over the network (covered by a separate feature
  concerned with link delivery and caching).

### Key Entities

- **Video Preview Image**: A single image extracted automatically from a video's own content,
  associated one-to-one with that video. Exists only after the generation process has processed
  that video; absence of one is a normal, expected state handled gracefully everywhere it would
  otherwise be shown.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of videos that finish conversion eventually get a preview image generated for
  them, with no manual step required.
- **SC-002**: 100% of videos that existed before this feature shipped eventually get a preview
  image generated for them through the same ongoing process, with 0% requiring a separate manual
  job.
- **SC-003**: A user can visually distinguish videos in a listing by their preview images without
  opening any of them, for every video that has a generated image.
- **SC-004**: 0% of video cards or detail/player pages show a broken image as a result of a
  missing preview image; a placeholder or normal empty state is shown instead in every case.

## Assumptions

- The exact frame chosen for extraction and the exact image size are implementation choices made
  during delivery (a fixed seek point into the video, scaled to a fixed width with aspect ratio
  preserved) rather than fixed requirements of this document; what matters functionally is that
  exactly one representative image is produced per video.
- "The same mechanism" for new and pre-existing videos means a single, ongoing background process
  that continually looks for videos missing a preview image, regardless of when the video itself
  was uploaded — not two separate code paths.
- How the generated preview image is actually delivered to web and mobile clients (direct
  storage links, caching behavior) is governed by separate, related features that depend on this
  one; this feature is only responsible for the image existing and appearing on web cards and the
  player poster.
- Mobile display of preview images is explicitly out of scope for this feature and is delivered
  by a separate, dependent feature.
