using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class StoringUsersInformations : Migration
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
                INSERT INTO access.testusers(UserId, lastname, firstname)
                SELECT DISTINCT "UserId", '', '' FROM (
                    SELECT "UserId" FROM access."AssingedToEvents"
                    UNION
                    SELECT "UserId" FROM access."AssingedToGroups"
                    UNION
                    SELECT "UserId" FROM access."EventAssigmentRequests"
                    UNION
                    SELECT "UserId" FROM access."GroupAssigmentRequests"
                ) AS CombinedUsers;
                
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SharedWith_UserId",
                schema: "access",
                table: "SharedWith",
                column: "UserId");

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
                name: "FK_SharedWith_Users_UserId",
                schema: "access",
                table: "SharedWith");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "access");

            migrationBuilder.DropIndex(
                name: "IX_SharedWith_UserId",
                schema: "access",
                table: "SharedWith");

            migrationBuilder.DropIndex(
                name: "IX_AssingedToGroups_UserId",
                schema: "access",
                table: "AssingedToGroups");

            migrationBuilder.DropIndex(
                name: "IX_AssingedToEvents_UserId",
                schema: "access",
                table: "AssingedToEvents");
        }
    }
}
