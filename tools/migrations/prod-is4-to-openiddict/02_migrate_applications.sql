-- Migration: IS4 → OpenIddict — Step 2: Scopes & Applications
-- Run against: tbauthwebdb
--
-- Creates all OpenIddict scopes and applications from scratch, filling in
-- production-specific data from IS4 via dblink:
--   - tbdancedancefront:    redirect URIs (from IS4 ClientRedirectUris)
--   - tbdancedanceconverter: client secret hash (from IS4 ClientSecrets)
--
-- IS4 and OpenIddict both hash client secrets as SHA256(UTF8(secret)) → base64,
-- so the stored hash is directly compatible — no re-hashing needed.
--
-- Idempotent: safe to run multiple times on both empty and already-seeded databases.

CREATE EXTENSION IF NOT EXISTS dblink;

-- Pass via psql -v: psql -d tbauthwebdb -v source_conn="host=... dbname=prodoriginaldata user=... password=..." -f 02_migrate_applications.sql
SELECT dblink_connect('src', :'source_conn');

BEGIN;

-- Scopes
INSERT INTO "Idp.Auth"."OpenIddictScopes" ("Id", "ConcurrencyToken", "Name", "DisplayName", "Resources")
VALUES ('scope_tbdancedanceapi_read', md5(random()::text || clock_timestamp()::text), 'tbdancedanceapi.read', 'TB DanceDance API - read', '["tbdancedanceapi"]')
ON CONFLICT ("Name") DO UPDATE
SET "DisplayName" = EXCLUDED."DisplayName",
    "Resources"   = EXCLUDED."Resources";

INSERT INTO "Idp.Auth"."OpenIddictScopes" ("Id", "ConcurrencyToken", "Name", "DisplayName", "Resources")
VALUES ('scope_tbdancedanceapi_convert', md5(random()::text || clock_timestamp()::text), 'tbdancedanceapi.convert', 'TB DanceDance API - converter', '["tbdancedanceapi"]')
ON CONFLICT ("Name") DO UPDATE
SET "DisplayName" = EXCLUDED."DisplayName",
    "Resources"   = EXCLUDED."Resources";

-- tbdancedancefront: redirect URIs sourced from IS4
INSERT INTO "Idp.Auth"."OpenIddictApplications" (
    "Id", "ClientId", "ClientType", "DisplayName", "ConcurrencyToken",
    "Permissions", "RedirectUris", "PostLogoutRedirectUris", "Requirements")
SELECT
    'app_tbdancedancefront',
    'tbdancedancefront',
    'public',
    'TB DanceDance Frontend',
    md5(random()::text || clock_timestamp()::text),
    '["ept:authorization","ept:end_session","ept:token","gt:authorization_code","gt:refresh_token","rst:code","scp:openid","scp:profile","scp:email","scp:offline_access","scp:tbdancedanceapi.read"]',
    (
        SELECT json_agg(r."RedirectUri")::text
        FROM dblink('src', '
            SELECT r."RedirectUri"
            FROM "IdpServer.Config"."ClientRedirectUris" r
            JOIN "IdpServer.Config"."Clients" c ON c."Id" = r."ClientId"
            WHERE c."ClientId" = ''tbdancedancefront''
        ') AS r("RedirectUri" text)
    ),
    (
        SELECT json_agg(r."RedirectUri")::text
        FROM dblink('src', '
            SELECT r."RedirectUri"
            FROM "IdpServer.Config"."ClientRedirectUris" r
            JOIN "IdpServer.Config"."Clients" c ON c."Id" = r."ClientId"
            WHERE c."ClientId" = ''tbdancedancefront''
              AND r."RedirectUri" NOT LIKE ''%/callback''
        ') AS r("RedirectUri" text)
    ),
    '["ft:pkce"]'
ON CONFLICT ("ClientId") DO UPDATE
SET "RedirectUris"          = EXCLUDED."RedirectUris",
    "PostLogoutRedirectUris"= EXCLUDED."PostLogoutRedirectUris",
    "Permissions"           = EXCLUDED."Permissions",
    "Requirements"          = EXCLUDED."Requirements",
    "ConcurrencyToken"      = md5(random()::text || clock_timestamp()::text);

-- tbdancedanceconverter: client secret sourced from IS4
INSERT INTO "Idp.Auth"."OpenIddictApplications" (
    "Id", "ClientId", "ClientType", "DisplayName", "ClientSecret", "ConcurrencyToken",
    "Permissions")
SELECT
    'app_tbdancedanceconverter',
    'tbdancedanceconverter',
    'confidential',
    'TB DanceDance Converter Daemon',
    (
        SELECT src."Value"
        FROM dblink('src', '
            SELECT s."Value"
            FROM "IdpServer.Config"."ClientSecrets" s
            JOIN "IdpServer.Config"."Clients" c ON c."Id" = s."ClientId"
            WHERE c."ClientId" = ''tbdancedanceconverter''
            LIMIT 1
        ') AS src("Value" text)
    ),
    md5(random()::text || clock_timestamp()::text),
    '["ept:token","gt:client_credentials","scp:tbdancedanceapi.convert"]'
ON CONFLICT ("ClientId") DO UPDATE
SET "ClientSecret"     = EXCLUDED."ClientSecret",
    "Permissions"      = EXCLUDED."Permissions",
    "ConcurrencyToken" = md5(random()::text || clock_timestamp()::text);

-- tbdancedanceandroidapp: fully static
INSERT INTO "Idp.Auth"."OpenIddictApplications" (
    "Id", "ClientId", "ClientType", "DisplayName", "ConcurrencyToken",
    "Permissions", "RedirectUris", "PostLogoutRedirectUris", "Requirements")
VALUES (
    'app_tbdancedanceandroidapp',
    'tbdancedanceandroidapp',
    'public',
    'TB DanceDance Android App',
    md5(random()::text || clock_timestamp()::text),
    '["ept:authorization","ept:end_session","ept:token","gt:authorization_code","gt:refresh_token","rst:code","scp:openid","scp:profile","scp:email","scp:offline_access","scp:tbdancedanceapi.read"]',
    '["tbdancedanceandroidapp://"]',
    '["tbdancedanceandroidapp://"]',
    '["ft:pkce"]'
)
ON CONFLICT ("ClientId") DO UPDATE
SET "Permissions"           = EXCLUDED."Permissions",
    "RedirectUris"          = EXCLUDED."RedirectUris",
    "PostLogoutRedirectUris"= EXCLUDED."PostLogoutRedirectUris",
    "Requirements"          = EXCLUDED."Requirements",
    "ConcurrencyToken"      = md5(random()::text || clock_timestamp()::text);

COMMIT;

SELECT dblink_disconnect('src');
