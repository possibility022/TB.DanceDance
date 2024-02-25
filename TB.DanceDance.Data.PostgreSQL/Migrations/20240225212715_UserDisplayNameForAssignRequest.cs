using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TB.DanceDance.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UserDisplayNameForAssignRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserDisplayName",
                schema: "access",
                table: "GroupAssigmentRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserDisplayName",
                schema: "access",
                table: "EventAssigmentRequests",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserDisplayName",
                schema: "access",
                table: "GroupAssigmentRequests");

            migrationBuilder.DropColumn(
                name: "UserDisplayName",
                schema: "access",
                table: "EventAssigmentRequests");
        }
    }
}
