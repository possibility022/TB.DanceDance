---
name: feature-pickup
description: >-
  Pick up a planned feature from YouTrack (project DD) and start working on it: fetch
  the issue (by ID or by browsing Backlog), check out a branch, move it into Develop and
  assign it, re-derive the implementation/test/verification plan from the issue and its
  subtasks, set up the local stack, and advance it to Review once implementation and
  tests land. Companion to the `feature-planning` skill. Triggers: "pick up DD-42",
  "let's start working on the thumbnails feature", "what's next in the backlog",
  "start implementing this issue".
---

# Feature pickup — fetch from YouTrack, branch, plan, build, verify

Picks up where `feature-planning` leaves off: an issue already exists in YouTrack with a
scope and (usually) linked `Implementation` / `Tests` / `Local setup` subtasks. This skill
turns that into checked-out code, an active plan, and a running local stack.

## 1. Find the issue

- **Given an ID or link** (e.g. "pick up DD-42"): fetch it directly with
  `mcp__youtrack__get_issue`, plus its comments and linked subtasks.
- **No ID given** ("what's next", "pick up the next feature"): search project `DD` for
  `Stage: Backlog` (or use the "Unassigned in DD" saved search via
  `mcp__youtrack__get_saved_issue_searches` / `mcp__youtrack__search_issues`), list the
  candidates with title + priority, and let the user choose.
- Read the parent description and every linked subtask — that's the scope, test plan, and
  verification checklist the planning step already wrote down. Don't re-derive from
  scratch if it's already there; only fill genuine gaps.

## 2. Branch

- Propose a branch name following the repo's `feature/<short-name>` convention (derive
  `<short-name>` from the issue title — short, kebab-case, no issue ID prefix; matches
  existing branches like `feature/thumbnails`, `feature/commenting-videos`).
- Confirm the name with the user, then create and check it out:
  `git checkout -b feature/<short-name>`.

## 3. Move the issue into Develop

- Assign the issue to the current user (`mcp__youtrack__change_issue_assignee`,
  `mcp__youtrack__get_current_user` for the login) and set `Stage: Develop`
  (`mcp__youtrack__update_issue`). Do this automatically — no need to confirm first, it's
  just reflecting that work has started.

## 4. Re-derive / refresh the implementation plan

- If the `Implementation` subtask already has a clear plan, summarize it back to the user
  and confirm it's still accurate (scope can drift between planning and pickup).
- If it's thin or stale, switch into `EnterPlanMode` and produce/refresh the plan,
  respecting the project's layering and conventions (FastEndpoints vertical slices —
  see the `fastendpoints-migration-conventions` memory — and Angular standalone/signals
  conventions in `src/my-dance.web/.claude/CLAUDE.md`).

## 5. Set up the local environment

Use the `local-stack` skill to get a working environment before writing code:
- Bring the stack up (rebuild only the services you'll be changing).
- Wait for seed data (`docker logs -f tbdanceInitializer`).
- Fetch a token if the feature needs authenticated API calls.

## 6. Build it

Work the subtasks in order — they mirror the natural sequence:
1. **Implementation** — follow the plan from step 4.
2. **Tests** — add/extend tests per the `Tests` subtask (xunit/NSubstitute/WireMock +
   Testcontainers for backend, Vitest for `my-dance.web`, `TB.DanceDance.Mobile.Tests`
   for mobile).
3. **Local setup / verification** — run the golden-path checklist from that subtask;
   use the `verify` skill to actually exercise the feature in the running stack.

Commit as you go rather than batching everything into one final commit: once a
subtask (or a coherent chunk of one — e.g. a layer of the implementation, a test file)
is in a working state, stage just those files and commit with a message describing that
slice. Small, focused commits make the eventual PR easier to review and give you safe
checkpoints to fall back to if a later change goes sideways.

## 7. Advance to Review

Once implementation and tests are in and verified locally, set `Stage: Review`
automatically (same rationale as step 3 — it reflects real progress, not a judgment call
that needs sign-off). Summarize what changed, which subtasks are complete, and any
follow-ups for the PR description.
