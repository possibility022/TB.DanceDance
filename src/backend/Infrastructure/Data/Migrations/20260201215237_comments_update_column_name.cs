using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class comments_update_column_name : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShaOfAnonymouseId",
                schema: "comments",
                table: "Comments",
                newName: "ShaOfAnonymousId");

            migrationBuilder.RenameColumn(
                name: "AnonymouseName",
                schema: "comments",
                table: "Comments",
                newName: "AnonymousName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShaOfAnonymousId",
                schema: "comments",
                table: "Comments",
                newName: "ShaOfAnonymouseId");

            migrationBuilder.RenameColumn(
                name: "AnonymousName",
                schema: "comments",
                table: "Comments",
                newName: "AnonymouseName");
        }
    }
}
