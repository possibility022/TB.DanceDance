# Feature Specification: Group Management (Creation, Administrators, and Member Access)

**Feature Branch**: `001-group-management`

**Created**: 2026-06-23

**Status**: Implemented (retroactively documented)

**Input**: User description: "Use the issue tracker and existing code to document features we have now" — scoped to Group Management on the web app (already shipped on `master`).

**Source material**: the project's tracked work items for group creation, backend
implementation, web UI implementation, tests, and a resolved local-verification item confirming
the golden path against real data; `src/backend/Application/Features/Groups/` and
`src/my-dance.web/src/app/features/groups/` in the current codebase.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create a group and become its admin (Priority: P1)

Any authenticated user can create a new dance group by giving it a name and a season date
range. The creator is automatically recorded as the group's first administrator, with no
separate approval step.

**Why this priority**: Without group creation, none of the rest of the feature (admin
management, member management) has anything to operate on. This is the entry point for the
whole feature and was previously entirely missing — groups could only be created out-of-band.

**Independent Test**: Log in as any user, submit the create-group form with a name and season
dates, and confirm a new group exists with that user listed as its sole administrator.

**Acceptance Scenarios**:

1. **Given** a logged-in user on the "Create group" form, **When** they submit a valid name and
   a season start/end date, **Then** the group is created and the user becomes its administrator.
2. **Given** a logged-in user, **When** they submit a name shorter than 3 characters, or a season
   end date before the season start date, **Then** the form is rejected with a validation error
   and no group is created.

---

### User Story 2 - Manage a group's administrators (Priority: P1)

Any current administrator of a group can view the list of administrators, add another user as
an administrator, or remove an existing administrator — including themselves — as long as the
group keeps at least one administrator afterward.

**Why this priority**: Administration rights are the access-control backbone of the feature;
every other management action (members, requests) is gated on being an admin, so this must be
correct and safe (no group can be left admin-less) before anything else matters.

**Independent Test**: As an admin, add a second user as admin, then remove the first admin —
this should succeed because one admin remains. Attempt to remove the last remaining admin and
confirm the system blocks it.

**Acceptance Scenarios**:

1. **Given** a group with one administrator, **When** that administrator adds a second user as
   administrator, **Then** the group now lists two administrators.
2. **Given** a group with two administrators, **When** one administrator removes the other (or
   removes themselves), **Then** the removal succeeds and one administrator remains.
3. **Given** a group with exactly one administrator, **When** that administrator attempts to
   remove themselves (or any caller attempts to remove that last administrator), **Then** the
   removal is rejected and the group still has exactly one administrator.
4. **Given** a user who is a member but not an administrator of a group, **When** they call any
   administrator-management action for that group, **Then** the action is rejected and no change
   is made.

---

### User Story 3 - Manage a group's members (Priority: P2)

An administrator can view the group's members, correct a member's recorded join date, and
revoke a member's access entirely.

**Why this priority**: Builds on admin rights (User Story 2) and is needed for ongoing
day-to-day upkeep (correcting mistakes, removing people who left), but the group is usable
without it immediately after creation.

**Independent Test**: As an admin, open a group's member list, change one member's join date,
save it, then remove a different member's access and confirm they no longer appear in the
member list (and lose access to that group's videos).

**Acceptance Scenarios**:

1. **Given** an admin viewing a group's member list, **When** they edit a member's join date and
   save, **Then** the new join date is persisted and reflected in the list.
2. **Given** an admin viewing a group's member list, **When** they remove a member's access (after
   confirming), **Then** that member no longer appears in the list and can no longer see the
   group's shared videos.
3. **Given** a non-administrator member of the group, **When** they attempt to edit or remove
   another member's access, **Then** the action is rejected.

---

### User Story 4 - Bootstrap administrators for groups that pre-date this feature (Priority: P2)

For groups that existed before this feature shipped (and therefore had no recorded
administrator), every existing member of such a group is automatically granted administrator
rights, once, so that the group is not left without anyone able to manage it.

**Why this priority**: Without this one-time step, every pre-existing group would be
permanently locked out of admin management (no one could add/remove admins or manage members)
the moment this feature went live. It's a one-time data fix rather than ongoing behavior, hence
P2 rather than P1.

**Independent Test**: Inspect a group that existed before the feature shipped and had members
but no administrators; after the one-time bootstrap step runs, confirm every prior member of
that group is now also listed as an administrator, and confirm running the step again makes no
further changes.

**Acceptance Scenarios**:

1. **Given** a pre-existing group with members but no recorded administrators, **When** the
   one-time bootstrap step runs, **Then** every existing member of that group becomes an
   administrator of it.
2. **Given** a group that already has at least one administrator for a given member, **When**
   the bootstrap step runs (including being re-run), **Then** that member is not duplicated as
   an administrator and no error occurs.

---

### User Story 5 - Manage pending access requests from within group management (Priority: P3)

An administrator handles pending requests to join their group (approve or decline) from the
same screen used for member and admin management, rather than a separate workflow.

**Why this priority**: This reuses an approval workflow that already existed before this
feature; surfacing it inside group management is a convenience/consolidation, not new
capability, so it is the lowest priority.

**Independent Test**: As an admin with a pending join request for their group, approve it from
the group management screen and confirm the requester becomes a member; decline a different
request and confirm it is no longer pending.

**Acceptance Scenarios**:

1. **Given** an admin viewing their group's management screen, **When** they approve a pending
   request, **Then** the requester becomes a member of the group.
2. **Given** an admin viewing their group's management screen, **When** they decline a pending
   request, **Then** the request is removed from the pending list and the requester does not
   become a member.

### Edge Cases

- What happens when an administrator tries to add a user who does not exist (e.g., a typo'd
  user id)? The action is rejected and no administrator is added.
- What happens when an administrator tries to add a user who is already an administrator of
  that group? The action succeeds without creating a duplicate entry (no error, no double
  membership).
- What happens when an administrator tries to edit the join date or remove access for a user
  who is not actually a member of that group? The action is rejected (nothing to update or
  remove).
- What happens when a user who is not logged in attempts any group-management action? The
  action is rejected, consistent with the rest of the application's authentication requirements.
- What happens to a group's existing pending access requests, events, or shared videos when
  administrators or members change? They are unaffected — this feature only changes who can
  manage the group and who is a member, not the group's other data.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow any authenticated user to create a new group by providing a
  name and a season start/end date range.
- **FR-002**: System MUST require a group name of at least 3 characters and a season start date
  on or before the season end date; creation MUST be rejected otherwise.
- **FR-003**: System MUST automatically record the creator of a group as that group's first
  administrator at the moment of creation.
- **FR-004**: System MUST allow any current administrator of a group to view the full list of
  that group's administrators.
- **FR-005**: System MUST allow any current administrator of a group to grant administrator
  rights to another existing user of the system.
- **FR-006**: System MUST allow any current administrator of a group to revoke another
  administrator's rights, including their own, **except** when doing so would leave the group
  with zero administrators.
- **FR-007**: System MUST reject every administrator-management and member-management action
  for a group when the acting user is not currently an administrator of that group.
- **FR-008**: System MUST allow any current administrator of a group to view the full list of
  that group's members, including each member's recorded join date.
- **FR-009**: System MUST allow any current administrator of a group to update a member's
  recorded join date.
- **FR-010**: System MUST allow any current administrator of a group to revoke a member's access
  to that group; once revoked, that person MUST no longer be able to see the group's shared
  videos.
- **FR-011**: System MUST provide a one-time mechanism that grants administrator rights to every
  existing member of every group that has members but no recorded administrator, so that no
  group created before this feature is left without an administrator.
- **FR-012**: The one-time administrator bootstrap MUST be safe to run more than once without
  creating duplicate administrator entries or other side effects.
- **FR-013**: System MUST continue to support the existing pending-access-request
  approve/decline workflow unchanged, and MUST surface it within the group management screen
  for administrators.
- **FR-014**: System MUST NOT expose group creation, administrator management, or member
  management to the mobile application; the mobile app retains only the existing
  request-access flow.

*Explicitly out of scope (per source requirements, not deferred for lack of a default):*

- Changing ownership of events.
- Deleting a group.
- A global, application-wide super-administrator role.
- A mobile management UI for groups.

### Key Entities

- **Group**: A dance group with a name and a season date range (start/end). Has zero or more
  administrators and zero or more members.
- **Group Administrator**: A relationship between a user and a group granting that user the
  right to manage the group's administrators and members. A group MUST have at least one
  administrator at all times once it has any.
- **Group Member**: A relationship between a user and a group representing approved access to
  the group, including the date the member's access began ("join date"). Distinct from
  administrator status — a member is not automatically an administrator.
- **Pending Access Request**: An existing, unchanged concept representing a user's request to
  join a group, which an administrator approves (creating a Group Member) or declines.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can go from "no group exists" to "group created with themselves as
  administrator" in a single form submission, with no separate approval step.
- **SC-002**: 100% of groups that existed before this feature shipped have at least one
  administrator after the one-time bootstrap step runs, with zero duplicate administrator
  entries introduced.
- **SC-003**: 100% of administrator- and member-management actions attempted by a
  non-administrator are rejected; 0% succeed.
- **SC-004**: It is never possible to reach a state where an existing group has zero
  administrators, across creation, manual admin removal, and self-removal.
- **SC-005**: An administrator can locate and revoke a specific member's access, or correct
  their join date, without leaving the group's management screen.

## Assumptions

- "Any authenticated user can create a group" — confirmed in source material: no
  additional approval or role is required to create a group.
- The one-time administrator bootstrap is a one-off data migration tied to the rollout of this
  feature, not an ongoing scheduled job; it runs once and is safe to re-run, but is not expected
  to run repeatedly in normal operation.
- "Existing user" in administrator-grant actions means a user already registered in the system;
  granting admin rights to an email address or identifier with no matching user account is
  rejected rather than creating a placeholder.
- The mobile app's exclusion from this feature (request-access flow only) is a deliberate,
  permanent scope boundary for this feature, not a temporary gap awaiting a future mobile
  implementation.
- Revoking a member's access removes their visibility into the group's shared videos but does
  not delete or otherwise affect the videos, events, or other data associated with the group.
