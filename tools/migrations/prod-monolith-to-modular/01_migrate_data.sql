-- Migration: prod monolith schema -> modular schema — Data copy
-- Run against: dancedance   (TARGET, new modular schema)
-- Source:      tbapi_db     (a local restore of prod, old monolithic schema)
--
-- Copies all application data from the old schema into the new one via dblink,
-- applying the refactor's table renames and schema moves:
--   access.AssingedToEvents        -> access.AssignedToEvents
--   access.AssingedToGroups        -> access.AssignedToGroups
--   access.EventAssigmentRequests  -> access.EventAssignmentRequests
--   access.GroupAssigmentRequests  -> access.GroupAssignmentRequests
--   access.SharedLinks             -> sharing.SharedLinks
--   access.SharedWith              -> sharing.SharedWith
-- All other tables keep their schema + name. Column sets and types are identical
-- between old and new, so each copy is a straight explicit-column INSERT … SELECT.
--
-- Re-runnable: TRUNCATEs the target data tables first, then reloads. Does NOT touch
-- the EF migration-history tables (public.Access_MigrationHistory / Video_MigrationHistory).
-- Wrapped in a single transaction — all-or-nothing.
--
-- Pass the source connection via psql -v, e.g.:
--   psql -U postgres -d dancedance \
--     -v source_conn="host=localhost port=5432 dbname=tbapi_db user=postgres password=…" \
--     -f 01_migrate_data.sql

CREATE EXTENSION IF NOT EXISTS dblink;

SELECT dblink_connect('src', :'source_conn');

BEGIN;

-- Clear target data tables (one statement covers the whole FK web; history tables omitted).
TRUNCATE
    access."AssignedToEvents",
    access."AssignedToGroups",
    access."EventAssignmentRequests",
    access."Events",
    access."GroupAssignmentRequests",
    access."Groups",
    access."GroupsAdmins",
    access."Users",
    comments."Comments",
    sharing."SharedLinks",
    sharing."SharedWith",
    video."VideoMetadata",
    video."Videos";

-- ── 1. access.Users (parent of Events + all access membership tables) ──────────
INSERT INTO access."Users" ("Id", "FirstName", "LastName", "Email", "StorageQuotaBytes")
SELECT "Id", "FirstName", "LastName", "Email", "StorageQuotaBytes"
FROM dblink('src', '
    SELECT "Id", "FirstName", "LastName", "Email", "StorageQuotaBytes"
    FROM access."Users"
') AS t(
    "Id"               text,
    "FirstName"        text,
    "LastName"         text,
    "Email"            text,
    "StorageQuotaBytes" bigint
);

-- ── 2. access.Groups, access.Events ───────────────────────────────────────────
INSERT INTO access."Groups" ("Id", "Name", "SeasonStart", "SeasonEnd")
SELECT "Id", "Name", "SeasonStart", "SeasonEnd"
FROM dblink('src', '
    SELECT "Id", "Name", "SeasonStart", "SeasonEnd"
    FROM access."Groups"
') AS t(
    "Id"          uuid,
    "Name"        text,
    "SeasonStart" date,
    "SeasonEnd"   date
);

INSERT INTO access."Events" ("Id", "Name", "Date", "Type", "Owner")
SELECT "Id", "Name", "Date", "Type", "Owner"
FROM dblink('src', '
    SELECT "Id", "Name", "Date", "Type", "Owner"
    FROM access."Events"
') AS t(
    "Id"    uuid,
    "Name"  text,
    "Date"  timestamptz,
    "Type"  integer,
    "Owner" text
);

-- ── 3. access membership tables (depend on Users / Groups / Events) ────────────
INSERT INTO access."GroupsAdmins" ("Id", "UserId", "GroupId")
SELECT "Id", "UserId", "GroupId"
FROM dblink('src', '
    SELECT "Id", "UserId", "GroupId"
    FROM access."GroupsAdmins"
') AS t(
    "Id"      uuid,
    "UserId"  text,
    "GroupId" uuid
);

INSERT INTO access."AssignedToGroups" ("Id", "GroupId", "UserId", "WhenJoined")
SELECT "Id", "GroupId", "UserId", "WhenJoined"
FROM dblink('src', '
    SELECT "Id", "GroupId", "UserId", "WhenJoined"
    FROM access."AssingedToGroups"
') AS t(
    "Id"         uuid,
    "GroupId"    uuid,
    "UserId"     text,
    "WhenJoined" timestamptz
);

INSERT INTO access."AssignedToEvents" ("Id", "EventId", "UserId")
SELECT "Id", "EventId", "UserId"
FROM dblink('src', '
    SELECT "Id", "EventId", "UserId"
    FROM access."AssingedToEvents"
') AS t(
    "Id"      uuid,
    "EventId" uuid,
    "UserId"  text
);

INSERT INTO access."GroupAssignmentRequests"
    ("Id", "UserId", "GroupId", "WhenJoined", "Approved", "ManagedBy")
SELECT "Id", "UserId", "GroupId", "WhenJoined", "Approved", "ManagedBy"
FROM dblink('src', '
    SELECT "Id", "UserId", "GroupId", "WhenJoined", "Approved", "ManagedBy"
    FROM access."GroupAssigmentRequests"
') AS t(
    "Id"         uuid,
    "UserId"     text,
    "GroupId"    uuid,
    "WhenJoined" timestamptz,
    "Approved"   boolean,
    "ManagedBy"  text
);

INSERT INTO access."EventAssignmentRequests"
    ("Id", "UserId", "EventId", "Approved", "ManagedBy")
SELECT "Id", "UserId", "EventId", "Approved", "ManagedBy"
FROM dblink('src', '
    SELECT "Id", "UserId", "EventId", "Approved", "ManagedBy"
    FROM access."EventAssigmentRequests"
') AS t(
    "Id"        uuid,
    "UserId"    text,
    "EventId"   uuid,
    "Approved"  boolean,
    "ManagedBy" text
);

-- ── 4. video.Videos (parent of VideoMetadata) ─────────────────────────────────
INSERT INTO video."Videos" (
    "Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime",
    "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId",
    "ConvertedBlobSize", "SourceBlobSize", "CommentVisibility")
SELECT
    "Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime",
    "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId",
    "ConvertedBlobSize", "SourceBlobSize", "CommentVisibility"
FROM dblink('src', '
    SELECT "Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime",
           "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId",
           "ConvertedBlobSize", "SourceBlobSize", "CommentVisibility"
    FROM video."Videos"
') AS t(
    "Id"                uuid,
    "BlobId"            text,
    "Name"             text,
    "UploadedBy"       text,
    "RecordedDateTime" timestamptz,
    "SharedDateTime"   timestamptz,
    "Duration"         interval,
    "Converted"        boolean,
    "FileName"         text,
    "LockedTill"       timestamptz,
    "SourceBlobId"     text,
    "ConvertedBlobSize" bigint,
    "SourceBlobSize"   bigint,
    "CommentVisibility" integer
);

-- ── 5. video.VideoMetadata (depends on Videos) ────────────────────────────────
INSERT INTO video."VideoMetadata" ("Id", "VideoId", "Metadata")
SELECT "Id", "VideoId", "Metadata"
FROM dblink('src', '
    SELECT "Id", "VideoId", "Metadata"
    FROM video."VideoMetadata"
') AS t(
    "Id"       uuid,
    "VideoId"  uuid,
    "Metadata" bytea
);

-- ── 6. sharing.* (moved out of access; no FKs in new schema) ───────────────────
INSERT INTO sharing."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId")
SELECT "Id", "VideoId", "UserId", "EventId", "GroupId"
FROM dblink('src', '
    SELECT "Id", "VideoId", "UserId", "EventId", "GroupId"
    FROM access."SharedWith"
') AS t(
    "Id"      uuid,
    "VideoId" uuid,
    "UserId"  text,
    "EventId" uuid,
    "GroupId" uuid
);

INSERT INTO sharing."SharedLinks" (
    "Id", "VideoId", "SharedBy", "CreatedAt", "ExpireAt", "IsRevoked",
    "AllowAnonymousComments", "AllowComments")
SELECT
    "Id", "VideoId", "SharedBy", "CreatedAt", "ExpireAt", "IsRevoked",
    "AllowAnonymousComments", "AllowComments"
FROM dblink('src', '
    SELECT "Id", "VideoId", "SharedBy", "CreatedAt", "ExpireAt", "IsRevoked",
           "AllowAnonymousComments", "AllowComments"
    FROM access."SharedLinks"
') AS t(
    "Id"                     text,
    "VideoId"                uuid,
    "SharedBy"               text,
    "CreatedAt"              timestamptz,
    "ExpireAt"               timestamptz,
    "IsRevoked"              boolean,
    "AllowAnonymousComments" boolean,
    "AllowComments"          boolean
);

-- ── 7. comments.Comments (no FKs in new schema) ───────────────────────────────
INSERT INTO comments."Comments" (
    "Id", "VideoId", "UserId", "SharedLinkId", "Content", "CreatedAt", "UpdatedAt",
    "IsHidden", "IsReported", "ReportedReason", "AnonymousName", "PostedAsAnonymous",
    "ShaOfAnonymousId")
SELECT
    "Id", "VideoId", "UserId", "SharedLinkId", "Content", "CreatedAt", "UpdatedAt",
    "IsHidden", "IsReported", "ReportedReason", "AnonymousName", "PostedAsAnonymous",
    "ShaOfAnonymousId"
FROM dblink('src', '
    SELECT "Id", "VideoId", "UserId", "SharedLinkId", "Content", "CreatedAt", "UpdatedAt",
           "IsHidden", "IsReported", "ReportedReason", "AnonymousName", "PostedAsAnonymous",
           "ShaOfAnonymousId"
    FROM comments."Comments"
') AS t(
    "Id"               uuid,
    "VideoId"          uuid,
    "UserId"           text,
    "SharedLinkId"     text,
    "Content"          varchar,
    "CreatedAt"        timestamptz,
    "UpdatedAt"        timestamptz,
    "IsHidden"         boolean,
    "IsReported"       boolean,
    "ReportedReason"   text,
    "AnonymousName"    varchar,
    "PostedAsAnonymous" boolean,
    "ShaOfAnonymousId" bytea
);

COMMIT;

SELECT dblink_disconnect('src');
