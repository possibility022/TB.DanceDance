-- OpenIddict seed for TB.Auth.Web
-- Important: update converter client secret manually after initialization.

INSERT INTO "Idp.Auth"."OpenIddictScopes" ("Id", "ConcurrencyToken", "Name", "DisplayName", "Resources")
VALUES ('scope_tbdancedanceapi_read', md5(random()::text || clock_timestamp()::text), 'tbdancedanceapi.read', 'TB DanceDance API - read', '["tbdancedanceapi"]')
ON CONFLICT ("Name") DO UPDATE
SET "DisplayName" = EXCLUDED."DisplayName",
    "Resources" = EXCLUDED."Resources";

INSERT INTO "Idp.Auth"."OpenIddictScopes" ("Id", "ConcurrencyToken", "Name", "DisplayName", "Resources")
VALUES ('scope_tbdancedanceapi_convert', md5(random()::text || clock_timestamp()::text), 'tbdancedanceapi.convert', 'TB DanceDance API - converter', '["tbdancedanceapi"]')
ON CONFLICT ("Name") DO UPDATE
SET "DisplayName" = EXCLUDED."DisplayName",
    "Resources" = EXCLUDED."Resources";

INSERT INTO "Idp.Auth"."OpenIddictApplications" (
    "Id", "ClientId", "ClientType", "DisplayName", "ConcurrencyToken",
    "Permissions", "RedirectUris", "PostLogoutRedirectUris", "Requirements")
VALUES (
    'app_tbdancedancefront',
    'tbdancedancefront',
    'public',
    'TB DanceDance Frontend',
    md5(random()::text || clock_timestamp()::text),
    '["ept:authorization","ept:end_session","ept:token","gt:authorization_code","gt:refresh_token","rst:code","scp:openid","scp:profile","scp:email","scp:offline_access","scp:tbdancedanceapi.read"]',
    '["http://localhost:3000/callback","http://localhost:4200/callback","http://localhost:5112/signin-callback.html","http://localhost:5112/signin-silent-callback.html","http://localhost:5112/index.html"]',
    '["http://localhost:3000"]',
    '["ft:pkce"]'
)
ON CONFLICT ("ClientId") DO UPDATE
SET "ClientType" = EXCLUDED."ClientType",
    "DisplayName" = EXCLUDED."DisplayName",
    "Permissions" = EXCLUDED."Permissions",
    "RedirectUris" = EXCLUDED."RedirectUris",
    "PostLogoutRedirectUris" = EXCLUDED."PostLogoutRedirectUris",
    "Requirements" = EXCLUDED."Requirements";

INSERT INTO "Idp.Auth"."OpenIddictApplications" (
    "Id", "ClientId", "ClientType", "DisplayName", "ClientSecret", "ConcurrencyToken",
    "Permissions")
VALUES (
    'app_tbdancedanceconverter',
    'tbdancedanceconverter',
    'confidential',
    'TB DanceDance Converter Daemon',
    'AQAAAAEAAYagAAAAEJfM9ZxHB62OVzW+PhwBkNqIxVZBdmJu0s5jQm9xcTUYqtH9Lfz2vku6TUyTb9l/Fw==', -- unencrypted = Other
    md5(random()::text || clock_timestamp()::text),
    '["ept:token","gt:client_credentials","scp:tbdancedanceapi.convert"]'
)
ON CONFLICT ("ClientId") DO UPDATE
SET "ClientType" = EXCLUDED."ClientType",
    "DisplayName" = EXCLUDED."DisplayName",
    "Permissions" = EXCLUDED."Permissions";

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
SET "ClientType" = EXCLUDED."ClientType",
    "DisplayName" = EXCLUDED."DisplayName",
    "Permissions" = EXCLUDED."Permissions",
    "RedirectUris" = EXCLUDED."RedirectUris",
    "PostLogoutRedirectUris" = EXCLUDED."PostLogoutRedirectUris",
    "Requirements" = EXCLUDED."Requirements";

-- Dev-only REST token client. Public (no secret) so any seeded user can fetch a token via
-- the OAuth2 password grant with a single REST call. The password grant is only enabled on
-- the auth server when AuthServer:AllowWeakPasswords=true (local development).
INSERT INTO "Idp.Auth"."OpenIddictApplications" (
    "Id", "ClientId", "ClientType", "DisplayName", "ConcurrencyToken",
    "Permissions")
VALUES (
    'app_tbdancedancehttpclient',
    'tbdancedancehttpclient',
    'public',
    'TB DanceDance Http Client',
    md5(random()::text || clock_timestamp()::text),
    '["ept:token","gt:password","scp:openid","scp:profile","scp:tbdancedanceapi.read"]'
)
ON CONFLICT ("ClientId") DO UPDATE
SET "ClientType" = EXCLUDED."ClientType",
    "DisplayName" = EXCLUDED."DisplayName",
    "ClientSecret" = NULL,
    "Permissions" = EXCLUDED."Permissions";
