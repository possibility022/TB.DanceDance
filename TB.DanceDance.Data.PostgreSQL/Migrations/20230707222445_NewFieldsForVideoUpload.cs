using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TB.DanceDance.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class NewFieldsForVideoUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LockedTill",
                schema: "video",
                table: "ToTransform",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Metadata",
                schema: "video",
                table: "ToTransform",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockedTill",
                schema: "video",
                table: "ToTransform");

            migrationBuilder.DropColumn(
                name: "Metadata",
                schema: "video",
                table: "ToTransform");
        }
    }
}
