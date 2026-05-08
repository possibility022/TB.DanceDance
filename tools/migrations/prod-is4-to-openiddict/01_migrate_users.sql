-- Migration: IS4 → OpenIddict — Step 1: Users & Claims
-- Run against: tbauthwebdb
--
-- Copies AspNetUsers + AspNetUserClaims from prodoriginaldata via dblink.
-- Idempotent: safe to run multiple times.
-- IMPORTANT: preserves original user IDs — the dancedance app DB references them.

CREATE EXTENSION IF NOT EXISTS dblink;

-- Pass via psql -v: psql -d tbauthwebdb -v source_conn="host=... dbname=prodoriginaldata user=... password=..." -f 01_migrate_users.sql
SELECT dblink_connect('src', :'source_conn');

BEGIN;

INSERT INTO "Idp.Ident"."AspNetUsers" (
    "Id", "UserName", "NormalizedUserName",
    "Email", "NormalizedEmail", "EmailConfirmed",
    "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled",
    "LockoutEnd", "LockoutEnabled", "AccessFailedCount"
)
SELECT *
FROM dblink('src', '
    SELECT
        "Id", "UserName", "NormalizedUserName",
        "Email", "NormalizedEmail", "EmailConfirmed",
        "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
        "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled",
        "LockoutEnd", "LockoutEnabled", "AccessFailedCount"
    FROM "Idp.Ident"."AspNetUsers"
') AS src(
    "Id"                   text,
    "UserName"             character varying(256),
    "NormalizedUserName"   character varying(256),
    "Email"                character varying(256),
    "NormalizedEmail"      character varying(256),
    "EmailConfirmed"       boolean,
    "PasswordHash"         text,
    "SecurityStamp"        text,
    "ConcurrencyStamp"     text,
    "PhoneNumber"          text,
    "PhoneNumberConfirmed" boolean,
    "TwoFactorEnabled"     boolean,
    "LockoutEnd"           timestamp with time zone,
    "LockoutEnabled"       boolean,
    "AccessFailedCount"    integer
)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "Idp.Ident"."AspNetUserClaims" ("UserId", "ClaimType", "ClaimValue")
SELECT src."UserId", src."ClaimType", src."ClaimValue"
FROM dblink('src', '
    SELECT "UserId", "ClaimType", "ClaimValue"
    FROM "Idp.Ident"."AspNetUserClaims"
') AS src(
    "UserId"     text,
    "ClaimType"  character varying(256),
    "ClaimValue" character varying(256)
)
WHERE NOT EXISTS (
    SELECT 1 FROM "Idp.Ident"."AspNetUserClaims" c
    WHERE c."UserId"     = src."UserId"
      AND c."ClaimType"  = src."ClaimType"
      AND c."ClaimValue" = src."ClaimValue"
);

COMMIT;

SELECT dblink_disconnect('src');
