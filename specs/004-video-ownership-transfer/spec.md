# Feature Specification: Video Ownership Transfer via Link

**Feature Branch**: `[004-video-ownership-transfer]`

**Created**: 2026-06-23

**Status**: Draft

**Input**: User description: "take DD-66 from youtrack and turn it into spec files. Don't mention youtrack"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sender creates a transfer (Priority: P1)

A video owner wants to hand off one or more of their private videos to someone else (for example, when a dance partner should now be the official owner of recorded sessions). From "My Library" they switch into multi-select, choose the videos they personally own, and create a transfer. The system generates a shareable link and shows the combined size of everything being transferred.

**Why this priority**: Without the ability to create a transfer, nothing else in this feature can happen. This is the entry point for the whole flow.

**Independent Test**: Can be fully tested by selecting one or more owned, private, converted videos in "My Library," creating a transfer, and verifying a shareable link is generated along with the correct total size — independent of whether anyone ever opens that link.

**Acceptance Scenarios**:

1. **Given** a logged-in user owns several converted, private videos, **When** they multi-select a subset and create a transfer, **Then** a shareable link is generated and the total size of the selected videos is displayed.
2. **Given** a user attempts to include a video they do not own, or one that is public, or one that has not finished converting, **When** they try to add it to a transfer, **Then** the system prevents that video from being selectable for transfer.
3. **Given** a video is already part of another pending outgoing transfer, **When** the owner tries to add it to a second transfer, **Then** the system blocks adding it until the first transfer is resolved.

---

### User Story 2 - Recipient reviews and accepts a transfer (Priority: P1)

The person receiving the link wants to see exactly what they're being offered before deciding. They open the link, log in if they aren't already, and land on a dedicated page listing each video's title, recorded date, size, and a playable preview, along with the combined size. They then accept the transfer, which moves ownership of every video to them.

**Why this priority**: Accepting is the core value-delivering action of the feature — without it, ownership never actually moves and the feature delivers no value.

**Independent Test**: Can be fully tested by opening a valid transfer link as the intended recipient, confirming the per-video details and total size render correctly, accepting, and verifying that ownership of every listed video and the corresponding storage usage have moved to the recipient.

**Acceptance Scenarios**:

1. **Given** a recipient is not logged in, **When** they open a transfer link, **Then** they are required to log in before the transfer page is shown.
2. **Given** a logged-in recipient opens a valid, pending transfer link, **When** the page loads, **Then** they see each video's title, recorded date, size, and a playable preview, plus the combined size of all videos in the transfer.
3. **Given** a recipient has enough remaining storage quota, **When** they accept the transfer, **Then** every video's ownership moves to the recipient, the recipient's used storage increases by the transfer's total size, and the sender's used storage decreases by the same amount, all as a single atomic outcome.
4. **Given** accepting the transfer would push the recipient over their storage quota, **When** they attempt to accept, **Then** the acceptance is blocked and they see a clear message explaining the quota would be exceeded.
5. **Given** ownership has just moved to the recipient, **When** the transfer completes, **Then** any share links the sender had previously created for those videos are revoked, while existing comments on the videos remain unchanged.

---

### User Story 3 - Recipient declines a transfer (Priority: P2)

A recipient may not want the videos being offered to them. They open the link and choose to decline instead of accepting, leaving ownership unchanged.

**Why this priority**: Declining is the natural counterpart to accepting and needs to be available, but it doesn't unlock new value beyond letting a recipient opt out — slightly lower priority than the accept path itself.

**Independent Test**: Can be fully tested by opening a valid transfer link as the recipient and choosing decline, then verifying ownership and storage are unchanged and the link no longer accepts further action.

**Acceptance Scenarios**:

1. **Given** a logged-in recipient is viewing a pending transfer, **When** they choose to decline, **Then** ownership of all videos in the transfer remains with the sender and no storage quotas change.
2. **Given** a transfer has been declined, **When** anyone opens that link again, **Then** the page indicates the transfer is no longer active and offers no accept/decline action.

---

### User Story 4 - Sender manages pending transfers (Priority: P2)

A sender wants visibility into transfers they've created that haven't been resolved yet, and the ability to cancel one before the recipient acts on it (for example, if they sent it to the wrong person).

**Why this priority**: Important for trust and control over an action that moves ownership, but the feature is still usable end-to-end without it — it's a safety/management layer on top of the core create/accept flow.

**Independent Test**: Can be fully tested by creating a transfer, viewing it in a "My Transfers" screen while pending, revoking it, and verifying the link no longer allows acceptance.

**Acceptance Scenarios**:

1. **Given** a sender has created one or more transfers, **When** they open "My Transfers," **Then** they see each transfer's status (pending, accepted, declined, revoked, or expired).
2. **Given** a transfer is still pending, **When** the sender revokes it, **Then** ownership is unaffected, used storage is unaffected, and the link no longer allows the recipient to accept or decline.
3. **Given** a transfer has already been accepted or declined, **When** the sender views "My Transfers," **Then** revoke is not available for that transfer.

---

### User Story 5 - Pending transfers expire automatically (Priority: P3)

If neither the sender revokes nor the recipient responds within a set period, the transfer should stop being actionable on its own, so offers don't linger indefinitely.

**Why this priority**: A cleanliness/safety-net behavior rather than something users actively trigger; valuable but the lowest-impact piece of the feature if temporarily missing.

**Independent Test**: Can be fully tested by creating a transfer, letting it pass its expiration period without action, and verifying the link no longer accepts accept/decline.

**Acceptance Scenarios**:

1. **Given** a transfer has been pending longer than the expiration period, **When** anyone opens the link, **Then** the page indicates the transfer has expired and offers no accept/decline action.
2. **Given** a transfer has expired, **When** the sender views "My Transfers," **Then** its status is shown as expired.

---

### Edge Cases

- What happens when a video included in a pending transfer is deleted by the sender before the recipient responds? The video is dropped from the transfer; the recipient sees the remaining videos and the adjusted total size (or the transfer is cancelled outright if no videos remain).
- What happens if the recipient is the same account as the sender? Self-transfer is not a meaningful operation and should be prevented.
- What happens if the recipient's available quota changes (e.g., shrinks) between opening the link and clicking accept? The quota check is re-validated at the moment of acceptance, not just when the page loads.
- What happens if two recipients try to act on the same transfer link at once (e.g., it was forwarded)? Only one accept/decline can succeed; the second attempt sees the transfer's resulting state (already accepted/declined).
- How does the system handle a sender trying to create a transfer with zero videos selected? The system prevents creating a transfer with no videos.
- What happens to a pending transfer if the sender's account is removed before it's resolved? The transfer is no longer actionable (treated consistently with revocation).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow a user to select one or more of their own converted, private videos and create a transfer for them.
- **FR-002**: System MUST restrict transfer eligibility to videos that are converted, private, and personally owned by the user creating the transfer.
- **FR-003**: System MUST prevent a video from being part of more than one pending outgoing transfer at the same time.
- **FR-004**: System MUST generate a unique shareable link for each created transfer and display the combined size of the selected videos to the sender at creation time.
- **FR-005**: System MUST require the recipient to be authenticated before viewing the contents of a transfer link.
- **FR-006**: System MUST display, for a pending transfer being viewed by its recipient, each video's title, recorded date, size, and a playable preview, plus the combined total size.
- **FR-007**: System MUST allow the recipient to accept a pending transfer, which reassigns ownership of every video in the transfer to the recipient as a single all-or-nothing operation.
- **FR-008**: System MUST update the sender's and recipient's storage usage to reflect the ownership change at the moment a transfer is accepted.
- **FR-009**: System MUST block acceptance, with a clear explanatory message, when accepting would cause the recipient's storage usage to exceed their storage quota.
- **FR-010**: System MUST allow the recipient to decline a pending transfer, leaving ownership and storage usage unchanged.
- **FR-011**: System MUST allow the sender to revoke a transfer while it is still pending, leaving ownership and storage usage unchanged.
- **FR-012**: System MUST prevent accept/decline/revoke actions on a transfer that is no longer pending (already accepted, declined, revoked, or expired).
- **FR-013**: System MUST automatically expire a pending transfer after a defined period of inactivity.
- **FR-014**: System MUST revoke any share links the sender previously created for a video once that video's ownership has transferred, while leaving the video's comments unaffected.
- **FR-015**: System MUST remove a video from a pending transfer if that video is deleted before the transfer is resolved, adjusting the displayed total size accordingly.
- **FR-016**: System MUST provide the sender a view of their created transfers and each one's current status (pending, accepted, declined, revoked, expired).
- **FR-017**: System MUST prevent a user from creating a transfer addressed to themselves.

### Key Entities

- **Video Transfer**: Represents one transfer offer — the link/batch as a whole. Has a sender, an intended recipient, a status (pending, accepted, declined, revoked, expired), a creation time, and an expiration time. Contains one or more transfer items.
- **Video Transfer Item**: Represents a single video within a transfer. References the video being transferred and carries enough information (title, recorded date, size) to display it without exposing the rest of the sender's library.
- **Video** (existing entity, referenced): Each transferred video's ownership attribute is reassigned from sender to recipient when a transfer is accepted; its private/converted state determines transfer eligibility.
- **User Storage Quota** (existing concept, referenced): Used storage is decremented for the sender and incremented for the recipient when a transfer is accepted; the recipient's quota is checked before acceptance is allowed to proceed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A sender can select videos and generate a transfer link in under 1 minute for a typical batch (up to 10 videos).
- **SC-002**: 100% of accepted transfers result in ownership and storage usage being consistent for both sender and recipient immediately afterward (no partial transfers).
- **SC-003**: 100% of acceptance attempts that would exceed the recipient's storage quota are blocked, with the recipient shown a clear reason.
- **SC-004**: 100% of transfers that are revoked, declined, or expired can no longer be accepted afterward.
- **SC-005**: A recipient can view the complete contents of a transfer (titles, dates, sizes, previews, total size) within a few seconds of opening the link while logged in.

## Assumptions

- "My Library," private/public video visibility, video conversion status, and per-user storage quotas already exist as concepts in the system and are reused rather than redefined by this feature.
- This feature targets the web application only; mobile support is out of scope for the initial version.
- The expiration period for a pending transfer is a single system-wide duration rather than something the sender configures per transfer.
- A transfer can only have one recipient; transferring the same batch of videos to multiple different people simultaneously is out of scope.
- Existing comments on a transferred video are tied to the video itself and are unaffected by a change in ownership.
