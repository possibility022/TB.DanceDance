using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SupportSeasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SeasonClosed",
                schema: "access",
                table: "Groups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "SeasonEnd",
                schema: "access",
                table: "Groups",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "SeasonStart",
                schema: "access",
                table: "Groups",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.Sql("""
                                 -- PostgreSQL Script: Split Groups into Seasons and Reassign Videos
                                 -- This script creates new seasonal groups and reassigns videos based on RecordedDateTime
                                 
                                 -- Step 1: Create a temporary table to store the new season groups
                                 CREATE TEMP TABLE temp_season_groups (
                                                                          old_group_id UUID,
                                                                          old_group_name TEXT,
                                                                          season_year INTEGER,
                                                                          season_start DATE,
                                                                          season_end DATE,
                                                                          season_closed BOOLEAN,
                                                                          new_group_id UUID
                                 );
                                 
                                 -- Step 2: Generate season groups based on existing groups and video recording dates
                                 -- Assuming a dance season runs from September 1st to August 31st of the following year
                                 INSERT INTO temp_season_groups (old_group_id, old_group_name, season_year, season_start, season_end, season_closed, new_group_id)
                                 SELECT DISTINCT ON (old_group_id, season_year)
                                     old_group_id,
                                     old_group_name,
                                     season_year,
                                     season_start,
                                     season_end,
                                     CASE
                                         WHEN season_end < CURRENT_DATE THEN true
                                         ELSE false
                                         END as season_closed,
                                     gen_random_uuid() as new_group_id
                                 FROM (
                                          SELECT DISTINCT
                                              g."Id" as old_group_id,
                                              g."Name" as old_group_name,
                                              CASE
                                                  WHEN EXTRACT(MONTH FROM v."RecordedDateTime") >= 9 THEN EXTRACT(YEAR FROM v."RecordedDateTime")
                                                  ELSE EXTRACT(YEAR FROM v."RecordedDateTime") - 1
                                                  END as season_year,
                                              MAKE_DATE(
                                                      CASE
                                                          WHEN EXTRACT(MONTH FROM v."RecordedDateTime") >= 9 THEN EXTRACT(YEAR FROM v."RecordedDateTime")::INTEGER
                                                          ELSE EXTRACT(YEAR FROM v."RecordedDateTime")::INTEGER - 1
                                                          END,
                                                      9, 1
                                              ) as season_start,
                                              MAKE_DATE(
                                                      CASE
                                                          WHEN EXTRACT(MONTH FROM v."RecordedDateTime") >= 9 THEN EXTRACT(YEAR FROM v."RecordedDateTime")::INTEGER + 1
                                                          ELSE EXTRACT(YEAR FROM v."RecordedDateTime")::INTEGER
                                                          END,
                                                      8, 31
                                              ) as season_end
                                          FROM access."Groups" g
                                                   INNER JOIN access."SharedWith" sw ON sw."GroupId" = g."Id"
                                                   INNER JOIN video."Videos" v ON v."Id" = sw."VideoId"
                                      ) subquery
                                 ORDER BY old_group_id, season_year;
                                 
                                 -- DEBUG: Show what season groups will be created
                                 SELECT
                                     old_group_name as "Original Group",
                                     season_year || '/' || (season_year + 1) as "Season",
                                     season_start as "Season Start",
                                     season_end as "Season End",
                                     season_closed as "Season Closed",
                                     old_group_name || ' - Season ' || season_year || '/' || (season_year + 1) as "New Group Name"
                                 FROM temp_season_groups
                                 ORDER BY old_group_name, season_year;
                                 
                                 -- DEBUG: Count of new groups per original group
                                 SELECT
                                     old_group_name as "Original Group",
                                     COUNT(*) as "Number of Seasons"
                                 FROM temp_season_groups
                                 GROUP BY old_group_name
                                 ORDER BY old_group_name;
                                 
                                 -- DEBUG: Total count
                                 SELECT COUNT(*) as "Total New Groups" FROM temp_season_groups;
                                 
                                 -- Step 3: Create new season-based groups
                                 INSERT INTO access."Groups" ("Id", "Name", "SeasonStart", "SeasonEnd", "SeasonClosed")
                                 SELECT
                                     new_group_id,
                                     old_group_name,
                                     season_start,
                                     season_end,
                                     season_closed
                                 FROM temp_season_groups;
                                 
                                 -- Step 4: Copy group admins to new seasonal groups
                                 INSERT INTO access."GroupsAdmins" ("Id", "GroupId", "UserId")
                                 SELECT
                                     gen_random_uuid(),
                                     tsg.new_group_id,
                                     ga."UserId"
                                 FROM access."GroupsAdmins" ga
                                          INNER JOIN temp_season_groups tsg ON tsg.old_group_id = ga."GroupId";
                                 
                                 -- Step 5: Copy group assignments (memberships) to new seasonal groups
                                 INSERT INTO access."AssingedToGroups" ("Id", "GroupId", "UserId", "WhenJoined")
                                 SELECT
                                     gen_random_uuid(),
                                     tsg.new_group_id,
                                     atg."UserId",
                                     atg."WhenJoined"
                                 FROM access."AssingedToGroups" atg
                                          INNER JOIN temp_season_groups tsg ON tsg.old_group_id = atg."GroupId";
                                 
                                 -- Step 6: Reassign videos to the appropriate seasonal groups based on RecordedDateTime
                                 UPDATE access."SharedWith" sw
                                 SET "GroupId" = (
                                     SELECT tsg.new_group_id
                                     FROM temp_season_groups tsg
                                              INNER JOIN video."Videos" v ON v."Id" = sw."VideoId"
                                     WHERE sw."GroupId" = tsg.old_group_id
                                       AND v."RecordedDateTime" >= tsg.season_start
                                       AND v."RecordedDateTime" < tsg.season_end + INTERVAL '1 day'
                                     LIMIT 1
                                 )
                                 WHERE sw."GroupId" IN (SELECT old_group_id FROM temp_season_groups);
                                 
                                 -- Step 7: Copy group assignment requests to new seasonal groups
                                 INSERT INTO access."GroupAssigmentRequests" ("Id", "GroupId", "WhenJoined", "Approved", "UserId", "ManagedBy")
                                 SELECT
                                     gen_random_uuid(),
                                     tsg.new_group_id,
                                     gar."WhenJoined",
                                     gar."Approved",
                                     gar."UserId",
                                     gar."ManagedBy"
                                 FROM access."GroupAssigmentRequests" gar
                                          INNER JOIN temp_season_groups tsg ON tsg.old_group_id = gar."GroupId";
                                 
                                 -- Step 8: Delete old groups (CAREFUL - uncomment only after verifying the migration)
                                 DELETE FROM access."GroupAssigmentRequests" WHERE "GroupId" IN (SELECT old_group_id FROM temp_season_groups);
                                 DELETE FROM access."AssingedToGroups" WHERE "GroupId" IN (SELECT old_group_id FROM temp_season_groups);
                                 DELETE FROM access."GroupsAdmins" WHERE "GroupId" IN (SELECT old_group_id FROM temp_season_groups);
                                 DELETE FROM access."SharedWith" WHERE "GroupId" IN (SELECT old_group_id FROM temp_season_groups);
                                 DELETE FROM access."Groups" WHERE "Id" IN (SELECT old_group_id FROM temp_season_groups);
                                 
                                 -- Final Summary
                                 SELECT
                                     old_group_name as "Original Group",
                                     season_year || '/' || (season_year + 1) as "Season",
                                     season_start as "Season Start",
                                     season_end as "Season End",
                                     season_closed as "Season Closed",
                                     (SELECT COUNT(*) FROM access."SharedWith" WHERE "GroupId" = new_group_id) as "Videos Assigned"
                                 FROM temp_season_groups
                                 ORDER BY old_group_name, season_year;
                                 
                                 -- Cleanup temp table when done
                                 -- DROP TABLE temp_season_groups;
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeasonClosed",
                schema: "access",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "SeasonEnd",
                schema: "access",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "SeasonStart",
                schema: "access",
                table: "Groups");
        }
    }
}
