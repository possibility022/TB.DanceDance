-- Migration: IS4 → OpenIddict — Step 3: Verification
-- Run against: tbauthwebdb after running 01 and 02
--
-- Expected results:
--   Users:        18 (17 prod + 1 test)
--   UserClaims:   72+ (68 prod + 4 test)
--   Applications: 3 (tbdancedancefront, tbdancedanceconverter, tbdancedanceandroidapp)
--   Scopes:       2

-- Row counts
SELECT 'Users'        AS entity, COUNT(*)::text AS count FROM "Idp.Ident"."AspNetUsers"
UNION ALL
SELECT 'UserClaims',               COUNT(*)::text FROM "Idp.Ident"."AspNetUserClaims"
UNION ALL
SELECT 'Applications',             COUNT(*)::text FROM "Idp.Auth"."OpenIddictApplications"
UNION ALL
SELECT 'Scopes',                   COUNT(*)::text FROM "Idp.Auth"."OpenIddictScopes";

-- Application redirect URIs and secrets
SELECT
    "ClientId",
    "ClientType",
    "RedirectUris",
    "PostLogoutRedirectUris",
    CASE WHEN "ClientSecret" IS NULL THEN 'NOT SET' ELSE 'SET' END AS secret_status
FROM "Idp.Auth"."OpenIddictApplications"
ORDER BY "ClientId";

-- All migrated users (excludes test user)
SELECT "Id", "UserName", "Email", "EmailConfirmed"
FROM "Idp.Ident"."AspNetUsers"
WHERE "Id" <> '31db6f5c-747d-4f75-9e5f-d953968c2fd2'
ORDER BY "Email";
