---
name: generate-migration
description: >-
  Generate or revert an EF Core migration for the backend DanceDbContext entirely
  through a single PowerShell script that spins up a throwaway PostgreSQL Docker
  container on a random host port, runs the EF tooling, and drops the container
  afterwards. Use whenever a domain/entity change needs a schema migration. Triggers:
  "add a migration", "generate an EF migration", "create a migration for X", "I changed
  an entity, make the migration", "revert/remove the last migration", "undo that
  migration".
---

# Generate migration - one-shot EF Core migrations via a throwaway Postgres container

Backend migrations live in `src/backend/Infrastructure/Data/Migrations` for `DanceDbContext`.
This skill never hand-writes a migration and never requires you to manage a database by hand:
each script **starts a disposable PostgreSQL container on a random host port, runs the EF
command, and removes the container** in one invocation.

## When to use

After (or as part of) any change to an entity under `Application/Domain/Entities/` or its EF
configuration: a new entity, a new/renamed/removed column, an FK or index change. Generate
the migration with the script rather than `dotnet ef` directly, so it is validated against a
real database and the container lifecycle is handled for you.

## Prerequisites

- Docker Desktop running.
- `dotnet-ef` (the script attempts `dotnet tool restore`, then tells you to
  `dotnet tool install --global dotnet-ef` if still missing).

## Add a migration

Make the entity / EF-config change first, then:

```powershell
.claude/skills/generate-migration/scripts/add-migration.ps1 -Name <PascalCaseName>
```

What it does, in order: start a throwaway postgres container on a random host port, run
`dotnet ef migrations add` into `Data/Migrations`, apply all migrations (existing + new) to
prove the migration applies cleanly, then remove the container (always, even on failure). Pass
`-NoApply` to skip the post-generation validation step.

Name the migration after the change, e.g. `-Name AddCompetitions`. Review the generated
`*.cs` + `*.Designer.cs` and the updated `DanceDbContextModelSnapshot.cs` before committing.

## Revert / remove the most recent migration

To undo a migration you just generated but have not shipped:

```powershell
.claude/skills/generate-migration/scripts/remove-migration.ps1
```

It spins up the container on a random host port, applies all migrations to that database,
runs `dotnet ef migrations remove --force` (deletes the last migration's files and reverts
the model snapshot), and drops the container. Re-run `add-migration.ps1` after fixing the
model to regenerate.

## Notes

- Both scripts resolve the `Infrastructure` project relative to the repo root, so they work
  from any working directory (including a git worktree).
- The container name starts with `tbdance-ef-migration-` and is fully disposable; no data
  persists between runs, and Docker picks the host port so it can run alongside the local
  stack's PostgreSQL on `5432`.
- The scripts set `TBDANCEDANCE_MIGRATION_CONNECTION_STRING` for their own process so EF
  design-time commands use the random-port database. Outside these scripts, the design-time
  factory still falls back to the normal `localhost:5432` connection string.
