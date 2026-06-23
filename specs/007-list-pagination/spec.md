# Feature Specification: List Pagination

**Feature Branch**: `[007-list-pagination]`

**Created**: 2026-06-23

**Status**: Draft

**Input**: User description: "Add pagination to list views that currently return their entire collection in one response (a user's own videos, a group's videos, an event's videos, and a video's comments), so these views stay fast and reliable as the underlying collections grow, with web and mobile users able to fetch additional items on demand via a 'Load more' control."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browsing my own videos in pages (Priority: P1)

A user opens their own video list ("My Videos") and sees the most recent videos immediately, without waiting for every video they have ever uploaded to load at once.

**Why this priority**: This is the most frequently visited list view and the first place unbounded collection growth causes a visible slowdown; it delivers the core paging pattern that every other list reuses.

**Independent Test**: Can be fully tested by uploading more videos than fit on one page, opening "My Videos", and confirming the first page loads quickly and a "Load more" control reveals the remaining videos in batches.

**Acceptance Scenarios**:

1. **Given** a user has more videos than fit on a single page, **When** they open "My Videos", **Then** only the first page of videos is shown along with an indication that more videos are available.
2. **Given** the first page of "My Videos" is shown, **When** the user activates "Load more", **Then** the next page of videos is appended to the list without losing the videos already shown.
3. **Given** a user has fewer videos than one page holds, **When** they open "My Videos", **Then** all of their videos are shown and no "Load more" control is presented.

---

### User Story 2 - Browsing group and event videos in pages (Priority: P2)

A member of a group, or a participant in an event, browses that group's or event's shared videos and can load additional videos on demand instead of waiting for the entire shared collection to load.

**Why this priority**: Group and event video collections grow with member contributions and are visited regularly, but are secondary to a user's own video list in usage frequency.

**Independent Test**: Can be fully tested by adding more videos to a group (or event) than fit on one page, opening that group's (or event's) video list, and confirming paged loading and "Load more" behavior match the "My Videos" experience.

**Acceptance Scenarios**:

1. **Given** a group has more videos than fit on a single page, **When** a member opens the group's video list, **Then** only the first page is shown with a way to load more.
2. **Given** an event has more videos than fit on a single page, **When** a participant opens the event's video list, **Then** only the first page is shown with a way to load more.

---

### User Story 3 - Browsing comments in pages (Priority: P2)

A viewer reading comments on a video — whether signed in or viewing through a shared link — sees the most relevant comments first and can load older comments on demand rather than waiting for the full comment thread to load.

**Why this priority**: Comment threads can grow long on popular videos; paging keeps the conversation view responsive for both signed-in viewers and shared-link viewers.

**Independent Test**: Can be fully tested by posting more comments on a video than fit on one page, opening the comment thread (both as the video owner and via a shared link), and confirming paged loading and "Load more" behavior.

**Acceptance Scenarios**:

1. **Given** a video has more comments than fit on a single page, **When** a signed-in viewer opens the comment thread, **Then** only the first page of comments is shown with a way to load more.
2. **Given** a video has more comments than fit on a single page, **When** someone views it through a shared link, **Then** only the first page of comments is shown with a way to load more.
3. **Given** additional comments are loaded, **When** the viewer posts, edits, deletes, hides, unhides, or reports a comment, **Then** all comments already loaded remain visible afterward instead of collapsing back to only the first page.

---

### Edge Cases

- What happens when a user requests a page beyond the end of the list (e.g., after items were deleted since the page count was last known)? The view shows no additional items and does not treat this as an error.
- How does the system behave when items are added to or removed from a list between page loads (e.g., new comment posted while paging through an older page)? Already-loaded items remain visible; counts reflect the latest state on the next fetch.
- What happens for a list with zero items? The view shows the empty state with no "Load more" control.
- What happens when a list's item count is an exact multiple of the page size? No "Load more" control is shown after the last full page, since there is nothing left to load.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST return results from the following list views in pages rather than as a single unbounded response: a user's own videos, a group's videos, an event's videos, and a video's comments (both for signed-in viewers and shared-link viewers).
- **FR-002**: Each paged response MUST report the total number of items available in the underlying collection, in addition to the items included in that page.
- **FR-003**: Callers MUST be able to request a specific page of a list; the system MUST apply a sensible default page size when none is specified and MUST cap the maximum page size a caller can request.
- **FR-004**: System MUST return list items in a stable, consistent order across consecutive page requests so that paging through a list does not skip or duplicate items under normal conditions.
- **FR-005**: Requesting a page beyond the end of a list MUST return an empty page along with the correct total item count, rather than an error.
- **FR-006**: Web and mobile clients MUST present a "Load more" control on each in-scope list view that has additional pages available, and MUST append newly loaded items to the items already shown.
- **FR-007**: Web and mobile clients MUST hide the "Load more" control once all items in a list have been loaded.
- **FR-008**: After a viewer adds, edits, deletes, hides, unhides, or reports a comment, the comment thread MUST continue showing at least as many comments as were already loaded, rather than collapsing back to only the first page.
- **FR-009**: Access-management and content-sharing list views are explicitly out of scope for paging, since their collections are not expected to grow large enough to need it.

### Key Entities *(include if feature involves data)*

- **Video List**: An ordered collection of videos belonging to a user, group, or event; in scope for paged browsing.
- **Comment Thread**: An ordered collection of comments belonging to a video, viewable by signed-in users or via a shared link; in scope for paged browsing.
- **Page**: A bounded, ordered subset of a list returned to a caller, together with the total number of items in the full list.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Opening any in-scope list view (My Videos, group videos, event videos, comments) loads its first page in under 2 seconds regardless of how many total items the underlying collection holds.
- **SC-002**: Users can reach any item in a list of arbitrary size by repeatedly using "Load more", without the view failing or freezing.
- **SC-003**: 100% of in-scope list views display an accurate total item count alongside the items currently loaded.
- **SC-004**: Performing a comment action (add/edit/delete/hide/unhide/report) never reduces the number of comments visible to the viewer below what was already loaded.

## Assumptions

- "Load more" (an explicit user-triggered action) is the chosen loading pattern for all in-scope list views on both web and mobile, rather than automatic scroll-triggered loading.
- A default page size and a maximum page size, sized for typical list/grid viewing on web and mobile, apply consistently across all in-scope list views unless a future need demands per-view tuning.
- Access-management list views (sharing/permission lists) and content-sharing list views are excluded from this feature because their collections are expected to stay small regardless of usage growth.
- The mobile app's comment-viewing surface is out of scope for this feature, since it has no comment-viewing capability at this time.
