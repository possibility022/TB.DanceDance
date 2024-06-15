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

# Try to login

Try to create a user and login to application.
Next steps require at least one user.

## Setup videos, events and groups


```

-------------------------------------------------------------------
-- MAKE SURE YOU CAN CREATE ACCOUNT AND LOG IN                   --
-- NEXT PART OF THIS SCRIPT REQUIRE AT LEAST ONE USER            --
-------------------------------------------------------------------

DO $$
DECLARE
userId text;
BEGIN

    select "Id" into userId from access."Users" Limit 1;
    INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('a48b84e0-cc0e-4557-a9e8-25d96aed36e8', '82b39019-d983-44ce-924a-f3fa2f651261', '20240106_131537.mp4', userId, '2024-01-06 12:19:17.000000 +00:00', '2024-01-09 20:44:54.684892 +00:00', '0 years 0 mons 0 days 0 hours 3 mins 40.402 secs', true, '20240106_131537.mp4', '2024-01-10 20:45:49.289257 +00:00', '369b58c5-226d-4bb7-9052-389a9ce52001');
    INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('34bfea40-4ea3-40b3-9f22-444c2416c74a', '412fcbe4-9dcc-435c-901f-58c9d71d3972', 'G4 - Ania i Damian - Free Spin + Duck', userId, '2024-01-28 17:21:12.000000 +00:00', '2024-01-28 17:30:08.564591 +00:00', '0 years 0 mons 0 days 0 hours 6 mins 30.766 secs', true, '20240128_173021~2.mp4', '2024-01-29 17:44:30.785360 +00:00', 'e1a92e75-82cb-4777-8efc-1802df6ed03a');
    INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('43e4d723-5363-4b38-8ccc-182ca2a4cde2', 'fd040bc4-3d09-4b42-829f-b036a1875d53', '20240107_143311.mp4', userId, '2024-01-07 13:35:11.000000 +00:00', '2024-01-08 21:50:05.623824 +00:00', '0 years 0 mons 0 days 0 hours 1 mins 59.114 secs', true, '20240107_143311.mp4', '2024-01-10 18:33:00.568537 +00:00', 'c8b32519-2149-4194-82b7-62c521701144');
    INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('c4029eb1-23ad-417b-9a3b-1b2ad4751c0b', 'abab3514-39f0-47d6-ba16-f8ec6b532db4', 'Promenada', userId, '2024-03-13 18:02:29.000000 +00:00', '2024-03-16 20:14:21.957149 +00:00', '0 years 0 mons 0 days 0 hours 1 mins 47.822 secs', true, '20240313_190040.mp4', '2024-03-17 20:36:19.362409 +00:00', 'c6f3ad7c-c9ad-4351-9130-cf0cafc537b3');
    INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('3af95d10-6f34-405f-8a0a-a6b1165b347a', '9ae02f12-e123-4548-931d-c5281b922bc5', 'Nie ma z³ej nogi! ', userId, '2023-12-20 18:09:13.000000 +00:00', '2023-12-22 21:53:42.231672 +00:00', '0 years 0 mons 0 days 0 hours 5 mins 21.331 secs', true, '20231220_190350.mp4', '2024-01-04 21:48:03.234641 +00:00', 'e7d2e188-08fb-4e91-a733-6781c7e6c117');
    INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('96102f89-e4ad-41ca-8849-6383b01cdc91', 'f91bded0-8de3-4cfd-bd79-1f6dbe5de5e6', 'LVL2 - Jordan Tatiana - Upper level basics', userId, '2024-01-07 17:20:49.000000 +00:00', '2024-01-08 21:51:19.751431 +00:00', '0 years 0 mons 0 days 0 hours 3 mins 17.957 secs', true, '20240107_181730.mp4', '2024-01-10 17:40:55.849553 +00:00', '65c4fb12-a62c-4a25-91cf-d4d564c9fffd');
    INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('9b799583-73bf-4e62-a260-08c59b47f6d8', '161ddf84-d0a9-488c-9f9a-948e79687fe7', 'lvl 2 - Jakub & Emeline - Slingshot', userId, '2024-01-06 13:35:17.000000 +00:00', '2024-01-08 21:49:14.823120 +00:00', '0 years 0 mons 0 days 0 hours 4 mins 52.131 secs', true, '20240106_143024.mp4', '2024-01-10 18:48:34.117999 +00:00', '0e545e5c-aa9a-4858-8c95-35c6eeb6f2a0');
    INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('5971a544-17c6-4aac-ba60-32d9f52aea5e', '0dda7622-cca4-4918-8aaa-30edd8d623b8', '20231221_225245.mp4', userId, '2023-12-21 21:57:57.000000 +00:00', '2023-12-22 21:54:45.942340 +00:00', '0 years 0 mons 0 days 0 hours 5 mins 10.755 secs', true, '20231221_225245.mp4', '2024-01-05 01:35:49.663196 +00:00', 'fba85976-f61c-437e-8a7b-06fd0d13a9b6');

    INSERT INTO access."Events" ("Id", "Name", "Date", "Type", "Owner") VALUES ('25fac427-1332-4fd8-98b1-5ae572b8da01', 'Warsztaty - Rama - 2022', '2022-10-01 00:00:00.000000 +00:00', 0, userId);
    INSERT INTO access."Events" ("Id", "Name", "Date", "Type", "Owner") VALUES ('6e78f313-7fcc-497e-b8c4-c87a6a9551e9', 'Warsztaty - Footworki - 2022', '2022-07-02 00:00:00.000000 +00:00', 0, userId);

    INSERT INTO access."Groups" ("Id", "Name") VALUES ('2a7554e4-0dc3-4f92-be4c-e4463adf1cee', 'Czwartki 21:30');
    INSERT INTO access."Groups" ("Id", "Name") VALUES ('7c30e3cc-e6d8-4cf7-8b64-eba68efa5366', 'Œrody 18:00');

    INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('07150966-57dc-441c-9ec4-43fe2952d90d', 'a48b84e0-cc0e-4557-a9e8-25d96aed36e8', userId, null, '2a7554e4-0dc3-4f92-be4c-e4463adf1cee');
    INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('076c27fa-702f-497a-8fb6-b609f06d493a', '34bfea40-4ea3-40b3-9f22-444c2416c74a', userId, null, '2a7554e4-0dc3-4f92-be4c-e4463adf1cee');
    INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('08d447aa-c751-46db-b36a-c119c5f0323e', '43e4d723-5363-4b38-8ccc-182ca2a4cde2', userId, null, '7c30e3cc-e6d8-4cf7-8b64-eba68efa5366');
    INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('095b900e-2f5a-4ae5-8392-5a38f0d71f2b', '43e4d723-5363-4b38-8ccc-182ca2a4cde2', userId, null, '7c30e3cc-e6d8-4cf7-8b64-eba68efa5366');
    INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('0ab36da8-c215-456a-a1a9-4516dc057844', '3af95d10-6f34-405f-8a0a-a6b1165b347a', userId, '25fac427-1332-4fd8-98b1-5ae572b8da01', null);
    INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('118857fc-f8a4-489f-b00f-85e04897df89', '96102f89-e4ad-41ca-8849-6383b01cdc91', userId, '25fac427-1332-4fd8-98b1-5ae572b8da01', null);
    INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('120496e3-ca42-4712-97a3-823da840ebaa', '9b799583-73bf-4e62-a260-08c59b47f6d8', userId, '6e78f313-7fcc-497e-b8c4-c87a6a9551e9', null);
    INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('2a4c882a-cda7-4f4e-b4b2-92556f768516', '5971a544-17c6-4aac-ba60-32d9f52aea5e', userId, '6e78f313-7fcc-497e-b8c4-c87a6a9551e9', null);

    INSERT INTO access."AssingedToEvents" ("Id", "EventId", "UserId") VALUES ('97a1b862-f25a-4120-895a-91d3a0efd6b8', '25fac427-1332-4fd8-98b1-5ae572b8da01', userId);
    INSERT INTO access."AssingedToEvents" ("Id", "EventId", "UserId") VALUES ('ace4bdeb-b325-4e95-840e-822c4c47a456', '6e78f313-7fcc-497e-b8c4-c87a6a9551e9', userId);

    INSERT INTO access."AssingedToGroups" ("Id", "GroupId", "UserId", "WhenJoined") VALUES ('9e9d8576-8020-40fa-a8cd-e5538e8dd602', '2a7554e4-0dc3-4f92-be4c-e4463adf1cee', userId, '2022-02-01 19:28:47.258000 +00:00');
    INSERT INTO access."AssingedToGroups" ("Id", "GroupId", "UserId", "WhenJoined") VALUES ('909882e5-6de7-4fe5-b895-ffb599f95b9c', '7c30e3cc-e6d8-4cf7-8b64-eba68efa5366', userId, '2022-02-01 19:28:47.258000 +00:00');

END $$;
```

## Setup videos in blob storage

Last part to do is to upload some videos to blob storage to be able to view them in application. Run script below in nodejs to do that.

```
// require package to install
// npm install @azure/storage-blob --no-save

const { BlobServiceClient, StorageSharedKeyCredential } = require('@azure/storage-blob');
const { get } = require('https');

const accountName = 'devstoreaccount1';
const accountKey = 'Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==';
const containerName = 'videos';

const sharedKeyCredential = new StorageSharedKeyCredential(accountName, accountKey);
const blobServiceClient = new BlobServiceClient(
  `http://127.0.0.1:10000/${accountName}`,
  sharedKeyCredential
);

async function createBlob(blobName, content) {
  const containerClient = blobServiceClient.getContainerClient(containerName);
  const blockBlobClient = containerClient.getBlockBlobClient(blobName);

  const uploadBlobResponse = await blockBlobClient.upload(content, content.length);
  console.log(`Upload block blob ${blobName} successfully`, uploadBlobResponse.requestId);
}

function readStreamAndUpload(res, blobName){
  const { statusCode } = res;
  if (statusCode !== 200) {
    console.error(`Request Failed.\nStatus Code: ${statusCode}`);
    res.resume(); // Consume response data to free up memory
    return;
  }

  let rawData = [];
  res.on('data', (chunk) => {
    rawData.push(chunk);
  });

  res.on('end', () => {
    try {
      const buffer = Buffer.concat(rawData);
      // Now buffer contains the content as bytes
      createBlob(blobName, buffer)
    } catch (e) {
      console.error(e.message);
    }
  });
}

get('https://sample-videos.com/video321/mp4/720/big_buck_bunny_720p_1mb.mp4',{}, (m) => readStreamAndUpload(m, '82b39019-d983-44ce-924a-f3fa2f651261'))
get('https://sample-videos.com/video321/mp4/480/big_buck_bunny_480p_1mb.mp4',{}, (m) => readStreamAndUpload(m, '412fcbe4-9dcc-435c-901f-58c9d71d3972'))
get('https://sample-videos.com/video321/mp4/240/big_buck_bunny_240p_1mb.mp4',{}, (m) => readStreamAndUpload(m, 'fd040bc4-3d09-4b42-829f-b036a1875d53'))
get('https://sample-videos.com/video321/mp4/360/big_buck_bunny_360p_2mb.mp4',{}, (m) => readStreamAndUpload(m, 'abab3514-39f0-47d6-ba16-f8ec6b532db4'))
get('https://sample-videos.com/video321/mp4/480/big_buck_bunny_480p_2mb.mp4',{}, (m) => readStreamAndUpload(m, '9ae02f12-e123-4548-931d-c5281b922bc5'))
get('https://sample-videos.com/video321/mp4/480/big_buck_bunny_480p_5mb.mp4',{}, (m) => readStreamAndUpload(m, 'f91bded0-8de3-4cfd-bd79-1f6dbe5de5e6'))
get('https://sample-videos.com/video321/mp4/240/big_buck_bunny_240p_2mb.mp4',{}, (m) => readStreamAndUpload(m, '161ddf84-d0a9-488c-9f9a-948e79687fe7'))
get('https://sample-videos.com/video321/mp4/240/big_buck_bunny_240p_10mb.mp4',{}, (m) => readStreamAndUpload(m, '0dda7622-cca4-4918-8aaa-30edd8d623b8'))


```

