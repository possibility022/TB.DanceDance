using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TB.DanceDance.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class MergeVideosToConvertIntoVideos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ToTransform",
                schema: "video");

            migrationBuilder.AlterColumn<string>(
                name: "BlobId",
                schema: "video",
                table: "Videos",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "Converted",
                schema: "video",
                table: "Videos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                schema: "video",
                table: "Videos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedTill",
                schema: "video",
                table: "Videos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceBlobId",
                schema: "video",
                table: "Videos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE video.\"Videos\" SET \"Converted\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Converted",
                schema: "video",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "FileName",
                schema: "video",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "LockedTill",
                schema: "video",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "SourceBlobId",
                schema: "video",
                table: "Videos");

            migrationBuilder.AlterColumn<string>(
                name: "BlobId",
                schema: "video",
                table: "Videos",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ToTransform",
                schema: "video",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedToEvent = table.Column<bool>(type: "boolean", nullable: false),
                    BlobId = table.Column<string>(type: "text", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    LockedTill = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<byte[]>(type: "bytea", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RecordedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SharedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SharedWithId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToTransform", x => x.Id);
                });
        }
    }
}
