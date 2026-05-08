-- Migration: IS4 → OpenIddict — Step 2: Application config (redirect URIs + secrets)
-- Run against: tbauthwebdb
--
-- OpenIddict applications already exist (seeded with correct permissions/grant types).
-- This script fills in production-specific data from IS4 via dblink:
--   - tbdancedancefront:   production redirect URIs (read from ClientRedirectUris)
--   - tbdancedanceconverter: client secret hash (read from ClientSecrets)
--
-- IS4 and OpenIddict both hash client secrets as SHA256(UTF8(secret)) → base64,
-- so the stored hash is directly compatible — no re-hashing needed.
--
-- Idempotent: safe to run multiple times.

CREATE EXTENSION IF NOT EXISTS dblink;

-- Pass via psql -v: psql -d tbauthwebdb -v source_conn="host=... dbname=prodoriginaldata user=... password=..." -f 02_migrate_applications.sql
SELECT dblink_connect('src', :'source_conn');

BEGIN;

-- tbdancedancefront: production redirect URIs
-- RedirectUris: all URIs registered in IS4
-- PostLogoutRedirectUris: non-callback URIs (i.e. base app URLs derived from IS4 redirect URIs)
UPDATE "Idp.Auth"."OpenIddictApplications"
SET
    "RedirectUris" = (
        SELECT json_agg(r."RedirectUri")::text
        FROM dblink('src', '
            SELECT r."RedirectUri"
            FROM "IdpServer.Config"."ClientRedirectUris" r
            JOIN "IdpServer.Config"."Clients" c ON c."Id" = r."ClientId"
            WHERE c."ClientId" = ''tbdancedancefront''
        ') AS r("RedirectUri" text)
    ),
    "PostLogoutRedirectUris" = (
        SELECT json_agg(r."RedirectUri")::text
        FROM dblink('src', '
            SELECT r."RedirectUri"
            FROM "IdpServer.Config"."ClientRedirectUris" r
            JOIN "IdpServer.Config"."Clients" c ON c."Id" = r."ClientId"
            WHERE c."ClientId" = ''tbdancedancefront''
              AND r."RedirectUri" NOT LIKE ''%/callback''
        ') AS r("RedirectUri" text)
    ),
    "ConcurrencyToken" = md5(random()::text || clock_timestamp()::text)
WHERE "ClientId" = 'tbdancedancefront';

-- tbdancedanceconverter: production client secret hash from IS4
UPDATE "Idp.Auth"."OpenIddictApplications"
SET
    "ClientSecret" = (
        SELECT src."Value"
        FROM dblink('src', '
            SELECT s."Value"
            FROM "IdpServer.Config"."ClientSecrets" s
            JOIN "IdpServer.Config"."Clients" c ON c."Id" = s."ClientId"
            WHERE c."ClientId" = ''tbdancedanceconverter''
            LIMIT 1
        ') AS src("Value" text)
    ),
    "ConcurrencyToken" = md5(random()::text || clock_timestamp()::text)
WHERE "ClientId" = 'tbdancedanceconverter';

COMMIT;

SELECT dblink_disconnect('src');
