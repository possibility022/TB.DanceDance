---
name: feature-planning
description: >-
  Plan a new feature with the user up to the point of hand-off: scope it through
  conversation, then draft and create linked YouTrack issues (parent feature +
  several small implementation subtasks split by layer/slice — each covering its own
  tests — plus a Local-setup/verification subtask, each written with enough direction to
  be picked up cold) in project DD. Does NOT produce a technical implementation plan —
  that's `feature-pickup`'s job once active work begins, closer to when the plan will
  actually be acted on. Triggers: "let's plan a feature for X", "I want to add Y to the
  app", "help me scope out Z before I start", "create a youtrack item for this feature",
  "what would it take to build W".
---

# Feature planning — scope and YouTrack issues

A feature is ready to hand off once it has an agreed scope and linked YouTrack issues whose
descriptions carry enough intent — implementation direction, test approach, local
verification steps — for `feature-pickup` to re-derive a detailed plan later. This skill
stops there. It deliberately does **not** call `EnterPlanMode`: a technical implementation
plan drafted now would describe a codebase that may have moved on by the time work actually
starts. `feature-pickup` re-derives that plan when the feature is picked up — fresher
context, closer to the code change itself.

## 1. Scope the feature through conversation

Ask whatever is missing — don't assume:
- What problem does it solve, and for whom (end user, converter, admin)?
- What does "done" look like (acceptance criteria)?
- Which layers does it touch? Map it onto the existing architecture: `Domain` /
  `Application` / `Infrastructure` / `API` (backend), `src/my-dance.web` (Angular SPA),
  `src/mobile` (MAUI), the converter daemon, or a DB migration (new schema/table under
  `access`, `video`, `comments`, or default).
- Anything explicitly out of scope?

Stop here until you can write the scope as a short paragraph the user agrees with — that
paragraph becomes the YouTrack description.

## 2. Draft YouTrack issues — project `DD` (key), don't create yet

Structure: **one parent feature issue + linked subtasks**. Always split the implementation
across **several small implementation subtasks** rather than one monolithic `Implementation`
issue. **Each implementation subtask covers writing the tests for its own slice** (when that
slice can be meaningfully tested in isolation) rather than deferring them to a separate
`Tests` issue. Then add one `Local setup / verification` subtask. Do **not** create a
standalone `Tests` subtask.

- Use `mcp__youtrack__create_issue` with `project: DD`. Known custom fields:
  `Stage` (Backlog → Develop → Review → Test → Staging → Done — start at **Backlog**) and
  `Priority` (Show-stopper, Critical, Major, Normal, Minor — ask the user, default Normal
  if they don't care).
- Write each subtask description with enough direction that someone picking it up cold —
  i.e. `feature-pickup` — can turn it into a step-by-step plan without re-litigating scope:
  - **Implementation (split into several small subtasks)**: never collapse the whole build
    into one issue. Break it into the smallest coherent, independently reviewable slices —
    each ideally its own PR — and create one subtask per slice (title them
    `Implementation: <slice>`). Default split is by layer/area in dependency order, e.g.
    backend domain + entity + EF migration; backend application/services; backend
    API/endpoints + contracts; `src/my-dance.web` UI; `src/mobile`; converter daemon —
    include only those a feature actually touches, and split a layer further if it's still
    large (e.g. separate frontend screens). For each subtask give the concrete changes and
    which layers/areas they land in, call out any new entity, migration, or seed-data change
    explicitly, and note its dependency on earlier subtasks so they can be picked up in
    order. When a slice adds or changes a DB schema, say that its migration is generated with
    the `generate-migration` skill (`add-migration.ps1`, revertable via `remove-migration.ps1`)
    against a throwaway Postgres container — never hand-written. **Each implementation subtask must also spell out the tests for its own slice**:
    what needs covering and roughly where it lives (backend integration tests in
    `src/tests/TB.DanceDance.Tests` — xunit v3, NSubstitute, WireMock.Net, Testcontainers;
    mobile tests in `src/tests/TB.DanceDance.Mobile.Tests`; frontend Vitest specs co-located
    in `src/my-dance.web`), plus any new fixtures/seed data/stubs anticipated. Only omit
    tests for a slice that genuinely can't be tested in isolation (e.g. a scaffolding- or
    migration-only change with no behaviour yet) — and when you do, say so explicitly in the
    subtask. Never split testing out into its own subtask.
  - **Local setup / verification**: the concrete "I'll know it works when..." golden-path
    check — what to click through or call (`curl`/`.http`/UI flow), which `local-stack`
    operations are involved (rebuild a container, reset DB/blobs, get a token, tail logs),
    and any migration/seed-data prerequisites.
- Show the **drafted title + description for each issue** to the user first. Only call
  `create_issue` (using `parentIssue` for subtasks, which auto-links them) after explicit
  approval — issue creation is visible to the team, so confirm before acting.

## 3. Wrap-up

Summarize the YouTrack issue links (parent + subtasks) and the agreed scope. That's the
hand-off — no `EnterPlanMode`, no separate planning doc. `feature-pickup` derives the
implementation/test/verification plan from these issues when work actually begins.
