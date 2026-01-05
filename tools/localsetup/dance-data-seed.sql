DO $$
    DECLARE
        userId text;
    BEGIN
        IF NOT EXISTS (SELECT 1 from access."Users" where "Id" = userId)
        THEN
        RAISE NOTICE 'Dance user NOT found. Inserting dance data.';
        userId = '31db6f5c-747d-4f75-9e5f-d953968c2fd2';
        INSERT INTO access."Users" ("Id", "FirstName", "LastName", "Email") VALUES (userId, 'Tom', 'B', 'testemail@email.com');
        INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('a48b84e0-cc0e-4557-a9e8-25d96aed36e8', '82b39019-d983-44ce-924a-f3fa2f651261', '20240106_131537.mp4', userId, '2024-01-06 12:19:17.000000 +00:00', '2024-01-09 20:44:54.684892 +00:00', '0 years 0 mons 0 days 0 hours 3 mins 40.402 secs', true, '20240106_131537.mp4', '2024-01-10 20:45:49.289257 +00:00', '369b58c5-226d-4bb7-9052-389a9ce52001');
        INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('34bfea40-4ea3-40b3-9f22-444c2416c74a', '412fcbe4-9dcc-435c-901f-58c9d71d3972', 'G4 - Ania i Damian - Free Spin + Duck', userId, '2024-01-28 17:21:12.000000 +00:00', '2024-01-28 17:30:08.564591 +00:00', '0 years 0 mons 0 days 0 hours 6 mins 30.766 secs', true, '20240128_173021~2.mp4', '2024-01-29 17:44:30.785360 +00:00', 'e1a92e75-82cb-4777-8efc-1802df6ed03a');
        INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('43e4d723-5363-4b38-8ccc-182ca2a4cde2', 'fd040bc4-3d09-4b42-829f-b036a1875d53', '20240107_143311.mp4', userId, '2024-01-07 13:35:11.000000 +00:00', '2024-01-08 21:50:05.623824 +00:00', '0 years 0 mons 0 days 0 hours 1 mins 59.114 secs', true, '20240107_143311.mp4', '2024-01-10 18:33:00.568537 +00:00', 'c8b32519-2149-4194-82b7-62c521701144');
        INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('c4029eb1-23ad-417b-9a3b-1b2ad4751c0b', 'abab3514-39f0-47d6-ba16-f8ec6b532db4', 'Promenada', userId, '2024-03-13 18:02:29.000000 +00:00', '2024-03-16 20:14:21.957149 +00:00', '0 years 0 mons 0 days 0 hours 1 mins 47.822 secs', true, '20240313_190040.mp4', '2024-03-17 20:36:19.362409 +00:00', 'c6f3ad7c-c9ad-4351-9130-cf0cafc537b3');
        INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('3af95d10-6f34-405f-8a0a-a6b1165b347a', '9ae02f12-e123-4548-931d-c5281b922bc5', 'Nie ma złej nogi! ', userId, '2023-12-20 18:09:13.000000 +00:00', '2023-12-22 21:53:42.231672 +00:00', '0 years 0 mons 0 days 0 hours 5 mins 21.331 secs', true, '20231220_190350.mp4', '2024-01-04 21:48:03.234641 +00:00', 'e7d2e188-08fb-4e91-a733-6781c7e6c117');
        INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('96102f89-e4ad-41ca-8849-6383b01cdc91', 'f91bded0-8de3-4cfd-bd79-1f6dbe5de5e6', 'LVL2 - Jordan Tatiana - Upper level basics', userId, '2024-01-07 17:20:49.000000 +00:00', '2024-01-08 21:51:19.751431 +00:00', '0 years 0 mons 0 days 0 hours 3 mins 17.957 secs', true, '20240107_181730.mp4', '2024-01-10 17:40:55.849553 +00:00', '65c4fb12-a62c-4a25-91cf-d4d564c9fffd');
        INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('9b799583-73bf-4e62-a260-08c59b47f6d8', '161ddf84-d0a9-488c-9f9a-948e79687fe7', 'lvl 2 - Jakub & Emeline - Slingshot', userId, '2024-01-06 13:35:17.000000 +00:00', '2024-01-08 21:49:14.823120 +00:00', '0 years 0 mons 0 days 0 hours 4 mins 52.131 secs', true, '20240106_143024.mp4', '2024-01-10 18:48:34.117999 +00:00', '0e545e5c-aa9a-4858-8c95-35c6eeb6f2a0');
        INSERT INTO video."Videos" ("Id", "BlobId", "Name", "UploadedBy", "RecordedDateTime", "SharedDateTime", "Duration", "Converted", "FileName", "LockedTill", "SourceBlobId") VALUES ('5971a544-17c6-4aac-ba60-32d9f52aea5e', '0dda7622-cca4-4918-8aaa-30edd8d623b8', '20231221_225245.mp4', userId, '2023-12-21 21:57:57.000000 +00:00', '2023-12-22 21:54:45.942340 +00:00', '0 years 0 mons 0 days 0 hours 5 mins 10.755 secs', true, '20231221_225245.mp4', '2024-01-05 01:35:49.663196 +00:00', 'fba85976-f61c-437e-8a7b-06fd0d13a9b6');

        INSERT INTO access."Events" ("Id", "Name", "Date", "Type", "Owner") VALUES ('25fac427-1332-4fd8-98b1-5ae572b8da01', 'Warsztaty - Rama - 2022', '2022-10-01 00:00:00.000000 +00:00', 0, userId);
        INSERT INTO access."Events" ("Id", "Name", "Date", "Type", "Owner") VALUES ('6e78f313-7fcc-497e-b8c4-c87a6a9551e9', 'Warsztaty - Footworki - 2022', '2022-07-02 00:00:00.000000 +00:00', 0, userId);

        INSERT INTO access."Groups" ("Id", "Name") VALUES ('2a7554e4-0dc3-4f92-be4c-e4463adf1cee', 'Czwartki 21:30');
        INSERT INTO access."Groups" ("Id", "Name") VALUES ('7c30e3cc-e6d8-4cf7-8b64-eba68efa5366', 'Środy 18:00');

        INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('07150966-57dc-441c-9ec4-43fe2952d90d', 'a48b84e0-cc0e-4557-a9e8-25d96aed36e8', userId, null, '2a7554e4-0dc3-4f92-be4c-e4463adf1cee');
        INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('076c27fa-702f-497a-8fb6-b609f06d493a', '34bfea40-4ea3-40b3-9f22-444c2416c74a', userId, null, '2a7554e4-0dc3-4f92-be4c-e4463adf1cee');
        INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('08d447aa-c751-46db-b36a-c119c5f0323e', '43e4d723-5363-4b38-8ccc-182ca2a4cde2', userId, null, '7c30e3cc-e6d8-4cf7-8b64-eba68efa5366');
        INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('095b900e-2f5a-4ae5-8392-5a38f0d71f2b', 'c4029eb1-23ad-417b-9a3b-1b2ad4751c0b', userId, null, '7c30e3cc-e6d8-4cf7-8b64-eba68efa5366');
        INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('0ab36da8-c215-456a-a1a9-4516dc057844', '3af95d10-6f34-405f-8a0a-a6b1165b347a', userId, '25fac427-1332-4fd8-98b1-5ae572b8da01', null);
        INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('118857fc-f8a4-489f-b00f-85e04897df89', '96102f89-e4ad-41ca-8849-6383b01cdc91', userId, '25fac427-1332-4fd8-98b1-5ae572b8da01', null);
        INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('120496e3-ca42-4712-97a3-823da840ebaa', '9b799583-73bf-4e62-a260-08c59b47f6d8', userId, '6e78f313-7fcc-497e-b8c4-c87a6a9551e9', null);
        INSERT INTO access."SharedWith" ("Id", "VideoId", "UserId", "EventId", "GroupId") VALUES ('2a4c882a-cda7-4f4e-b4b2-92556f768516', '5971a544-17c6-4aac-ba60-32d9f52aea5e', userId, '6e78f313-7fcc-497e-b8c4-c87a6a9551e9', null);

        INSERT INTO access."AssingedToEvents" ("Id", "EventId", "UserId") VALUES ('97a1b862-f25a-4120-895a-91d3a0efd6b8', '25fac427-1332-4fd8-98b1-5ae572b8da01', userId);
        INSERT INTO access."AssingedToEvents" ("Id", "EventId", "UserId") VALUES ('ace4bdeb-b325-4e95-840e-822c4c47a456', '6e78f313-7fcc-497e-b8c4-c87a6a9551e9', userId);

        INSERT INTO access."AssingedToGroups" ("Id", "GroupId", "UserId", "WhenJoined") VALUES ('9e9d8576-8020-40fa-a8cd-e5538e8dd602', '2a7554e4-0dc3-4f92-be4c-e4463adf1cee', userId, '2022-02-01 19:28:47.258000 +00:00');
        INSERT INTO access."AssingedToGroups" ("Id", "GroupId", "UserId", "WhenJoined") VALUES ('909882e5-6de7-4fe5-b895-ffb599f95b9c', '7c30e3cc-e6d8-4cf7-8b64-eba68efa5366', userId, '2022-02-01 19:28:47.258000 +00:00');
        ELSE
            RAISE NOTICE 'Dance user found. Skipping insert.';
        end if;        

    END $$;
