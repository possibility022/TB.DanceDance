# Feature Specification: Invite via Single-Use Link

**Feature Branch**: `008-invite-links`

**Created**: 2026-06-23

**Status**: Draft

**Input**: User description: "I want to have a option to invite people to groups and events via link. Link can be used only once. Help me plan this feature. Maybe some brainstorm?"

## Clarifications

### Session 2026-06-23

- Q: Who should be able to view and revoke an invite link after it's created — only the admin who created it, or any current admin of that group/event? → A: Any current admin of that group/event can view/revoke it, regardless of who created it.
- Q: Should every invite link expire after the same fixed, system-wide time window, or should the admin be able to choose/customize the expiration window per link? → A: Fixed system-wide default for every link; not configurable per link.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Generate and share an invite link (Priority: P1)

A group administrator or event owner wants to bring a new person into their group or event without manually searching for that person and adding them. They generate an invite link from the group/event management screen and send it to the person through whatever channel they prefer (chat, email, etc.).

**Why this priority**: This is the core value of the feature — without the ability to create a link, nothing else matters. It directly replaces a slower, manual lookup-and-add workflow.

**Independent Test**: Can be fully tested by having an administrator generate an invite link for a group or event and confirming a shareable URL is produced, without needing the redemption flow to exist yet.

**Acceptance Scenarios**:

1. **Given** a user has administrative rights over a group, **When** they request a new invite link for that group, **Then** the system produces a unique, shareable link tied to that group.
2. **Given** a user has administrative rights over an event, **When** they request a new invite link for that event, **Then** the system produces a unique, shareable link tied to that event.
3. **Given** a user without administrative rights over a group/event, **When** they attempt to generate an invite link for it, **Then** the system denies the request.

---

### User Story 2 - Redeem an invite link to join (Priority: P1)

A person receives an invite link and opens it. The system adds them to the corresponding group or event so they can immediately see and interact with its content.

**Why this priority**: Redemption is the other half of the core loop — a link that cannot be redeemed delivers no value. Equal priority to Story 1.

**Independent Test**: Can be fully tested by taking a freshly generated, unused invite link, opening it as a different person, and confirming that person becomes a member of the target group/event.

**Acceptance Scenarios**:

1. **Given** a valid, unused invite link for a group, **When** a person opens it, **Then** they become a member of that group and can access its shared content.
2. **Given** a valid, unused invite link for an event, **When** a person opens it, **Then** they gain access to that event's content.
3. **Given** a person who is already a member of the target group/event, **When** they open a valid invite link for it, **Then** the system recognizes their existing membership and does not treat it as an error.

---

### User Story 3 - Invite link becomes unusable after one redemption (Priority: P1)

Once an invite link has been used by one person, anyone else who tries to open the same link afterward is blocked — the link only ever grants access once.

**Why this priority**: This is the defining constraint the user asked for. Without it, the feature is just a generic, reusable "join group" link, which is a materially different (and less controlled) capability.

**Independent Test**: Can be fully tested by redeeming an invite link once, then attempting to redeem the same link again and confirming the second attempt is rejected with a clear explanation.

**Acceptance Scenarios**:

1. **Given** an invite link that has already been redeemed by someone, **When** a different person opens the same link, **Then** the system rejects the attempt and explains the link has already been used.
2. **Given** an invite link that has already been redeemed, **When** the same person who redeemed it opens it again, **Then** the system does not grant any additional action and clearly indicates the link is no longer active.

---

### User Story 4 - Manage outstanding invite links (Priority: P2)

A group administrator or event owner wants to see which invite links they've created that are still unused, and wants to be able to cancel one before anyone redeems it (e.g., it was sent to the wrong person, or shared somewhere it shouldn't have been).

**Why this priority**: Important for trust and control, but the feature delivers value (Stories 1–3) even before this management view exists.

**Independent Test**: Can be fully tested by creating an invite link, viewing it in a list of outstanding links, revoking it, and confirming it can no longer be redeemed.

**Acceptance Scenarios**:

1. **Given** one or more invite links exist for a group/event, **When** any current admin of that group/event views its invite links, **Then** they see each link's status (active, used, expired, or revoked), regardless of which admin created it.
2. **Given** an active, unused invite link, **When** any current admin of that group/event revokes it, **Then** any future attempt to redeem that link is rejected.
3. **Given** an invite link that has already been redeemed, **When** an admin attempts to revoke it, **Then** the system indicates revocation has no effect since the link was already consumed.

---

### Edge Cases

- What happens when two people open the same unused invite link at nearly the same time? Exactly one redemption MUST succeed; the other MUST be rejected as already-used.
- What happens when an invite link's target group or event no longer exists (e.g., the event ended and was removed) by the time someone tries to redeem it?
- What happens when someone opens an invite link without being logged in? They MUST see a message explaining sign-in/login is required (not an automatic redirect), and MUST be able to complete redemption afterward without re-opening the link.
- How does the system respond when someone opens a malformed or unrecognized link (not just an expired/used one)?
- What happens when an administrator who created a link loses their administrative rights before the link is redeemed or revoked? The link remains manageable (viewable and revocable) by any other current admin of that group/event — it does not become orphaned.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow a group administrator to generate an invite link scoped to one specific group.
- **FR-002**: System MUST allow an event owner/administrator to generate an invite link scoped to one specific event.
- **FR-003**: System MUST reject invite-link creation requests from users who do not have administrative rights over the target group/event.
- **FR-004**: Each invite link MUST permit at most one successful redemption; every redemption attempt after the first successful one MUST be rejected.
- **FR-005**: System MUST present a clear, non-technical message when someone attempts to redeem a link that has already been used, has expired, or has been revoked.
- **FR-006**: System MUST automatically expire an unused invite link after a fixed, system-wide period of time, identical for every link, so that stale, forgotten links cannot be redeemed indefinitely; this period is not configurable by the admin creating the link.
- **FR-007**: System MUST allow any current admin of a group/event — not only the one who created a given invite link — to revoke that link manually at any time before it is redeemed.
- **FR-008**: System MUST allow any current admin of a group/event to view all invite links for it, regardless of which admin created them, along with each link's current status (active, used, expired, revoked) and, for used links, who redeemed it and when.
- **FR-009**: Successfully redeeming an invite link MUST grant the redeeming person membership/access to the target group or event without requiring a separate manual approval step.
- **FR-010**: System MUST treat redemption by someone who is already a member of the target group/event as a no-op that does not consume the link's single use.
- **FR-011**: An invite link MUST be redeemable by whichever person opens it and completes redemption first; it is not bound to a specific invitee's identity (no email/account pre-binding).
- **FR-012**: Redeeming an invite link MUST require the person to be signed in with an existing account. If they open the link while signed out, the system MUST show them a clear message explaining they need to sign in or log in to continue, rather than redirecting them straight to the sign-in/login page automatically.
- **FR-013**: After signing in from that message, the person MUST be able to complete redemption of the same invite link without needing to re-open or re-request it.

### Key Entities

- **Invite Link**: A single-use invitation tied to exactly one group or one event. Tracks who created it, when, its current status (active / redeemed / expired / revoked), and — once redeemed — who redeemed it and when.
- **Group**: Existing entity that an invite link can target; successful redemption results in the redeemer gaining group membership.
- **Event**: Existing entity that an invite link can target; successful redemption results in the redeemer gaining access to the event.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An administrator can produce a shareable invite link for a group or event in under 10 seconds from initiating the request.
- **SC-002**: 100% of invite links are unusable for further redemption immediately after their first successful redemption.
- **SC-003**: 0% of redemption attempts against an already-used, expired, or revoked link result in the redeemer gaining access.
- **SC-004**: An administrator can revoke an unused invite link and have that revocation take effect (block all future redemptions) within seconds.
- **SC-005**: People can join a group/event via invite link without an administrator needing to manually search for and add them, reducing manual membership-management steps for invited joins to zero.

## Assumptions

- Only users who already have administrative rights over a group (existing group-admin role) or ownership/administration rights over an event can generate invite links for it — consistent with who can manage membership today.
- Invite links expire automatically after a fixed, system-wide default period (e.g., a small number of days) if never redeemed, in addition to becoming unusable after their single redemption. The exact duration is a configuration detail (not exposed for admins to customize per link), not a scope question.
- The system only generates the shareable link and records its lifecycle; sending or distributing the link to the intended recipient (chat, email, etc.) is done by the administrator outside the system.
- Redeeming an invite link requires no additional information from the recipient beyond confirming their identity (i.e., no extra application form).
- An invite link is not bound to a specific invitee — whoever opens and completes redemption first joins the group/event; the creator is responsible for sharing it only with the intended person.
- New-user account creation/sign-up is out of scope for this feature; a person must already have an account to redeem a link, though they don't need to be signed in at the moment they first open it.
