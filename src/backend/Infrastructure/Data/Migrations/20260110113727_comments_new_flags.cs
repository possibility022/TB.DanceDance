using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class comments_new_flags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnonymouseName",
                schema: "comments",
                table: "Comments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PostedAsAnonymous",
                schema: "comments",
                table: "Comments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnonymouseName",
                schema: "comments",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "PostedAsAnonymous",
                schema: "comments",
                table: "Comments");
        }
    }
}
