DO
$$
DECLARE
    userId text = '31db6f5c-747d-4f75-9e5f-d953968c2fd2';
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "Idp.Ident"."AspNetUsers" WHERE "Id" = userId) THEN
        RAISE NOTICE 'Test user not found. Inserting into Idp.Ident.AspNetUsers';

        INSERT INTO "Idp.Ident"."AspNetUsers" ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
                                               "EmailConfirmed", "PasswordHash", "SecurityStamp",
                                               "ConcurrencyStamp",
                                               "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled",
                                               "LockoutEnd",
                                               "LockoutEnabled", "AccessFailedCount")
        VALUES (userId, 'testemail@email.com', 'TESTEMAIL@EMAIL.COM', 'testemail@email.com',
                'TESTEMAIL@EMAIL.COM', false,
                'AQAAAAIAAYagAAAAELUtg1+KSabFIDi3guZ/hVZfnGtMvbJEM7zvQnfuxFGkNi06ZapZ1lHloP9hmRBUmg==',
                '66PAUXBTJGDL526DJFEBRYG4P2F5ZR4W', 'fbba58d2-dfc7-4c7f-802e-b100a5dbb7b0', null, false, false,
                null,
                true, 0);

        INSERT INTO "Idp.Ident"."AspNetUserClaims" ("Id", "UserId", "ClaimType", "ClaimValue")
        VALUES (DEFAULT, userId, 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name', 'Tom Test');
        INSERT INTO "Idp.Ident"."AspNetUserClaims" ("Id", "UserId", "ClaimType", "ClaimValue")
        VALUES (DEFAULT, userId, 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress', 'testemail@email.com');
        INSERT INTO "Idp.Ident"."AspNetUserClaims" ("Id", "UserId", "ClaimType", "ClaimValue")
        VALUES (DEFAULT, userId, 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname', 'Tom');
        INSERT INTO "Idp.Ident"."AspNetUserClaims" ("Id", "UserId", "ClaimType", "ClaimValue")
        VALUES (DEFAULT, userId, 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname', 'Test');
    ELSE
        RAISE NOTICE 'Test user found. Skipping insertion';
    END IF;
END
$$;

-- Second dev user: testemail2@email.com / 1234 (same salted ASP.NET Identity hash as user 1;
-- the v3 hash is not bound to the username, so it stays valid for password "1234").
DO
$$
DECLARE
    userId text = '9f1c8e2a-4b6d-4c3e-8a7f-2d5e6b1a0c34';
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "Idp.Ident"."AspNetUsers" WHERE "Id" = userId) THEN
        RAISE NOTICE 'Second test user not found. Inserting into Idp.Ident.AspNetUsers';

        INSERT INTO "Idp.Ident"."AspNetUsers" ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
                                               "EmailConfirmed", "PasswordHash", "SecurityStamp",
                                               "ConcurrencyStamp",
                                               "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled",
                                               "LockoutEnd",
                                               "LockoutEnabled", "AccessFailedCount")
        VALUES (userId, 'testemail2@email.com', 'TESTEMAIL2@EMAIL.COM', 'testemail2@email.com',
                'TESTEMAIL2@EMAIL.COM', false,
                'AQAAAAIAAYagAAAAELUtg1+KSabFIDi3guZ/hVZfnGtMvbJEM7zvQnfuxFGkNi06ZapZ1lHloP9hmRBUmg==',
                '7QK3M5N2P8R4S6T9V1W3X5Y7Z2A4B6C8', 'a1b2c3d4-e5f6-47a8-9b0c-d1e2f3a4b5c6', null, false, false,
                null,
                true, 0);

        INSERT INTO "Idp.Ident"."AspNetUserClaims" ("Id", "UserId", "ClaimType", "ClaimValue")
        VALUES (DEFAULT, userId, 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name', 'Tom Test2');
        INSERT INTO "Idp.Ident"."AspNetUserClaims" ("Id", "UserId", "ClaimType", "ClaimValue")
        VALUES (DEFAULT, userId, 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress', 'testemail2@email.com');
        INSERT INTO "Idp.Ident"."AspNetUserClaims" ("Id", "UserId", "ClaimType", "ClaimValue")
        VALUES (DEFAULT, userId, 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname', 'Test');
        INSERT INTO "Idp.Ident"."AspNetUserClaims" ("Id", "UserId", "ClaimType", "ClaimValue")
        VALUES (DEFAULT, userId, 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname', 'Tom2');
    ELSE
        RAISE NOTICE 'Second test user found. Skipping insertion';
    END IF;
END
$$;
