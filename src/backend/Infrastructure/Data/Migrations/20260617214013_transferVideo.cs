using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class transferVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UploadedBy",
                schema: "video",
                table: "Videos",
                newName: "UploadedByUserId");

            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                schema: "video",
                table: "Videos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("update video.\"Videos\" set \"OwnerUserId\" = \"UploadedByUserId\"");

            migrationBuilder.CreateTable(
                name: "VideoTransfers",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpireAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AcceptedByUserId = table.Column<string>(type: "text", nullable: true),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoTransfers_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "access",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoTransferItems",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferId = table.Column<string>(type: "text", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoTransferItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoTransferItems_VideoTransfers_TransferId",
                        column: x => x.TransferId,
                        principalSchema: "access",
                        principalTable: "VideoTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoTransferItems_Videos_VideoId",
                        column: x => x.VideoId,
                        principalSchema: "video",
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoTransferItems_TransferId_VideoId",
                schema: "access",
                table: "VideoTransferItems",
                columns: new[] { "TransferId", "VideoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoTransferItems_VideoId",
                schema: "access",
                table: "VideoTransferItems",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoTransfers_CreatedBy",
                schema: "access",
                table: "VideoTransfers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VideoTransfers_Id",
                schema: "access",
                table: "VideoTransfers",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoTransferItems",
                schema: "access");

            migrationBuilder.DropTable(
                name: "VideoTransfers",
                schema: "access");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "video",
                table: "Videos");

            migrationBuilder.RenameColumn(
                name: "UploadedByUserId",
                schema: "video",
                table: "Videos",
                newName: "UploadedBy");
        }
    }
}
