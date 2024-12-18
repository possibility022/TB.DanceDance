DO
$$
    BEGIN
        IF NOT EXISTS (select 1 from "IdpServer.Config"."ApiScopes" where "Name" = 'tbdancedanceapi.read') THEN
            RAISE NOTICE 'Inserting initial data for oauth';

            -- Inserts API Scopes
            INSERT INTO "IdpServer.Config"."ApiScopes" ("Id", "Enabled", "Name", "DisplayName", "Description",
                                                        "Required",
                                                        "Emphasize", "ShowInDiscoveryDocument")
            VALUES (DEFAULT, true, 'tbdancedanceapi.read', 'TB DanceDance API - read', null, false, false, true);
            INSERT INTO "IdpServer.Config"."ApiScopes" ("Id", "Enabled", "Name", "DisplayName", "Description",
                                                        "Required",
                                                        "Emphasize", "ShowInDiscoveryDocument")
            VALUES (DEFAULT, true, 'tbdancedanceapi.write', 'TB DanceDance API - write', null, false, false, true);
            INSERT INTO "IdpServer.Config"."ApiScopes" ("Id", "Enabled", "Name", "DisplayName", "Description",
                                                        "Required",
                                                        "Emphasize", "ShowInDiscoveryDocument")
            VALUES (DEFAULT, true, 'tbdancedanceapi.convert', 'TB DanceDance API - converter', null, false, false,
                    true);

            -- Insert identity resources
            INSERT INTO "IdpServer.Config"."IdentityResources" ("Id", "Enabled", "Name", "DisplayName", "Description",
                                                                "Required",
                                                                "Emphasize", "ShowInDiscoveryDocument", "Created",
                                                                "Updated",
                                                                "NonEditable")
            VALUES (DEFAULT, true, 'openid', 'Your user identifier', null, true, false, true,
                    '2023-06-17 22:55:40.076041 +00:00', null,
                    false);
            INSERT INTO "IdpServer.Config"."IdentityResources" ("Id", "Enabled", "Name", "DisplayName", "Description",
                                                                "Required",
                                                                "Emphasize", "ShowInDiscoveryDocument", "Created",
                                                                "Updated",
                                                                "NonEditable")
            VALUES (DEFAULT, true, 'profile', 'User profile',
                    'Your user profile information (first name, last name, etc.)', false, true,
                    true, '2023-06-17 22:55:40.128069 +00:00', null, false);
            INSERT INTO "IdpServer.Config"."IdentityResources" ("Id", "Enabled", "Name", "DisplayName", "Description",
                                                                "Required",
                                                                "Emphasize", "ShowInDiscoveryDocument", "Created",
                                                                "Updated",
                                                                "NonEditable")
            VALUES (DEFAULT, true, 'email', 'Your email address', null, false, true, true,
                    '2023-06-17 22:55:40.128621 +00:00', null,
                    false);

            -- Insert claims
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 1, 'sub');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 2, 'name');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 2, 'family_name');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 2, 'given_name');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 2, 'middle_name');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 2, 'nickname');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 2, 'preferred_username');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 2, 'profile');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 2, 'picture');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 2, 'website');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 2, 'gender');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 2, 'birthdate');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 2, 'zoneinfo');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 2, 'locale');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 2, 'updated_at');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 3, 'email');
            INSERT INTO "IdpServer.Config"."IdentityResourceClaims" ("Id", "IdentityResourceId", "Type")
            VALUES (DEFAULT, 3, 'email_verified');

        else
            RAISE NOTICE 'tbdancedance.read api scope found. Skipping oauth initialization.';
        end if;
    end;
$$;


DO
$$
    DECLARE
        newRecordId INT;
    BEGIN
        IF
            NOT EXISTS (SELECT 1 FROM "IdpServer.Config"."Clients" WHERE "ClientId" = 'tbdancedancefront') THEN
            RAISE NOTICE 'Inserting data for tbdancedancefront client.';
            INSERT INTO "IdpServer.Config"."Clients" ("Id", "Enabled", "ClientId", "ProtocolType",
                                                      "RequireClientSecret",
                                                      "ClientName", "Description", "ClientUri", "LogoUri",
                                                      "RequireConsent",
                                                      "AllowRememberConsent", "AlwaysIncludeUserClaimsInIdToken",
                                                      "RequirePkce",
                                                      "AllowPlainTextPkce", "RequireRequestObject",
                                                      "AllowAccessTokensViaBrowser",
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
                                                      "UserSsoLifetime", "UserCodeType", "DeviceCodeLifetime",
                                                      "NonEditable")
            VALUES (DEFAULT, true, 'tbdancedancefront', 'oidc', false, null, null, null, null, false, true, false, true,
                    false,
                    false,
                    false, null, true, null, true, true, 300, null, 3600, 300, null, 2592000, 1296000, 1, false, 1, 0,
                    true, true,
                    false, 'client_', null, '2023-06-17 22:55:39.390503 +00:00', null, null, null, null, 300, false)
            returning "Id" into newRecordId;

            INSERT INTO "IdpServer.Config"."ClientSecrets" ("Id", "ClientId", "Description", "Value", "Expiration",
                                                            "Type",
                                                            "Created")
            VALUES (DEFAULT, newRecordId, null, 'K7gNU3sdo+OL0wNhqoVWhr3g6s1xYv72ol/pe/Unols=', null, 'SharedSecret',
                    '2023-06-18 19:57:12.505010 +00:00');


            -- Insert Client Redirects
            INSERT INTO "IdpServer.Config"."ClientRedirectUris" ("Id", "RedirectUri", "ClientId")
            VALUES (DEFAULT, 'http://localhost:3000/callback', newRecordId);
            INSERT INTO "IdpServer.Config"."ClientRedirectUris" ("Id", "RedirectUri", "ClientId")
            VALUES (DEFAULT, 'http://localhost:3000/', newRecordId);
            INSERT INTO "IdpServer.Config"."ClientCorsOrigins" ("Id", "Origin", "ClientId")
            VALUES (DEFAULT, 'http://localhost:3000', newRecordId);

            -- Insert Client Scopes
            INSERT INTO "IdpServer.Config"."ClientGrantTypes" ("Id", "GrantType", "ClientId")
            VALUES (DEFAULT, 'authorization_code', newRecordId);

            -- Insert Scopes
            INSERT INTO "IdpServer.Config"."ClientScopes" ("Id", "Scope", "ClientId")
            VALUES (DEFAULT, 'tbdancedanceapi.read', newRecordId);
            INSERT INTO "IdpServer.Config"."ClientScopes" ("Id", "Scope", "ClientId")
            VALUES (DEFAULT, 'openid', newRecordId);
            INSERT INTO "IdpServer.Config"."ClientScopes" ("Id", "Scope", "ClientId")
            VALUES (DEFAULT, 'profile', newRecordId);
        ELSE
            RAISE NOTICE 'Found client id tbdancedancefront. Skipping initialization.';
        END IF;
    END
$$;

-- INSERT CLIENT FOR tbdancedanceconverter
DO
$$
    DECLARE
        newRecordId INT;
    BEGIN
        IF
            NOT EXISTS (SELECT 1 FROM "IdpServer.Config"."Clients" WHERE "ClientId" = 'tbdancedanceconverter') THEN
            RAISE NOTICE 'Inserting tbdancedanceconverter client.';
            INSERT INTO "IdpServer.Config"."Clients" ("Id", "Enabled", "ClientId", "ProtocolType",
                                                      "RequireClientSecret",
                                                      "ClientName", "Description", "ClientUri", "LogoUri",
                                                      "RequireConsent",
                                                      "AllowRememberConsent", "AlwaysIncludeUserClaimsInIdToken",
                                                      "RequirePkce",
                                                      "AllowPlainTextPkce", "RequireRequestObject",
                                                      "AllowAccessTokensViaBrowser",
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
                                                      "UserSsoLifetime", "UserCodeType", "DeviceCodeLifetime",
                                                      "NonEditable")
            VALUES (DEFAULT, true, 'tbdancedanceconverter', 'oidc', true, 'Converter Service', null, null, null, false,
                    true,
                    false, true,
                    false, false, false, null, true, null, true, false, 300, null, 3600, 300, null, 2592000, 1296000, 1,
                    false, 1,
                    0, true, true, false, 'client_', null, '2023-06-18 19:57:12.504811 +00:00', null, null, null, null,
                    300, false)
            RETURNING "Id" into newRecordId;

            INSERT INTO "IdpServer.Config"."ClientSecrets" ("Id", "ClientId", "Description", "Value", "Expiration",
                                                            "Type",
                                                            "Created")
            VALUES (DEFAULT, newRecordId, null, '2SmKENGwc1g33EvYXaxkGw887yekfl1TpU8vP1svz/o=', null, 'SharedSecret',
                    '2023-06-17 22:55:39.655054 +00:00');

            INSERT INTO "IdpServer.Config"."ClientScopes" ("Id", "Scope", "ClientId")
            VALUES (DEFAULT, 'openid', newRecordId);
            INSERT INTO "IdpServer.Config"."ClientScopes" ("Id", "Scope", "ClientId")
            VALUES (DEFAULT, 'profile', newRecordId);
            INSERT INTO "IdpServer.Config"."ClientScopes" ("Id", "Scope", "ClientId")
            VALUES (DEFAULT, 'tbdancedanceapi.convert', newRecordId);

            INSERT INTO "IdpServer.Config"."ClientGrantTypes" ("Id", "GrantType", "ClientId")
            VALUES (DEFAULT, 'client_credentials', 2);
        ELSE
            RAISE NOTICE 'Found client id tbdancedancefront. Skipping initialization.';
        END IF;
    END
$$;
