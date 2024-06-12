# How to run

## Setup database
Use for example Docker, Podman and run image: docker.io/library/postgres
Example podman commands:
```
podman pull postgres
podman run -p 5432:5432 --name localpostgresdb -e POSTGRES_PASSWORD=rgFraWIuyxONqWCQ71wh -d postgres
```

### Run Migrations.
```
cd .\Instrastructure
dotnet ef database update --context PersistedGrantDbContext
dotnet ef database update --context ConfigurationDbContext
dotnet ef database update --context IdentityStoreContext
dotnet ef database update --context DanceDbContext
```

Setup data:
```
delete
from "IdpServer.Config"."ClientScopes";
delete
from "IdpServer.Config"."IdentityResourceClaims";
delete
from "IdpServer.Config"."IdentityResources";
delete
from "IdpServer.Config"."ApiScopes";
delete
from "IdpServer.Config"."ClientCorsOrigins";
delete
from "IdpServer.Config"."ClientRedirectUris";
delete
from "IdpServer.Config"."ClientSecrets";
delete
from "IdpServer.Config"."Clients";

INSERT INTO "IdpServer.Config"."Clients" ("Id", "Enabled", "ClientId", "ProtocolType", "RequireClientSecret",
                                          "ClientName", "Description", "ClientUri", "LogoUri", "RequireConsent",
                                          "AllowRememberConsent", "AlwaysIncludeUserClaimsInIdToken", "RequirePkce",
                                          "AllowPlainTextPkce", "RequireRequestObject", "AllowAccessTokensViaBrowser",
                                          "FrontChannelLogoutUri", "FrontChannelLogoutSessionRequired",
                                          "BackChannelLogoutUri", "BackChannelLogoutSessionRequired",
                                          "AllowOfflineAccess", "IdentityTokenLifetime",
                                          "AllowedIdentityTokenSigningAlgorithms", "AccessTokenLifetime",
                                          "AuthorizationCodeLifetime", "ConsentLifetime",
                                          "AbsoluteRefreshTokenLifetime", "SlidingRefreshTokenLifetime",
                                          "RefreshTokenUsage", "UpdateAccessTokenClaimsOnRefresh",
                                          "RefreshTokenExpiration", "AccessTokenType", "EnableLocalLogin",
                                          "IncludeJwtId", "AlwaysSendClientClaims", "ClientClaimsPrefix",
                                          "PairWiseSubjectSalt", "Created", "Updated", "LastAccessed",
                                          "UserSsoLifetime", "UserCodeType", "DeviceCodeLifetime", "NonEditable")
VALUES (1, true, 'tbdancedancefront', 'oidc', false, null, null, null, null, false, true, false, true, false, false,
        false, null, true, null, true, true, 300, null, 3600, 300, null, 2592000, 1296000, 1, false, 1, 0, true, true,
        false, 'client_', null, '2023-06-17 22:55:39.390503 +00:00', null, null, null, null, 300, false);
INSERT INTO "IdpServer.Config"."Clients" ("Id", "Enabled", "ClientId", "ProtocolType", "RequireClientSecret",
                                          "ClientName", "Description", "ClientUri", "LogoUri", "RequireConsent",
                                          "AllowRememberConsent", "AlwaysIncludeUserClaimsInIdToken", "RequirePkce",
                                          "AllowPlainTextPkce", "RequireRequestObject", "AllowAccessTokensViaBrowser",
                                          "FrontChannelLogoutUri", "FrontChannelLogoutSessionRequired",
                                          "BackChannelLogoutUri", "BackChannelLogoutSessionRequired",
                                          "AllowOfflineAccess", "IdentityTokenLifetime",
                                          "AllowedIdentityTokenSigningAlgorithms", "AccessTokenLifetime",
                                          "AuthorizationCodeLifetime", "ConsentLifetime",
                                          "AbsoluteRefreshTokenLifetime", "SlidingRefreshTokenLifetime",
                                          "RefreshTokenUsage", "UpdateAccessTokenClaimsOnRefresh",
                                          "RefreshTokenExpiration", "AccessTokenType", "EnableLocalLogin",
                                          "IncludeJwtId", "AlwaysSendClientClaims", "ClientClaimsPrefix",
                                          "PairWiseSubjectSalt", "Created", "Updated", "LastAccessed",
                                          "UserSsoLifetime", "UserCodeType", "DeviceCodeLifetime", "NonEditable")
VALUES (2, true, 'tbdancedanceconverter', 'oidc', true, 'Converter Service', null, null, null, false, true, false, true,
        false, false, false, null, true, null, true, false, 300, null, 3600, 300, null, 2592000, 1296000, 1, false, 1,
        0, true, true, false, 'client_', null, '2023-06-18 19:57:12.504811 +00:00', null, null, null, null, 300, false);

INSERT INTO "IdpServer.Config"."ClientSecrets" ("Id", "ClientId", "Description", "Value", "Expiration", "Type",
                                                "Created")
VALUES (1, 1, null, 'K7gNU3sdo+OL0wNhqoVWhr3g6s1xYv72ol/pe/Unols=', null, 'SharedSecret',
        '2023-06-18 19:57:12.505010 +00:00');
INSERT INTO "IdpServer.Config"."ClientSecrets" ("Id", "ClientId", "Description", "Value", "Expiration", "Type",
                                                "Created")
VALUES (2, 2, null, '2SmKENGwc1g33EvYXaxkGw887yekfl1TpU8vP1svz/o=', null, 'SharedSecret',
        '2023-06-17 22:55:39.655054 +00:00');

INSERT INTO "IdpServer.Config"."ClientRedirectUris" ("Id", "RedirectUri", "ClientId")
VALUES (1, 'http://localhost:3000/callback', 1);
INSERT INTO "IdpServer.Config"."ClientRedirectUris" ("Id", "RedirectUri", "ClientId")
VALUES (2, 'http://localhost:3000/', 1);

INSERT INTO "IdpServer.Config"."ClientCorsOrigins" ("Id", "Origin", "ClientId")
VALUES (1, 'http://localhost:3000', 1);

INSERT INTO "IdpServer.Config"."ApiScopes" ("Id", "Enabled", "Name", "DisplayName", "Description", "Required",
                                            "Emphasize", "ShowInDiscoveryDocument")
VALUES (1, true, 'tbdancedanceapi.read', 'TB DanceDance API - read', null, false, false, true);
INSERT INTO "IdpServer.Config"."ApiScopes" ("Id", "Enabled", "Name", "DisplayName", "Description", "Required",
                                            "Emphasize", "ShowInDiscoveryDocument")
VALUES (2, true, 'tbdancedanceapi.write', 'TB DanceDance API - write', null, false, false, true);
INSERT INTO "IdpServer.Config"."ApiScopes" ("Id", "Enabled", "Name", "DisplayName", "Description", "Required",
                                            "Emphasize", "ShowInDiscoveryDocument")
VALUES (3, true, 'tbdancedanceapi.convert', 'TB DanceDance API - converter', null, false, false, true);

INSERT INTO "IdpServer.Config"."IdentityResources" ("Id", "Enabled", "Name", "DisplayName", "Description", "Required",
                                                    "Emphasize", "ShowInDiscoveryDocument", "Created", "Updated",
                                                    "NonEditable")
VALUES (1, true, 'openid', 'Your user identifier', null, true, false, true, '2023-06-17 22:55:40.076041 +00:00', null,
        false);
INSERT INTO "IdpServer.Config"."IdentityResources" ("Id", "Enabled", "Name", "DisplayName", "Description", "Required",
                                                    "Emphasize", "ShowInDiscoveryDocument", "Created", "Updated",
                                                    "NonEditable")
VALUES (2, true, 'profile', 'User profile', 'Your user profile information (first name, last name, etc.)', false, true,
        true, '2023-06-17 22:55:40.128069 +00:00', null, false);
INSERT INTO "IdpServer.Config"."IdentityResources" ("Id", "Enabled", "Name", "DisplayName", "Description", "Required",
                                                    "Emphasize", "ShowInDiscoveryDocument", "Created", "Updated",
                                                    "NonEditable")
VALUES (3, true, 'email', 'Your email address', null, false, true, true, '2023-06-17 22:55:40.128621 +00:00', null,
        false);

INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (1, 1, 'sub');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (2, 2, 'name');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (3, 2, 'family_name');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (4, 2, 'given_name');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (5, 2, 'middle_name');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (6, 2, 'nickname');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (7, 2, 'preferred_username');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (8, 2, 'profile');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (9, 2, 'picture');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (10, 2, 'website');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (11, 2, 'gender');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (12, 2, 'birthdate');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (13, 2, 'zoneinfo');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (14, 2, 'locale');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (15, 2, 'updated_at');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (16, 3, 'email');
INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
VALUES (17, 3, 'email_verified');


INSERT INTO "IdpServer.Config"."ClientScopes" ("Id", "Scope", "ClientId")
VALUES (1, 'tbdancedanceapi.read', 1);
INSERT INTO "IdpServer.Config"."ClientScopes" ("Id", "Scope", "ClientId")
VALUES (2, 'openid', 1);
INSERT INTO "IdpServer.Config"."ClientScopes" ("Id", "Scope", "ClientId")
VALUES (3, 'profile', 1);
INSERT INTO "IdpServer.Config"."ClientScopes" ("Id", "Scope", "ClientId")
VALUES (4, 'tbdancedanceapi.read', 1);
INSERT INTO "IdpServer.Config"."ClientScopes" ("Id", "Scope", "ClientId")
VALUES (5, 'openid', 2);
INSERT INTO "IdpServer.Config"."ClientScopes" ("Id", "Scope", "ClientId")
VALUES (6, 'profile', 2);
INSERT INTO "IdpServer.Config"."ClientScopes" ("Id", "Scope", "ClientId")
VALUES (7, 'tbdancedanceapi.convert', 2);

INSERT INTO "IdpServer.Config"."ClientGrantTypes" ("Id", "GrantType", "ClientId")
VALUES (1, 'authorization_code', 1);
INSERT INTO "IdpServer.Config"."ClientGrantTypes" ("Id", "GrantType", "ClientId")
VALUES (3, 'client_credentials', 2);


```

## Setup blob container
```
docker pull mcr.microsoft.com/azure-storage/azurite
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 --name tbdanceblobcontainer mcr.microsoft.com/azure-storage/azurite
```

Then setup cors policy for azure blob container. (not docker container)


## Build and Run

Build and run API
```
cd .\TB.DanceDance.API
dotnet run
```

On 2nd terminal - build and run frontend
```
cd .\tb.dancedance.frontend
npm install
npm run start

```

