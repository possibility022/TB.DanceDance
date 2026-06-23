# Feature Specification: Honest Content-Type for Streamed Video and Thumbnail Blobs

**Feature Branch**: `008-thumbnail-content-type`

**Created**: 2026-06-23

**Status**: Partially Implemented

**Input**: User description: "Use the issue tracker and existing code to document features we
have now" — scoped to making video and thumbnail blobs carry their true content type end-to-end.

**Source material**: a tracked work item describing the problem (converted video and thumbnail
blobs upload with no explicit content type, so they default to a generic binary type; the
streaming endpoint also hardcodes a video type rather than serving the blob's real one) and its
breakdown into three steps: set the real content type on upload, serve the real content type on
read, and backfill blobs that were already uploaded before this fix; cross-checked against
`src/backend/TB.DanceDance.Services.Converter.Deamon/DanceDanceApiClient.cs` and
`src/backend/Application/Features/Videos/VideoService.cs` in the current codebase.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A newly converted video and its thumbnail report their real type (Priority: P1)

When the conversion pipeline finishes processing a video, both the converted video file and its
generated thumbnail are uploaded to storage carrying their actual content type, instead of a
generic "unknown binary data" type.

**Why this priority**: This is the root cause — every other behavior in this feature (correct
playback hints, correct image rendering when fetched directly from storage) depends on the bytes
being labeled honestly the moment they're written.

**Independent Test**: Convert a video end-to-end, then inspect the stored properties of the
resulting video blob and thumbnail blob directly in storage and confirm each reports its real
type (a web video type for the video, a JPEG type for the thumbnail), not a generic binary type.

**Acceptance Scenarios**:

1. **Given** the conversion pipeline has just finished processing a video, **When** the converted
   video file is uploaded to storage, **Then** the stored blob's content type is the real video
   type, not a generic binary type.
2. **Given** the conversion pipeline has just generated a thumbnail for a video, **When** the
   thumbnail is uploaded to storage, **Then** the stored blob's content type is the real image
   type, not a generic binary type.

---

### User Story 2 - Streaming a video reports the type that was actually stored (Priority: P1)

When a user's player requests a video, the response tells the player the video's actual stored
type rather than always claiming the same fixed type regardless of what was really uploaded.

**Why this priority**: Any consumer that trusts the declared type over sniffing the bytes
(unlike the mobile app's player today) depends on this being correct; it's the read-side
counterpart to User Story 1 and equally foundational.

**Independent Test**: Stream a video whose blob has a real stored content type and confirm the
response's declared type matches what's actually stored, not a hardcoded constant.

**Acceptance Scenarios**:

1. **Given** a video blob with a real, specific content type already stored on it, **When** a
   user streams that video, **Then** the response declares that same specific type.
2. **Given** a video blob that, for any reason, has no usable stored content type, **When** a
   user streams that video, **Then** the response still declares a reasonable, working video
   type rather than failing or declaring a generic binary type.

---

### User Story 3 - Older content gets the correct type without being re-uploaded (Priority: P2)

Video and thumbnail blobs that were uploaded before this fix existed — and therefore still carry
a generic binary type — get their stored content type corrected in place, so that fetching them
directly (not through a type-correcting endpoint) also reports the right type.

**Why this priority**: Without this, every blob uploaded before the fix remains mislabeled
forever; this matters especially for thumbnails, which are fetched directly from storage rather
than through an endpoint that can paper over a missing type at read time. It's lower priority
than User Stories 1 and 2 only because it is a one-time cleanup rather than something that
affects every new upload going forward.

**Independent Test**: Identify a video or thumbnail blob uploaded before this fix shipped (still
showing a generic binary type), run the backfill, and confirm that same blob now reports its real
type when inspected directly in storage — with no change to the bytes themselves.

**Acceptance Scenarios**:

1. **Given** a video blob uploaded before this fix that currently has a generic binary content
   type, **When** the backfill runs, **Then** that blob's stored content type is corrected to the
   real video type, and its contents are unchanged.
2. **Given** a thumbnail blob uploaded before this fix that currently has a generic binary
   content type, **When** the backfill runs, **Then** that blob's stored content type is
   corrected to the real image type, and its contents are unchanged.
3. **Given** a video or thumbnail blob that already has a correct, specific content type, **When**
   the backfill runs (including being run more than once), **Then** that blob is left unchanged.

### Edge Cases

- What happens to a blob's actual bytes during the backfill? They are never modified — only the
  stored content-type property changes.
- What happens if the backfill is interrupted partway through and re-run later? Blobs already
  corrected are left alone; only blobs still showing a generic binary type are updated, so the
  backfill is safe to resume or re-run.
- What happens to a consumer that already worked around the mislabeling by sniffing the
  container instead of trusting the declared type? It continues to work unaffected — this
  feature only makes the declared type honest, it does not change the underlying bytes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST set an explicit, accurate content type on a converted video blob at
  upload time, reflecting the real format produced by the conversion pipeline.
- **FR-002**: System MUST set an explicit, accurate content type on a generated thumbnail blob at
  upload time, reflecting the real image format produced.
- **FR-003**: System MUST serve a streamed video using the content type actually stored on its
  blob, rather than a value that is the same regardless of what was uploaded.
- **FR-004**: System MUST fall back to a reasonable, working video content type when streaming a
  video blob whose stored content type is missing or generic, so playback is never broken by an
  absent type.
- **FR-005**: System MUST provide a one-time backfill that corrects the stored content type of
  every existing video and thumbnail blob that currently has a missing or generic content type,
  setting it to the real type for that blob.
- **FR-006**: The backfill MUST NOT alter the underlying bytes of any blob — only the stored
  content-type property changes.
- **FR-007**: The backfill MUST be safe to run more than once, leaving already-correct blobs
  unchanged and only updating blobs still showing a missing or generic content type.

*Explicitly out of scope (per source requirements, not deferred for lack of a default):*

- Changing the conversion output format itself (e.g., switching the video codec/container) —
  that is a separate decision unrelated to honesty of the declared type.

### Key Entities

- **Video Blob**: The converted video file stored in the videos container; carries a stored
  content-type property that should reflect its real format.
- **Thumbnail Blob**: The generated preview image stored in the thumbnails container; carries a
  stored content-type property that should reflect its real format. Fetched directly from
  storage by clients, so an incorrect stored type cannot be papered over at read time the way
  video streaming can.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of video and thumbnail blobs uploaded after this feature ships carry an
  accurate, specific stored content type — 0% default to a generic binary type.
- **SC-002**: 100% of streamed video responses declare the type actually stored on the blob when
  one is present, with a working fallback type used for the remainder.
- **SC-003**: After the backfill runs once, 0% of existing video or thumbnail blobs still report
  a generic binary content type.
- **SC-004**: Re-running the backfill after it has already completed changes 0 additional blobs.

## Assumptions

- The real video content type is the type actually produced by the conversion pipeline today
  (a WebM-family video type), and the real thumbnail content type is JPEG — both per the current
  conversion pipeline's actual output, not a new format decision made by this feature.
- "Generic binary type" means the default Azure Blob Storage applies to a blob uploaded without
  an explicit content type, which is functionally indistinguishable from no type at all for
  client purposes.
- The backfill is a one-time data-correction step tied to this feature's rollout, not an ongoing
  scheduled job; new uploads after this feature ships never need it because they get the correct
  type at upload time (User Stories 1 and 2).
- Live-serving correctness for newly uploaded content (User Stories 1 and 2) is already in place
  in the current codebase; the backfill for pre-existing blobs (User Story 3) is the remaining,
  not-yet-built part of this feature.
