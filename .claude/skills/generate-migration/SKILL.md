---
name: generate-migration
description: >-
  Generate or revert an EF Core migration for the backend DanceDbContext entirely
  through a single PowerShell script that spins up a throwaway PostgreSQL Docker
  container, runs the EF tooling, and drops the container afterwards. Use whenever a
  domain/entity change needs a schema migration. Triggers: "add a migration", "generate
  an EF migration", "create a migration for X", "I changed an entity, make the migration",
  "revert/remove the last migration", "undo that migration".
---

# Generate migration — one-shot EF Core migrations via a throwaway Postgres container

Backend migrations live in `src/backend/Infrastructure/Data/Migrations` for `DanceDbContext`.
The design-time factory (`Infrastructure/Data/DesignTimeContextFactory.cs`) is hardcoded to
`localhost:5432` / db `dancedance`. This skill never hand-writes a migration and never
requires you to manage a database by hand: each script **starts a disposable PostgreSQL
container, runs the EF command, and removes the container** in one invocation.

## When to use

After (or as part of) any change to an entity under `Application/Domain/Entities/` or its EF
configuration — a new entity, a new/renamed/removed column, an FK or index change. Generate
the migration with the script rather than `dotnet ef` directly, so it is validated against a
real database and the container lifecycle is handled for you.

## Prerequisites

- Docker Desktop running.
- The **local stack must be stopped** first — it also binds port `5432` (`DC down` if it's
  up; see the `local-stack` skill). The script fails fast with a clear message on a port clash.
- `dotnet-ef` (the script attempts `dotnet tool restore`, then tells you to
  `dotnet tool install --global dotnet-ef` if still missing).

## Add a migration

Make the entity / EF-config change first, then:

```powershell
.claude/skills/generate-migration/scripts/add-migration.ps1 -Name <PascalCaseName>
```

What it does, in order: start a throwaway postgres container → `dotnet ef migrations add`
(into `Data/Migrations`) → apply it to the fresh DB to prove it applies cleanly →
remove the container (always, even on failure). Pass `-NoApply` to skip the validation step
and only generate the files.

Name the migration after the change, e.g. `-Name AddCompetitions`. Review the generated
`*.cs` + `*.Designer.cs` and the updated `DanceDbContextModelSnapshot.cs` before committing.

## Revert / remove the most recent migration

To undo a migration you just generated but have not shipped:

```powershell
.claude/skills/generate-migration/scripts/remove-migration.ps1
```

It spins up the container, runs `dotnet ef migrations remove --force` (deletes the last
migration's files and reverts the model snapshot), and drops the container. Re-run
`add-migration.ps1` after fixing the model to regenerate.

## Notes

- Both scripts resolve the `Infrastructure` project relative to the repo root, so they work
  from any working directory (including a git worktree).
- The container is named `tbdance-ef-migration` and is fully disposable — no data persists
  between runs; it exists only so EF can build and validate against a real PostgreSQL.
