# prod monolith → modular — data migration

Copies application data from a local restore of the **old production database**
(`tbapi_db`, pre-refactor monolithic schema) into the **new modular database**
(`dancedance`, with the modular EF migrations already applied). Both databases live on
the same local PostgreSQL server; the copy runs through `dblink`.

The new schema is structurally identical to the old one **except** for a few `access`
table renames (typo fixes) and the two share tables moving to a new `sharing` schema:

| Old (tbapi_db)                       | New (dancedance)                       |
|--------------------------------------|----------------------------------------|
| `access.AssingedToEvents`            | `access.AssignedToEvents`              |
| `access.AssingedToGroups`            | `access.AssignedToGroups`              |
| `access.EventAssigmentRequests`      | `access.EventAssignmentRequests`       |
| `access.GroupAssigmentRequests`      | `access.GroupAssignmentRequests`       |
| `access.SharedLinks`                 | `sharing.SharedLinks`                  |
| `access.SharedWith`                  | `sharing.SharedWith`                   |
| _(everything else)_                  | _same schema + name_                   |

Column sets and types are identical on both sides, so each table is a straight
explicit-column `INSERT … SELECT`.

## Files

- `01_migrate_data.sql` — one transaction: `TRUNCATE`s the target data tables (the EF
  `*_MigrationHistory` tables are left alone), then reloads every table in FK-parent-first
  order. Re-runnable.
- `02_verify_migration.sql` — side-by-side source/target row counts with a `match`/`MISMATCH`
  column, referential spot-checks, and a history-table sanity check.
- `run-migration.ps1` — guided runner: tests both connections, shows before counts, asks for
  confirmation, runs `01` then `02`, and fails on any `MISMATCH`.

## Run

```powershell
.\run-migration.ps1
```

Or manually (source password supplied at runtime, never committed):

```powershell
$env:PGPASSWORD = "<postgres-password>"
psql -U postgres -d dancedance `
  -v ON_ERROR_STOP=1 `
  -v source_conn="host=localhost port=5432 dbname=tbapi_db user=postgres password=$env:PGPASSWORD" `
  -f 01_migrate_data.sql
psql -U postgres -d dancedance `
  -v source_conn="host=localhost port=5432 dbname=tbapi_db user=postgres password=$env:PGPASSWORD" `
  -f 02_verify_migration.sql
```

## Notes

- **Local only.** `tbapi_db` is read-only (source); all writes go to `dancedance`.
- The migration touches **data only** — no DDL. A failed row rolls the whole transaction back.
- The EF history tables (`public.Access_MigrationHistory`, `public.Video_MigrationHistory`)
  are never truncated or modified.
