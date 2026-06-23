# Feature Specification: Thumbnail Access via Stable, Cache-Friendly URLs

**Feature Branch**: `009-thumbnail-sas-urls`

**Created**: 2026-06-23

**Status**: Implemented (retroactively documented)

**Input**: User description: "Use the issue tracker and existing code to document features we
have now" — scoped to how thumbnails are delivered to clients on the web app (already shipped on
`master`).

**Source material**: a tracked work item describing a move away from loading thumbnails through
an endpoint that required attaching a full access credential to the request URL, in favor of a
direct, time-limited storage link embedded in video-list responses, with the link's expiry
computed so that repeated requests for the same thumbnail within a short window produce an
identical link (enabling ordinary browser image caching); cross-checked against
`src/backend/Application/Features/Videos/ThumbnailUrlService.cs`,
`src/backend/Application/Domain/Models/SasExpiry.cs`, and
`src/my-dance.web/src/app/shared/ui/video-card/video-card.ts` in the current codebase.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See thumbnails on video listings without a credential in the image URL (Priority: P1)

A user browsing a video listing sees each video's thumbnail rendered directly, where the image
address embedded in the listing is a direct, time-limited storage link rather than an address
carrying the user's full access credential.

**Why this priority**: This is the core behavior change and the reason the feature exists —
removing a full access credential from a URL that browsers may log, cache, or otherwise expose
more broadly than intended.

**Independent Test**: Load any video listing that includes thumbnails and inspect the image
addresses used by the page; confirm each is a direct storage link scoped to that one thumbnail
with its own expiry, not a request carrying the user's access credential.

**Acceptance Scenarios**:

1. **Given** a user viewing a video listing, **When** the listing loads, **Then** each video
   card's thumbnail renders using a direct storage link supplied by the listing response.
2. **Given** a video that has no generated thumbnail yet, **When** its card renders, **Then** no
   broken image request is made and the card's placeholder treatment is shown instead.

---

### User Story 2 - Repeated views of the same thumbnail reuse the cached image (Priority: P1)

When a user revisits a listing or the same video shows up in more than one listing within a
short time window, the thumbnail's link is identical each time, so the browser can serve it from
its own image cache instead of re-fetching it.

**Why this priority**: Without identical, repeatable links, every render of the same thumbnail
would look like a brand-new image to the browser, defeating ordinary caching and adding needless
load — this is the other half of the feature's purpose alongside User Story 1.

**Independent Test**: Load a listing containing a given thumbnail, note its image link, then
reload the page (or view the same video in a second listing) shortly after and confirm the link
for that same thumbnail is byte-for-byte identical.

**Acceptance Scenarios**:

1. **Given** a thumbnail was just requested as part of a listing, **When** the same thumbnail is
   requested again a short time later (within the same expiry window), **Then** the link
   returned is identical to the first one.
2. **Given** enough time has passed that a thumbnail's link has crossed into a new expiry window,
   **When** the thumbnail is requested again, **Then** a new link is returned, still valid for
   that next window.

---

### User Story 3 - The old credential-bearing thumbnail address no longer works (Priority: P2)

The previous way of fetching a thumbnail — an address that required attaching a full access
credential as a query parameter — is no longer available; thumbnails are only reachable through
the direct storage link supplied in listing responses.

**Why this priority**: Retiring the old path is necessary to actually remove the credential
exposure that motivated this feature, but it is a cleanup step that follows naturally once User
Stories 1 and 2 are in place, rather than independently valuable on its own.

**Independent Test**: Attempt to fetch a thumbnail using the old credential-bearing address
pattern and confirm it no longer succeeds.

**Acceptance Scenarios**:

1. **Given** the old credential-bearing thumbnail address pattern, **When** a request is made
   using that pattern, **Then** the request does not succeed.
2. **Given** a client that previously relied on the old address pattern, **When** it instead uses
   the direct storage link from a listing response, **Then** it successfully retrieves the
   thumbnail image.

### Edge Cases

- What happens when a listing is requested by a user with no access to a given video? No
  thumbnail link for that video is included, consistent with the user having no access to the
  video itself.
- What happens right at the boundary between two expiry windows? A request just before the
  boundary gets a link valid for the closing window; a request just after gets a new link valid
  for the next window — there is no scenario where a returned link is already expired the moment
  it is issued.
- What happens if a thumbnail's direct storage link is shared outside the application (e.g.,
  copy-pasted)? It remains usable by anyone who has it until that link's expiry passes, same as
  any time-limited storage link.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST include a direct, time-limited storage link for a video's thumbnail in
  every video-list response where the video has a generated thumbnail.
- **FR-002**: System MUST omit the thumbnail link for a video that has no generated thumbnail,
  without causing an error in the response.
- **FR-003**: System MUST compute each thumbnail link's expiry on a fixed time grid, so that
  multiple requests for the same thumbnail within the same grid window produce an identical link.
- **FR-004**: System MUST issue a new, validly-scoped link for a thumbnail once the current grid
  window has elapsed.
- **FR-005**: System MUST NOT include a user's general access credential in a thumbnail link;
  each link MUST be scoped to that one thumbnail only.
- **FR-006**: System MUST remove the previous credential-bearing thumbnail-fetch address so it no
  longer serves requests.
- **FR-007**: System MUST only include a thumbnail link for a video in a listing response when
  the requesting user has access to that video, consistent with existing access rules.

### Key Entities

- **Thumbnail Link**: A direct, time-limited storage address for one specific thumbnail image,
  embedded in video-list API responses. Its expiry falls on a fixed time grid so repeated
  requests within the same window yield the same address.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 0% of thumbnail addresses returned to clients carry a user's general access
  credential.
- **SC-002**: Repeated requests for the same thumbnail within the same expiry window return an
  identical address 100% of the time, enabling the browser to serve it from cache without a
  network request.
- **SC-003**: 100% of requests using the old credential-bearing thumbnail address pattern fail
  after this feature ships.
- **SC-004**: Videos without a generated thumbnail render their placeholder treatment in 100% of
  cases, with 0% broken-image requests.

## Assumptions

- The fixed time grid for thumbnail link expiry is a short, fixed interval (a few tens of
  minutes) chosen to balance cache-friendliness against link lifetime, per the current
  implementation's actual interval rather than a value newly chosen for this document.
- This feature concerns the web app's delivery of thumbnails only; mobile consumption of the same
  links is covered separately by a follow-up feature that depends on this one.
- Removing the old credential-bearing address is a permanent retirement, not a temporary
  deprecation window with planned removal later.
