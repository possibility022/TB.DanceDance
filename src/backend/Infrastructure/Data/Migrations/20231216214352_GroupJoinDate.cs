using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TB.DanceDance.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class GroupJoinDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WhenJoined",
                schema: "access",
                table: "GroupAssigmentRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "WhenJoined",
                schema: "access",
                table: "AssingedToGroups",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhenJoined",
                schema: "access",
                table: "GroupAssigmentRequests");

            migrationBuilder.DropColumn(
                name: "WhenJoined",
                schema: "access",
                table: "AssingedToGroups");
        }
    }
}
