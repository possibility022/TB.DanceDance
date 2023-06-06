using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TB.DanceDance.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class ExtractingMetadataToAnotherTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetadataAsJson",
                table: "Videos");

            migrationBuilder.CreateTable(
                name: "VideoMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Metadata = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoMetadata_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoMetadata_VideoId",
                table: "VideoMetadata",
                column: "VideoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoMetadata");

            migrationBuilder.AddColumn<string>(
                name: "MetadataAsJson",
                table: "Videos",
                type: "text",
                nullable: true);
        }
    }
}
