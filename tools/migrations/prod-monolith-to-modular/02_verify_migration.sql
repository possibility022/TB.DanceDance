-- Migration: prod monolith -> modular — Verification
-- Run against: dancedance, after 01_migrate_data.sql.
--
-- Compares every target table's row count against its source counterpart (via dblink),
-- runs a few referential spot-checks on the loaded data, and confirms the EF history
-- tables were left untouched.
--
-- Pass the source connection via psql -v (same value as 01):
--   psql -U postgres -d dancedance -v source_conn="host=… dbname=tbapi_db …" -f 02_verify_migration.sql

CREATE EXTENSION IF NOT EXISTS dblink;

SELECT dblink_connect('src', :'source_conn');

-- ── Row-count parity: target vs source (old table names on the source side) ────
WITH src AS (
    SELECT *
    FROM dblink('src', $$
        SELECT 'Users',                   count(*) FROM access."Users"
        UNION ALL SELECT 'Groups',         count(*) FROM access."Groups"
        UNION ALL SELECT 'GroupsAdmins',   count(*) FROM access."GroupsAdmins"
        UNION ALL SELECT 'Events',         count(*) FROM access."Events"
        UNION ALL SELECT 'AssignedToGroups',        count(*) FROM access."AssingedToGroups"
        UNION ALL SELECT 'AssignedToEvents',        count(*) FROM access."AssingedToEvents"
        UNION ALL SELECT 'GroupAssignmentRequests', count(*) FROM access."GroupAssigmentRequests"
        UNION ALL SELECT 'EventAssignmentRequests', count(*) FROM access."EventAssigmentRequests"
        UNION ALL SELECT 'Videos',         count(*) FROM video."Videos"
        UNION ALL SELECT 'VideoMetadata',  count(*) FROM video."VideoMetadata"
        UNION ALL SELECT 'SharedWith',     count(*) FROM access."SharedWith"
        UNION ALL SELECT 'SharedLinks',    count(*) FROM access."SharedLinks"
        UNION ALL SELECT 'Comments',       count(*) FROM comments."Comments"
    $$) AS s(entity text, src_count bigint)
),
tgt AS (
    SELECT 'Users' AS entity,                  count(*) AS tgt_count FROM access."Users"
    UNION ALL SELECT 'Groups',                 count(*) FROM access."Groups"
    UNION ALL SELECT 'GroupsAdmins',           count(*) FROM access."GroupsAdmins"
    UNION ALL SELECT 'Events',                 count(*) FROM access."Events"
    UNION ALL SELECT 'AssignedToGroups',       count(*) FROM access."AssignedToGroups"
    UNION ALL SELECT 'AssignedToEvents',       count(*) FROM access."AssignedToEvents"
    UNION ALL SELECT 'GroupAssignmentRequests',count(*) FROM access."GroupAssignmentRequests"
    UNION ALL SELECT 'EventAssignmentRequests',count(*) FROM access."EventAssignmentRequests"
    UNION ALL SELECT 'Videos',                 count(*) FROM video."Videos"
    UNION ALL SELECT 'VideoMetadata',          count(*) FROM video."VideoMetadata"
    UNION ALL SELECT 'SharedWith',             count(*) FROM sharing."SharedWith"
    UNION ALL SELECT 'SharedLinks',            count(*) FROM sharing."SharedLinks"
    UNION ALL SELECT 'Comments',               count(*) FROM comments."Comments"
)
SELECT
    t.entity,
    s.src_count,
    t.tgt_count,
    CASE WHEN s.src_count = t.tgt_count THEN 'match' ELSE 'MISMATCH' END AS status
FROM tgt t
JOIN src s USING (entity)
ORDER BY t.entity;

SELECT dblink_disconnect('src');

-- ── Referential spot-checks on the loaded target (all should return 0) ─────────
SELECT 'SharedWith.VideoId orphan' AS check, count(*) AS bad_rows
FROM sharing."SharedWith" sw
LEFT JOIN video."Videos" v ON v."Id" = sw."VideoId"
WHERE v."Id" IS NULL
UNION ALL
SELECT 'Comments.SharedLinkId orphan', count(*)
FROM comments."Comments" c
LEFT JOIN sharing."SharedLinks" sl ON sl."Id" = c."SharedLinkId"
WHERE c."SharedLinkId" IS NOT NULL AND sl."Id" IS NULL
UNION ALL
SELECT 'Comments.VideoId orphan', count(*)
FROM comments."Comments" c
LEFT JOIN video."Videos" v ON v."Id" = c."VideoId"
WHERE v."Id" IS NULL;

-- ── EF history tables must be untouched (expect 1 row each) ────────────────────
SELECT 'Access_MigrationHistory' AS history_table, count(*) AS rows FROM public."Access_MigrationHistory"
UNION ALL
SELECT 'Video_MigrationHistory', count(*) FROM public."Video_MigrationHistory";
