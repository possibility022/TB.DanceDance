using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TB.DanceDance.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class StoringUserInformations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.Sql("""
                INSERT INTO access."Users"("Id", "FirstName", "LastName", "Email")
                SELECT DISTINCT "UserId", '', '', '' FROM (
                    SELECT "UserId" FROM access."AssingedToEvents"
                    UNION
                    SELECT "UserId" FROM access."AssingedToGroups"
                    UNION
                    SELECT "UserId" FROM access."EventAssigmentRequests"
                    UNION
                    SELECT "UserId" FROM access."GroupAssigmentRequests"
                    UNION
                    SELECT "UserId" from access."SharedWith"
                ) AS CombinedUsers;
                """);

            migrationBuilder.AddColumn<string>(
                name: "Owner",
                schema: "access",
                table: "Events",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                update access."Events" set "Owner" = '104737052481294069059';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Owner",
                schema: "access",
                table: "Events",
                type: "text",
                nullable: false
                );

            migrationBuilder.CreateTable(
                name: "GroupsAdmins",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupsAdmins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupsAdmins_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "access",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupsAdmins_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "access",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SharedWith_UserId",
                schema: "access",
                table: "SharedWith",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Owner",
                schema: "access",
                table: "Events",
                column: "Owner");

            migrationBuilder.CreateIndex(
                name: "IX_AssingedToGroups_UserId",
                schema: "access",
                table: "AssingedToGroups",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssingedToEvents_UserId",
                schema: "access",
                table: "AssingedToEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupsAdmins_GroupId",
                schema: "access",
                table: "GroupsAdmins",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupsAdmins_UserId",
                schema: "access",
                table: "GroupsAdmins",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssingedToEvents_Users_UserId",
                schema: "access",
                table: "AssingedToEvents",
                column: "UserId",
                principalSchema: "access",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AssingedToGroups_Users_UserId",
                schema: "access",
                table: "AssingedToGroups",
                column: "UserId",
                principalSchema: "access",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Users_Owner",
                schema: "access",
                table: "Events",
                column: "Owner",
                principalSchema: "access",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SharedWith_Users_UserId",
                schema: "access",
                table: "SharedWith",
                column: "UserId",
                principalSchema: "access",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssingedToEvents_Users_UserId",
                schema: "access",
                table: "AssingedToEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_AssingedToGroups_Users_UserId",
                schema: "access",
                table: "AssingedToGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_Users_Owner",
                schema: "access",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_SharedWith_Users_UserId",
                schema: "access",
                table: "SharedWith");

            migrationBuilder.DropTable(
                name: "GroupsAdmins",
                schema: "access");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "access");

            migrationBuilder.DropIndex(
                name: "IX_SharedWith_UserId",
                schema: "access",
                table: "SharedWith");

            migrationBuilder.DropIndex(
                name: "IX_Events_Owner",
                schema: "access",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_AssingedToGroups_UserId",
                schema: "access",
                table: "AssingedToGroups");

            migrationBuilder.DropIndex(
                name: "IX_AssingedToEvents_UserId",
                schema: "access",
                table: "AssingedToEvents");

            migrationBuilder.DropColumn(
                name: "Owner",
                schema: "access",
                table: "Events");
        }
    }
}
