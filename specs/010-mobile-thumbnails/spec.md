# Feature Specification: Mobile Video Thumbnail Previews

**Feature Branch**: `010-mobile-thumbnails`

**Created**: 2026-06-23

**Status**: Implemented (retroactively documented)

**Input**: User description: "Use the issue tracker and existing code to document features we
have now" — scoped to the mobile app's video-list thumbnail previews (already shipped).

**Source material**: a tracked work item describing the addition of thumbnail previews to the
mobile app's video lists, consuming the same direct thumbnail link added to video-list API
responses by the web thumbnail-delivery feature, with no custom authentication or caching code
needed on the mobile side because the platform's image control handles both automatically given
a stable, cache-friendly link; cross-checked against
`src/mobile/TB.DanceDance.Mobile/Pages/Controls/VideoThumbnail.xaml`,
`src/mobile/TB.DanceDance.Mobile.Library/Data/Models/Video.cs`, and the pages that use the
thumbnail control in the current codebase.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See a thumbnail preview for each video in a list (Priority: P1)

A user browsing their own videos, or a group's shared videos, on the mobile app sees a preview
image for each video in the list, so they can recognize a video at a glance instead of seeing
only its name.

**Why this priority**: This is the entire value of the feature — without a rendered preview,
there is nothing else to test or build on.

**Independent Test**: Open the "My Videos" list and a group's video list on the mobile app and
confirm each video that has a thumbnail shows its preview image.

**Acceptance Scenarios**:

1. **Given** a user viewing their own video list on the mobile app, **When** the list loads,
   **Then** each video with a thumbnail shows its preview image.
2. **Given** a user viewing a group's shared video list on the mobile app, **When** the list
   loads, **Then** each video with a thumbnail shows its preview image there too.

---

### User Story 2 - See a consistent placeholder for videos without a thumbnail (Priority: P2)

A video that has no generated thumbnail shows a placeholder treatment — a gradient background
with a centered play icon — instead of a blank space or a broken image.

**Why this priority**: Keeps the list visually consistent and avoids a jarring blank/broken
appearance, but the list is still usable without it (User Story 1 already covers the common
case), so it ranks below the core preview behavior.

**Independent Test**: Find a video with no thumbnail in either video list and confirm it shows
the gradient-and-play-button placeholder rather than a blank or broken image.

**Acceptance Scenarios**:

1. **Given** a video with no thumbnail in a list, **When** the list renders, **Then** that
   video's entry shows a gradient background with a centered play icon.
2. **Given** a list containing a mix of videos with and without thumbnails, **When** the list
   renders, **Then** each entry independently shows either its real preview or the placeholder,
   with no visual inconsistency between them.

### Edge Cases

- What happens while a thumbnail image is still loading over the network? The entry does not
  show a broken state during the brief loading period; the platform's image control handles the
  transition.
- What happens if the same thumbnail link was already shown recently in another list (e.g., the
  same video appears in both "My Videos" and a group's list)? The platform's own image caching
  may reuse the previously fetched image rather than re-downloading it, without any
  feature-specific caching code.
- What happens on a video whose thumbnail link has expired by the time the image is requested?
  This mirrors the web behavior of the underlying thumbnail link and is not a mobile-specific
  concern introduced by this feature.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Mobile app MUST display each video's thumbnail preview image on the "My Videos"
  list, for every video that has one.
- **FR-002**: Mobile app MUST display each video's thumbnail preview image on a group's shared
  video list, for every video that has one.
- **FR-003**: Mobile app MUST display a gradient-background, centered-play-icon placeholder for
  any video that has no thumbnail, in place of a blank or broken image.
- **FR-004**: Mobile app MUST render the thumbnail directly from the link supplied by the video
  list response, without attaching any additional access credential to the image request.
- **FR-005**: Mobile app's placeholder treatment for a missing thumbnail MUST visually mirror the
  equivalent placeholder already used on the web app (gradient background, centered play icon).

*Explicitly out of scope (per source requirements, not deferred for lack of a default):*

- Any custom authentication handling for thumbnail image requests.
- Any custom image-caching logic beyond what the mobile platform's image control provides
  automatically.

### Key Entities

- **Mobile Video List Entry**: A single video's row/card within "My Videos" or a group's video
  list on the mobile app, showing either its thumbnail preview or the placeholder treatment.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of videos with a thumbnail show their preview image on both "My Videos" and
  group video lists on the mobile app.
- **SC-002**: 100% of videos without a thumbnail show the gradient-and-play-button placeholder,
  with 0% blank or broken-image entries.
- **SC-003**: No additional authentication or caching code is required on the mobile side beyond
  binding the supplied thumbnail link to the platform's standard image control.

## Assumptions

- This feature depends on video-list API responses already including a direct, time-limited
  thumbnail link per video (delivered by a separate, prerequisite thumbnail-delivery feature);
  it does not change how that link is produced.
- The mobile platform's built-in image control provides adequate on-disk caching for repeat views
  of the same thumbnail without any feature-specific caching code, given that the underlying
  links are stable and repeatable within their expiry window.
- This feature covers "My Videos" and group video lists only; any other mobile screens that may
  later show videos are out of scope unless and until a future feature extends thumbnail display
  to them.
