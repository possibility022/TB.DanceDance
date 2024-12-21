DO
$$
    DECLARE
        userId text = '31db6f5c-747d-4f75-9e5f-d953968c2fd2';
    BEGIN
        IF NOT EXISTS (select 1 from "Idp.Ident"."AspNetUsers" where "Id" = userId) THEN
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
            VALUES (DEFAULT, userId, 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
                    'testemail@email.com');
            INSERT INTO "Idp.Ident"."AspNetUserClaims" ("Id", "UserId", "ClaimType", "ClaimValue")
            VALUES (DEFAULT, userId, 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname', 'Tom');
            INSERT INTO "Idp.Ident"."AspNetUserClaims" ("Id", "UserId", "ClaimType", "ClaimValue")
            VALUES (DEFAULT, userId, 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname', 'Test');
        ELSE
            RAISE NOTICE 'Test user found. Skipping insertion';
        END IF;
    END
$$